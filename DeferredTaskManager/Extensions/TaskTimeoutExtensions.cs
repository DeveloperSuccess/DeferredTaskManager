﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DTM.Extensions
{
    internal static class TaskTimeoutExtensions
    {
        #region WaitAsync polyfills
        // Test polyfills when targeting a platform that doesn't have these ConfigureAwait overloads on Task

        internal static Task WaitAsync(this Task task, int millisecondsTimeout) =>
            task.WaitAsync(TimeSpan.FromMilliseconds(millisecondsTimeout), default);

        internal static Task WaitAsync(this Task task, TimeSpan timeout) =>
            task.WaitAsync(timeout, default);

        internal static Task WaitAsync(this Task task, CancellationToken cancellationToken) =>
            task.WaitAsync(Timeout.InfiniteTimeSpan, cancellationToken);

        internal async static Task WaitAsync(this Task task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (new Timer(s => ((TaskCompletionSource<bool>)s).TrySetException(new TimeoutException()), tcs, timeout, Timeout.InfiniteTimeSpan))
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetCanceled(), tcs))
            {
                await (await Task.WhenAny(task, tcs.Task).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        internal static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, int millisecondsTimeout) =>
            task.WaitAsync(TimeSpan.FromMilliseconds(millisecondsTimeout), default);

        internal static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, TimeSpan timeout) =>
            task.WaitAsync(timeout, default);

        internal static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, CancellationToken cancellationToken) =>
            task.WaitAsync(Timeout.InfiniteTimeSpan, cancellationToken);

        internal static async Task<TResult> WaitAsync<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<TResult>();
            using (new Timer(s => ((TaskCompletionSource<TResult>)s).TrySetException(new TimeoutException()), tcs, timeout, Timeout.InfiniteTimeSpan))
            using (cancellationToken.Register(s => ((TaskCompletionSource<TResult>)s).TrySetCanceled(), tcs))
            {
                return await (await Task.WhenAny(task, tcs.Task).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }
        #endregion

        internal static async Task WhenAllOrAnyFailed(this Task[] tasks, int millisecondsTimeout) =>
            await tasks.WhenAllOrAnyFailed().WaitAsync(TimeSpan.FromMilliseconds(millisecondsTimeout));

        internal static async Task WhenAllOrAnyFailed(Task t1, Task t2, int millisecondsTimeout) =>
            await new Task[] { t1, t2 }.WhenAllOrAnyFailed(millisecondsTimeout);

        internal static async Task WhenAllOrAnyFailed(this Task[] tasks)
        {
            try
            {
                await tasks.WhenAllOrAnyFailedCore().ConfigureAwait(false);
            }
            catch
            {
                // Wait a bit to allow other tasks to complete so we can include their exceptions
                // in the error we throw.
                try
                {
                    await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(3)); // arbitrary delay; can be dialed up or down in the future
                }
                catch { }

                var exceptions = new List<Exception>();
                foreach (Task t in tasks)
                {
                    if (t.IsCompleted && t.GetRealException() is Exception e)
                    {
                        exceptions.Add(e);
                    }
                }

                Debug.Assert(exceptions.Count > 0);
                if (exceptions.Count > 1)
                {
                    throw new AggregateException(exceptions);
                }
                throw;
            }
        }

        private static Task WhenAllOrAnyFailedCore(this Task[] tasks)
        {
            int remaining = tasks.Length;
            var tcs = new TaskCompletionSource<bool>();
            foreach (Task t in tasks)
            {
                t.ContinueWith(a =>
                {
                    if (a.GetRealException() is Exception e)
                    {
                        tcs.TrySetException(e);
                    }
                    else if (Interlocked.Decrement(ref remaining) == 0)
                    {
                        tcs.TrySetResult(true);
                    }
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            }
            return tcs.Task;
        }

        // Gets the exception (if any) from the Task, for both faulted and cancelled tasks
        private static Exception? GetRealException(this Task task)
        {
            try
            {
                task.GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                return e;
            }

            return null;
        }
    }
}
