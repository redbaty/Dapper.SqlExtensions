using System;
using System.Linq.Expressions;

namespace Dapper.SqlExtensions.Extensions
{
    public static class ObjectExtensions
    {
        public static string GetInsertSql<T>(this T model, DapperObjectOptions options = null)
        {
            var dapperObject = new DapperObject<T>(options);
            return dapperObject.Insert(model);
        }

        public static string GetUpdateSql<T, TProperty>(this T model, Expression<Func<T, TProperty>> propertyLambda,
            T oldModel = default(T), DapperObjectOptions options = null)
        {
            return new DapperObject<T>(options).Update(model, oldModel, propertyLambda);
        }
    }
}