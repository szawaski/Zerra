// Copyright � KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Zerra.Providers;
using Zerra.Repository.Reflection;

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

            _ = context.InitializeEngine<ITransactStoreEngine>(true);

            var provider = Resolver.Get<ITransactStoreProvider<MsSqlTestTypesModel>>();
            var relationProvider = Resolver.Get<ITransactStoreProvider<MsSqlTestRelationsModel>>();

            TestModelMethods.TestSequence(provider, relationProvider);

            const string changeColumn = "ALTER TABLE [TestTypes] ALTER COLUMN [Int32Thing] bigint NULL";
            const string addColumn = "ALTER TABLE [TestTypes] ADD [DummyToMakeNullable] int NOT NULL";
            const string dropColumn = "ALTER TABLE [TestTypes] DROP COLUMN [ByteThing]";
            ExecuteSql(context, changeColumn);
            ExecuteSql(context, addColumn);
            ExecuteSql(context, dropColumn);

            _ = context.InitializeEngine<ITransactStoreEngine>(true);

            var sb = new StringBuilder();
            var modelDetails = ModelAnalyzer.GetModel<MsSqlTestTypesModel>();
            foreach (var property in modelDetails.Properties)
            {
                if (property.IsIdentity || property.IsIdentityAutoGenerated || property.IsRelated)
                    continue;
                if (modelDetails.Properties.Any(x => x.ForeignIdentity == property.Name))
                    continue;
                sb.Append("ALTER TABLE [TestTypes] DROP COLUMN [").Append(property.PropertySourceName).Append("]\r\n");
            }
            var dropAllColumns = sb.ToString();

            ExecuteSql(context, dropAllColumns);

            _ = context.InitializeEngine<ITransactStoreEngine>(true);

            sb.Clear();
            foreach (var property in modelDetails.Properties)
            {
                if (property.IsIdentity || property.IsIdentityAutoGenerated || property.IsRelated)
                    continue;
                if (modelDetails.Properties.Any(x => x.ForeignIdentity == property.Name))
                    continue;
                if (property.IsNullable)
                {
                    sb.Append("ALTER TABLE [TestTypes] ADD [Junk").Append(property.PropertySourceName).Append("] ");
                    MsSql.MsSqlEngine.WriteSqlTypeFromModel(sb, property, true);
                    sb.Insert(sb.Length - 4, "NOT ");
                    sb.Append("\r\n");
                }
            }
            var addJunkColumns = sb.ToString();

            ExecuteSql(context, addJunkColumns);

            _ = context.InitializeEngine<ITransactStoreEngine>(true);
        }
    }
}
