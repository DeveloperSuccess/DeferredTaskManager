using DTM.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DTM
{

    internal class EventSenderDefault<T> : IEventSender<T>
    {
        private readonly IPoolPubSub _pubSub;
        private readonly DeferredTaskManagerOptions<T> _options;
        private readonly IEventStorage<T> _eventStorage;

        public EventSenderDefault(IOptions<DeferredTaskManagerOptions<T>> options, IEventStorage<T> eventStorage, IPoolPubSub pubSub)
        {
            _options = options.Value;
            _eventStorage = eventStorage;
            _pubSub = pubSub;
        }

        public Task StartBackgroundTasks(CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            for (int i = 0; i < _options.PoolSize; i++)
            {
                tasks.Add(SenderAsync(cancellationToken));
            }

            if (_options.SendDelayOptions != null)
            {
                tasks.Add(StartSendDelay(cancellationToken));
            }

            return Task.WhenAll(tasks);
        }

        public void SendEvents()
        {
            _pubSub.SendEvents();
        }

        private async Task StartSendDelay(CancellationToken cancellationToken)
        {
            if (_options.SendDelayOptions == null) return;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!_options.SendDelayOptions.ConsiderDifference)
                {
                    SendEvents();
                    await Task.Delay((_options.SendDelayOptions.MillisecondsSendDelay), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                SendEvents();

                stopWatch.Stop();

                var delay = _options.SendDelayOptions.MillisecondsSendDelay - (int)stopWatch.ElapsedMilliseconds;

                await Task.Delay(delay > 0 ? delay : 0, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SenderAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _pubSub.Subscribe(out Guid subscriberKey, out Task<bool> task);

                if (_eventStorage.IsEmpty)
                {
                    try
                    {
                        await task.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch
                    {
                        _pubSub.Unsubscribe(subscriberKey);
                        continue;
                    }
                }
                else
                {
                    _pubSub.Unsubscribe(subscriberKey);
                }

                var events = _eventStorage.GetEventsAndClearStorage();
                if (events.Count == 0) continue;

                await SendEventsAsync(events, _options.RetryOptions.RetryCount, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SendEventsAsync(List<T> events, int retryCount, CancellationToken cancellationToken)
        {
            try
            {
                await _options.EventConsumer(events, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Task.Delay(_options.RetryOptions.MillisecondsRetryDelay, cancellationToken).ConfigureAwait(false);

                if (_options.RetryOptions.EventConsumerRetryExhausted != null)
                {
                    await _options.RetryOptions.EventConsumerRetryExhausted(events, ex, cancellationToken).ConfigureAwait(false);
                }

                if (retryCount == 0) return;

                await SendEventsAsync(events, retryCount - 1, cancellationToken).ConfigureAwait(false);
            }
        }
    };
}