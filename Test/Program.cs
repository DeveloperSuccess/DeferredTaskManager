using DTM;
using Microsoft.Extensions.DependencyInjection;
using Test;

var testProcess = new TestProcess(threadCount: 4, itemCount: 5_000_000);

using (var scope = GetServiceProvider().CreateScope())
{
    var manager = scope.ServiceProvider.GetService<IDeferredTaskManagerService<string>>();

    await testProcess.StartTest(manager);
}

Console.ReadKey();

ServiceProvider GetServiceProvider()
{
    var services = new ServiceCollection();
    services.AddDeferredTaskManager<string>(options =>
    {
        options.PoolSize = 4;
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
    return services.BuildServiceProvider();
}
