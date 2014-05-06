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
            var client = new MultiplexStream(queue);
            var server = new MultiplexStream(queue.Peer);
            TestUtils.RunTwister(client.Connect(), server.Accept());
        }
    }
}
