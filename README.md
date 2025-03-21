[![ru](https://img.shields.io/badge/lang-ru-green.svg)](./README.ru.md)

# Deferred Task Manager C# based on the Runners pattern

[![NuGet version (DeferredTaskManager)](https://img.shields.io/nuget/v/DeferredTaskManager.svg?style=flat-square)](https://www.nuget.org/packages/DeferredTaskManager)


The implementation allows you to use multiple background tasks (or «runners») to process tasks from the queue. Classic runners usually do not wait, but constantly check the queue for tasks. The current implementation uses the PubSub pattern to wait for new tasks, which makes this approach more reactive but less resource-intensive.

## Usage example

1️⃣ Injection of the Singleton dependency with the required data type:

```
services.AddSingleton<IDeferredTaskManagerService<object>, DeferredTaskManagerService<object>>();
```

2️⃣ Background tasks are executed in a separate thread from the background service, if desired, you can run each DeferredTaskManager in a separate thread:

```
internal sealed class EventManagerService : BackgroundService
{
    private readonly IDeferredTaskManagerService<object> _deferredTaskManager;

    public EventManagerService(IDeferredTaskManagerService<object> deferredTaskManager)
    {
        _deferredTaskManager = deferredTaskManager ?? throw new ArgumentNullException(nameof(deferredTaskManager));
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Func<List<object>, CancellationToken, Task> taskDelegate = (events, cancellationToken) =>
        {
            return Task.Delay(1000000, cancellationToken);
        };

        Func<List<object>, CancellationToken, Task> taskDelegateRetryExhausted = async (events, cancellationToken) =>
        {
            Console.WriteLine("Something went wrong...");
        };

        var dtmOptions = new DeferredTaskManagerOptions<string>
        {
            TaskFactory = taskDelegate,
            TaskPoolSize = 1,
            CollectionType = CollectionType.Queue,
            RetryOptions = new RetryOptions<string>
            {
                RetryCount = 3,
                MillisecondsRetryDelay = 10000,
                TaskFactoryRetryExhausted = taskDelegateRetryExhausted
            }
        };

        return Task.Run(() => _deferredTaskManager.StartAsync(dtmOptions, cancellationToken), cancellationToken);
    }
}
```

A delegate with your logic must be passed to the launch method. The size of the runners pool and the parameters for resending in case of errors are variable. You can also specify the collection type, «Bag» for the Unordered collection of objects (it works faster) or «Queue» for the Ordered collection of objects.

You can also pass an error handling delegate that will trigger when the specified number of retries is exhausted.

3️⃣ Sending data to the Deferred Task Manager for subsequent execution:

```
_deferredTaskManager.Add(events);
```
