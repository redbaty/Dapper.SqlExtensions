using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dapper.SqlExtensions
{
    public interface ISqlObject
    {
        /// <summary>
        ///     Gets or sets the SQL table name.
        /// </summary>
        /// <value>
        ///     The table name.
        /// </value>
        string Table { get; set; }

        /// <summary>
        ///     Gets the insert SQL statement.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Can't insert with null object</exception>
        string GetInsert();

        /// <summary>
        ///     Gets the select SQL statement.
        /// </summary>
        /// <param name="where">The where.</param>
        /// <param name="ignoreIncludeInSelect">if set to <c>true</c> [ignore include in select].</param>
        /// <returns></returns>
        string GetSelect(string where = null, bool ignoreIncludeInSelect = false);

        /// <summary>
        /// Gets the columns with values in the following format: COLUMN=FORMATTEDVALUE.
        /// </summary>
        /// <param name="columns">The columns.</param>
        /// <param name="values">The values.</param>
        /// <returns></returns>
        IEnumerable<string> GetColumnsWithValues(IEnumerable<string> columns, IEnumerable<string> values);

        /// <summary>
        ///     Gets the SQL columns.
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        IEnumerable<string> GetColumns(IEnumerable<PropertyInfo> properties);

        /// <summary>
        ///     Gets the object values.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        IEnumerable<string> GetValues(IEnumerable<PropertyInfo> properties, object obj);

        /// <summary>
        ///     Gets the name of the column.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <returns></returns>
        string GetColumnName(MemberInfo propertyInfo);

        /// <summary>
        ///     Gets the value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        string GetValue(PropertyInfo propertyInfo, object obj);
    }
}