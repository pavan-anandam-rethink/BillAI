using System.Data.Common;

namespace Rethink.Services.Common.Infrastructure.Connection
{
    public interface IDbConnectionFactory
    {
        string ConnectionString { get; }
        DbConnection CreateConnection();
    }
}