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
    /// <summary>
    /// <c>Stream</c> backed by in-memory buffer.
    /// Writes to <c>QueueStream</c> come out as reads from the same <c>QueueStream</c>.
    /// Only one write (of any length) is buffered before the stream starts blocking.
    /// </summary>
    public class QueueStream : Stream
    {
        AsyncCollection<byte[]> Queue = new AsyncCollection<byte[]>(1);
        byte[] ReadBuffer;
        int ReadOffset;
        bool EndOfStream;

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:JuiceStream.QueueStream"/> can read.
        /// Always <c>true</c>.
        /// </summary>
        /// <value><c>true</c> if can read; otherwise, <c>false</c>.</value>
        public override bool CanRead { get { return true; } }
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:JuiceStream.QueueStream"/> can seek.
        /// Always <c>false</c>.
        /// </summary>
        /// <value><c>true</c> if can seek; otherwise, <c>false</c>.</value>
        public override bool CanSeek { get { return false; } }
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:JuiceStream.QueueStream"/> can timeout.
        /// Always <c>false</c>.
        /// </summary>
        /// <value><c>true</c> if can timeout; otherwise, <c>false</c>.</value>
        public override bool CanTimeout { get { return false; } }
        /// <summary>
        /// Gets a value indicating whether this <see cref="T:JuiceStream.QueueStream"/> can write.
        /// Always <c>true</c>.
        /// </summary>
        /// <value><c>true</c> if can write; otherwise, <c>false</c>.</value>
        public override bool CanWrite { get { return true; } }
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
        /// Gets or sets the read timeout. Not supported. Throws <see cref="T:System.NotSupportedException"/>.
        /// </summary>
        /// <value>The read timeout.</value>
        public override int ReadTimeout { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        /// <summary>
        /// Gets or sets the write timeout. Not supported. Throws <see cref="T:System.NotSupportedException"/>.
        /// </summary>
        /// <value>The write timeout.</value>
        public override int WriteTimeout { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Queue.AddAsync(null);
            base.Dispose(disposing);
        }

        /// <summary>
        /// Flush this instance. It has no effect on <see cref="T:JuiceStream.QueueStream"/>.
        /// </summary>
        public override void Flush() { }
        /// <summary>
        /// Flush this instance asynchronously. It has no effect on <see cref="T:JuiceStream.QueueStream"/>.
        /// </summary>
        /// <returns>Async task (already completed).</returns>
        /// <param name="token">Cancellation token.</param>
        public override Task FlushAsync(CancellationToken token) { return TaskConstants.Completed; }
        /// <summary>
        /// Read data into the specified buffer at specified offset up to specified count.
        /// </summary>
        /// <returns>Bytes actually read.</returns>
        /// <param name="buffer">Buffer where data will be stored.</param>
        /// <param name="offset">Offset into the buffer.</param>
        /// <param name="count">Count of bytes to read.</param>
        public override int Read(byte[] buffer, int offset, int count) { return ReadAsync(buffer, offset, count).Result; }
        /// <summary>
        /// Read data asynchronously into the specified buffer at specified offset up to specified count.
        /// </summary>
        /// <returns>Async task.</returns>
        /// <param name="buffer">Buffer where data will be stored.</param>
        /// <param name="offset">Offset into the buffer.</param>
        /// <param name="count">Count of bytes to read.</param>
        /// <param name="token">Cancellation token.</param>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (count > 0 && !EndOfStream)
            {
                if (ReadBuffer == null || ReadOffset >= ReadBuffer.Length)
                {
                    ReadBuffer = await Queue.TakeAsync(token);
                    ReadOffset = 0;
                    if (ReadBuffer == null)
                        EndOfStream = true;
                }
                if (!EndOfStream)
                {
                    var read = Math.Min(ReadBuffer.Length - ReadOffset, count);
                    Array.Copy(ReadBuffer, ReadOffset, buffer, offset, read);
                    ReadOffset += read;
                    return read;
                }
            }
            return 0;
        }
        /// <summary>
        /// Seek to the specified offset relative to specified origin.
        /// Not supported. Throws <see cref="T:System.NotSupportedException"/>.
        /// </summary>
        /// <returns>The new position.</returns>
        /// <param name="offset">Position relative to origin.</param>
        /// <param name="origin">Origin where offset is counted from.</param>
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        /// <summary>
        /// Sets the length of the stream.
        /// Not supported. Throws <see cref="T:System.NotSupportedException"/>.
        /// </summary>
        /// <param name="value">New length.</param>
        public override void SetLength(long value) { throw new NotSupportedException(); }
        /// <summary>
        /// Write the specified buffer, from offset and up to count bytes.
        /// </summary>
        /// <param name="buffer">Buffer with data to write.</param>
        /// <param name="offset">Offset into the buffer.</param>
        /// <param name="count">Count of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count) { WriteAsync(buffer, offset, count).Wait(); }
        /// <summary>
        /// Write the specified buffer asynchronously, from offset and up to count bytes.
        /// </summary>
        /// <returns>Async task.</returns>
        /// <param name="buffer">Buffer with data to write.</param>
        /// <param name="offset">Offset into the buffer.</param>
        /// <param name="count">Count of bytes to write.</param>
        /// <param name="token">Cancellation token.</param>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (count != 0)
            {
                var slice = new byte[count];
                Array.Copy(buffer, offset, slice, 0, count);
                return Queue.AddAsync(slice, token);
            }
            else
                return TaskConstants.Completed;
        }
    }
}
