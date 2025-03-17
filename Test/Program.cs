using DTM;
using Test;

var testProcess = new TestProcess();

Func<List<string>, CancellationToken, Task> taskDelegate = async (events, cancellationToken) =>
{
    try
    {
        var test = string.Join(",", events);

        testProcess.AddNumberCompletedEvents(events.Count);

        // Тестовое исключение
        // throw new Exception("Тестовое исключение");

        await Task.Delay(10, cancellationToken);
    }
    catch (Exception ex)
    {
        // Пример обработки исключений (в случае ошибки можно оставить в коллекции выполненные а остальные пойдут в retry)
        events.Remove(events.FirstOrDefault());

        throw new Exception("Sending to retry after exclusion");
    }
};

Func<List<string>, CancellationToken, Task> taskDelegateRetryExhausted = async (events, cancellationToken) =>
{
    Console.WriteLine("Something went wrong...");
};

IDeferredTaskManagerService<string> _manager = new DeferredTaskManagerService<string>(
    new DeferredTaskManagerOptions<string>
    {
        TaskFactory = taskDelegate,
        TaskPoolSize = 1,
        CollectionType = CollectionType.Queue,
        RetryOptions = new RetryOptions<string> { RetryCount = 3, MillisecondsRetryDelay = 10000 }
    });

await testProcess.StartTest(_manager);

Console.ReadKey();
