using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Connection
{
    [ExcludeFromCodeCoverage]
    public class DbConnectionFactory : IDbConnectionFactory
    {
        public DbConnectionFactory(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; }

        public DbConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString);
        }
    }
}