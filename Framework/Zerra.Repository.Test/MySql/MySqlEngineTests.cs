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
        private int ExecuteSql(MySqlTestSqlDataContext context, string sql)
        {
            using (var connection = new MySqlConnection(context.ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    return command.ExecuteNonQuery();
                }
            }
        }

        private void DropDatabase(MySqlTestSqlDataContext context)
        {
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

        [TestMethod]
        public void TestSequence()
        {
            var context = new MySqlTestSqlDataContext();
            var provider = new MySqlTestTypesCustomerSqlProvider();

            DropDatabase(context);

            _ = context.InitializeEngine(true);

            TestModelMethods.TestSequence(provider);

            const string changeColumn = "ALTER TABLE `TestTypes` ALTER COLUMN `Int32Thing` bigint NULL";
            const string addColumn = "ALTER TABLE `TestTypes` ADD `DummyToMakeNullable` int NOT NULL";
            const string dropColumn = "ALTER TABLE `TestTypes` DROP COLUMN `ByteThing`";
            ExecuteSql(context, changeColumn);
            ExecuteSql(context, addColumn);
            ExecuteSql(context, dropColumn);

            _ = context.InitializeEngine(true);
        }
    }
}
