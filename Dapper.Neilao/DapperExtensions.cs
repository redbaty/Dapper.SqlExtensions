using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Dapper.SqlExtensions
{
    public static class DapperExtensions
    {
        /// <summary>
        ///     Get a table name from the Table attribute.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetTable(this Type type)
        {
            try
            {
                return type.GetCustomAttribute<TableAttribute>().Name;
            }
            catch
            {
                return type.Name;
            }
        }

        /// <summary>
        /// Gets the SQL select statement.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="tablename">The tablename.</param>
        /// <param name="where">The where.</param>
        /// <param name="ignoreIncludeInSelect">if set to <c>true</c> [ignore include in select].</param>
        /// <returns></returns>
        public static string GetSqlSelect(this Type type, string tablename = null, string where = null,
            bool ignoreIncludeInSelect = false)
        {
            return new SqlObject(type, tablename).GetSelect(where, ignoreIncludeInSelect);
        }

        /// <summary>
        /// Determines whether this instance is numeric.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>
        ///   <c>true</c> if the specified expression is numeric; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNumeric(this string expression)
        {
            return double.TryParse(Convert.ToString(expression), System.Globalization.NumberStyles.Any,
                System.Globalization.NumberFormatInfo.InvariantInfo, out _);
        }

        /// <summary>
        /// Adds the quotes.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns></returns>
        public static string AddQuotes(this string str)
        {
            if (str.StartsWith("'") && str.EndsWith("'"))
                return str;

            return str.IsNumeric() ? str : $"'{str}'";
        }

        /// <summary>
        /// Gets the insert SQL statement.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static string GetInsertSql<T>(this T obj)
        {
            var sqlObject = new SqlObject(obj);
            return sqlObject.GetInsert();
        }
    }
}