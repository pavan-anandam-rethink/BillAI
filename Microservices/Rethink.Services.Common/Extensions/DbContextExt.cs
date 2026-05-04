using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Base;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Rethink.Services.Common.Extensions
{
    public static class DbContextExt
    {
        public static void AddOrUpdateById<TEntity>(this DbSet<TEntity> dbSet, TEntity data)
            where TEntity : BasePersistEntity
        {
            var t = typeof(TEntity);
            PropertyInfo keyField = null;
            foreach (var propt in t.GetProperties())
            {
                var keyAttr = propt.GetCustomAttribute<KeyAttribute>();
                if (keyAttr != null)
                {
                    keyField = propt;
                    break; // assume no composite keys
                }
            }
            if (keyField == null)
            {
                throw new Exception($"{t.FullName} does not have a KeyAttribute field. Unable to exec AddOrUpdate call.");
            }
            var keyVal = keyField.GetValue(data);
            var key = Convert.ToInt32(keyVal);
            var exists = dbSet.AsNoTracking().Any(s => s.Id == key);


            if (exists)
            {
                dbSet.Update(data);

            }
            else
            {
                dbSet.Add(data);
            }
        }

    }
}
