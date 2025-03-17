using DTM;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Test
{
    internal class TestProcess
    {
        const int _threadCount = 10;
        const int _itemCount = 500000;

        private readonly ConcurrentBag<int> _numberCompletedEvents = [];
        private IDeferredTaskManagerService<string> _manager;

        bool _start = false;

        internal TestProcess()
        {

        }

        internal async Task StartTest(IDeferredTaskManagerService<string> manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));

            Stopwatch stopwatch = new();

            stopwatch.Start();

            Task.Run(() => _manager.StartAsync(default), default);

            await AddAsync();
            CountingTotalExecutionTime();

            stopwatch.Stop();

            Console.WriteLine($"Total execution time: {stopwatch.ElapsedMilliseconds} ms, events completed {_numberCompletedEvents.Sum()}");
        }

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
