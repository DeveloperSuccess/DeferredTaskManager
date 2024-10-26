namespace DeferredTaskManager
{
    public interface IDeferredTaskManagerService<T>
    {
        void Add(T @event);
        void Add(IEnumerable<T> events);
        Task StartAsync(Func<IEnumerable<T>, CancellationToken, Task> taskFactory, int taskPoolSize = 1000, CancellationToken cancellationToken = default);
    }
}
