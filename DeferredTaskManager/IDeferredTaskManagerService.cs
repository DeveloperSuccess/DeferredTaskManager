using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DTM
{
    public interface IDeferredTaskManagerService<T>
    {
        void Add(T @event);
        void Add(IEnumerable<T> events);
        Task StartAsync(DeferredTaskManagerOptions<T> deferredTaskManagerOptions, CancellationToken cancellationToken = default);
        int TaskCount { get; }
        int SubscribersCount { get; }
    }
}
