using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Rethink.Services.Common.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Infrastructure.Repository
{
    public interface IRepository<TContext, T>
        where T : BasePersistEntity
        where TContext : DbContext
    {
        void Add(T entity);
        void Update(T entity);
        Task UpdateAsync(T entity);
        void UpdateRange(List<T> entity);
        void Delete(T entity);
        void Delete(IEnumerable<T> entities);
        void Delete(Expression<Func<T, bool>> where);
        Task AddAsync(T entity);
        Task<T> AddAndGetAsync(T entity);
        List<T> GetMany(Expression<Func<T, bool>> where = null, Func<IQueryable<T>,
            IOrderedQueryable<T>> orderBy = null, string includeProperties = null, int skip = 0, int take = 0);

        Task<IQueryable<T>> GetAllAsync(Expression<Func<T, bool>> predicate = null,
            IEnumerable<string> includedProps = null);

        Task<T> GetByIdAsync(int id);
        IQueryable<T> GetByRawSql(string sql, Type entityType);
        void Commit();
        IQueryable<T> Query();
        Task CommitAsync();
        Task SaveChangesAsync();
        T Refresh(int id, T entity);
        object FirstOrDefault(Func<object, bool> p);
        Task AddRangeAsync(IEnumerable<T> entities);
        void RemoveRange(IEnumerable<T> entities);
        Task BulkReadContainsAsync(List<T> entities);

        EntityEntry<T> Entry(T entity);
    }
}