using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BTM
{
    public interface IBackgroundTaskManagerService<T>
    {
        void Add(T @event);
        void Add(IEnumerable<T> events);
        Task StartAsync(BackgroundTaskManagerOptions<T> BackgroundTaskManagerOptions, CancellationToken cancellationToken = default);
        int TaskCount { get; }
        int SubscribersCount { get; }
    }
}
