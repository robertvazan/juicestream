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
    public class QueueStreamTest
    {
        [Test]
        public void Twister()
        {
            var queue = new QueueStream();
            TestUtils.RunOneWayTwister(queue, queue);
        }
    }
}
