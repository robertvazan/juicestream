// Part of JuiceStream: https://juicestream.machinezoo.com
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JuiceStreamTests
{
    static class TestUtils
    {
        static TestUtils()
        {
            var configuration = new LoggingConfiguration();
            var consoleTarget = new ConsoleTarget() { Layout = new SimpleLayout("${date:format=yyyy-MM-dd HH\\:mm\\:ss.fff} : ${message}") };
            configuration.AddTarget("Console", consoleTarget);
            configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, consoleTarget));
            LogManager.Configuration = configuration;
        }

        public static void SetupLogging() { }

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

        public static void RunTwister(Stream endpoint1, Stream endpoint2, TimeSpan? timeout = null)
        {
            Parallel.Invoke(
                () => RunOneWayTwister(endpoint1, endpoint2, timeout),
                () => RunOneWayTwister(endpoint2, endpoint1, timeout));
        }

        public static void RunOneWayTwister(Stream sink, Stream source, TimeSpan? timeout = null)
        {
            var end = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < end)
                RunOneTimeTwister(sink, source, timeout);
        }

        public static void RunOneTimeTwister(Stream sink, Stream source, TimeSpan? timeout = null)
        {
            var random = new Random(sink.GetHashCode() * 31 * 31 + source.GetHashCode() * 31 + (int)DateTime.UtcNow.Ticks + 3);
            var rubbish = new byte[random.Next(10, 100000)];
            random.NextBytes(rubbish);
            var crunching = Task.WhenAll(Task.Run(() => WriteRubbish(sink, rubbish)), Task.Run(() => ReadRubbish(source, rubbish)));
            if (Task.WhenAny(crunching, Task.Delay(timeout ?? TimeSpan.FromSeconds(10))).Result != crunching)
                throw new TimeoutException();
        }

        static void WriteRubbish(Stream stream, byte[] rubbish)
        {
            var random = new Random(stream.GetHashCode() * 31 + (int)DateTime.UtcNow.Ticks + 1);
            int offset = 0;
            try
            {
                while (offset < rubbish.Length)
                {
                    if (random.Next(2) == 0)
                    {
                        stream.WriteByte(rubbish[offset]);
                        ++offset;
                    }
                    else
                    {
                        int count = random.Next(rubbish.Length - offset + 1);
                        if (random.Next(2) == 0)
                            stream.Write(rubbish, offset, count);
                        else
                            stream.WriteAsync(rubbish, offset, count).Wait();
                        offset += count;
                    }
                    if (random.Next(2) == 0)
                    {
                        if (random.Next(2) == 0)
                            stream.Flush();
                        else
                            stream.FlushAsync().Wait();
                    }
                }
                if (random.Next(2) == 0)
                    stream.Flush();
                else
                    stream.FlushAsync().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        static void ReadRubbish(Stream stream, byte[] rubbish)
        {
            var random = new Random(stream.GetHashCode() * 31 + (int)DateTime.UtcNow.Ticks + 2);
            var buffer = new byte[rubbish.Length];
            int offset = 0;
            try
            {
                while (offset < rubbish.Length)
                {
                    if (random.Next(2) == 0)
                    {
                        var read = stream.ReadByte();
                        Assert.AreEqual(rubbish[offset], read);
                        ++offset;
                    }
                    else
                    {
                        int count = random.Next(rubbish.Length - offset + 1);
                        int read;
                        if (random.Next(2) == 0)
                            read = stream.Read(buffer, offset, count);
                        else
                            read = stream.ReadAsync(buffer, offset, count).Result;
                        Assert.That(count == 0 && read == 0 || read <= count && read > 0);
                        for (int i = 0; i < read; ++i)
                            Assert.AreEqual(rubbish[offset + i], buffer[offset + i]);
                        offset += read;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
