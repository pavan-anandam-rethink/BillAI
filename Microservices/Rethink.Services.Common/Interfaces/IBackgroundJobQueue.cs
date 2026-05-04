using System.Threading;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Interfaces
{
    public interface IBackgroundJobQueue
    {
        ValueTask EnqueueAsync(object job);
        ValueTask<object> DequeueAsync(CancellationToken cancellationToken);
    }
}
