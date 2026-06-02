using Microsoft.Data.SqlClient;

namespace BillingService.Persistence.Legacy;

public interface IBillingSqlConnectionFactory
{
    SqlConnection CreateOpenConnection();
    SqlConnection CreateConnection();
}

public sealed class BillingSqlConnectionFactory(string connectionString) : IBillingSqlConnectionFactory
{
    public SqlConnection CreateOpenConnection()
    {
        var connection = new SqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    public SqlConnection CreateConnection() => new SqlConnection(connectionString);
}
