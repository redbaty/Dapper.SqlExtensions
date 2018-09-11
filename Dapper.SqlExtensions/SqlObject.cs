using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Reflection;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Dapper.SqlExtensions
{
    public class SqlObject
    {
        private static IAppCache Cache { get; } = new CachingService();

        /// <summary>
        /// Gets or sets the SQL table name.
        /// </summary>
        /// <value>
        /// The table name.
        /// </value>
        public string Table { get; set; }

        /// <summary>
        /// Gets or sets the object.
        /// </summary>
        /// <value>
        /// The object.
        /// </value>
        public object Object { get; set; }

        /// <summary>
        /// Gets the sql object's type.
        /// </summary>
        /// <value>
        /// The object's type.
        /// </value>
        private Type Type { get; set; }

        private IDbContextDependencies Context { get; }

        public SqlObject(Type type, IDbContextDependencies context)
        {
            Context = context;
            InitializeByType(type,
                context.Model.FindEntityType(type).GetAnnotation("Relational:TableName").Value as string);
        }

        public SqlObject(object obj, IDbContextDependencies context)
        {
            Object = obj;
            Context = context;
            InitializeByType(obj.GetType(),
                context.Model.FindEntityType(obj.GetType()).GetAnnotation("Relational:TableName").Value as string);
        }

        /// <inheritdoc />
        public SqlObject(Type type, string tableName = null)
        {
            InitializeByType(type, tableName);
        }

        private void InitializeByType(Type type, string tableName)
        {
            Type = type;
            Table = tableName ?? Type.GetTable();
        }

        /// <inheritdoc />
        public SqlObject(object obj, string tableName = null)
        {
            Object = obj;
            Type = Object.GetType();
            Table = tableName ?? Type.GetTable();
        }

        /// <summary>
        /// Gets the insert SQL statement.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Can't insert with null object</exception>
        public string GetInsert()
        {
            if (Object == null)
                throw new Exception("Cant insert with null object");

            return
                $"INSERT INTO {Table} ({Cache.GetOrAdd($"SqlObject_AggregatedColumnsFor_{Type.FullName}", () => GetColumns().Aggregate((c, n) => $"{c},{n}"))}) VALUES ({GetValues().Aggregate((c, n) => $"{c},{n}")})";
        }

        /// <summary>
        /// Gets the select SQL statement.
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
                {
                    if (propertyInfo.GetCustomAttribute<IncludeInSelect>() != null)
                    {
                        if (returnString != "")
                            returnString += ",";

                        returnString += GetColumnName(propertyInfo);
                    }
                }

                var returnStrignEx = returnString == "" || ignoreIncludeInSelect ? "*" : returnString;

                return string.IsNullOrEmpty(where)
                    ? $"SELECT {returnStrignEx} FROM \"{Table}\""
                    : $"SELECT {returnStrignEx} FROM \"{Table}\" WHERE {where}";
            });
        }

        /// <summary>
        /// Gets the SQL columns.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetColumns()
        {
            return GetProperties().Select(GetColumnName)
                .Where(i => !string.IsNullOrEmpty(i));
        }

        /// <summary>
        /// Gets the object values.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetValues()
        {
            return GetProperties().Select(GetValue)
                .Where(i => !string.IsNullOrEmpty(i));
        }

        private IEnumerable<PropertyInfo> GetProperties()
        {
            return Cache.GetOrAdd($"SqlObj_ProprtiesFor_{Type.FullName}",
                () => Type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        }

        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <returns></returns>
        public string GetColumnName(MemberInfo propertyInfo)
        {
            return Cache.GetOrAdd($"SqlObj_Property[{Type.FullName}]={propertyInfo.Name}", () =>
            {
                if (Context != null && propertyInfo is PropertyInfo info)
                {
                    return Context.Model.FindEntityType(Type).AsEntityType().FindProperty(info).Name;
                }

                if (propertyInfo.GetCustomAttribute<InversePropertyAttribute>() is InversePropertyAttribute _)
                    return string.Empty;

                if (propertyInfo.GetCustomAttribute<NotMappedAttribute>() is NotMappedAttribute _)
                    return string.Empty;

                return propertyInfo.GetCustomAttribute<ColumnAttribute>() is ColumnAttribute attribute
                    ? attribute.Name
                    : propertyInfo.Name;
            });
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetValue(PropertyInfo propertyInfo)
        {
            if (propertyInfo.GetCustomAttribute<InversePropertyAttribute>() is InversePropertyAttribute _)
                return string.Empty;

            if (propertyInfo.GetCustomAttribute<NotMappedAttribute>() is NotMappedAttribute _)
                return string.Empty;

            var val = propertyInfo.GetValue(Object);
            if (val == null)
            {
                if (propertyInfo.GetCustomAttribute<RequiredAttribute>() != null)
                {
                    throw new Exception($"A coluna '{GetColumnName(propertyInfo)}' não pode ser nula.");
                }

                return "NULL";
            }

            return GetStringWithinBounds(propertyInfo, val);
        }

        /// <summary>
        /// Gets the default.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static object GetDefault(Type type)
        {
            return Cache.GetOrAdd($"SqlObject_DefaultFor_{type.FullName}",
                () => type.IsValueType ? Activator.CreateInstance(type) : null);
        }

        /// <summary>
        /// Gets the string within bounds.
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

            if (value is DateTime dateTime)
            {
                returnValue = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (value is double d)
            {
                returnValue = d.ToString(CultureInfo.CurrentCulture).Replace(",", ".");
            }


            if (propertyInfo.GetCustomAttribute<StringLengthAttribute>() is StringLengthAttribute stringLengthAttribute)
            {
                returnValue = new string(value?.ToString().Take(stringLengthAttribute.MaximumLength).ToArray())
                    .AddQuotes();
            }

            if (propertyInfo.PropertyType.IsEnum && value != null)
            {
                return ((int)value).ToString();
            }

            return returnValue.AddQuotes(value?.GetType());
        }
    }
}