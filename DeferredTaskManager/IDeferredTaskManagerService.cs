using DeferredTaskManager.Enum;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DeferredTaskManager
{
    public interface IDeferredTaskManagerService<T>
    {
        void Add(T @event);
        void Add(IEnumerable<T> events);
        Task StartAsync(Func<List<T>, CancellationToken, Task> taskFactory, int taskPoolSize = 1000, CollectionType collectionType = CollectionType.Queue, int retry = 0, int millisecondsRetryDelay = 100, CancellationToken cancellationToken = default);
        Task StartAsync(Func<List<T>, CancellationToken, Task> taskFactory, int taskPoolSize = 1000, int retry = 0, int millisecondsRetryDelay = 100, CancellationToken cancellationToken = default);
        int TaskCount { get; }
        int SubscribersCount { get; }
    }
}
