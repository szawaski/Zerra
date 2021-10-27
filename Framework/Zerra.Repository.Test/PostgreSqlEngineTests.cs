// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using System;

namespace Zerra.Repository.Test
{
    [TestClass]
    public class PostgreSqlEngineTests
    {
        private void DropDatabase()
        {
            var context = new PostgreSqlTestSqlDataContext();
            var builder = new NpgsqlConnectionStringBuilder(context.ConnectionString);
            var testDatabase = builder.Database;
            builder.Database = "postgres";
            var connectionStringForMaster = builder.ToString();
            using (var connection = new NpgsqlConnection(connectionStringForMaster))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"DROP DATABASE IF EXISTS {testDatabase}";
                    command.ExecuteNonQuery();
                }
            }
        }

        public void BuildStoreFromModels()
        {
            DropDatabase();
            var context = new PostgreSqlTestSqlDataContext();
            _ = context.InitializeEngine<ITransactStoreEngine>();
        }

        [TestMethod]
        public void Crud()
        {
            BuildStoreFromModels();

            var provider = new PostgreSqlTestTypesCustomerSqlProvider();

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
