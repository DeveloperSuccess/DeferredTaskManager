﻿using DTM;
using System.Collections.Concurrent;
using System.Diagnostics;

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

            _manager.StartAsync(EventConsumer, EventConsumerRetryExhausted);

            await AddAsync();
            CountingTotalExecutionTime();

            stopwatch.Stop();

            Console.WriteLine($"Total execution time: {stopwatch.ElapsedMilliseconds} ms, events completed {_numberCompletedEvents.Sum()}");
        }

        private async Task EventConsumer(List<string> events, CancellationToken cancellationToken)
        {
            Thread.Sleep(1000);
            await Task.Delay(1000, cancellationToken);

            //throw new Exception("Test Exception");

            var test = string.Join(",", events);

            AddNumberCompletedEvents(events.Count);
        }


        private async Task EventConsumerRetryExhausted(List<string> events, Exception ex, int retryCount, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Retry Count: {retryCount}; {ex}");
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
