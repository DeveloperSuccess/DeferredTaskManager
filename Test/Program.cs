using DTM;
using Test;

var testProcess = new TestProcess();

Func<List<string>, CancellationToken, Task> taskDelegate = async (events, cancellationToken) =>
{
    try
    {
        Thread.Sleep(1000);

        await Task.Delay(1000, cancellationToken);        

        var test = string.Join(",", events);

        testProcess.AddNumberCompletedEvents(events.Count);

        // Тестовое исключение
        // throw new Exception("Тестовое исключение");        
    }
    catch
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

var dtmOptions = new DeferredTaskManagerOptions<string>
{
    EventConsumer = taskDelegate,
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
        EventConsumerRetryExhausted = taskDelegateRetryExhausted
    }
};

IDeferredTaskManagerService<string> manager = new DeferredTaskManagerService<string>();

await testProcess.StartTest(manager, dtmOptions);

Console.ReadKey();
