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
    public class DuplexQueueStreamTest
    {
        [Test]
        public void Twister()
        {
            var queue = new DuplexQueueStream();
            TestUtils.RunTwister(queue, queue.Peer);
        }
    }
}
