using DTM;
using System.Collections.Concurrent;
using System.Diagnostics;
using EventConsumer = System.Func<System.Collections.Generic.List<string>, System.Threading.CancellationToken, System.Threading.Tasks.Task>;

namespace Test
{
    internal class TestProcess
    {
        const int _threadCount = 10;
        const int _itemCount = 500000;

        private readonly ConcurrentBag<int> _numberCompletedEvents = [];
        private IDeferredTaskManagerService<string> _manager = default!;

        bool _start = false;

        internal async Task StartTest(IDeferredTaskManagerService<string> manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));

            Stopwatch stopwatch = new();

            stopwatch.Start();

            var consumers = GetConsumers();

            _manager.StartAsync(consumers.EventConsumer, consumers.EventConsumerRetryExhausted);

            await AddAsync();
            CountingTotalExecutionTime();

            stopwatch.Stop();

            Console.WriteLine($"Total execution time: {stopwatch.ElapsedMilliseconds} ms, events completed {_numberCompletedEvents.Sum()}");
        }

        private (EventConsumer EventConsumer, EventConsumer EventConsumerRetryExhausted) GetConsumers()
        {
            EventConsumer eventConsumer = async (events, cancellationToken) =>
            {
                try
                {
                    Thread.Sleep(1000);

                    await Task.Delay(1000, cancellationToken);

                    var test = string.Join(",", events);

                    AddNumberCompletedEvents(events.Count);

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

            EventConsumer eventConsumerRetryExhausted = async (events, cancellationToken) =>
            {
                Console.WriteLine("Something went wrong...");
            };

            return (eventConsumer, eventConsumerRetryExhausted);
        }

        async Task AddAsync()
        {
            while (_start == false && _manager?.FreePoolCount == 0) ;

            _start = true;

            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            var tasks = new List<Task>();

            for (int i = 0; i < _threadCount; i++)
                tasks.Add(Task.Run(() => Add()));

            await Task.WhenAll(tasks);

            stopwatch.Stop();

            Console.WriteLine($"The time of adding was: {stopwatch.ElapsedMilliseconds} ms, events completed {_numberCompletedEvents.Sum()}");
        }

        void Add()
        {
            for (int i = 0; i < _itemCount; i++)
            {
                _manager.Add(Guid.NewGuid().ToString() + " The implementation allows you to use multiple background tasks (or «runners») to process tasks from the queue.");
            }
        }

        public void AddNumberCompletedEvents(int value)
        {
            _numberCompletedEvents.Add(value);
        }

        void CountingTotalExecutionTime()
        {
            while (_numberCompletedEvents.Sum() < _threadCount * _itemCount) ;
        }
    }
}
