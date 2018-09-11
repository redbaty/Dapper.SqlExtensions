using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Dapper.SqlExtensions
{
    public static class DbsetExtensions
    {
        public static IEnumerable<T> UseDapper<T>(this DbSet<T> dbSet) where T : class
        {
            var context = dbSet.GetContext();
            var sqlObject = new SqlObject(Activator.CreateInstance<T>(), context);
            return context.Database.GetDbConnection()
                .Query<T>(sqlObject
                    .GetSelect());
        }

        public static DbContext GetContext<TEntity>(this DbSet<TEntity> dbSet)
            where TEntity : class
        {
            return dbSet
                .GetType()
                .GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(dbSet) as DbContext;
        }
    }
}