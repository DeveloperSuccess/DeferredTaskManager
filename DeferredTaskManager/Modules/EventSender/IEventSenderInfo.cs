using System;

namespace DTM
{
    /// <summary>
    /// Interface for interacting with the sender module
    /// </summary>
    /// <typeparamref name="T"/>
    public interface IEventSenderInfo<T>
    {
        /// <summary>
        /// Date and time since the runners were created before they were expected
        /// </summary>
        DateTimeOffset StartAt { get; }

        /// <summary>
        /// Date and time of the last access to the method before sending data to the user delegate
        /// </summary>
        DateTimeOffset LastSendAt { get; }
    }
}
