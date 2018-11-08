using Dapper.SqlExtensions.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dapper.SqlExtensions.Tests
{
    [TestClass]
    public class SqlTests
    {
        [TestMethod]
        public void TestInsert()
        {
            var dapper = new DapperObject<Person>();
            var instance = GetRandomPerson();
            var insert = dapper.Insert(instance);
            Assert.AreEqual(insert,
                $"INSERT INTO PESSOA (ID, NAME, AGE) VALUES ({instance.Id}, '{instance.Name}', {instance.Age})");
        }

        [TestMethod]
        public void TestDefaultSelect()
        {
            var dapper = new DapperObject<Person>();
            Assert.AreEqual(dapper.Select(), "SELECT * FROM PESSOA");
        }

        [TestMethod]
        public void TestComplexSelect()
        {
            var dapper = new DapperObject<Person>();

            var instance = GetRandomPerson();

            Assert.AreEqual(
                dapper.SelectWhere(p => p.Age == instance.Age && p.Name == instance.Name && p.Id == instance.Id),
                $"SELECT * FROM PESSOA WHERE NAME='{instance.Name}' AND AGE={instance.Age} AND ID={instance.Id}");
        }

        private static Person GetRandomPerson()
        {
            var instance = new Bogus.Faker<Person>()
                .RuleFor(i => i.Id, f => f.Random.Number(1, 100))
                .RuleFor(i => i.Name, f => f.Person.FullName)
                .RuleFor(i => i.Age, f => f.Random.Number(10, 40))
                .Generate();
            return instance;
        }

        [TestMethod]
        public void TestDefaultUpdate()
        {
            var instance = GetRandomPerson();
            var dapper = new DapperObject<Person>();
            Assert.AreEqual(dapper.Update(instance, i => i.Id),
                $"UPDATE PESSOA SET NAME='{instance.Name.Replace("'", "''")}', AGE={instance.Age} WHERE ID={instance.Id}");
        }

        [TestMethod]
        public void TestComplexUpdate()
        {
            var instance = GetRandomPerson();

            var oldInstance = new Bogus.Faker<Person>()
                .RuleFor(i => i.Id, f => instance.Id)
                .RuleFor(i => i.Name, f => instance.Name)
                .RuleFor(i => i.Age, f => f.Random.Number(10, 40))
                .Generate();

            var dapper = new DapperObject<Person>();
            var update = dapper.Update(instance, oldInstance, i => i.Id);
            Assert.AreEqual($"UPDATE PESSOA SET AGE={instance.Age} WHERE ID={instance.Id}", update);
        }

        [TestMethod]
        public void TestDelete()
        {
            var dapper = new DapperObject<Person>();
            var instance = GetRandomPerson();

            var sql = dapper.Delete(new Person {Id = instance.Id}, i => i.Id);
            Assert.AreEqual($"DELETE FROM PESSOA WHERE ID={instance.Id}", sql);
        }
    }
}