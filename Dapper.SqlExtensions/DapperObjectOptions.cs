using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Dapper.SqlExtensions
{
    public class DapperObjectOptions
    {
        /// <summary>
        ///     This function will be called for each property on every
        ///     SQL call, to determine this property's SQL column name.
        /// </summary>
        public Func<PropertyInfo, string> ColumnResolver { get; } = info =>
        {
            if (info.GetCustomAttribute<ColumnAttribute>() is ColumnAttribute columnAttribute)
                return columnAttribute.Name;

            return info.Name.ToUpper();
        };

        /// <summary>
        ///     The relevant properties. (Will be used on every SQL call)
        ///     If none are provided, all the public non-complex type
        ///     properties will be used.
        /// </summary>
        public List<PropertyInfo> Properties { get; set; }

        /// <summary>
        ///     Gets/sets the SQL table name.
        ///     If none is provided the class
        ///     name or the name provided by <see cref="TableAttribute" />
        ///     will be used.
        /// </summary>
        public string Table { get; set; }

        internal DapperObjectOptions GetFinal(Type objectType)
        {
            if (Properties == null || Properties.Count <= 0)
                Properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(i => IsSupported(i.PropertyType)).ToList();

            if (string.IsNullOrEmpty(Table))
                Table = objectType.GetCustomAttribute<TableAttribute>() is TableAttribute tableAttribute
                    ? tableAttribute.Name
                    : objectType.Name.ToUpper();

            return this;
        }

        private static Type GetRealType(Type oldType)
        {
            return Nullable.GetUnderlyingType(oldType) ?? oldType;
        }

        private static bool IsSupported(Type type)
        {
            type = GetRealType(type);

            return type.IsPrimitive
                   || type.IsEnum
                   || type == typeof(string)
                   || type == typeof(DateTime)
                   || type == typeof(decimal);
        }
    }
}