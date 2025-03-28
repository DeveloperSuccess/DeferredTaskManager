[![en](https://img.shields.io/badge/lang-en-red.svg)](./README.md)

# Диспетчер фоновых задач C# на основе паттерна Runners

[![NuGet version (BackgroundTaskManager)](https://img.shields.io/nuget/v/BackgroundTaskManager.svg?style=flat-square)](https://www.nuget.org/packages/BackgroundTaskManager)

Реализация позволяет использовать несколько фоновых задач (или «раннеров») для фоновой обработки консолидированных данных. Раннеры построены на шаблоне PubSub для асинхронного ожидания новых задач, что делает этот подход более реактивным, но менее ресурсоемким.

## Отличительное преимущество

Решение позволяет производить консолидацию данных в текущей инстанции с возможностью вариативного проведения дедупликации или любых других операций на усмотрение разработчика, что может сократить ресурсы при дальнейшей передаче и обработке, а также увеличить быстродействие.

## Пример использования

1️⃣ Внедрение Singleton зависимости с требуемым типом данных:

```
services.AddSingleton<IBackgroundTaskManagerService<object>, BackgroundTaskManagerService<object>>();
```

2️⃣ Фоновые задачи выполняются в отдельном потоке от фоновой службы, при желании вы можете запустить каждый BackgroundTaskManager в отдельном потоке:

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

Размер пула вариативен и подбирается разработчиком для конкретного спектра задач, ориентируясь на скорость выполнения и количество потребляемых ресурсов.

Можно указать тип коллекции: «Bag» для неупорядоченной коллекции объектов (это работает быстрее) или «Queue» для упорядоченной коллекции объектов.

Вы также можете передать делегат обработки ошибок, который сработает, когда будет исчерпано указанное количество повторных попыток. 

3️⃣ Отправка данных в Background Task Manager для последующего выполнения:

```
_backgroundTaskManager.Add(events);
```
