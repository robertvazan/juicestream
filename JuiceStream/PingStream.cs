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
                await Stream.WriteAsync(ping, 0, ping.Length, Cancel.Token);
            }
        }

        async Task AckPings()
        {
            var ack = new byte[] { 1 };
            while (true)
            {
                await AckQueue.WaitAsync(Cancel.Token);
                await Stream.WriteAsync(ack, 0, ack.Length, Cancel.Token);
            }
        }

        async Task ReadPings()
        {
            byte[] received = new byte[1];
            while (true)
            {
                var read = await Stream.ReadAsync(received, 0, 1, Cancel.Token);
                if (read == 0)
                    throw new EndOfStreamException();
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
