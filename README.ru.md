[![en](https://img.shields.io/badge/lang-en-red.svg)](./README.md)

# Событийный диспетчер отложенных задач на C#

[![NuGet version (DeferredTaskManager)](https://img.shields.io/nuget/v/DeferredTaskManager.svg?style=flat-square)](https://www.nuget.org/packages/DeferredTaskManager)

Реализация позволяет использовать несколько фоновых задач (или «раннеров») для отложенной обработки консолидированных данных. Раннеры построены на шаблоне PubSub для асинхронного ожидания новых задач, что делает этот подход более реактивным, но менее ресурсоемким.

## Отличительное преимущество

Решение позволяет производить консолидацию данных в текущей инстанции с возможностью вариативного проведения дедупликации или любых других операций на усмотрение разработчика, что может сократить ресурсы при дальнейшей передаче и обработке, а также увеличить быстродействие.

## Пример использования

### 1️⃣ Внедрение Singleton зависимости с требуемым типом данных
В качестве примера `DeferredTaskManager` регистрируется в `DI` с типом `string`:
```
services.AddDeferredTaskManager<string>(options =>
{
    options.PoolSize = Environment.ProcessorCount;
    options.CollectionType = CollectionType.Bag;
    options.SendDelayOptions = new SendDelayOptions()
    {
        MillisecondsSendDelay = 60000,
        ConsiderDifference = true
    };
    options.RetryOptions = new RetryOptions<string>
    {
        RetryCount = 3,
        MillisecondsRetryDelay = 10000,
    };
});
```
#### ⚪ `PoolSize` — размер пула (количество доступных раннеров)
Размер пула вариативен и подбирается разработчиком для конкретного спектра задач, ориентируясь на скорость выполнения и количество потребляемых ресурсов.
#### ⚪ `CollectionType` — тип коллекции
Можно указать тип коллекции хранения эвентов: «Bag» для неупорядоченной коллекции объектов (это работает быстрее) или «Queue» для упорядоченной коллекции объектов. Использовать «Queue» целесообразно только в том случае, если `PoolSize = 1`, в противном случае порядок выполнения не гарантирован. 
#### ⚪ `SendDelayOptions` — настройка отправки событий через временной интервал
Настраивает отправку добавленных событий на обработку через определенный промежуток времени с возможностью переменного вычета времени предыдущей операции. Имеет смысл указывать, когда при добавлении событий используется флаг `sendEvents = true`, который добавляет события без отправки на обработку.
#### ⚪ `RetryOptions` — настройка обработки исключений
Вы также можете указать параметры для повторных попыток обработки событий в случае возникновения исключений.

### 2️⃣ Создание фоновой службы
В качестве примера приведено создание фоновой службы для `DeferredTaskManager<string>`:
```
internal sealed class EventManagerService : BackgroundService
{
    private readonly IDeferredTaskManagerService<string> _deferredTaskManager;

    public EventManagerService(IDeferredTaskManagerService<string> deferredTaskManager)
    {
        _deferredTaskManager = deferredTaskManager;
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
       // Делегат для кастомной логики, в который поступают события от запущенных раннеров. В качестве примера в нём производится конкатенирование событий,
       // но возмажна любая другая вариативная обработка или отправка куда-либо.
       Func<List<string>, CancellationToken, Task> eventConsumer = async (events, cancellationToken) =>
        {
            // Конкатенация событий
            var concatenatedEvents = string.Join(",", events);

            /// Любая дальнейшая обработка/отправка конкатенированных событий
            Thread.Sleep(1000);
            await Task.Delay(1000, cancellationToken);
        };

        Func<List<string>, CancellationToken, Task> eventConsumerRetryExhausted = async (events, cancellationToken) =>
        {
            Console.WriteLine("Что-то пошло не так...");
        };

        return _deferredTaskManager.StartAsync(eventConsumer, eventConsumerRetryExhausted, cancellationToken);
    }
}
```
#### ⚪ ```EventConsumer``` — основной делегат для кастомной логики

Вся кастомная логика размещается в делегате `EventConsumer`, в который приходит коллекция консолидированных событий. Именно здесь можно осуществить необходимые операции над ними перед дальнейшей передачей/обработкой. Также в делегате можно обработать исключения (это актуально, если события обрабатываются по отдельности), отправив необработанные события на следующий заход после указанной в параметрах временной задержки `MillisecondsRetryDelay`. В приведенном примере в делегате выполняется конкатенация поступаемых событий от запущенных раннеров.
```
try
{
    // Тестовое исключение
    // throw new Exception("Тестовое исключение");     
}
catch (Exception ex)
{
    // В случае обработки событий по отдельности,
    // можно удалить из коллекции эвентов выполненные события, тогда незавершенные пойдут в retry
    events.RemoveRange(successEvents);

    // Любая кастомная логика (логирование и т. п.)
}
```

### 3️⃣ Получение внедренной зависимости и осуществление добавления события(й)

```
_deferredTaskManager.Add(events);
```

## Альтернативные варианты использования
```DeferredTaskManager``` можно использовать как обычное хранилище событий, получая эвенты по требованию методом ```GetEventsAndClearStorage``` минуя раннеры, или осуществлять отправку доступных событий в делегат любому доступному раннеру по требованию — с помощью метода ```SendEvents```.
