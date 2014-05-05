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
        readonly Dictionary<long, MultiplexBrook> Brooks = new Dictionary<long, MultiplexBrook>();
        long NextBrookId = 1;
        long InboundBrook;
        long OutboundBrook;
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

        public Stream Connect()
        {
            lock (Brooks)
            {
                var brook = new MultiplexBrook(this, NextBrookId);
                Brooks[NextBrookId] = brook;
                ++NextBrookId;
                return brook;
            }
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
                    var packet = await Task.Run(() => Serializer.DeserializeWithLengthPrefix<MultiplexPacket>(Inner, PrefixStyle.Base128), CancelAll.Token);
                    if (packet.BrookId != 0)
                        InboundBrook = -packet.BrookId;
                    else if (InboundBrook == 0)
                        throw new FormatException();
                    MultiplexBrook brook;
                    bool created = false;
                    lock (Brooks)
                    {
                        if (!Brooks.TryGetValue(InboundBrook, out brook) && InboundBrook < 0)
                        {
                            Brooks[InboundBrook] = new MultiplexBrook(this, InboundBrook);
                            created = true;
                        }
                    }
                    if (created)
                        await Pending.AddAsync(brook, CancelAll.Token);
                    if (brook != null)
                    {
                        if (packet.Data != null && packet.Data.Length > 0)
                            await brook.ReceiveAsync(packet.Data, CancelAll.Token);
                        if (packet.EndOfStream)
                            await brook.ReceiveEndOfStream(CancelAll.Token);
                    }
                }
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

        internal Task SendAsync(long brookId, byte[] data, CancellationToken cancellation)
        {
            return SendAsync(linked =>
            {
                return Task.Run(() => Serializer.SerializeWithLengthPrefix(Inner, new MultiplexPacket()
                {
                    BrookId = brookId == OutboundBrook ? 0 : OutboundBrook = brookId,
                    Data = data
                }, PrefixStyle.Base128), linked);
            }, cancellation);
        }

        internal Task FlushAsync(CancellationToken cancellation) { return SendAsync(linked => Inner.FlushAsync(linked), cancellation); }

        internal void QueueClose(long brookId)
        {
            lock (Brooks)
                Brooks.Remove(brookId);
            SendAsync(async linked =>
            {
                await Task.Run(() => Serializer.SerializeWithLengthPrefix(Inner, new MultiplexPacket()
                {
                    BrookId = brookId == OutboundBrook ? 0 : OutboundBrook = brookId,
                    EndOfStream = true
                }, PrefixStyle.Base128), linked);
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
