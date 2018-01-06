// Part of JuiceStream: https://juicestream.machinezoo.com
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JuiceStream
{
    /// <summary>
    /// Writes out specified binary prefix before first write and checks presence and correctness of the prefix before first read.
    /// </summary>
    public class PrefixStream : Stream
    {
        readonly byte[] Prefix;
        readonly Stream Inner;
        bool PrefixWritten;
        bool PrefixRead;

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:JuiceStream.PrefixStream"/> can read.
        /// </summary>
        /// <value><c>true</c> if can read; otherwise, <c>false</c>. Taken from inner stream.</value>
        public override bool CanRead { get { return Inner.CanRead; } }
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:JuiceStream.PrefixStream"/> can seek.
        /// </summary>
        /// <value><c>true</c> if can seek; otherwise, <c>false</c>. Always <c>false</c>.</value>
        public override bool CanSeek { get { return false; } }
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:JuiceStream.PrefixStream"/> can timeout.
        /// </summary>
        /// <value><c>true</c> if can timeout; otherwise, <c>false</c>. Taken from inner stream.</value>
        public override bool CanTimeout { get { return Inner.CanTimeout; } }
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:JuiceStream.PrefixStream"/> can write.
        /// </summary>
        /// <value><c>true</c> if can write; otherwise, <c>false</c>. Taken from inner stream.</value>
        public override bool CanWrite { get { return Inner.CanWrite; } }
        /// <summary>
        /// Gets the length. Not supported. Throws <see cref="T:System.NotSupportedException"/>.
        /// </summary>
        /// <value>The length.</value>
        public override long Length { get { throw new NotSupportedException(); } }
        /// <summary>
        /// Gets or sets the position. Not supported. Throws <see cref="T:System.NotSupportedException"/>.
        /// </summary>
        /// <value>The position.</value>
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        /// <summary>
        /// Gets or sets the read timeout.
        /// </summary>
        /// <value>The read timeout. Passed from/to the underlying stream.</value>
        public override int ReadTimeout { get { return Inner.ReadTimeout; } set { Inner.ReadTimeout = value; } }
        /// <summary>
        /// Gets or sets the write timeout.
        /// </summary>
        /// <value>The write timeout. Passed from/to the underlying stream.</value>
        public override int WriteTimeout { get { return Inner.WriteTimeout; } set { Inner.WriteTimeout = value; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:JuiceStream.PrefixStream"/> class with string prefix.
        /// </summary>
        /// <param name="prefix">Prefix to add/check in from of the inner stream (UTF-8).</param>
        /// <param name="stream">Inner stream to wrap.</param>
        public PrefixStream(string prefix, Stream stream) : this(Encoding.UTF8.GetBytes(prefix), stream) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="T:JuiceStream.PrefixStream"/> class with binary prefix.
        /// </summary>
        /// <param name="prefix">Prefix to add/check in from of the inner stream.</param>
        /// <param name="stream">Inner stream to wrap.</param>
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
        /// <summary>
        /// Flush this instance.
        /// </summary>
        public override void Flush() { WritePrefix(); Inner.Flush(); }
        /// <summary>
        /// Flush this instance asynchronously.
        /// </summary>
        /// <returns>Async task (already completed).</returns>
        /// <param name="cancellation">Cancellation token.</param>
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
