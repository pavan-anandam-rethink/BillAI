using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Interfaces
{
    public interface IDbHelper<in TIn> where TIn : DbContext
    {
        //DataTable GetArrayParameter(ICollection<int> list);
        //MultipleResultSetWrapper MultipleResultSet(string storedProcedure, List<SqlParameter> parameters = null, CommandType commandType = CommandType.StoredProcedure);
        //T ExecuteScalar<T>(string commandText, List<SqlParameter> parameters = null, CommandType commandType = CommandType.StoredProcedure);
        //int ExecuteScalar<T>(string commandText, T model, CommandType commandType = CommandType.StoredProcedure);
        //DbDataReader ExecuteReader(string commandText, List<SqlParameter> parameters = null, CommandType commandType = CommandType.StoredProcedure);
        List<T> ExecuteList<T>(string commandText, List<SqlParameter> parameters = null, CommandType commandType = CommandType.StoredProcedure) where T : class;
        Task<List<T>> ExecuteListAsync<T>(string commandText, List<SqlParameter> parameters = null, CommandType commandType = CommandType.StoredProcedure) where T : class;
        Task<DbDataReader> ExecuteReaderAsync(string commandText, List<SqlParameter> parameters = null, CommandType commandType = CommandType.StoredProcedure);
        //T? ReadNullableValue<T>(DbDataReader reader, string columnName) where T : struct;
        //string ReadString(DbDataReader reader, string columnName);

        //Task<List<T>> GetTableFunctionResult<T>(string command, List<string> parameteres) where T : class;
    }
}
