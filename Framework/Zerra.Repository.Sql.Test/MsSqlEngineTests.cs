// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data.SqlClient;

namespace Zerra.Repository.Sql.Test
{
    [TestClass]
    public class MsSqlEngineTests
    {
        private void DropDatabase()
        {
            var context = new TestSqlDataContext();
            var builder = new SqlConnectionStringBuilder(context.ConnectionString);
            var testDatabase = builder.InitialCatalog;
            builder.InitialCatalog = "master";
            var connectionStringForMaster = builder.ToString();
            using (var connection = new SqlConnection(connectionStringForMaster))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"IF EXISTS(SELECT[dbid] FROM master.dbo.sysdatabases where[name] = '{testDatabase}')\r\nDROP DATABASE {testDatabase}";
                    command.ExecuteNonQuery();
                }
            }
        }

        public void AssureDatabase()
        {
            DropDatabase();
            var context = new TestSqlDataContext();
            context.AssureDataStore();
        }

        [TestMethod]
        public void Crud()
        {
            AssureDatabase();

            var modelOrigin = TestModels.GetTestTypesModel();
            modelOrigin.Key = Guid.NewGuid();
            Repo.Persist(new Create<TestTypesModel>(modelOrigin));

            var model2 = Repo.Query(new QuerySingle<TestTypesModel>(x => x.Key == modelOrigin.Key));
            TestModels.AssertAreEqual(modelOrigin, model2);

            TestModels.UpdateModel(modelOrigin);
            Repo.Persist(new Update<TestTypesModel>(modelOrigin));
            model2 = Repo.Query(new QuerySingle<TestTypesModel>(x => x.Key == modelOrigin.Key));
            TestModels.AssertAreEqual(modelOrigin, model2);

            Repo.Persist(new Delete<TestTypesModel>(modelOrigin));
            model2 = Repo.Query(new QuerySingle<TestTypesModel>(x => x.Key == modelOrigin.Key));
            Assert.IsNull(model2);
        }
    }
}
