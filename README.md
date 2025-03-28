[![ru](https://img.shields.io/badge/lang-ru-green.svg)](./README.ru.md)

# Background Task Manager C# based on the Runners pattern

[![NuGet version (BackgroundTaskManager)](https://img.shields.io/nuget/v/BackgroundTaskManager.svg?style=flat-square)](https://www.nuget.org/packages/BackgroundTaskManager)


The implementation allows you to use multiple background tasks (or "runners") for background processing of consolidated data. Runners are based on the PubSub template for asynchronous waiting for new tasks, which makes this approach more reactive but less resource-intensive.

## Distinctive advantage

The solution allows data consolidation in the current instance with the possibility of variable deduplication or any other operations at the discretion of the developer, which can reduce resources during further transmission and processing, as well as increase performance.

## Usage example

1️⃣ Injection of the Singleton dependency with the required data type:

```
services.AddSingleton<IBackgroundTaskManagerService<object>, BackgroundTaskManagerService<object>>();
```

2️⃣ Background tasks are executed in a separate thread from the background service, if desired, you can run each BackgroundTaskManager in a separate thread:

```
internal sealed class EventManagerService : BackgroundService
{
    private readonly IBackgroundTaskManagerService<object> _backgroundTaskManager;

    public EventManagerService(IBackgroundTaskManagerService<object> backgroundTaskManager)
    {
        _backgroundTaskManager = backgroundTaskManager ?? throw new ArgumentNullException(nameof(backgroundTaskManager));
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

        var dtmOptions = new BackgroundTaskManagerOptions<string>
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

        return Task.Run(() => _backgroundTaskManager.StartAsync(dtmOptions, cancellationToken), cancellationToken);
    }
}
```

The pool size is variable and is selected by the developer for a specific range of tasks, focusing on the speed of execution and the amount of resources consumed.

You can also specify the collection type, «Bag» for the Unordered collection of objects (it works faster) or «Queue» for the Ordered collection of objects.

You can also pass an error handling delegate that will trigger when the specified number of retries is exhausted.

3️⃣ Sending data to the Background Task Manager for subsequent execution:

```
_backgroundTaskManager.Add(events);
```
