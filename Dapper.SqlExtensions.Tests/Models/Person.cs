using System.ComponentModel.DataAnnotations.Schema;

namespace Dapper.SqlExtensions.Tests.Models
{
    [Table("PESSOA")]
    internal class Person
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }
    }
}