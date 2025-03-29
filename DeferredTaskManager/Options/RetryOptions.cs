using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace DTM
{
    public class RetryOptions<T>
    {
        [Range(0, int.MaxValue, ErrorMessage = "The value must be greater than or equal to 0")]
        public int RetryCount { get; set; } = 0;

        [Range(0, int.MaxValue, ErrorMessage = "The value must be greater than or equal to 0")]
        public int MillisecondsRetryDelay { get; set; } = 100;

        public Func<List<T>, CancellationToken, Task>? TaskFactoryRetryExhausted { get; set; } = null;
    }
}
