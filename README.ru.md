[![en](https://img.shields.io/badge/lang-en-red.svg)](./README.md)

# Диспетчер отложенных задач C# на основе паттерна Runners

[![NuGet version (DeferredTaskManager)](https://img.shields.io/nuget/v/DeferredTaskManager.svg?style=flat-square)](https://www.nuget.org/packages/DeferredTaskManager)

Реализация позволяет использовать несколько фоновых задач (или «раннеров») для отложенной обработки консолидированных данных. Раннеры построены на шаблоне PubSub для асинхронного ожидания новых задач, что делает этот подход более реактивным, но менее ресурсоемким.

## Отличительное преимущество

Решение позволяет производить консолидацию данных в текущей инстанции с возможностью вариативного проведения дедупликации или любых других операций на усмотрение разработчика, что может сократить ресурсы при дальнейшей передаче и обработке, а также увеличить быстродействие.

## Пример использования

1️⃣ Внедрение Singleton зависимости с требуемым типом данных:

```
services.AddSingleton<IDeferredTaskManagerService<object>, DeferredTaskManagerService<object>>();
```

2️⃣ Фоновые задачи выполняются в отдельном потоке от фоновой службы, при желании вы можете запустить каждый DeferredTaskManager в отдельном потоке:

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
            PoolSize = 1,
            CollectionType = CollectionType.Queue,
            SendDelayOptions = new SendDelayOptions()
            {
                MillisecondsSendDelay = 60000,
                ConsiderDifference = true
            },
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
### Делегат TaskFactory

Вся кастомная логика размещается в делегате ```TaskFactory```, в который приходит коллекция консолидированных событий. Именно здесь можно осуществить необходимые операции над ними перед дальнейшей передачей/обработкой. Также в делегате можно обработать исключения (это актуально, если события обрабатываются по отдельности), отправив необработанные события на следующий заход после указанной в параметрах временной задержки ```MillisecondsRetryDelay```.
```
try
{
    // Кастомная операция над полученными событиями
}
catch (Exception ex)
{
    events.RemoveRange(successEvents);

    // Можно выдать исключение после удаления успешных эвентова
    // или добавить собственные условия
    throw new Exception("Отправка на повторную попытку после исключения");
}
```
### Размер пула (количество доступных раннеров)
Размер пула вариативен и подбирается разработчиком для конкретного спектра задач, ориентируясь на скорость выполнения и количество потребляемых ресурсов.
### Тип коллекции
Можно указать тип коллекции: «Bag» для неупорядоченной коллекции объектов (это работает быстрее) или «Queue» для упорядоченной коллекции объектов.
### Настройки обработки исключений
Вы также можете передать делегат обработки ошибок, который сработает, когда будет исчерпано указанное количество повторных попыток. 

3️⃣ Отправка данных в Deferred Task Manager для последующего выполнения:

```
_deferredTaskManager.Add(events);
```
