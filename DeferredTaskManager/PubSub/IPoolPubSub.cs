using System;
using System.Threading.Tasks;

namespace DTM
{
    public interface IPoolPubSub
    {
        int SubscribersCount { get; }
        void SendEvents();
        void Subscribe(out Guid subscriberKey, out Task<bool> task);
        void Unsubscribe(Guid subscriberKey);
    }
}
