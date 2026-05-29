using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DTM
{
    /// <inheritdoc/>
    public class PoolPubSub<T> : IPoolPubSub<T>
    {
        private readonly Channel<bool> _channel = Channel.CreateUnbounded<bool>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

        private int _subscribersCount;

        /// <inheritdoc/>
        public int SubscribersCount => Volatile.Read(ref _subscribersCount);

        /// <inheritdoc/>
        public void SendEvents()
        {
            _channel.Writer.TryWrite(true);
        }

        /// <inheritdoc/>
        public async Task<bool> WaitForSignalAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _subscribersCount);
            try
            {
                if (await _channel.Reader.WaitToReadAsync(cancellationToken))
                {
                    if (_channel.Reader.TryRead(out _))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref _subscribersCount);
            }
        }
    }

}
