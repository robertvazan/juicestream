// Part of JuiceStream: https://juicestream.machinezoo.com
using Nito.AsyncEx;
using NLog;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JuiceStream
{
    public class MultiplexStream : IDisposable
    {
        readonly Stream Inner;
        readonly CancellationTokenSource CancelAll = new CancellationTokenSource();
        readonly Dictionary<long, MultiplexSubstream> Substreams = new Dictionary<long, MultiplexSubstream>();
        long NextId = 1;
        long InboundSubstream;
        long OutboundSubstream;
        readonly AsyncLock WriteLock = new AsyncLock();
        readonly AsyncCollection<Stream> Pending = new AsyncCollection<Stream>(5);
        static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public MultiplexStream(Stream stream)
        {
            Inner = stream;
            ReceiveAsync();
        }

        public void Dispose()
        {
            CancelAll.Cancel();
            Inner.Dispose();
        }

        public Stream Connect() { return ConnectAsync().Result; }
        public Task<Stream> ConnectAsync() { return ConnectAsync(CancellationToken.None); }
        public async Task<Stream> ConnectAsync(CancellationToken cancellation)
        {
            MultiplexSubstream substream;
            lock (Substreams)
            {
                substream = new MultiplexSubstream(this, NextId);
                Substreams[NextId] = substream;
                ++NextId;
            }
            await SendAsync(linked => ProtobufEx.SerializeWithLengthPrefixAsync(Inner, new MultiplexPacket()
            {
                SubstreamId = OutboundSubstream = substream.Id
            }, PrefixStyle.Base128, linked), cancellation);
            return substream;
        }

        public Stream Accept() { return AcceptAsync().Result; }
        public Task<Stream> AcceptAsync() { return AcceptAsync(CancellationToken.None); }
        public Task<Stream> AcceptAsync(CancellationToken cancellation) { return Pending.TakeAsync(cancellation); }

        async void ReceiveAsync()
        {
            try
            {
                while (true)
                {
                    var packet = await ProtobufEx.DeserializeWithLengthPrefixAsync<MultiplexPacket>(Inner, PrefixStyle.Base128, CancelAll.Token);
                    if (packet.SubstreamId != 0)
                        InboundSubstream = -packet.SubstreamId;
                    else if (InboundSubstream == 0)
                        throw new FormatException();
                    MultiplexSubstream substream;
                    bool created = false;
                    lock (Substreams)
                    {
                        if (!Substreams.TryGetValue(InboundSubstream, out substream) && InboundSubstream < 0)
                        {
                            Substreams[InboundSubstream] = substream = new MultiplexSubstream(this, InboundSubstream);
                            created = true;
                        }
                    }
                    if (created)
                        await Pending.AddAsync(substream, CancelAll.Token);
                    if (substream != null)
                    {
                        if (packet.Data != null && packet.Data.Length > 0)
                            await substream.ReceiveAsync(packet.Data, CancelAll.Token);
                        if (packet.EndOfStream)
                            await substream.ReceiveEndOfStream(CancelAll.Token);
                    }
                }
            }
            catch (EndOfStreamException)
            {
                Dispose();
                List<MultiplexSubstream> substreams;
                lock (Substreams)
                    substreams = Substreams.Values.ToList();
                foreach (var substream in substreams)
                    substream.Dispose();
            }
            catch (Exception e)
            {
                if (!CancelAll.Token.IsCancellationRequested)
                {
                    Logger.Error(e);
                    Dispose();
                }
            }
        }

        internal Task SendAsync(long substreamId, byte[] data, CancellationToken cancellation)
        {
            return SendAsync(linked => ProtobufEx.SerializeWithLengthPrefixAsync(Inner, new MultiplexPacket()
            {
                SubstreamId = substreamId == OutboundSubstream ? 0 : OutboundSubstream = substreamId,
                Data = data
            }, PrefixStyle.Base128, linked), cancellation);
        }

        internal Task FlushAsync(CancellationToken cancellation) { return SendAsync(linked => Inner.FlushAsync(linked), cancellation); }

        internal void QueueClose(long substreamId)
        {
            lock (Substreams)
                Substreams.Remove(substreamId);
            SendAsync(async linked =>
            {
                await ProtobufEx.SerializeWithLengthPrefixAsync(Inner, new MultiplexPacket()
                {
                    SubstreamId = substreamId == OutboundSubstream ? 0 : OutboundSubstream = substreamId,
                    EndOfStream = true
                }, PrefixStyle.Base128, linked);
                await Inner.FlushAsync(linked);
            }, CancellationToken.None).ContinueWith(t => Logger.Error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        async Task SendAsync(Func<CancellationToken, Task> batch, CancellationToken cancellation)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(CancelAll.Token, cancellation))
            using (var guard = await WriteLock.LockAsync(linked.Token))
            {
                try
                {
                    await batch(linked.Token);
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }
        }
    }
}
