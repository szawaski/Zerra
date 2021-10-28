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
                    command.CommandText = $"IF EXISTS(SELECT[dbid] FROM master.dbo.sysdatabases where[name] = '{testDatabase}')\r\nBEGIN\r\nALTER DATABASE [{testDatabase}] SET single_user WITH ROLLBACK IMMEDIATE\r\nDROP DATABASE {testDatabase}\r\nEND";
                    command.ExecuteNonQuery();
                }
            }
        }

        [TestMethod]
        public void TestSequence()
        {
            var context = new MsSqlTestSqlDataContext();

            DropDatabase(context);

            _ = context.InitializeEngine(true);

            var provider = new MsSqlTestTypesSqlProvider();
            var relationProvider = new MsSqlTestRelationsSqlProvider();

            TestModelMethods.TestSequence(provider, relationProvider);

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
