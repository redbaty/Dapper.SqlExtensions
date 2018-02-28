using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Dapper.SqlExtensions
{
    public class SqlObject
    {
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
        private Type Type { get; }

        /// <inheritdoc />
        public SqlObject(Type type, string tableName = null)
        {
            Table = tableName ?? (type.GetCustomAttribute<TableAttribute>() is TableAttribute table
                        ? table.Name
                        : type.Name);
            Type = type;
        }

        /// <inheritdoc />
        public SqlObject(object obj, string tableName = null)
        {
            Table = tableName ?? (obj.GetType().GetCustomAttribute<TableAttribute>() is TableAttribute table
                        ? table.Name
                        : obj.GetType().Name);
            Object = obj;
            Type = Object.GetType();
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
                $"INSERT INTO {Table} ({GetColumns().Aggregate((c, n) => $"{c},{n}")}) VALUES ({GetValues().Aggregate((c, n) => $"{c},{n}")})";
        }

        /// <summary>
        /// Gets the select SQL statement.
        /// </summary>
        /// <param name="where">The where.</param>
        /// <param name="ignoreIncludeInSelect">if set to <c>true</c> [ignore include in select].</param>
        /// <returns></returns>
        public string GetSelect(string where = null, bool ignoreIncludeInSelect = false)
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
                ? $"SELECT {returnStrignEx} FROM {Type.GetTable()}"
                : $"SELECT {returnStrignEx} FROM {Type.GetTable()} WHERE {where}";
        }

        /// <summary>
        /// Gets the SQL columns.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetColumns()
        {
            return Type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(GetColumnName)
                .Where(i => !string.IsNullOrEmpty(i));
        }

        /// <summary>
        /// Gets the object values.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetValues()
        {
            return Type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(GetValue)
                .Where(i => !string.IsNullOrEmpty(i));
        }

        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <returns></returns>
        public static string GetColumnName(MemberInfo propertyInfo)
        {
            if (propertyInfo.GetCustomAttribute<InversePropertyAttribute>() is InversePropertyAttribute _)
                return string.Empty;

            return propertyInfo.GetCustomAttribute<ColumnAttribute>() is ColumnAttribute attribute
                ? attribute.Name
                : propertyInfo.Name;
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
            return type.IsValueType ? Activator.CreateInstance(type) : null;
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
            {
                return "'NULL'";
            }

            var returnValue = value?.ToString();

            switch (value)
            {
                case DateTime dateTime:
                    returnValue = dateTime.ToString("yyyy-MM-dd hh:mm:ss");
                    break;
                case double d:
                    returnValue = d.ToString(CultureInfo.CurrentCulture).Replace(",", ".");
                    break;
            }

            if (propertyInfo.GetCustomAttribute<StringLengthAttribute>() is StringLengthAttribute stringLengthAttribute)
            {
                returnValue = new string(value?.ToString().Take(stringLengthAttribute.MaximumLength).ToArray())
                    .AddQuotes();
            }

            return returnValue.AddQuotes();
        }
    }
}