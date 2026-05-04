using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Utils
{
    [ExcludeFromCodeCoverage]
    public class AdoHelper : IDisposable
    {
        // Internal members
        protected string _connString = null;
        protected SqlConnection _conn = null;
        protected SqlTransaction _trans = null;
        protected bool _disposed = false;

        /// <summary>
        /// Sets or returns the connection string use by all instances of this class.
        /// </summary>
        public static string ConnectionString { get; set; }

        /// <summary>
        /// Returns the current SqlTransaction object or null if no transaction
        /// is in effect.
        /// </summary>
        public SqlTransaction Transaction { get { return _trans; } }

        /// <summary>
        /// Constructor using global connection string.
        /// </summary>
        public AdoHelper()
        {
            _connString = ConnectionString;
            Connect();
        }

        /// <summary>
        /// Constructure using connection string override
        /// </summary>
        /// <param name="connString">Connection string for this instance</param>
        public AdoHelper(string connString)
        {
            _connString = connString;
            Connect();
        }

        // Creates a SqlConnection using the current connection string
        protected void Connect()
        {
            _conn = new SqlConnection(_connString);
            _conn.Open();

        }

        /// <summary>
        /// Constructs a SqlCommand with the given parameters. This method is normally called
        /// from the other methods and not called directly. But here it is if you need access
        /// to it.
        /// </summary>
        /// <param name="qry">SQL query or stored procedure name</param>
        /// <param name="type">Type of SQL command</param>
        /// <param name="args">Query arguments. Arguments should be in pairs where one is the
        /// name of the parameter and the second is the value. The very last argument can
        /// optionally be a SqlParameter object for specifying a custom argument type</param>
        /// <returns></returns>
        public SqlCommand CreateCommand(string qry, CommandType type, params object[] args)
        {
            SqlCommand cmd = new SqlCommand(qry, _conn);

            // update command timeout to match connection timeout if it is set to be more
            // ConnectionTimeout - default 15
            // CommandTimeout - default 30
            if (_conn.ConnectionTimeout > cmd.CommandTimeout)
            {
                cmd.CommandTimeout = _conn.ConnectionTimeout;
            }

            // Associate with current transaction, if any
            if (_trans != null)
                cmd.Transaction = _trans;

            // Set command type
            cmd.CommandType = type;

            // Construct SQL parameters
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is string && i < (args.Length - 1))
                {
                    SqlParameter parm = new SqlParameter();
                    parm.ParameterName = (string)args[i];
                    parm.Value = args[++i];
                    cmd.Parameters.Add(parm);
                }
                else if (args[i] is SqlParameter)
                {
                    var parameter = (SqlParameter)args[i];

                    if (parameter.Value == null)
                        parameter.Value = DBNull.Value;
                    cmd.Parameters.Add((SqlParameter)args[i]);
                }
                else throw new ArgumentException("Invalid number or type of arguments supplied");
            }
            return cmd;
        }

        #region Data Retrieval Helpers
        public T? ReadNullableValue<T>(SqlDataReader reader, string columnName) where T : struct
        {
            try
            {
                int FieldIndex;
                try { FieldIndex = reader.GetOrdinal(columnName); }
                catch { return default(T); }

                if (reader.IsDBNull(FieldIndex))
                {
                    return default(T);
                }
                else
                {
                    var value = reader[columnName];
                    return value == DBNull.Value ? null : (T?)value;
                }
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("Stored procedure not returning column '{0}'", columnName), e);
            }
        }

        public string ReadString(SqlDataReader reader, string columnName)
        {
            int FieldIndex;
            try { FieldIndex = reader.GetOrdinal(columnName); }
            catch { return ""; }

            if (reader.IsDBNull(FieldIndex))
            {
                return "";
            }
            else
            {
                var value = reader[columnName];
                return value == DBNull.Value ? null : (string)value;
            }

        }
        #endregion

        #region Exec Members

        /// <summary>
        /// Executes a query that returns no results
        /// </summary>
        /// <param name="qry">Query text</param>
        /// <param name="args">Any number of parameter name/value pairs and/or SQLParameter arguments</param>
        /// <returns>The number of rows affected</returns>
        public int ExecNonQuery(string qry, params object[] args)
        {
            using (SqlCommand cmd = CreateCommand(qry, CommandType.Text, args))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Executes a stored procedure that returns no results
        /// </summary>
        /// <param name="proc">Name of stored proceduret</param>
        /// <param name="args">Any number of parameter name/value pairs and/or SQLParameter arguments</param>
        /// <returns>The number of rows affected</returns>
        public int ExecNonQueryProc(string proc, params object[] args)
        {
            using (SqlCommand cmd = CreateCommand(proc, CommandType.StoredProcedure, args))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Executes a stored procedure that returns no results async
        /// </summary>
        /// <param name="proc">Name of stored proceduret</param>
        /// <param name="args">Any number of parameter name/value pairs and/or SQLParameter arguments</param>
        /// <returns>The number of rows affected</returns>
        public async Task<int> ExecNonQueryProcAsync(string proc, params object[] args)
        {
            using (SqlCommand cmd = CreateCommand(proc, CommandType.StoredProcedure, args))
            {
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Executes a query that returns a single value
        /// </summary>
        /// <param name="qry">Query text</param>
        /// <param name="args">Any number of parameter name/value pairs and/or SQLParameter arguments</param>
        /// <returns>Value of first column and first row of the results</returns>
        public object ExecScalar(string qry, params object[] args)
        {
            using (SqlCommand cmd = CreateCommand(qry, CommandType.Text, args))
            {
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Executes a query that returns a single value
        /// </summary>
        /// <param name="proc">Name of stored proceduret</param>
        /// <param name="args">Any number of parameter name/value pairs and/or SQLParameter arguments</param>
        /// <returns>Value of first column and first row of the results</returns>
        public object ExecScalarProc(string qry, params object[] args)
        {
            using (SqlCommand cmd = CreateCommand(qry, CommandType.StoredProcedure, args))
            {
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Executes a query and returns the results as a SqlDataReader
        /// </summary>
        /// <param name="qry">Query text</param>
        /// <param name="args">Any number of parameter name/value pairs and/or SQLParameter arguments</param>
        /// <returns>Results as a SqlDataReader</returns>
        public SqlDataReader ExecDataReader(string qry, params object[] args)
        {
            using (SqlCommand cmd = CreateCommand(qry, CommandType.Text, args))
            {
                return cmd.ExecuteReader();
            }
        }

        /// <summary>
        /// Executes a stored procedure and returns the results as a SqlDataReader
        /// </summary>
        /// <param name="proc">Name of stored proceduret</param>
        /// <param name="args">Any number of parameter name/value pairs and/or SQLParameter arguments</param>
        /// <returns>Results as a SqlDataReader</returns>
        public SqlDataReader ExecDataReaderProc(string qry, params object[] args)
        {
            using (SqlCommand cmd = CreateCommand(qry, CommandType.StoredProcedure, args))
            {
                return cmd.ExecuteReader();
            }
        }

        /// <summary>
        /// Executes a stored procedure and returns the results as a SqlDataReader
        /// </summary>
        /// <param name="proc">Name of stored proceduret</param>
        /// <param name="args">Any number of parameter name/value pairs and/or SQLParameter arguments</param>
        /// <returns>Results as a SqlDataReader</returns>
        public async Task<SqlDataReader> ExecDataReaderProcAsync(string qry, params object[] args)
        {
            using (SqlCommand cmd = CreateCommand(qry, CommandType.StoredProcedure, args))
            {
                return await cmd.ExecuteReaderAsync();
            }
        }
        public async Task<SqlDataReader> ExecDataReaderProcLongTaskAsync(string qry, params object[] args)
        {
            using (SqlCommand cmd = CreateCommand(qry, CommandType.StoredProcedure, args))
            {
                cmd.CommandTimeout = 1440;
                return await cmd.ExecuteReaderAsync();
            }
        }

        /// <summary>
        /// Executes a query and returns the results as a DataSet
        /// </summary>
        /// <param name="qry">Query text</param>
        /// <param name="args">Any number of parameter name/value pairs and/or SQLParameter arguments</param>
        /// <returns>Results as a DataSet</returns>
        public DataSet ExecDataSet(string qry, params object[] args)
        {
            using (SqlCommand cmd = CreateCommand(qry, CommandType.Text, args))
            {
                SqlDataAdapter adapt = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                adapt.Fill(ds);
                return ds;
            }
        }

        /// <summary>
        /// Executes a stored procedure and returns the results as a Data Set
        /// </summary>
        /// <param name="proc">Name of stored proceduret</param>
        /// <param name="args">Any number of parameter name/value pairs and/or SQLParameter arguments</param>
        /// <returns>Results as a DataSet</returns>
        public DataSet ExecDataSetProc(string qry, params object[] args)
        {
            using (SqlCommand cmd = CreateCommand(qry, CommandType.StoredProcedure, args))
            {
                SqlDataAdapter adapt = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                adapt.Fill(ds);
                return ds;
            }
        }

        public DataTable ExecDataTableProc(string qry, params object[] args)
        {
            using (SqlCommand cmd = CreateCommand(qry, CommandType.StoredProcedure, args))
            {
                SqlDataAdapter adapt = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapt.Fill(dt);
                return dt;
            }
        }

        public void ExcecuteObjectToNonQuery(string Procedure, Object Obj, string Properties = "")
        {
            var sqlCommand = CreateStoredProcedureCommand(Procedure, Obj, Properties);
            sqlCommand.ExecuteNonQuery();
        }
        private SqlCommand CreateStoredProcedureCommand<T>(string Procedure, T Obj, string Properties = "")
        {
            var Parameters = StoredProcedureParameters[Procedure.ToLower()];
            var PropMap = new Dictionary<string, string>();
            if (Properties != string.Empty)
            {
                Properties.Split(',').ToList().ForEach(p =>
                {
                    var s = p.Split(':');
                    PropMap.Add(s[0], s[1]);
                });
            }
            var Props = Obj.GetType().GetProperties().ToList();
            var Accessor = FastMember.ObjectAccessor.Create(Obj);

            var sqlCommand = new System.Data.SqlClient.SqlCommand(Procedure, _conn);
            sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;

            Props.ForEach(p =>
            {
                var t = p.PropertyType;
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    t = Nullable.GetUnderlyingType(t);
                }
                if (!t.IsPrimitive
                    && t != typeof(Decimal)
                    && t != typeof(String)
                    && t != typeof(DateTime))
                {
                    return;
                }
                var parameter = p.Name;
                if (PropMap.ContainsKey(p.Name))
                {
                    parameter = PropMap[p.Name];
                }
                if (!Parameters.Contains(parameter.ToLower()))
                {
                    return;
                }
                sqlCommand.Parameters.AddWithValue(parameter, Accessor[p.Name]);
            });

            return sqlCommand;
        }

        private static Dictionary<string, List<String>> _StoredProcedureParameters { get; set; }
        public static Dictionary<string, List<String>> StoredProcedureParameters
        {
            get
            {
                if (_StoredProcedureParameters != null)
                {
                    return _StoredProcedureParameters;
                }
                _StoredProcedureParameters = new Dictionary<string, List<string>>();
                lock (_StoredProcedureParameters)
                {
                    var Conn =
                        new SqlConnection(ConfigurationManager.ConnectionStrings["RethinkAutism"].ConnectionString);
                    var sqlCommand = new SqlCommand("select Specific_Name, Parameter_Name from information_schema.parameters", Conn);
                    var da = new SqlDataAdapter(sqlCommand);
                    var dt = new System.Data.DataTable();
                    da.Fill(dt);
                    _StoredProcedureParameters = new Dictionary<string, List<string>>();
                    foreach (System.Data.DataRow dr in dt.Rows)
                    {
                        var procedure = dr["Specific_Name"].ToString().ToLower();
                        var parameter = dr["Parameter_Name"].ToString().Replace("@", "").ToLower();
                        if (!_StoredProcedureParameters.ContainsKey(procedure))
                        {
                            _StoredProcedureParameters.Add(procedure, new List<string>());
                        }
                        var Parameters = _StoredProcedureParameters[procedure];
                        Parameters.Add(parameter);
                    }
                }
                return _StoredProcedureParameters;
            }
        }


        #endregion

        #region Transaction Members

        /// <summary>
        /// Begins a transaction
        /// </summary>
        /// <returns>The new SqlTransaction object</returns>
        public SqlTransaction BeginTransaction()
        {
            Rollback();
            _trans = _conn.BeginTransaction();
            return Transaction;
        }

        /// <summary>
        /// Commits any transaction in effect.
        /// </summary>
        public void Commit()
        {
            if (_trans != null)
            {
                _trans.Commit();
                _trans = null;
            }
        }

        /// <summary>
        /// Rolls back any transaction in effect.
        /// </summary>
        public void Rollback()
        {
            if (_trans != null)
            {
                _trans.Rollback();
                _trans = null;
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // Need to dispose managed resources if being called manually
                if (disposing)
                {
                    if (_conn != null)
                    {
                        Rollback();
                        _conn.Dispose();
                        _conn = null;
                    }
                }
                _disposed = true;
            }
        }

        #endregion
    }
}