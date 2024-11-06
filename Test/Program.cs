using DeferredTaskManager;
using System.Collections.Concurrent;
using System.Diagnostics;

const int _threadCount = 10;
const int _itemCount = 500000;

bool _start = false;

Stopwatch stopwatch = new Stopwatch();

stopwatch.Start();

IDeferredTaskManagerService<string> _manager = new DeferredTaskManagerService<string>();
ConcurrentBag<int> _numberCompletedEvents = new ConcurrentBag<int>();

ExecuteAsync(default);

await AddAsync();

await CountingTotalExecutionTime();

stopwatch.Stop();

Console.WriteLine($"Общее время выполнения: {stopwatch.ElapsedMilliseconds} ms, эвентов завершено {_numberCompletedEvents.Sum()}");

Console.ReadKey();

async Task AddAsync()
{
    while (_start == false && _manager?.SubscribersCount == 0) ;

    _start = true;

    Stopwatch stopwatch = new Stopwatch();

    stopwatch.Start();

    var tasks = new List<Task>();

    for (int i = 0; i < _threadCount; i++)
        tasks.Add(Task.Run(() => Add()));

    await Task.WhenAll(tasks);

    stopwatch.Stop();

    Console.WriteLine($"Время добавления составило: {stopwatch.ElapsedMilliseconds} ms, эвентов завершено {_numberCompletedEvents.Sum()}");
}

void Add()
{
    for (int i = 0; i < _itemCount; i++)
    {
        _manager.Add(Guid.NewGuid().ToString() + "Ахуенно Ахуенно Ахуенно gwgrge ehehehe erherhrehreh erhrehrehrhreherhrehrehre rehrehrehreh erhrehreherherhrehreh erhreherhrehrehre");
    }
}

Task ExecuteAsync(CancellationToken cancellationToken)
{
    Func<List<string>, CancellationToken, Task> taskDelegate = async (events, cancellationToken) =>
    {
        try
        {
            var test = string.Join(",", events);

            _numberCompletedEvents.Add(events.Count);

            // Тестовое исключение
            // throw new Exception("Тестовое исключение");

            await Task.Delay(10, cancellationToken);
        }
        catch (Exception ex)
        {
            // Пример обработки исключений (в случае ошибки можно оставить в коллекции выполненные а остальные пойдут в retry)
            events.Remove(events.FirstOrDefault());

            throw new Exception("Отправка в retry после исключения");
        }
    };

    return Task.Run(() => _manager.StartAsync(taskDelegate, taskPoolSize: 1, retry: 3, retryDelayMilliseconds: 1000, cancellationToken: cancellationToken));
}

async Task CountingTotalExecutionTime()
{
    while (_numberCompletedEvents.Sum() < _threadCount * _itemCount) ;
}