using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Interfaces
{
    public interface IMessageBus
    {
        Task SendAsync<T>(T data, string entityPath);
        Task SendBatchAsync<T>(string entityName, List<T> batch);
        Task SendBatchAsync<T>(string entityName, List<T> batch, int chunkSize);
    }
}
