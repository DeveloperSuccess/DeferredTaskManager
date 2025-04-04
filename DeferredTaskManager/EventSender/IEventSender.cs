using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DTM
{
    public interface IEventSender<T>
    {
        IEnumerable<Task> CreateBackgroundTasks(CancellationToken cancellationToken);
    }
}
