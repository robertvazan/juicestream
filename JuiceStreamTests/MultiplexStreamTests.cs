using JuiceStream;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JuiceStreamTests
{
    [TestFixture]
    public class MultiplexStreamTests
    {
        [Test]
        public void SingleTwister()
        {
            var queue = new DuplexQueueStream();
            var clientPX = new MultiplexStream(queue);
            var serverPX = new MultiplexStream(queue.Peer);
            var client = clientPX.Connect();
            client.WriteByte(123);
            var server = serverPX.Accept();
            Assert.AreEqual(123, server.ReadByte());
            TestUtils.RunTwister(client, server);
        }
    }
}
