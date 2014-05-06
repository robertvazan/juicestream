using JuiceStream;
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
    [TestFixture]
    public class CancellableStreamTest
    {
        [Test]
        public void CommonOperations()
        {
            TestUtils.TestCommonOperations(inner => new CancellableStream(inner, new CancellationTokenSource().Token), bytes => bytes);
        }

        [Test]
        public void Twister()
        {
            var queue = new DuplexQueueStream();
            TestUtils.RunTwister(new CancellableStream(queue, new CancellationTokenSource().Token), new CancellableStream(queue.Peer, new CancellationTokenSource().Token));
        }
    }
}
