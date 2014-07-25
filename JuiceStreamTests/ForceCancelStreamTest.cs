using JuiceStream;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JuiceStreamTests
{
    [TestFixture]
    public class ForceCancelStreamTest
    {
        [Test]
        public void StandardNetworkStream()
        {
            TestWith(s => s, false);
        }

        [Test]
        public void ForcedCancel()
        {
            TestWith(s => new ForceCancelStream(s), true);
        }

        void TestWith(Func<Stream, Stream> wrapper, bool expected)
        {
            var listener = new TcpListener(IPAddress.Loopback, 57234);
            listener.Start();
            try
            {
                using (var client = new TcpClient())
                {
                    TcpClient server = null;
                    var accept = Task.Run(() => server = listener.AcceptTcpClient());
                    client.Connect(IPAddress.Loopback, 57234);
                    accept.Wait();
                    using (server)
                    using (var stream = wrapper(client.GetStream()))
                    using (var cancellation = new CancellationTokenSource())
                    {
                        var read = stream.ReadAsync(new byte[1], cancellation.Token);
                        cancellation.Cancel();
                        bool result = false;
                        try
                        {
                            result = read.Wait(3000);
                            Assert.IsFalse(expected);
                        }
                        catch (AggregateException e)
                        {
                            Assert.IsInstanceOf<TaskCanceledException>(e.InnerException);
                            result = true;
                            Assert.IsTrue(expected);
                        }
                        Assert.AreEqual(expected, result);
                        client.Close();
                        try { read.Wait(); }
                        catch { }
                    }
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        [Test]
        public void Twister()
        {
            var queue = new DuplexQueueStream();
            TestUtils.RunTwister(new ForceCancelStream(queue), new ForceCancelStream(queue.Peer));
        }
    }
}
