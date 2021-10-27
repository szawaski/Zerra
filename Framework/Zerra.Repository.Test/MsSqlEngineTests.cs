// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data.SqlClient;

namespace Zerra.Repository.Test
{
    [TestClass]
    public class MsSqlEngineTests
    {
        private void DropDatabase()
        {
            var context = new MsSqlTestSqlDataContext();
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
            var context = new MsSqlTestSqlDataContext();
            _ = context.InitializeEngine<ITransactStoreEngine>();
        }

        [TestMethod]
        public void Crud()
        {
            AssureDatabase();

            var provider = new MsSqlTestTypesCustomerSqlProvider();

            var modelOrigin = TestModels.GetTestTypesModel();
            modelOrigin.Key = Guid.NewGuid();
            provider.Persist(new Create<TestTypesModel>(modelOrigin));

            var model2 = (TestTypesModel)provider.Query(new QuerySingle<TestTypesModel>(x => x.Key == modelOrigin.Key));
            TestModels.AssertAreEqual(modelOrigin, model2);

            TestModels.UpdateModel(modelOrigin);
            provider.Persist(new Update<TestTypesModel>(modelOrigin));
            model2 = (TestTypesModel)provider.Query(new QuerySingle<TestTypesModel>(x => x.Key == modelOrigin.Key));
            TestModels.AssertAreEqual(modelOrigin, model2);

            provider.Persist(new Delete<TestTypesModel>(modelOrigin));
            model2 = (TestTypesModel)provider.Query(new QuerySingle<TestTypesModel>(x => x.Key == modelOrigin.Key));
            Assert.IsNull(model2);
        }
    }
}
