using System;

namespace Dapper.SqlExtensions
{
    /// <summary>
    /// Includes this property into the SQL statement.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Property)]
    public class IncludeInSelect : Attribute
    {
    }
}