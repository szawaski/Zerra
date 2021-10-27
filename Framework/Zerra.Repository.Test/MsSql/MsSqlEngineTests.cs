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
        private int ExecuteSql(MsSqlTestSqlDataContext context, string sql)
        {
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

        private void DropDatabase(MsSqlTestSqlDataContext context)
        {
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
            var context = new MsSqlTestSqlDataContext();
            var provider = new MsSqlTestTypesCustomerSqlProvider();

            DropDatabase(context);

            _ = context.InitializeEngine(true);

            TestModelMethods.TestSequence(provider);

            const string changeColumn = "ALTER TABLE [TestTypes] ALTER COLUMN [Int32Thing] bigint NULL";
            const string addColumn = "ALTER TABLE [TestTypes] ADD [DummyToMakeNullable] int NOT NULL";
            const string dropColumn = "ALTER TABLE [TestTypes] DROP COLUMN [ByteThing]";
            ExecuteSql(context, changeColumn);
            ExecuteSql(context, addColumn);
            ExecuteSql(context, dropColumn);

            _ = context.InitializeEngine(true);
        }
    }
}
