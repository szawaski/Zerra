// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using System;

namespace Zerra.Repository.Test
{
    [TestClass]
    public class MySqlEngineTests
    {
        private void DropDatabase()
        {
            var context = new MySqlTestSqlDataContext();
            var builder = new MySqlConnectionStringBuilder(context.ConnectionString);
            var testDatabase = builder.Database;
            builder.Database = "sys";
            var connectionStringForMaster = builder.ToString();
            using (var connection = new MySqlConnection(connectionStringForMaster))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"DROP DATABASE IF EXISTS {testDatabase}";
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

            var provider = new MySqlTestTypesCustomerSqlProvider();

            var modelOrigin = TestModels.GetTestTypesModel();
            modelOrigin.KeyA = Guid.NewGuid();
            provider.Persist(new Create<TestTypesModel>(modelOrigin));

            var model2 = (TestTypesModel)provider.Query(new QuerySingle<TestTypesModel>(x => x.KeyA == modelOrigin.KeyA));
            TestModels.AssertAreEqual(modelOrigin, model2);

            TestModels.UpdateModel(modelOrigin);
            provider.Persist(new Update<TestTypesModel>(modelOrigin));
            model2 = (TestTypesModel)provider.Query(new QuerySingle<TestTypesModel>(x => x.KeyA == modelOrigin.KeyA));
            TestModels.AssertAreEqual(modelOrigin, model2);

            provider.Persist(new Delete<TestTypesModel>(modelOrigin));
            model2 = (TestTypesModel)provider.Query(new QuerySingle<TestTypesModel>(x => x.KeyA == modelOrigin.KeyA));
            Assert.IsNull(model2);
        }
    }
}
