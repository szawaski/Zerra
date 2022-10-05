// Copyright � KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using System.Linq;
using System.Text;
using Zerra.Providers;
using Zerra.Repository.Reflection;

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
                    command.CommandText = $"SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '{testDatabase}'; DROP DATABASE IF EXISTS {testDatabase};";
                    _ = command.ExecuteNonQuery();
                }
            }
        }


        [TestMethod]
        public void TestSequence()
        {
            var context = new PostgreSqlTestSqlDataContext();

            DropDatabase(context);

            _ = context.InitializeEngine<ITransactStoreEngine>(true);

            var provider = ProviderResolver.Get<ITransactStoreProvider<PostgreSqlTestTypesModel>>();
            var relationProvider = ProviderResolver.Get<ITransactStoreProvider<PostgreSqlTestRelationsModel>>();

            TestModelMethods.TestSequence(provider, relationProvider);

            const string changeColumn = "ALTER TABLE testtypes ALTER COLUMN int32thing TYPE bigint; ALTER TABLE testtypes ALTER COLUMN int32thing DROP NOT NULL;";
            const string addColumn = "ALTER TABLE testtypes ADD dummytomakenullable int NOT NULL";
            const string dropColmn = "ALTER TABLE testtypes DROP COLUMN bytething";
            _ = ExecuteSql(context, changeColumn);
            _ = ExecuteSql(context, addColumn);
            _ = ExecuteSql(context, dropColmn);

            _ = context.InitializeEngine<ITransactStoreEngine>(true);

            var sb = new StringBuilder();
            var modelDetails = ModelAnalyzer.GetModel<MsSqlTestTypesModel>();
            foreach (var property in modelDetails.Properties)
            {
                if (property.IsIdentity || property.IsIdentityAutoGenerated || property.IsRelated)
                    continue;
                if (modelDetails.Properties.Any(x => x.ForeignIdentity == property.Name))
                    continue;
                _ = sb.Append("ALTER TABLE testtypes DROP COLUMN ").Append(property.PropertySourceName.ToLower()).Append(";\r\n");
            }
            var dropAllColumns = sb.ToString();

            _ = ExecuteSql(context, dropAllColumns);

            _ = context.InitializeEngine<ITransactStoreEngine>(true);

            _ = sb.Clear();
            foreach (var property in modelDetails.Properties)
            {
                if (property.IsIdentity || property.IsIdentityAutoGenerated || property.IsRelated)
                    continue;
                if (modelDetails.Properties.Any(x => x.ForeignIdentity == property.Name))
                    continue;
                if (property.IsNullable)
                {
                    _ = sb.Append("ALTER TABLE testtypes ADD Junk").Append(property.PropertySourceName).Append(" ");
                    PostgreSql.PostgreSqlEngine.WriteSqlTypeFromModel(sb, property);
                    PostgreSql.PostgreSqlEngine.WriteTypeEndingFromModel(sb, property);
                    _ = sb.Insert(sb.Length - 4, "NOT ");
                    _ = sb.Append(";\r\n");
                }
            }
            var addJunkColumns = sb.ToString();

            _ = ExecuteSql(context, addJunkColumns);

            _ = context.InitializeEngine<ITransactStoreEngine>(true);
        }
    }
}
