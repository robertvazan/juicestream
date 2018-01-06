// Part of JuiceStream: https://juicestream.machinezoo.com
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

static class StreamEx
{
    internal static int Read(this Stream stream, byte[] buffer) { return stream.Read(buffer, 0, buffer.Length); }
    internal static Task<int> ReadAsync(this Stream stream, byte[] buffer) { return stream.ReadAsync(buffer, 0, buffer.Length); }
    internal static Task<int> ReadAsync(this Stream stream, byte[] buffer, CancellationToken cancellation) { return stream.ReadAsync(buffer, 0, buffer.Length, cancellation); }
    internal static void ReadFixed(this Stream stream, byte[] buffer) { stream.ReadFixed(buffer, 0, buffer.Length); }
    internal static void ReadFixed(this Stream stream, byte[] buffer, int offset, int count)
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
    internal static byte[] ReadFixed(this Stream stream, int count)
    {
        var buffer = new byte[count];
        stream.ReadFixed(buffer);
        return buffer;
    }
    internal static Task ReadFixedAsync(this Stream stream, byte[] buffer) { return ReadFixedAsync(stream, buffer, 0, buffer.Length, CancellationToken.None); }
    internal static Task ReadFixedAsync(this Stream stream, byte[] buffer, CancellationToken cancellation) { return ReadFixedAsync(stream, buffer, 0, buffer.Length, cancellation); }
    internal static Task ReadFixedAsync(this Stream stream, byte[] buffer, int offset, int count) { return ReadFixedAsync(stream, buffer, offset, count, CancellationToken.None); }
    internal static async Task ReadFixedAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellation)
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
    internal static async Task<byte[]> ReadFixedAsync(this Stream stream, int count)
    {
        var buffer = new byte[count];
        await stream.ReadFixedAsync(buffer);
        return buffer;
    }
    internal static void Write(this Stream stream, byte[] buffer) { stream.Write(buffer, 0, buffer.Length); }
    internal static Task WriteAsync(this Stream stream, byte[] buffer) { return stream.WriteAsync(buffer, 0, buffer.Length); }
    internal static Task WriteAsync(this Stream stream, byte[] buffer, CancellationToken cancellation) { return stream.WriteAsync(buffer, 0, buffer.Length, cancellation); }
    internal static Task<int> ReadByteAsync(this Stream stream) { return stream.ReadByteAsync(CancellationToken.None); }
    internal static async Task<int> ReadByteAsync(this Stream stream, CancellationToken cancellation)
    {
        var buffer = new byte[1];
        var read = await stream.ReadAsync(buffer, 0, 1, cancellation);
        return read == 1 ? buffer[0] : -1;
    }
    internal static Task WriteByteAsync(this Stream stream, byte value) { return stream.WriteAsync(new byte[] { value }); }
}
