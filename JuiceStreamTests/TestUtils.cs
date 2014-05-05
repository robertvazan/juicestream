using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JuiceStreamTests
{
    static class TestUtils
    {
        public static void TestCommonOperations(Func<Stream, Stream> factory, Func<byte[], IEnumerable<byte>> decorate)
        {
            var inner = new MemoryStream();
            var stream = factory(inner);
            stream.Write(new byte[] { 0, 1, 2, 3, 4 }, 1, 3);
            stream.WriteByte((byte)5);
            stream.Flush();
            stream.FlushAsync().Wait();
            stream.WriteAsync(new byte[] { 6, 7, 8, 9, 10 }, 1, 3).Wait();
            stream.Dispose();
            CollectionAssert.AreEqual(decorate(new byte[] { 1, 2, 3, 5, 7, 8, 9 }), inner.ToArray());
            inner = new MemoryStream(inner.ToArray());
            stream = factory(inner);
            var buffer = new byte[5];
            Assert.AreEqual(3, stream.Read(buffer, 1, 3));
            CollectionAssert.AreEqual(new byte[] { 0, 1, 2, 3, 0 }, buffer);
            Assert.AreEqual(5, stream.ReadByte());
            Assert.AreEqual(3, stream.ReadAsync(buffer, 1, 3).Result);
            CollectionAssert.AreEqual(new byte[] { 0, 7, 8, 9, 0 }, buffer);
            Assert.AreEqual(0, stream.Read(buffer, 1, 3));
            Assert.AreEqual(-1, stream.ReadByte());
            Assert.AreEqual(0, stream.ReadAsync(buffer, 1, 3).Result);
        }
    }
}
