using DTM;
using Microsoft.Extensions.DependencyInjection;
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
    PoolSize = Environment.ProcessorCount,
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

using (var scope = GetServiceProvider().CreateScope())
{
    var manager = scope.ServiceProvider.GetService<IDeferredTaskManagerService<string>>();

    await testProcess.StartTest(manager, dtmOptions);
}

Console.ReadKey();

ServiceProvider GetServiceProvider()
{
    var services = new ServiceCollection();
    services.AddDeferredTaskManager<string>();
    return services.BuildServiceProvider();
}
