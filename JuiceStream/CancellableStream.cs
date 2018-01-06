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
    /// Enforces use of specified <see cref="T:System.Threading.CancellationToken"/> in all calls to the controlled <see cref="T:System.IO.Stream"/>.
    /// </summary>
    public class CancellableStream : Stream
    {
        readonly Stream Inner;
        readonly CancellationToken Cancellation;

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:JuiceStream.CancellableStream"/> can read.
        /// </summary>
        /// <value><c>true</c> if can read; otherwise, <c>false</c>. Taken from wrapped stream.</value>
        public override bool CanRead { get { return Inner.CanRead; } }
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:JuiceStream.CancellableStream"/> can seek.
        /// </summary>
        /// <value><c>true</c> if can seek; otherwise, <c>false</c>. Taken from wrapped stream.</value>
        public override bool CanSeek { get { return Inner.CanSeek; } }
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:JuiceStream.CancellableStream"/> can timeout.
        /// </summary>
        /// <value><c>true</c> if can timeout; otherwise, <c>false</c>. Taken from wrapped stream.</value>
        public override bool CanTimeout { get { return Inner.CanTimeout; } }
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:JuiceStream.CancellableStream"/> can write.
        /// </summary>
        /// <value><c>true</c> if can write; otherwise, <c>false</c>. Taken from wrapped stream.</value>
        public override bool CanWrite { get { return Inner.CanWrite; } }
        /// <summary>
        /// Gets the length of the stream.
        /// </summary>
        /// <value>The length. Taken from wrapped stream.</value>
        public override long Length { get { return Inner.Length; } }
        /// <summary>
        /// Gets or sets the position in the stream.
        /// </summary>
        /// <value>The position. Taken from or written to the wrapped stream.</value>
        public override long Position { get { return Inner.Position; } set { Inner.Position = value; } }
        /// <summary>
        /// Gets or sets the read timeout.
        /// </summary>
        /// <value>The read timeout. Taken from or written to the wrapped stream.</value>
        public override int ReadTimeout { get { return Inner.ReadTimeout; } set { Inner.ReadTimeout = value; } }
        /// <summary>
        /// Gets or sets the write timeout.
        /// </summary>
        /// <value>The write timeout. Taken from or written to the wrapped stream.</value>
        public override int WriteTimeout { get { return Inner.WriteTimeout; } set { Inner.WriteTimeout = value; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:JuiceStream.CancellableStream"/> class.
        /// </summary>
        /// <param name="stream">Stream to wrap.</param>
        /// <param name="cancellation">Cancellation token to enforce.</param>
        public CancellableStream(Stream stream, CancellationToken cancellation)
        {
            Inner = stream;
            Cancellation = cancellation;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Inner.Dispose();
        }
        /// <summary>
        /// Begins asynchronous read from the stream.
        /// Cancellation cannot be enforced for this method.
        /// </summary>
        /// <returns>Async operation status.</returns>
        /// <param name="buffer">Target buffer.</param>
        /// <param name="offset">Offset into the buffer.</param>
        /// <param name="count">Count of bytes to read.</param>
        /// <param name="callback">Async callback for this operation.</param>
        /// <param name="state">State to pass to the callback.</param>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) { return Inner.BeginRead(buffer, offset, count, callback, state); }
        /// <summary>
        /// Begins asynchronous write to the stream.
        /// Cancellation cannot be enforced for this method.
        /// </summary>
        /// <returns>Async operation status.</returns>
        /// <param name="buffer">Source buffer.</param>
        /// <param name="offset">Offset into the buffer.</param>
        /// <param name="count">Count of bytes to write.</param>
        /// <param name="callback">Async callback for this operation.</param>
        /// <param name="state">State to pass to the callback.</param>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) { return Inner.BeginWrite(buffer, offset, count, callback, state); }
        /// <summary>
        /// Copies to another stream asynchronously.
        /// </summary>
        /// <returns>The to async.</returns>
        /// <param name="destination">Destination.</param>
        /// <param name="bufferSize">Buffer size.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            if (cancellationToken == CancellationToken.None)
                await Inner.CopyToAsync(destination, bufferSize, Cancellation);
            else
            {
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, Cancellation))
                    await Inner.CopyToAsync(destination, bufferSize, linked.Token);
            }
        }
        /// <summary>
        /// Completes asynchronous read.
        /// </summary>
        /// <returns>Bytes read.</returns>
        /// <param name="asyncResult">Async result.</param>
        public override int EndRead(IAsyncResult asyncResult) { return Inner.EndRead(asyncResult); }
        /// <summary>
        /// Completes asynchronous write.
        /// </summary>
        /// <param name="asyncResult">Async result.</param>
        public override void EndWrite(IAsyncResult asyncResult) { Inner.EndWrite(asyncResult); }
        /// <summary>
        /// Flush this instance.
        /// Synchronous operation. Cancellation cannot be enforced for this method.
        /// </summary>
        public override void Flush() { Inner.Flush(); }
        /// <summary>
        /// Flush this instance asynchronously.
        /// Cancellation can be enforced in addition to the passed cancellation token.
        /// </summary>
        /// <returns>Async task.</returns>
        /// <param name="cancellationToken">Cancellation token.</param>
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken == CancellationToken.None)
                await Inner.FlushAsync(Cancellation);
            else
            {
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, Cancellation))
                    await Inner.FlushAsync(linked.Token);
            }
        }
        /// <summary>
        /// Read data into the specified buffer at specified offset up to specified count.
        /// Synchronous operation. Cancellation cannot be enforced for this method.
        /// </summary>
        /// <returns>Bytes actually read.</returns>
        /// <param name="buffer">Buffer where data will be stored.</param>
        /// <param name="offset">Offset into the buffer.</param>
        /// <param name="count">Count of bytes to read.</param>
        public override int Read(byte[] buffer, int offset, int count) { return Inner.Read(buffer, offset, count); }
        /// <summary>
        /// Read data asynchronously into the specified buffer at specified offset up to specified count.
        /// Cancellation can be enforced in addition to the passed cancellation token.
        /// </summary>
        /// <returns>Async task.</returns>
        /// <param name="buffer">Buffer where data will be stored.</param>
        /// <param name="offset">Offset into the buffer.</param>
        /// <param name="count">Count of bytes to read.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken == CancellationToken.None)
                return await Inner.ReadAsync(buffer, offset, count, Cancellation);
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, Cancellation))
                return await Inner.ReadAsync(buffer, offset, count, linked.Token);
        }
        /// <summary>
        /// Reads one byte from the stream.
        /// Synchronous operation. Cancellation cannot be enforced for this method.
        /// </summary>
        /// <returns>The byte.</returns>
        public override int ReadByte() { return Inner.ReadByte(); }
        /// <summary>
        /// Seek to the specified offset relative to specified origin.
        /// </summary>
        /// <returns>The new position.</returns>
        /// <param name="offset">Position relative to origin.</param>
        /// <param name="origin">Origin where offset is counted from.</param>
        public override long Seek(long offset, SeekOrigin origin) { return Inner.Seek(offset, origin); }
        /// <summary>
        /// Sets the length of the stream.
        /// </summary>
        /// <param name="value">New length.</param>
        public override void SetLength(long value) { Inner.SetLength(value); }
        /// <summary>
        /// Write the specified buffer, from offset and up to count bytes.
        /// Synchronous operation. Cancellation cannot be enforced for this method.
        /// </summary>
        /// <param name="buffer">Buffer with data to write.</param>
        /// <param name="offset">Offset into the buffer.</param>
        /// <param name="count">Count of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count) { Inner.Write(buffer, offset, count); }
        /// <summary>
        /// Write the specified buffer asynchronously, from offset and up to count bytes.
        /// Cancellation can be enforced in addition to the passed cancellation token.
        /// </summary>
        /// <returns>Async task.</returns>
        /// <param name="buffer">Buffer with data to write.</param>
        /// <param name="offset">Offset into the buffer.</param>
        /// <param name="count">Count of bytes to write.</param>
        /// <param name="token">Cancellation token.</param>
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken == CancellationToken.None)
                await Inner.WriteAsync(buffer, offset, count, Cancellation);
            else
            {
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, Cancellation))
                    await Inner.WriteAsync(buffer, offset, count, linked.Token);
            }
        }
        /// <summary>
        /// Writes one byte.
        /// Synchronous operation. Cancellation cannot be enforced for this method.
        /// </summary>
        /// <param name="value">Byte to write.</param>
        public override void WriteByte(byte value) { Inner.WriteByte(value); }
    }
}
