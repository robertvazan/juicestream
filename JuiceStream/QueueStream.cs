﻿using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JuiceStream
{
    public class QueueStream : Stream
    {
        AsyncCollection<byte[]> Queue = new AsyncCollection<byte[]>(1);
        byte[] ReadBuffer;
        int ReadOffset;
        bool EndOfStream;

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanTimeout { get { return false; } }
        public override bool CanWrite { get { return true; } }
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override int ReadTimeout { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override int WriteTimeout { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Queue.AddAsync(null);
            base.Dispose(disposing);
        }

        public override void Flush() { }
        public override Task FlushAsync(CancellationToken token) { return TaskConstants.Completed; }
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
                }
                else
                {
                    ReadBuffer = await Queue.TakeAsync(token);
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
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (offset != 0 || count != buffer.Length)
            {
                var slice = new byte[buffer.Length];
                Array.Copy(buffer, offset, slice, 0, count);
                buffer = slice;
            }
            return Queue.AddAsync(buffer, token);
        }
    }
}
