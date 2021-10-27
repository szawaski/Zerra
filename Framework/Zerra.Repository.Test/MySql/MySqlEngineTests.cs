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
        private int ExecuteSql(string sql)
        {
            var context = new PostgreSqlTestSqlDataContext();
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

        [TestMethod]
        public void TestSequence()
        {
            DropDatabase();

            var context = new MySqlTestSqlDataContext();
            var provider = new MySqlTestTypesCustomerSqlProvider();
            TestModelMethods.TestSequence(context, provider);

            const string changeColumn = "ALTER TABLE `TestTypes` ALTER COLUMN `Int32Thing` bigint NULL";
            const string addColumn = "ALTER TABLE `TestTypes` ADD `DummyToMakeNullable` int NOT NULL";
            const string dropColumn = "ALTER TABLE `TestTypes` DROP COLUMN `ByteThing`";
            ExecuteSql(changeColumn);
            ExecuteSql(addColumn);
            ExecuteSql(dropColumn);
        }
    }
}
