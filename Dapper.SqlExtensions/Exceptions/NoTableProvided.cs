using System;

namespace Dapper.SqlExtensions.Exceptions
{
    public class NoTableProvided : Exception
    {
        public NoTableProvided() : base("No table was provided in this DapperObject's options. (Null or empty)")
        {
        }
    }
}