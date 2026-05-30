using System;
using System.Threading;
using System.Threading.Tasks;

namespace DTM.Extensions
{
    internal static class TaskTimeoutExtensions
    {
        internal static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, CancellationToken cancellationToken) =>
            task.WaitAsync(Timeout.InfiniteTimeSpan, cancellationToken);

        internal static async Task<TResult> WaitAsync<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var delayTask = Task.Delay(timeout, cts.Token);

            var completedTask = await Task.WhenAny(task, delayTask).ConfigureAwait(false);

            if (completedTask == task)
            {
                cts.Cancel();

                return await task.ConfigureAwait(false);
            }
            else
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
                else
                {
                    cts.Cancel();

                    throw new TimeoutException();
                }
            }
        }
    }
}
