// Part of JuiceStream: https://juicestream.machinezoo.com
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JuiceStream
{
    static class ForcedCancellable
    {
        internal static Task FromTask(Task inner, CancellationToken cancellation)
        {
            var source = new TaskCompletionSource();
            var registration = cancellation.Register(() => source.TrySetCanceled());
            inner.ContinueWith(t =>
            {
                registration.Dispose();
                if (t.IsCanceled)
                    source.TrySetCanceled();
                else if (t.IsFaulted)
                    source.TrySetException(t.Exception);
                else
                    source.TrySetResult();
            });
            return source.Task;
        }

        internal static Task<T> FromTask<T>(Task<T> inner, CancellationToken cancellation)
        {
            var source = new TaskCompletionSource<T>();
            var registration = cancellation.Register(() => source.TrySetCanceled());
            inner.ContinueWith(t =>
            {
                registration.Dispose();
                if (t.IsCanceled)
                    source.TrySetCanceled();
                else if (t.IsFaulted)
                    source.TrySetException(t.Exception);
                else
                    source.TrySetResult(t.Result);
            });
            return source.Task;
        }
    }
}
