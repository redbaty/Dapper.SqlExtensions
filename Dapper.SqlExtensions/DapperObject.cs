using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dapper.SqlExtensions.Exceptions;
using Dapper.SqlExtensions.Extensions;
using Dapper.SqlExtensions.Interfaces;

namespace Dapper.SqlExtensions
{
    public class DapperObject<T> : IDapperObject<T>
    {
        private DapperObjectOptions Options { get; }

        public string Select(Expression<Func<T, bool>> propertyLambda,
            bool ignoreAttributes = false)
        {
            EnsureTable();

            var propertyValueExpressions = propertyLambda.GetPropertyAndValuePairFromBinaryExpression();
            var whereSql = " WHERE " + propertyValueExpressions.Select(i =>
                                   $"{Options.ColumnResolver.Invoke(i.Key)}={GetSafeSqlValue(i.Key, i.Value)}")
                               .Aggregate((x1, y1) => $"{x1} AND {y1}");
            return Select(ignoreAttributes) + whereSql;
        }

        public string Insert(T instance)
        {
            EnsureTable();

            if (Options.Properties.Count <= 0) throw new NoPropertiesProvided();

            return
                $"INSERT INTO {Options.Table} ({Options.Properties.Select(i => Options.ColumnResolver.Invoke(i)).Aggregate((x, y) => $"{x}, {y}")}) " +
                $"VALUES ({Options.Properties.Select(i => GetSafeSqlValue(i, i.GetValue(instance))).Aggregate((x, y) => $"{x}, {y}")})";
        }

        public string Select(bool ignoreAttributes = false)
        {
            EnsureTable();

            if (Options.Properties.All(i => i.GetCustomAttribute<UseOnSqlExtensionsSelect>() == null) ||
                ignoreAttributes)
                return $"SELECT * FROM {Options.Table}";

            return
                $"SELECT {Options.Properties.Where(i => i.GetCustomAttribute<UseOnSqlExtensionsSelect>() != null).Select(i => Options.ColumnResolver.Invoke(i)).Aggregate((x, y) => $"{x}, {y}")} FROM {Options.Table}";
        }

        public string Update<TProperty>(
            T source,
            Expression<Func<T, TProperty>> propertyLambda)
        {
            return Update(source, default(T), propertyLambda);
        }

        public string Update<TProperty>(
            T source,
            T oldInstance,
            Expression<Func<T, TProperty>> propertiesLambda)
        {
            return Update(source, propertiesLambda.GetPropertiesFromExpression().ToList(), oldInstance);
        }

        public string Update(T instance, IList<PropertyInfo> keyProperty, T oldInstance = default(T))
        {
            EnsureTable();

            if (Equals(oldInstance, default(T)))
                return GetUpdateSql(instance, keyProperty, Options.Properties.Where(i => keyProperty.All(o => o != i)));

            var getDifferentProperties = Options.Properties.Where(i => keyProperty.All(o => o != i))
                .Select(i => new {newValue = i.GetValue(instance), oldValue = i.GetValue(oldInstance), property = i})
                .Where(i => !Equals(i.newValue, i.oldValue)).Select(i => i.property).ToList();

            if (getDifferentProperties.Count <= 0) throw new NoDifferenceFoundOnObjects();

            return GetUpdateSql(instance, keyProperty, getDifferentProperties);
        }

        public string Delete<TProperty>(T instance, Expression<Func<T, TProperty>> propertiesLambda)
        {
            return
                $"DELETE FROM {Options.Table} WHERE {GetWhereValuePair(instance, propertiesLambda.GetPropertiesFromExpression())}";
        }

        public DapperObject(DapperObjectOptions options = null)
        {
            Options = options ?? new DapperObjectOptions();
            Options = Options.GetFinal(typeof(T));
        }

        private void EnsureTable()
        {
            if (string.IsNullOrEmpty(Options.Table)) throw new NoTableProvided();
        }

        private string GetUpdateSql(T instance, IEnumerable<PropertyInfo> keyProperty,
            IEnumerable<PropertyInfo> properties)
        {
            return
                $"UPDATE {Options.Table} SET {GetColumnValuePair(instance, properties)} WHERE {GetWhereValuePair(instance, keyProperty)}";
        }

        private string GetWhereValuePair(T instance, IEnumerable<PropertyInfo> properties)
        {
            return properties.Select(property =>
                    $"{Options.ColumnResolver.Invoke(property)}={GetSafeSqlValue(property, property.GetValue(instance))}")
                .Aggregate((x, y) => $"{x} AND {y}");
        }

        private string GetColumnValuePair(T instance, IEnumerable<PropertyInfo> properties)
        {
            return properties.Select(property =>
                    $"{Options.ColumnResolver.Invoke(property)}={GetSafeSqlValue(property, property.GetValue(instance))}")
                .Aggregate((x, y) => $"{x}, {y}");
        }

        private static object GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static string GetSafeSqlValue(PropertyInfo propertyInfo, object value)
        {
            if (propertyInfo.PropertyType.GetRealType() != typeof(string) && value == null &&
                GetDefault(propertyInfo.PropertyType.GetRealType()) == null)
                return "NULL";

            var returnValue = value?.ToString();

            if (value is DateTime dateTime) returnValue = dateTime.ToString("yyyy-MM-dd HH:mm:ss");

            if (value is double d) returnValue = d.ToString(CultureInfo.CurrentCulture).Replace(",", ".");


            if (propertyInfo.GetCustomAttribute<StringLengthAttribute>() is StringLengthAttribute stringLengthAttribute)
                returnValue = new string(value?.ToString().Take(stringLengthAttribute.MaximumLength).ToArray())
                    .AddQuotes();

            if (propertyInfo.PropertyType.GetRealType().IsEnum && value != null) return ((int) value).ToString();

            return returnValue.AddQuotes(value?.GetType());
        }
    }
}