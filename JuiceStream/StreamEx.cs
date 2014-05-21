using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public static class StreamEx
{
    public static int Read(this Stream stream, byte[] buffer) { return stream.Read(buffer, 0, buffer.Length); }
    public static Task<int> ReadAsync(this Stream stream, byte[] buffer) { return stream.ReadAsync(buffer, 0, buffer.Length); }
    public static Task<int> ReadAsync(this Stream stream, byte[] buffer, CancellationToken cancellation) { return stream.ReadAsync(buffer, 0, buffer.Length, cancellation); }
    public static void ReadFixed(this Stream stream, byte[] buffer) { stream.ReadFixed(buffer, 0, buffer.Length); }
    public static void ReadFixed(this Stream stream, byte[] buffer, int offset, int count)
    {
        while (count > 0)
        {
            var read = stream.Read(buffer, offset, count);
            if (read == 0)
                throw new EndOfStreamException();
            offset += read;
            count -= read;
        }
    }
    public static Task ReadFixedAsync(this Stream stream, byte[] buffer) { return ReadFixedAsync(stream, buffer, 0, buffer.Length, CancellationToken.None); }
    public static Task ReadFixedAsync(this Stream stream, byte[] buffer, CancellationToken cancellation) { return ReadFixedAsync(stream, buffer, 0, buffer.Length, cancellation); }
    public static Task ReadFixedAsync(this Stream stream, byte[] buffer, int offset, int count) { return ReadFixedAsync(stream, buffer, offset, count, CancellationToken.None); }
    public static async Task ReadFixedAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellation)
    {
        while (count > 0)
        {
            var read = await stream.ReadAsync(buffer, offset, count, cancellation);
            if (read == 0)
                throw new EndOfStreamException();
            offset += read;
            count -= read;
        }
    }
    public static void Write(this Stream stream, byte[] buffer) { stream.Write(buffer, 0, buffer.Length); }
    public static Task WriteAsync(this Stream stream, byte[] buffer) { return stream.WriteAsync(buffer, 0, buffer.Length); }
    public static Task WriteAsync(this Stream stream, byte[] buffer, CancellationToken cancellation) { return stream.WriteAsync(buffer, 0, buffer.Length, cancellation); }
    public static Task<int> ReadByteAsync(this Stream stream) { return stream.ReadByteAsync(CancellationToken.None); }
    public static async Task<int> ReadByteAsync(this Stream stream, CancellationToken cancellation)
    {
        var buffer = new byte[1];
        var read = await stream.ReadAsync(buffer, 0, 1, cancellation);
        return read == 1 ? buffer[0] : -1;
    }
    public static Task WriteByteAsync(this Stream stream, byte value) { return stream.WriteAsync(new byte[] { value }); }
}
