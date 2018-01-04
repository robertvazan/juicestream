// Part of JuiceStream: https://juicestream.machinezoo.com
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
    public class PrefixStreamTests
    {
        [Test]
        public void CommonOperations()
        {
            TestUtils.TestCommonOperations(inner => new PrefixStream(new byte[] { 33, 22, 11 }, inner), bytes => new byte[] { 33, 22, 11 }.Concat(bytes));
        }

        [Test]
        public void Twister()
        {
            var queue = new DuplexQueueStream();
            TestUtils.RunTwister(new PrefixStream("prefix", queue), new PrefixStream("prefix", queue.Peer));
        }
    }
}
