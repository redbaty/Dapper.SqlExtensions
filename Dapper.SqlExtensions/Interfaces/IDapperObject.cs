using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Dapper.SqlExtensions.Interfaces
{
    public interface IDapperObject<T>
    {
        string Select(Expression<Func<T, bool>> propertyLambda,
            bool ignoreAttributes = false);

        string Insert(T instance);
        string Select(bool ignoreAttributes = false);

        string Update<TProperty>(
            T source,
            Expression<Func<T, TProperty>> propertyLambda);

        string Update<TProperty>(
            T source,
            T oldInstance,
            Expression<Func<T, TProperty>> propertiesLambda);

        string Update(T instance, IList<PropertyInfo> keyProperty, T oldInstance = default(T));
        string Delete<TProperty>(T instance, Expression<Func<T, TProperty>> propertiesLambda);
    }
}