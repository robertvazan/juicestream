using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JuiceStream
{
    static class ProtobufEx
    {
        public static Task SerializeWithLengthPrefixAsync<T>(Stream stream, T instance, PrefixStyle prefix, CancellationToken cancellation)
        {
            var buffer = new MemoryStream();
            Serializer.SerializeWithLengthPrefix(buffer, instance, prefix);
            buffer.Close();
            var serialized = buffer.ToArray();
            return stream.WriteAsync(serialized, cancellation);
        }

        public static async Task<T> DeserializeWithLengthPrefixAsync<T>(Stream stream, PrefixStyle prefix, CancellationToken cancellation)
        {
            if (prefix != PrefixStyle.Base128)
                throw new NotSupportedException();
            byte[] lengthBuf = new byte[1];
            int length = 0, shift = 0;
            do
            {
                if (shift >= 28)
                    throw new OverflowException();
                await stream.ReadFixedAsync(lengthBuf, cancellation);
                length |= (lengthBuf[0] & 0x7f) << shift;
                shift += 7;
            } while ((lengthBuf[0] & 0x80) != 0);
            var buffer = new byte[length];
            await stream.ReadFixedAsync(buffer, cancellation);
            return Serializer.Deserialize<T>(new MemoryStream(buffer));
        }
    }
}
