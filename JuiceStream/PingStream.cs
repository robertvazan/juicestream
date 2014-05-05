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
        readonly TaskCompletionSource<Exception> FailTask = new TaskCompletionSource<Exception>();
        readonly CancellationTokenSource Cancel = new CancellationTokenSource();
        readonly SemaphoreSlim AckQueue = new SemaphoreSlim(0);
        static readonly byte[] PingMessage = new byte[] { 0 };
        static readonly byte[] AckMessage = new byte[] { 1 };

        public TimeSpan Interval { get; set; }
        public Task<Exception> Failed { get { return FailTask.Task; } }

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
        }

        async void Run()
        {
            await Task.WhenAll(new Func<Task>[] { SendPings, AckPings, ReadPings }.Select(t => CaptureErrors(t)).ToArray());
            AckQueue.Dispose();
        }

        async Task SendPings()
        {
            while (true)
            {
                await Task.Delay(Interval, Cancel.Token);
                await Stream.WriteAsync(PingMessage, 0, PingMessage.Length, Cancel.Token);
            }
        }

        async Task AckPings()
        {
            while (true)
            {
                await AckQueue.WaitAsync(Cancel.Token);
                await Stream.WriteAsync(AckMessage, 0, AckMessage.Length, Cancel.Token);
            }
        }

        async Task ReadPings()
        {
            try
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
            catch (Exception e)
            {
                FailTask.TrySetResult(e);
                Dispose();
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
                FailTask.TrySetResult(e);
                Dispose();
            }
        }
    }
}
