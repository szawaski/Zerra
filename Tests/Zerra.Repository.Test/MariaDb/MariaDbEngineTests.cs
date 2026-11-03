// Copyright � KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using System.Text;
using Zerra.Repository.Reflection;
using Zerra.Repository.MySql;
using MySqlConnector;
using Zerra.Repository.MariaDb;

namespace Zerra.Repository.Test.MariaDb
{
    public class MariaDbEngineTests
    {
        private static int ExecuteSql(string connectionString, string sql)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    return command.ExecuteNonQuery();
                }
            }
        }

        private static void DropDatabase(string connectionString)
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            var testDatabase = builder.Database;
            builder.Database = "sys";
            var connectionStringForMaster = builder.ToString();
            using (var connection = new MySqlConnection(connectionStringForMaster))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT CONCAT('KILL ', id, ';') FROM INFORMATION_SCHEMA.PROCESSLIST WHERE `db` = '{testDatabase}'; DROP DATABASE IF EXISTS {testDatabase};";
                    _ = command.ExecuteNonQuery();
                }
            }
        }

        private static IDisposable HoldParityTable(string connectionString)
        {
            var connection = new MySqlConnection(connectionString);
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "LOCK TABLES `QueryParity` WRITE";
                _ = command.ExecuteNonQuery();
            }
            return new QueryParityTests.HeldTable(() =>
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UNLOCK TABLES";
                    _ = command.ExecuteNonQuery();
                }
                connection.Dispose();
            });
        }

        [Fact]
        public async Task TestSequence()
        {
            var connectionString = MariaDbTestSqlDataContext.ConnectionString;
            var engine = new MariaDbEngine(connectionString);

            DropDatabase(connectionString);

            var modelTypes = new[] { typeof(TestTypesModel), typeof(TestRelationsModel) }.Concat(RelationPersistTests.ModelTypes).Concat(QueryParityTests.ModelTypes).Concat(TransactionTests.ModelTypes).Concat(BatchWriteTests.ModelTypes).Concat(CompositeRelationTests.ModelTypes).Concat(StreamTests.ModelTypes).ToArray();

            CodeFirstGeneration.Generate(engine, DataStoreGenerationType.CodeFirst, modelTypes);
            RepoTest.AssertSchemaMatchesModels(engine, modelTypes);

            RepoTest.TestSequenceTransactStore(engine);
            await RepoTest.TestSequenceTransactStoreAsync(engine);
            RelationPersistTests.TestSequence(engine);
            await RelationPersistTests.TestSequenceAsync(engine);
            QueryParityTests.TestSequence(engine);
            await QueryParityTests.TestSequenceAsync(engine);
            await QueryParityTests.TestCancellationAsync(engine, () => HoldParityTable(connectionString));
            TransactionTests.TestSequence(engine, false);
            await TransactionTests.TestSequenceAsync(engine, false);
            BatchWriteTests.TestSequence(engine, true);
            await BatchWriteTests.TestSequenceAsync(engine, true);
            CompositeRelationTests.TestSequence(engine, true);
            CompositeRelationTests.TestAcrossEngines(engine, new Zerra.Repository.Memory.MemoryEngine());
            await CompositeRelationTests.TestSequenceAsync(engine);
            await StreamTests.TestSequence(engine, true);
            await StreamTests.TestAcrossEngines(engine, new Zerra.Repository.Memory.MemoryEngine());
            TransactionTests.TestAcrossEngines(engine);
            TransactionTests.TestSameStore(engine, new MariaDbEngine(connectionString));
            SchemaChangeTests.Test(engine, table => ExecuteSql(connectionString, $"DROP TABLE IF EXISTS `{table}`"));

            const string changeColumn = "ALTER TABLE `TestTypes` MODIFY `Int32Thing` bigint NULL";
            const string addColumn = "ALTER TABLE `TestTypes` ADD `DummyToMakeNullable` int NOT NULL";
            const string dropColumn = "ALTER TABLE `TestTypes` DROP COLUMN `ByteThing`";
            _ = ExecuteSql(connectionString, changeColumn);
            _ = ExecuteSql(connectionString, addColumn);
            _ = ExecuteSql(connectionString, dropColumn);

            CodeFirstGeneration.Generate(engine, DataStoreGenerationType.CodeFirst, modelTypes);
            RepoTest.AssertSchemaMatchesModels(engine, modelTypes);

            var sb = new StringBuilder();
            var modelDetails = ModelAnalyzer.GetModel(typeof(TestTypesModel));
            foreach (var property in modelDetails.Members)
            {
                if (property.IsIdentity || property.IsIdentityAutoGenerated || property.IsRelated)
                    continue;
                if (modelDetails.Members.Any(x => x.ForeignIdentities?.Contains(property.Name) == true))
                    continue;
                _ = sb.Append("ALTER TABLE `TestTypes` DROP COLUMN `").Append(property.StoreName).Append("`;\r\n");
            }
            var dropAllColumns = sb.ToString();

            _ = ExecuteSql(connectionString, dropAllColumns);

            CodeFirstGeneration.Generate(engine, DataStoreGenerationType.CodeFirst, modelTypes);
            RepoTest.AssertSchemaMatchesModels(engine, modelTypes);

            _ = sb.Clear();
            foreach (var property in modelDetails.Members)
            {
                if (property.IsIdentity || property.IsIdentityAutoGenerated || property.IsRelated)
                    continue;
                if (modelDetails.Members.Any(x => x.ForeignIdentities?.Contains(property.Name) == true))
                    continue;
                if (property.IsNullable)
                {
                    _ = sb.Append("ALTER TABLE `TestTypes` ADD `Junk").Append(property.StoreName).Append("` ");
                    MySqlEngine.WriteSqlTypeFromModel(sb, property);
                    _ = sb.Insert(sb.Length - 4, "NOT ");
                    _ = sb.Append(";\r\n");
                }
            }
            var addJunkColumns = sb.ToString();

            _ = ExecuteSql(connectionString, addJunkColumns);

            CodeFirstGeneration.Generate(engine, DataStoreGenerationType.CodeFirst, modelTypes);
            RepoTest.AssertSchemaMatchesModels(engine, modelTypes);

            DropDatabase(connectionString);
        }
    }
}
