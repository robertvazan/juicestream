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
    public class ForceCancelStream : Stream
    {
        readonly Stream Inner;

        public override bool CanRead { get { return Inner.CanRead; } }
        public override bool CanSeek { get { return Inner.CanSeek; } }
        public override bool CanTimeout { get { return Inner.CanTimeout; } }
        public override bool CanWrite { get { return Inner.CanWrite; } }
        public override long Length { get { return Inner.Length; } }
        public override long Position { get { return Inner.Position; } set { Inner.Position = value; } }
        public override int ReadTimeout { get { return Inner.ReadTimeout; } set { Inner.ReadTimeout = value; } }
        public override int WriteTimeout { get { return Inner.WriteTimeout; } set { Inner.WriteTimeout = value; } }

        public ForceCancelStream(Stream stream) { Inner = stream; }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Inner.Dispose();
        }
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) { return Inner.BeginRead(buffer, offset, count, callback, state); }
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) { return Inner.BeginWrite(buffer, offset, count, callback, state); }
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ForcedCancellable.FromTask(Inner.CopyToAsync(destination, bufferSize, cancellationToken), cancellationToken);
        }
        public override int EndRead(IAsyncResult asyncResult) { return Inner.EndRead(asyncResult); }
        public override void EndWrite(IAsyncResult asyncResult) { Inner.EndWrite(asyncResult); }
        public override void Flush() { Inner.Flush(); }
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ForcedCancellable.FromTask(Inner.FlushAsync(cancellationToken), cancellationToken);
        }
        public override int Read(byte[] buffer, int offset, int count) { return Inner.Read(buffer, offset, count); }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await ForcedCancellable.FromTask(Inner.ReadAsync(buffer, offset, count, cancellationToken), cancellationToken);
        }
        public override int ReadByte() { return Inner.ReadByte(); }
        public override long Seek(long offset, SeekOrigin origin) { return Inner.Seek(offset, origin); }
        public override void SetLength(long value) { Inner.SetLength(value); }
        public override void Write(byte[] buffer, int offset, int count) { Inner.Write(buffer, offset, count); }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ForcedCancellable.FromTask(Inner.WriteAsync(buffer, offset, count, cancellationToken), cancellationToken);
        }
        public override void WriteByte(byte value) { Inner.WriteByte(value); }
    }
}
