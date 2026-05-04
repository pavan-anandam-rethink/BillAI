using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Factories
{
    [ExcludeFromCodeCoverage]
    public class DapperFactory
    {
        private string ConnectionString { get; set; }

        public string GetConnectionString()
        {
            return ConnectionString;
        }

        public IDbConnection GetConnection()
        {
            using var connection = new SqlConnection(ConnectionString);
            return connection;
        }
    }
}