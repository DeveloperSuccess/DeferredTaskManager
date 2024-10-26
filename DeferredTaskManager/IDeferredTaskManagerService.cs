namespace DeferringTasksManager
{
    public interface IDeferredTaskManagerService<T>
    {
        void Add(T @event);
        void Add(IEnumerable<T> events);
        Task StartAsync(Func<IEnumerable<T>, CancellationToken, Task> taskFactory, CancellationToken cancellationToken);
    }
}
