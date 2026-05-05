using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Infrastructure.Context;
using Rethink.Services.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Utils
{
    [ExcludeFromCodeCoverage]
    public class DbHelper<T> : IDbHelper<T> where T : DbContext
    {
        private readonly BaseDbContext<T> _dbContext;

        public DbHelper(BaseDbContext<T> dbContext)
        {
            _dbContext = dbContext;
        }

        //public DataTable GetArrayParameter(ICollection<int> list)
        //{
        //    DataTable tvp = new DataTable();
        //    tvp.Columns.Add(new DataColumn("ID", typeof(int)));
        //    foreach (var id in list)
        //    {
        //        tvp.Rows.Add(id);
        //    }

        //    return tvp;
        //}


        //public async Task<List<T>> GetTableFunctionResult<T>(string command, List<string> parameteres) where T : class
        //{
        //    var p = string.Join(", ", parameteres);
        //    var commandText = $"select * from {command}({p})";
        //    return await _dbContext.SetEntity<T>().FromSql(commandText).ToListAsync();
        //}

        //public MultipleResultSetWrapper MultipleResultSet(string storedProcedure, List<SqlParameter> parameters = null, CommandType commandType = CommandType.StoredProcedure)
        //{
        //    return new MultipleResultSetWrapper(_dbContext, storedProcedure, parameters, commandType);
        //}

        //public T ExecuteScalar<T>(string commandText, List<SqlParameter> parameters = null,
        //    CommandType commandType = CommandType.StoredProcedure)
        //{
        //    // var results = new List<IEnumerable>();

        //    var connection = _dbContext.GetDbConnection();
        //    try
        //    {
        //        connection.Open();
        //        var command = connection.CreateCommand();
        //        command.CommandText = commandText;
        //        command.CommandType = commandType;
        //        if (parameters != null)
        //        {
        //            foreach (var sqlParameter in parameters)
        //            {
        //                command.Parameters.Add(sqlParameter);
        //            }
        //        }

        //        var result = command.ExecuteScalar();
        //        return (T)result;

        //    }
        //    finally
        //    {
        //        connection.Close();
        //    }
        //}

        //public int ExecuteScalar<T>(string commandText, T model, CommandType commandType = CommandType.StoredProcedure)
        //{
        //    // var results = new List<IEnumerable>();
        //    var parameters = GetParamsFromModel(model);

        //    var connection = _dbContext.GetDbConnection();
        //    try
        //    {
        //        connection.Open();
        //        var command = connection.CreateCommand();
        //        command.CommandText = commandText;
        //        command.CommandType = commandType;
        //        if (parameters != null)
        //        {
        //            foreach (var sqlParameter in parameters)
        //            {
        //                command.Parameters.Add(sqlParameter);
        //            }
        //        }

        //        var result = command.ExecuteScalar();
        //        return result != null ? (int)result : 0;

        //    }
        //    finally
        //    {
        //        connection.Close();
        //    }
        //}

        //public DbDataReader ExecuteReader(string commandText, List<SqlParameter> parameters = null,
        //    CommandType commandType = CommandType.StoredProcedure)
        //{
        //    // var results = new List<IEnumerable>();

        //    var connection = _dbContext.GetDbConnection();
        //    try
        //    {
        //        connection.Open();
        //        var command = connection.CreateCommand();
        //        command.CommandText = commandText;
        //        command.CommandType = commandType;
        //        if (parameters != null)
        //        {
        //            foreach (var sqlParameter in parameters)
        //            {
        //                command.Parameters.Add(sqlParameter);
        //            }
        //        }

        //        return command.ExecuteReader();
        //    }
        //    finally
        //    {
        //        connection.Close();
        //    }
        //}

        //private List<SqlParameter> GetParamsFromModel<T>(T model)
        //{
        //    var props = model.GetType().GetProperties().ToList();
        //    var accessor = ObjectAccessor.Create(model);
        //    var list = new List<SqlParameter>();
        //    var PropMap = new List<string>();
        //    props.ForEach(p =>
        //    {
        //        var t = p.PropertyType;
        //        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
        //        {
        //            t = Nullable.GetUnderlyingType(t);
        //        }
        //        if (!t.IsPrimitive
        //            && t != typeof(Decimal)
        //            && t != typeof(String)
        //            && t != typeof(DateTime))
        //        {
        //            return;
        //        }
        //        var parameter = p.Name.ToLower();
        //        if (!PropMap.Contains(parameter))
        //        {
        //            PropMap.Add(parameter);
        //            list.Add(new SqlParameter(parameter, accessor[p.Name]));
        //        }
        //    });

        //    return list;
        //}

        public List<T> ExecuteList<T>(string commandText, List<SqlParameter> parameters = null, CommandType commandType = CommandType.StoredProcedure) where T : class
        {
            var connection = _dbContext.GetDbConnection();
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = commandText;
                command.CommandType = commandType;
                if (parameters != null)
                {
                    foreach (var sqlParameter in parameters)
                    {
                        command.Parameters.Add(sqlParameter);
                    }
                }

                using (var reader = command.ExecuteReader())
                {
                    return reader.Translate<T>();
                }

            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<List<T>> ExecuteListAsync<T>(string commandText, List<SqlParameter> parameters = null, CommandType commandType = CommandType.StoredProcedure) where T : class
        {
            var connection = _dbContext.GetDbConnection();
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = commandText;
                command.CommandType = commandType;
                if (parameters != null)
                {
                    foreach (var sqlParameter in parameters)
                    {
                        command.Parameters.Add(sqlParameter);
                    }
                }

                await using var reader = await command.ExecuteReaderAsync();
                return reader.Translate<T>();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<DbDataReader> ExecuteReaderAsync(string commandText, List<SqlParameter> parameters = null, CommandType commandType = CommandType.StoredProcedure)
        {
            var connection = _dbContext.GetDbConnection();
            var command = connection.CreateCommand();

            command.CommandText = commandText;
            command.CommandType = commandType;

            if (parameters != null)
            {
                foreach (var param in parameters)
                    command.Parameters.Add(param);
            }

            await connection.OpenAsync();

            // Connection closes when reader is disposed
            return (SqlDataReader)await command.ExecuteReaderAsync(
                CommandBehavior.CloseConnection);
        }


        //public T? ReadNullableValue<T>(DbDataReader reader, string columnName) where T : struct
        //{
        //    try
        //    {
        //        var value = reader[columnName];
        //        return value == DBNull.Value ? null : (T?)value;
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception(String.Format("Stored procedure not returning column '{0}'", columnName), e);
        //    }
        //}

        //public string ReadString(DbDataReader reader, string columnName)
        //{
        //    var value = reader[columnName];
        //    return value == DBNull.Value ? null : (string)value;

        //}
    }
}
