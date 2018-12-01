using System;

// ReSharper disable ClassNeverInstantiated.Global

namespace Dapper.SqlExtensions
{
    /// <inheritdoc />
    /// <summary>
    ///     Use this attribute in properties you want to be included in a 'SELECT' statement.
    ///     Please note that only the properties with this attribute in a class will be selected, if none is present then a '*'
    ///     will be used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class UseOnSqlExtensionsSelect : Attribute
    {
    }
}