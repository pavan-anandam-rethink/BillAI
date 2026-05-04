using Dapper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Infrastructure.Repository
{
    [ExcludeFromCodeCoverage]
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly string Conn;
        protected readonly string TableName;

        protected GenericRepository(string conn, string tableName)
        {
            Conn = conn;
            TableName = tableName;

            //DbSet = _dataContext.SetEntity<T>();
        }

        private SqlConnection SqlConnection()
        {
            return new SqlConnection(Conn);
        }

        protected IDbConnection CreateConnection()
        {
            var conn = SqlConnection();
            conn.Open();
            return conn;
        }

        private IEnumerable<PropertyInfo> GetProperties => typeof(T).GetProperties();

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            using var connection = CreateConnection();
            return await connection.QueryAsync<T>($"SELECT * FROM {TableName}");
        }

        public async Task DeleteRowAsync(int id)
        {
            using var connection = CreateConnection();
            await connection.ExecuteAsync($"DELETE FROM {TableName} WHERE Id=@Id", new { Id = id });
        }

        public async Task<T> GetAsync(int id)
        {
            using var connection = CreateConnection();
            var result = await connection.QuerySingleOrDefaultAsync<T>($"SELECT * FROM {TableName} WHERE Id=@Id", new { Id = id });
            if (result == null)
                throw new KeyNotFoundException($"{TableName} with id [{id}] could not be found.");

            return result;
        }

        public async Task<int> SaveRangeAsync(IEnumerable<T> list)
        {
            var query = GenerateInsertQuery();
            using var connection = CreateConnection();
            var inserted = await connection.ExecuteAsync(query, list);

            return inserted;
        }

        private static List<string> GenerateListOfProperties(IEnumerable<PropertyInfo> listOfProperties)
        {
            return (from prop in listOfProperties
                    let attributes = prop.GetCustomAttributes(typeof(DescriptionAttribute), false)
                    where attributes.Length <= 0 || (attributes[0] as DescriptionAttribute)?.Description != "ignore"
                    select prop.Name).ToList();
        }

        public async Task InsertAsync(T t)
        {
            var insertQuery = GenerateInsertQuery();

            using var connection = CreateConnection();
            await connection.ExecuteAsync(insertQuery, t);
        }

        private string GenerateInsertQuery()
        {
            var insertQuery = new StringBuilder($"INSERT INTO {TableName} ");

            insertQuery.Append("(");

            var properties = GenerateListOfProperties(GetProperties);
            properties.ForEach(prop => { insertQuery.Append($"[{prop}],"); });

            insertQuery
                .Remove(insertQuery.Length - 1, 1)
                .Append(") VALUES (");

            properties.ForEach(prop => { insertQuery.Append($"@{prop},"); });

            insertQuery
                .Remove(insertQuery.Length - 1, 1)
                .Append(")");

            return insertQuery.ToString();
        }

        public async Task UpdateAsync(T t)
        {
            var updateQuery = GenerateUpdateQuery();

            using var connection = CreateConnection();
            await connection.ExecuteAsync(updateQuery, t);
        }

        private string GenerateUpdateQuery()
        {
            var updateQuery = new StringBuilder($"UPDATE {TableName} SET ");
            var properties = GenerateListOfProperties(GetProperties);

            properties.ForEach(property =>
            {
                if (!property.Equals("Id"))
                {
                    updateQuery.Append($"{property}=@{property},");
                }
            });

            updateQuery.Remove(updateQuery.Length - 1, 1);
            updateQuery.Append(" WHERE Id=@Id");

            return updateQuery.ToString();
        }

        public IQueryable<T> Query()
        {
            //return DbSet.AsQueryable();
            throw new NotImplementedException();
        }
    }

}