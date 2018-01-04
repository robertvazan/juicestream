// Part of JuiceStream: https://juicestream.machinezoo.com
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JuiceStream
{
    class MultiplexSubstream : Stream
    {
        readonly MultiplexStream ParentStream;
        public readonly long Id;
        byte[] ReadBuffer;
        int ReadOffset;
        bool EndOfStream;
        const int MaxPacketSize = (1 << 14) - 1;
        readonly CancellationTokenSource CancelBrook = new CancellationTokenSource();
        readonly AsyncCollection<byte[]> ReadQueue = new AsyncCollection<byte[]>(2);

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        public MultiplexSubstream(MultiplexStream stream, long id)
        {
            ParentStream = stream;
            Id = id;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ParentStream.QueueClose(Id);
            base.Dispose(disposing);
        }

        public override void Flush() { FlushAsync().Wait(); }
        public override Task FlushAsync(CancellationToken token) { return ParentStream.FlushAsync(token); }
        public override int Read(byte[] buffer, int offset, int count) { return ReadAsync(buffer, offset, count).Result; }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            int total = 0;
            while (count > 0 && !EndOfStream)
            {
                if (ReadBuffer != null && ReadOffset < ReadBuffer.Length)
                {
                    var read = Math.Min(ReadBuffer.Length - ReadOffset, count);
                    Array.Copy(ReadBuffer, ReadOffset, buffer, offset, read);
                    total += read;
                    count -= read;
                    offset += read;
                    ReadOffset += read;
                }
                else
                {
                    ReadBuffer = await ReadQueue.TakeAsync(token);
                    ReadOffset = 0;
                    if (ReadBuffer == null)
                        EndOfStream = true;
                }
            }
            return total;
        }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { WriteAsync(buffer, offset, count).Wait(); }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            while (count > 0)
            {
                var slice = new byte[Math.Min(count, MaxPacketSize)];
                Array.Copy(buffer, offset, slice, 0, slice.Length);
                await ParentStream.SendAsync(Id, slice, token);
                count -= slice.Length;
                offset += slice.Length;
            }
        }

        public async Task ReceiveAsync(byte[] data, CancellationToken cancellation)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, CancelBrook.Token))
                await ReadQueue.AddAsync(data, linked.Token);
        }
        public async Task ReceiveEndOfStream(CancellationToken cancellation)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, CancelBrook.Token))
                await ReadQueue.AddAsync(null, linked.Token);
        }
    }
}
