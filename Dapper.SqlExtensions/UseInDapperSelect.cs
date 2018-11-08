using System;

namespace Dapper.SqlExtensions
{
    [AttributeUsage(AttributeTargets.Property)]
    public class UseInDapperSelect : Attribute
    {
    }
}