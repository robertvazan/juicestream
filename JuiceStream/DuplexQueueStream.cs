using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JuiceStream
{
    public class DuplexQueueStream : Stream
    {
        public readonly DuplexQueueStream Pair;
        readonly QueueStream Outbound;
        readonly QueueStream Inbound;

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanTimeout { get { return false; } }
        public override bool CanWrite { get { return true; } }
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override int ReadTimeout { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override int WriteTimeout { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        public DuplexQueueStream()
        {
            Outbound = new QueueStream();
            Inbound = new QueueStream();
            Pair = new DuplexQueueStream(this);
        }
        
        DuplexQueueStream(DuplexQueueStream pair)
        {
            Outbound = pair.Inbound;
            Inbound = pair.Outbound;
            Pair = pair;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Outbound.Dispose();
                Inbound.Dispose();
            }
            base.Dispose(disposing);
        }

        public override void Flush() { Outbound.Flush(); }
        public override Task FlushAsync(CancellationToken token) { return Outbound.FlushAsync(token); }
        public override int Read(byte[] buffer, int offset, int count) { return Inbound.Read(buffer, offset, count); }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) { return Inbound.ReadAsync(buffer, offset, count, token); }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { Outbound.Write(buffer, offset, count); }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token) { return Outbound.WriteAsync(buffer, offset, count, token); }
    }
}
