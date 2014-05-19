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
            using (var client = new MultiplexStream(queue))
            using (var server = new MultiplexStream(queue.Peer))
                TestUtils.RunTwister(client.Connect(), server.Accept());
        }

        [Test]
        public void ParallelTwister()
        {
            TestUtils.SetupLogging();
            var queue = new DuplexQueueStream();
            using (var client = new MultiplexStream(queue))
            using (var server = new MultiplexStream(queue.Peer))
            {
                for (int n = 1; n <= 10; ++n)
                {
                    NLog.LogManager.GetCurrentClassLogger().Info("Running {0} parallel streams", n);
                    var jobs = Enumerable.Range(0, n).Select(nn =>
                    {
                        var subclient = client.Connect();
                        var subserver = server.Accept();
                        return new Action(() => TestUtils.RunTwister(subclient, subserver, TimeSpan.FromSeconds(30)));
                    }).ToList();
                    Task.WhenAll(jobs.Select(job => Task.Run(job)).ToList()).Wait();
                }
            }
        }
    }
}
