// Part of JuiceStream: https://juicestream.machinezoo.com
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JuiceStream
{
    /// <summary>
    /// Half-open TCP connections can cause serious sync issues in applications.
    /// Default 2-hour TCP keepalive cannot be configured on a per-connection basis on Windows.
    /// Common wisdom is to implement application-layer ping. <c>PingStream</c> will spare you of this duty.
    /// Designed to work over single substream of <c>MultiplexStream</c>,
    /// both endpoints of the <c>PingStream</c> will perform regular pings in order to kill broken connections as early as possible.
    /// </summary>
    public class PingStream : IDisposable
    {
        readonly Stream Stream;
        readonly TaskCompletionSource CompletionSource = new TaskCompletionSource();
        readonly CancellationTokenSource Cancel = new CancellationTokenSource();
        readonly SemaphoreSlim AckQueue = new SemaphoreSlim(0);

        public TimeSpan Interval { get; set; }
        public Task Completed { get { return CompletionSource.Task; } }

        public PingStream(Stream stream)
        {
            Stream = stream;
            Interval = TimeSpan.FromMinutes(5);
        }

        public void Start() { Run(); }

        public void Dispose()
        {
            Cancel.Cancel();
            Stream.Dispose();
            CompletionSource.TrySetResult();
        }

        async void Run()
        {
            await Task.WhenAll(new Func<Task>[] { SendPings, AckPings, ReadPings }.Select(t => CaptureErrors(t)).ToArray());
            AckQueue.Dispose();
        }

        async Task SendPings()
        {
            var ping = new byte[] { 0 };
            while (true)
            {
                await Task.Delay(Interval, Cancel.Token);
                await Stream.WriteAsync(ping, Cancel.Token);
            }
        }

        async Task AckPings()
        {
            var ack = new byte[] { 1 };
            while (true)
            {
                await AckQueue.WaitAsync(Cancel.Token);
                await Stream.WriteAsync(ack, Cancel.Token);
            }
        }

        async Task ReadPings()
        {
            byte[] received = new byte[1];
            while (true)
            {
                await Stream.ReadFixedAsync(received, Cancel.Token);
                if (received[0] == 0)
                    AckQueue.Release();
            }
        }

        async Task CaptureErrors(Func<Task> task)
        {
            try
            {
                await task();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                CompletionSource.TrySetException(e);
                Dispose();
            }
        }
    }
}
