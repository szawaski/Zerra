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
        private int ExecuteSql(string sql)
        {
            var context = new MsSqlTestSqlDataContext();
            using (var connection = new SqlConnection(context.ConnectionString))
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

        [TestMethod]
        public void TestSequence()
        {
            DropDatabase();

            var context = new MsSqlTestSqlDataContext();
            var provider = new MsSqlTestTypesCustomerSqlProvider();
            TestModelMethods.TestSequence(context, provider);

            const string changeColumn = "ALTER TABLE [TestTypes] ALTER COLUMN [Int32Thing] bigint NULL";
            const string addColumn = "ALTER TABLE [TestTypes] ADD [DummyToMakeNullable] int NOT NULL";
            const string dropColumn = "ALTER TABLE [TestTypes] DROP COLUMN [ByteThing]";
            ExecuteSql(changeColumn);
            ExecuteSql(addColumn);
            ExecuteSql(dropColumn);

            _ = context.InitializeEngine(true);
        }
    }
}
