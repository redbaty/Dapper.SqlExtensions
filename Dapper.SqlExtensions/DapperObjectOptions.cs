using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Dapper.SqlExtensions
{
    public class DapperObjectOptions
    {
        public List<PropertyInfo> Properties { get; set; }

        public string Table { get; set; }

        public Func<PropertyInfo, string> ColumnResolver { get; } = info =>
        {
            if (info.GetCustomAttribute<ColumnAttribute>() is ColumnAttribute columnAttribute)
            {
                return columnAttribute.Name;
            }

            return info.Name.ToUpper();
        };

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