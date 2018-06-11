using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Dapper.SqlExtensions
{
    internal class SqlObject<T> : SqlObject
    {
        public new T Object { get; }

        public SqlObject() : base(typeof(T))
        {
            Object = Activator.CreateInstance<T>();
        }

        public SqlObject(T instance) : this()
        {
            Object = instance;
        }

        public SqlObject(IDbContextDependencies context) : this()
        {
            Object = Activator.CreateInstance<T>();
            Context = context;
            InitializeByType(
                context.Model.FindEntityType(Object.GetType()).GetAnnotation("Relational:TableName").Value as string);
        }

        public SqlObject(T instance, IDbContextDependencies context) : this()
        {
            Object = instance;
            Context = context;
            InitializeByType(
                context.Model.FindEntityType(Object.GetType()).GetAnnotation("Relational:TableName").Value as string);
        }

        public string GetUpdate(T newInstance)
        {
            return GetUpdate(newInstance, GetKeyPropertyInfo());
        }

        public string GetUpdate<TProperty>(T newInstance, [NotNull]
            Expression<Func<T, TProperty>> propertyLambda)
        {
            return GetUpdate(newInstance, ValidatePropertyExpression(propertyLambda));
        }

        public string GetUpdate(T newInstance, PropertyInfo key)
        {
            return GetUpdate(key, GetProperties(Type)
                .Where(i => !Equals(i.GetValue(Object), i.GetValue(newInstance)))
                .Where(i => !i.PropertyType.IsGenericType), newInstance);
        }

        public string GetUpdate(IEnumerable<PropertyInfo> properties, string where = null)
        {
            return GetUpdate(where != null ? null : GetKeyPropertyInfo(), properties, Object, where);
        }

        public string GetUpdate<TProperty>(IEnumerable<PropertyInfo> properties,
            Expression<Func<T, TProperty>> propertyLambda)
        {
            return GetUpdate(ValidatePropertyExpression(propertyLambda), properties, Object);
        }

        public string GetUpdate(IEnumerable<PropertyInfo> properties, PropertyInfo propertyInfo)
        {
            return GetUpdate(propertyInfo, properties, Object);
        }

        public string GetUpdate()
        {
            return GetUpdate(GetProperties(Type));
        }

        public string GetUpdate<TProperty>(Expression<Func<T, TProperty>> propertyLambda)
        {
            return GetUpdate(propertyLambda, GetProperties(Type));
        }

        public string GetUpdate<TProperty>(Expression<Func<T, TProperty>> propertyLambda,
            IEnumerable<PropertyInfo> properties)
        {
            return GetUpdate(ValidatePropertyExpression(propertyLambda, typeof(T)), properties, Object);
        }

        private PropertyInfo ValidatePropertyExpression<TProperty>(Expression<Func<T, TProperty>> propertyLambda)
        {
            return ValidatePropertyExpression(propertyLambda, Type);
        }

        private static PropertyInfo ValidatePropertyExpression<TProperty>(Expression<Func<T, TProperty>> propertyLambda,
            Type type)
        {
            if (propertyLambda == null) return null;

            if (!(propertyLambda.Body is MemberExpression member))
                throw new ArgumentException(
                    $"Expression '{propertyLambda}' refers to a method, not a property.");

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(
                    $"Expression '{propertyLambda}' refers to a field, not a property.");

            if (propInfo.ReflectedType != null && type != propInfo.ReflectedType &&
                !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException(
                    $"Expresion '{propertyLambda}' refers to a property that is not from type {type}.");
            return propInfo;
        }

        private string GetUpdate(PropertyInfo key, IEnumerable<PropertyInfo> properties, T obj, string where = null)
        {
            var propertyInfos = properties.ToList();

            var formattableString =
                $"UPDATE {Table} SET {GetColumnsWithValues(GetColumns(propertyInfos), GetValues(propertyInfos, obj)).Aggregate((x, y) => $"{x}, {y}")} {where ?? $"WHERE {GetColumnName(key)}={GetStringWithinBounds(key, key.GetValue(obj))}"}";
            return
                formattableString;
        }
    }

    public class SqlObject : ISqlObject
    {
        /// <summary>
        ///     Gets the name of the column.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <returns></returns>
        public string GetColumnName(MemberInfo propertyInfo)
        {
            return Cache.GetOrAdd($"SqlObj_Property[{Type.FullName}]={propertyInfo.Name}", () =>
            {
                if (Context != null && propertyInfo is PropertyInfo info)
                    return Context.Model.FindEntityType(Type).AsEntityType().FindProperty(info).Name;

                if (propertyInfo.GetCustomAttribute<InversePropertyAttribute>() is InversePropertyAttribute _)
                    return string.Empty;

                return propertyInfo.GetCustomAttribute<ColumnAttribute>() is ColumnAttribute attribute
                    ? attribute.Name
                    : propertyInfo.Name;
            });
        }

        public IEnumerable<string> GetColumns(IEnumerable<PropertyInfo> properties)
        {
            return properties
                .Select(GetColumnName)
                .Where(i => !string.IsNullOrEmpty(i));
        }

        public IEnumerable<string> GetColumnsWithValues(IEnumerable<string> columns, IEnumerable<string> values)
        {
            return columns.Zip(values, (s, s1) => $"{s}={s1}");
        }

        /// <summary>
        ///     Gets the insert SQL statement.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Can't insert with null object</exception>
        public string GetInsert()
        {
            if (Object == null)
                throw new Exception("Cant insert with null object");

            return
                $"INSERT INTO {Table} ({Cache.GetOrAdd($"SqlObject_AggregatedColumnsFor_{Type.FullName}", () => GetColumns().Aggregate((c, n) => $"{c},{n}"))}) VALUES ({GetValues(GetProperties(Type), Object).Aggregate((c, n) => $"{c},{n}")})";
        }

        /// <summary>
        ///     Gets the select SQL statement.
        /// </summary>
        /// <param name="where">The where.</param>
        /// <param name="ignoreIncludeInSelect">if set to <c>true</c> [ignore include in select].</param>
        /// <returns></returns>
        public string GetSelect(string where = null, bool ignoreIncludeInSelect = false)
        {
            return Cache.GetOrAdd($"SqlObject_SelectFor_{Type.FullName}", () =>
            {
                var props = Type.GetProperties();
                var returnString = "";
                foreach (var propertyInfo in props)
                    if (propertyInfo.GetCustomAttribute<IncludeInSelect>() != null)
                    {
                        if (returnString != "")
                            returnString += ",";

                        returnString += GetColumnName(propertyInfo);
                    }

                var returnStrignEx = returnString == "" || ignoreIncludeInSelect ? "*" : returnString;

                return string.IsNullOrEmpty(where)
                    ? $"SELECT {returnStrignEx} FROM \"{Table}\""
                    : $"SELECT {returnStrignEx} FROM \"{Table}\" WHERE {where}";
            });
        }

        /// <summary>
        ///     Gets the value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetValue(PropertyInfo propertyInfo, object obj)
        {
            if (propertyInfo.GetCustomAttribute<InversePropertyAttribute>() is InversePropertyAttribute _)
                return string.Empty;

            var val = propertyInfo.GetValue(obj);
            if (val == null)
            {
                if (propertyInfo.GetCustomAttribute<RequiredAttribute>() != null)
                    throw new Exception($"A coluna '{GetColumnName(propertyInfo)}' não pode ser nula.");

                return "NULL";
            }

            return GetStringWithinBounds(propertyInfo, val);
        }

        /// <summary>
        ///     Gets the object values.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public IEnumerable<string> GetValues(IEnumerable<PropertyInfo> properties, object obj)
        {
            return properties.Select(propertyInfo => GetValue(propertyInfo, obj))
                .Where(i => !string.IsNullOrEmpty(i));
        }

        /// <summary>
        ///     Gets or sets the SQL table name.
        /// </summary>
        /// <value>
        ///     The table name.
        /// </value>
        public string Table { get; set; }

        /// <summary>
        ///     Gets or sets the object.
        /// </summary>
        /// <value>
        ///     The object.
        /// </value>
        public object Object { get; set; }

        protected IDbContextDependencies Context { get; set; }

        /// <summary>
        ///     Gets the sql object's type.
        /// </summary>
        /// <value>
        ///     The object's type.
        /// </value>
        protected Type Type { get; set; }

        internal static IAppCache Cache { get; } = new CachingService();

        public SqlObject(object obj, IDbContextDependencies context)
        {
            Object = obj;
            Type = obj.GetType();
            Context = context;
            InitializeByType(
                context.Model.FindEntityType(Type).GetAnnotation("Relational:TableName").Value as string);
        }

        static SqlObject()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        /// <inheritdoc />
        public SqlObject(Type objectType, string tableName = null)
        {
            Type = objectType;
            InitializeByType(tableName);
        }

        /// <inheritdoc />
        public SqlObject(object obj, string tableName = null)
        {
            Object = obj;

            if (obj == null)
            {
                throw new InvalidOperationException("Can't get type from a null object, please specify it splicity");
            }

            Type = obj.GetType();
            Table = tableName ?? Type.GetTable();
        }

        /// <summary>
        ///     Gets the SQL columns.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetColumns()
        {
            return Cache.GetOrAdd($"SqlObject_ColumnsFor_{Type.FullName}", () => GetProperties(Type)
                .Select(GetColumnName)
                .Where(i => !string.IsNullOrEmpty(i)));
        }

        protected void InitializeByType(string tableName)
        {
            Table = tableName ?? Type.GetTable();
        }

        public PropertyInfo GetKeyPropertyInfo()
        {
            if (Context != null)
            {
                var readOnlyList = Context.Model.FindEntityType(Type).FindPrimaryKey().AsKey().Properties;
                return readOnlyList.FirstOrDefault()?.PropertyInfo;
            }

            var singleOrDefault =
                Type.GetProperties().SingleOrDefault(i => i.GetCustomAttribute<KeyAttribute>() != null);
            if (singleOrDefault != null) return singleOrDefault;

            throw new InvalidOperationException(
                $"Could not find primary key on model {Type.Name}, try passing the key as an argument, or add the [Key] attribute from components model");
        }

        protected IEnumerable<PropertyInfo> GetProperties(Type type)
        {
            return Cache.GetOrAdd($"SqlObj_ProprtiesFor_{type.FullName}",
                () => type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        }

        /// <summary>
        ///     Gets the default.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static object GetDefault(Type type)
        {
            return Cache.GetOrAdd($"SqlObject_DefaultFor_{type.FullName}",
                () => type.IsValueType ? Activator.CreateInstance(type) : null);
        }

        /// <summary>
        ///     Gets the string within bounds.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string GetStringWithinBounds(PropertyInfo propertyInfo, object value)
        {
            if (propertyInfo.PropertyType != typeof(string) && value == null &&
                GetDefault(propertyInfo.PropertyType) == null)
                return "'NULL'";

            var returnValue = value?.ToString();

            if (value is DateTime dateTime) returnValue = dateTime.ToString("yyyy-MM-dd hh:mm:ss");

            if (value is double || value is decimal) returnValue = value.ToString().Replace(",", ".");

            if (propertyInfo.GetCustomAttribute<StringLengthAttribute>() is StringLengthAttribute stringLengthAttribute)
                returnValue = new string(value?.ToString().Take(stringLengthAttribute.MaximumLength).ToArray())
                    .AddQuotes();

            return returnValue.AddQuotes(value?.GetType());
        }
    }
}