using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace DTM
{
    /// <summary>
    /// Options for Deferred Task Manager
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DeferredTaskManagerOptions<T>
    {
        /// <summary>
        /// A delegate for custom logic that receives a collection of consolidated events. 
        /// This is where you can perform the necessary operations on them before further transmission/processing. 
        /// You can also handle exceptions in the delegate.
        /// </summary>
        [Required]
        public Func<List<T>, CancellationToken, Task> TaskFactory { get; set; }

        /// <summary>
        /// The number of runners available to handle incoming events. The pool size setting is variable and is 
        /// selected by the developer for a specific range of tasks, focusing on the speed of 
        /// execution and the amount of resources consumed.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "The value must be greater than 0")]
        public int PoolSize { get; set; } = 1000;

        /// <summary>
        /// Select collection type, «Bag» for the Unordered collection of objects (it works faster) or «Queue» for the Ordered collection of objects.
        /// </summary>
        [Required]
        public CollectionType CollectionType { get; set; } = CollectionType.Queue;

        /// <summary>
        /// Options up the processing of added events after a certain time interval with the 
        /// possibility of variable deducting the time of the previous operation
        /// </summary>
        public SendDelayOptions? SendDelayOptions { get; set; }

        /// <summary>
        /// Settings for retries in case of exceptions
        /// </summary>
        [Required]
        public RetryOptions<T> RetryOptions { get; set; } = new RetryOptions<T>();
    }
}