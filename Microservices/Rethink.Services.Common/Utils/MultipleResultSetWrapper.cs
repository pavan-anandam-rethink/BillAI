using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Infrastructure.Context;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Utils
{
    [ExcludeFromCodeCoverage]
    public class MultipleResultSetWrapper<T> where T : DbContext
    {
        private readonly BaseDbContext<T> _dbContext;
        private readonly string _storedProcedure;
        private readonly CommandType _commandType;
        private readonly List<Func<DbDataReader, IEnumerable>> _resultSets;
        private readonly List<SqlParameter> _params;

        internal MultipleResultSetWrapper(BaseDbContext<T> dbContext, string storedProcedure, List<SqlParameter> parameter = null,
            CommandType commandType = CommandType.StoredProcedure)
        {
            _dbContext = dbContext;
            _storedProcedure = storedProcedure;
            _params = parameter;
            _commandType = commandType;
            _resultSets = new List<Func<DbDataReader, IEnumerable>>();
        }

        public MultipleResultSetWrapper<T> With<TResult>() where TResult : class
        {
            _resultSets.Add(reader => reader.Translate<TResult>()
                .ToList());

            return this;
        }

        public List<IEnumerable> Execute()
        {
            var results = new List<IEnumerable>();

            var connection = _dbContext.GetDbConnection();
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = _storedProcedure;
                command.CommandType = _commandType;
                if (_params != null)
                {
                    foreach (var sqlParameter in _params)
                    {
                        command.Parameters.Add(sqlParameter);
                    }
                }



                using (var reader = command.ExecuteReader())
                {
                    foreach (var resultSet in _resultSets)
                    {
                        results.Add(resultSet(reader));
                        reader.NextResult();
                    }
                }

                return results;
            }

            finally
            {
                connection.Close();
            }
        }
        public async Task<List<IEnumerable>> ExecuteAsync()
        {
            var results = new List<IEnumerable>();

            var connection = _dbContext.GetDbConnection();
            try
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = _storedProcedure;
                command.CommandType = _commandType;
                if (_params != null)
                {
                    foreach (var sqlParameter in _params)
                    {
                        command.Parameters.Add(sqlParameter);
                    }
                }
                using (var reader = await command.ExecuteReaderAsync())
                {
                    foreach (var resultSet in _resultSets)
                    {
                        results.Add(resultSet(reader));
                        reader.NextResult();
                    }
                }

                return results;
            }

            finally
            {
                connection.Close();
            }
        }
    }
}
