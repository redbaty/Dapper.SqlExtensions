using System;
using System.Globalization;

namespace Dapper.SqlExtensions.Extensions
{
    internal static class StringExtensions
    {
        /// <summary>
        ///     Adds the quotes.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="objectType">The objects type (used in some specific cases)</param>
        /// <returns></returns>
        public static string AddQuotes(this string str, Type objectType = null)
        {
            if (str.StartsWith("'") && str.EndsWith("'")) return str;

            str = str.Replace("'", "''");

            return !(objectType != null && objectType == typeof(string))
                ? str.IsNumeric() ? str : $"'{str}'"
                : $"'{str}'";
        }

        /// <summary>
        ///     Determines whether this instance is numeric.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>
        ///     <c>true</c> if the specified expression is numeric; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNumeric(this string expression)
        {
            return double.TryParse(Convert.ToString(expression), NumberStyles.Any,
                NumberFormatInfo.InvariantInfo, out _);
        }
    }
}