using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace DTM
{
    public class DeferredTaskManagerOptions<T>
    {
        [Required]
        public Func<List<T>, CancellationToken, Task> TaskFactory { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "The value must be greater than 0")]
        public int TaskPoolSize { get; set; } = 1000;

        [Required]
        public CollectionType CollectionType { get; set; } = CollectionType.Queue;

        public SendDelayOptions? SendDelayOptions { get; set; }

        [Required]
        public RetryOptions<T> RetryOptions { get; set; } = new RetryOptions<T>();
    }
}