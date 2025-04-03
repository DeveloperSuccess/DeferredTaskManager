using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace DTM
{
    /// <summary>
    /// Settings for retries in case of exceptions
    /// </summary>
    /// <typeparamref name="T"/>
    public class RetryOptions<T>
    {
        /// <summary>
        /// Number of retry
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "The value must be greater than or equal to 0")]
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Time interval between retry of execution in milliseconds
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "The value must be greater than or equal to 0")]
        public int MillisecondsRetryDelay { get; set; } = 100;

        /// <summary>
        /// An error handling delegate that will trigger when the specified number of retries is exhausted
        /// </summary>
        public Func<List<T>, CancellationToken, Task>? TaskFactoryRetryExhausted { get; set; } = null;
    }
}
