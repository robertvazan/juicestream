using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JuiceStream
{
    public class PrefixStream : Stream
    {
        readonly byte[] Prefix;
        readonly Stream Inner;
        bool PrefixWritten;
        bool PrefixRead;

        public override bool CanRead { get { return Inner.CanRead; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanTimeout { get { return Inner.CanTimeout; } }
        public override bool CanWrite { get { return Inner.CanWrite; } }
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override int ReadTimeout { get { return Inner.ReadTimeout; } set { Inner.ReadTimeout = value; } }
        public override int WriteTimeout { get { return Inner.WriteTimeout; } set { Inner.WriteTimeout = value; } }

        public PrefixStream(string prefix, Stream stream) : this(Encoding.UTF8.GetBytes(prefix), stream) { }
        public PrefixStream(byte[] prefix, Stream stream)
        {
            if (prefix.Length == 0)
                throw new ArgumentException();
            Prefix = prefix;
            Inner = stream;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Inner.Dispose();
        }
        public override void Flush() { WritePrefix(); Inner.Flush(); }
        public override async Task FlushAsync(CancellationToken cancellation) { await WritePrefixAsyc(cancellation); await Inner.FlushAsync(cancellation); }
        public override int Read(byte[] buffer, int offset, int count) { ReadPrefix(); return Inner.Read(buffer, offset, count); }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellation)
        {
            await ReadPrefixAsync(cancellation);
            return await Inner.ReadAsync(buffer, offset, count, cancellation);
        }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { WritePrefix(); Inner.Write(buffer, offset, count); }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellation)
        {
            await WritePrefixAsyc(cancellation);
            await Inner.WriteAsync(buffer, offset, count, cancellation);
        }

        void ReadPrefix()
        {
            if (!PrefixRead)
            {
                try
                {
                    var buffer = new byte[Prefix.Length];
                    Inner.ReadFixed(buffer);
                    if (!Prefix.SequenceEqual(buffer))
                        throw new FormatException();
                }
                catch
                {
                    Dispose();
                    throw;
                }
                PrefixRead = true;
            }
        }

        async Task ReadPrefixAsync(CancellationToken cancellation)
        {
            if (!PrefixRead)
            {
                try
                {
                    var buffer = new byte[Prefix.Length];
                    await Inner.ReadFixedAsync(buffer, cancellation);
                    if (!Prefix.SequenceEqual(buffer))
                        throw new FormatException();
                }
                catch
                {
                    Dispose();
                    throw;
                }
                PrefixRead = true;
            }
        }

        void WritePrefix()
        {
            if (!PrefixWritten)
            {
                try
                {
                    Inner.Write(Prefix);
                }
                catch
                {
                    Dispose();
                    throw;
                }
                PrefixWritten = true;
            }
        }

        async Task WritePrefixAsyc(CancellationToken cancellation)
        {
            if (!PrefixWritten)
            {
                try
                {
                    await Inner.WriteAsync(Prefix, cancellation);
                }
                catch
                {
                    Dispose();
                    throw;
                }
                PrefixWritten = true;
            }
        }
    }
}
