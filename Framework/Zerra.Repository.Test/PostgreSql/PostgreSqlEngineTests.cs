// Copyright � KaKush LLC
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
        private int ExecuteSql(PostgreSqlTestSqlDataContext context, string sql)
        {
            using (var connection = new NpgsqlConnection(context.ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    return command.ExecuteNonQuery();
                }
            }
        }

        private void DropDatabase(PostgreSqlTestSqlDataContext context)
        {
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


        [TestMethod]
        public void TestSequence()
        {
            var context = new PostgreSqlTestSqlDataContext();
            var provider = new PostgreSqlTestTypesCustomerSqlProvider();

            DropDatabase(context);

            _ = context.InitializeEngine(true);

            TestModelMethods.TestSequence(provider);

            const string changeColumn = "ALTER TABLE testtypes ALTER COLUMN int32thing TYPE bigint; ALTER TABLE testtypes ALTER COLUMN int32thing DROP NOT NULL;";
            const string addColumn = "ALTER TABLE testtypes ADD dummytomakenullable int NOT NULL";
            const string dropColmn = "ALTER TABLE testtypes DROP COLUMN bytething";
            ExecuteSql(context, changeColumn);
            ExecuteSql(context, addColumn);
            ExecuteSql(context, dropColmn);

            _ = context.InitializeEngine(true);
        }
    }
}
