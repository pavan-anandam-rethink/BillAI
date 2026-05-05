using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Infrastructure.Context;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Infrastructure.Repository
{
    [ExcludeFromCodeCoverage]
    public class Repository<TContext, T> : IRepository<TContext, T>
        where TContext : DbContext
        where T : BasePersistEntity
    {
        private readonly BaseDbContext<TContext> _dbContext;
        private readonly DbSet<T> _dbSet;

        private readonly int _maxQueryContainsSize;

        public Repository(BaseDbContext<TContext> dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _dbSet = _dbContext.SetEntity<T>();
            _maxQueryContainsSize = configuration.GetValue<int>("QueryContainsLimit");
        }

        public void Add(T entity)
        {
            _dbSet.Add(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _dbContext.SaveChangesAsync();
        }

        public void UpdateRange(List<T> entity)
        {
            _dbSet.UpdateRange(entity);
        }
        public Task<List<T>> GetAllAsync()
        {
            return _dbSet.ToListAsync();
        }

        public T Refresh(int id, T entity)
        {
            var type = entity.GetType();
            T old = _dbSet.Find(id);
            if (old != null)
            {
                // _dataContext.Entry(old).CurrentValues.SetValues(entity);
                //replace on db updated
                foreach (PropertyInfo propInfo in type.GetProperties())
                {
                    if (propInfo.CanRead && (propInfo.PropertyType.IsPrimitive || !propInfo.GetGetMethod().IsVirtual))
                    {
                        object newVal = propInfo.GetValue(entity, null);
                        propInfo.SetValue(old, newVal);
                    }
                }

                return old;
            }

            return null;
        }

        public object FirstOrDefault(Func<object, bool> p)
        {
            throw new NotImplementedException();
        }


        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public void Delete(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        public void Delete(Expression<Func<T, bool>> where)
        {
            var objects = _dbSet.Where(where).AsEnumerable();
            foreach (var obj in objects)
            {
                Delete(obj);
                //deletedItems++;
            }
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public async Task<T> AddAndGetAsync(T entity)
        {
            try
            {
                var result = await _dbSet.AddAsync(entity);
                await CommitAsync();

                return result.Entity;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<T> GetMany(Expression<Func<T, bool>> where = null, Func<IQueryable<T>,
            IOrderedQueryable<T>> orderBy = null, string includeProperties = null, int skip = 0, int take = 0)
        {
            IQueryable<T> query = _dbSet.AsQueryable();

            if (where != null)
            {
                query = query.Where(where);
            }

            if (!String.IsNullOrEmpty(includeProperties))
            {
                var includes = includeProperties.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                query = includes.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));
            }

            if (orderBy != null)
            {
                query = orderBy(query).AsQueryable();

                if (skip != 0)
                {
                    query = query.Skip(skip);
                }

                if (take != 0)
                {
                    query = query.Take(take);
                }
            }
            return query.ToList();
        }

        public async Task<IQueryable<T>> GetAllAsync(Expression<Func<T, bool>> predicate = null,
            IEnumerable<string> includedProps = null)
        {
            var result = this._dbSet.AsQueryable();

            if (includedProps != null)
            {
                foreach (var prop in includedProps)
                {
                    result = result.Include(prop);
                }
            }

            return
                predicate == null
                    ? await Task.Run(() => result)
                    : await Task.Run(() => result.Where(predicate));
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public IQueryable<T> GetByRawSql(string sqlString, Type entityType)
        {
            var tableName = _dbContext.GetTableName(_dbContext, entityType);
            var sqlCommand = sqlString.Replace("TableName", tableName);
            var result = _dbSet.FromSqlRaw(sqlCommand);

            return result;
        }

        public virtual void Commit()
        {
            _dbContext.Commit();
        }

        public virtual async Task CommitAsync()
        {
            try
            {
                await _dbContext.CommitAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public virtual async Task SaveChangesAsync()
        {
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public IQueryable<T> Query()
        {
            return _dbSet.AsQueryable();
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        public async Task BulkReadContainsAsync(List<T> entities)
        {
            if (entities.Count == 0) return;

            if (entities.Count > _maxQueryContainsSize)
            {
                var config = new BulkConfig { UpdateByProperties = new List<string> { nameof(BasePersistEntity.Id) } };
                await _dbContext.BulkReadAsync(entities, config);
            }
            else
            {
                var entitiesIds = entities.Select(x => x.Id).ToList();
                var existingEntities = await _dbSet.AsQueryable().AsNoTracking().Where(x => entitiesIds.Contains(x.Id)).ToListAsync();

                entities.Clear();
                entities.AddRange(existingEntities);
            }
        }

        public EntityEntry<T> Entry(T entity)
        {
            return _dbContext.Entry(entity);
        }


    }
}