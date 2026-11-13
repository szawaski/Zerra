// Copyright � KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Npgsql;
using System.Text;
using Zerra.Repository.Reflection;
using Zerra.Repository.PostgreSql;

namespace Zerra.Repository.Test.PostgreSql
{
    public class PostgreSqlEngineTests
    {
        private static int ExecuteSql(string connectionString, string sql)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    return command.ExecuteNonQuery();
                }
            }
        }

        private static long ExecuteScalar(string connectionString, string sql)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    return Convert.ToInt64(command.ExecuteScalar());
                }
            }
        }

        private static void DropDatabase(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var testDatabase = builder.Database;
            builder.Database = "postgres";
            var connectionStringForMaster = builder.ToString();
            using (var connection = new NpgsqlConnection(connectionStringForMaster))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname='{testDatabase}';";
                    _ = command.ExecuteNonQuery();
                    command.CommandText = $"DROP DATABASE IF EXISTS {testDatabase};";
                    _ = command.ExecuteNonQuery();
                }
            }
        }


        private static IDisposable HoldParityTable(string connectionString)
        {
            var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "BEGIN; LOCK TABLE queryparity IN ACCESS EXCLUSIVE MODE";
                _ = command.ExecuteNonQuery();
            }
            return new QueryParityTests.HeldTable(() =>
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "COMMIT";
                    _ = command.ExecuteNonQuery();
                }
                connection.Dispose();
            });
        }

        [Fact]
        public async Task TestSequence()
        {
            var connectionString = PostgreSqlTestSqlDataContext.ConnectionString;
            var engine = new PostgreSqlEngine(connectionString);

            DropDatabase(connectionString);

            var modelTypes = new[] { typeof(TestTypesModel), typeof(TestRelationsModel) }.Concat(RelationPersistTests.ModelTypes).Concat(QueryParityTests.ModelTypes).Concat(TransactionTests.ModelTypes).Concat(BatchWriteTests.ModelTypes).Concat(CompositeRelationTests.ModelTypes).Concat(StreamTests.ModelTypes).Concat(SchemaTests.ModelTypes).ToArray();

            //without CreateDataStore a missing database throws
            _ = Assert.Throws<InvalidOperationException>(() => CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, modelTypes));

            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, modelTypes);
            RepoTest.AssertSchemaMatchesModels(engine, modelTypes);
            DatabaseFirstTests.TestSequence(engine, modelTypes);

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
            CharComparisonTests.TestSequence(engine);
            LayeredGraphTests.TestSequence(engine);
            await LayeredGraphTests.TestSequenceAsync(engine);
            SchemaTests.TestSequence(engine);
            await SchemaTests.TestSequenceAsync(engine);
            await StreamTests.TestAcrossEngines(engine, new Zerra.Repository.Memory.MemoryEngine());
            TransactionTests.TestAcrossEngines(engine);
            TransactionTests.TestSameStore(engine, new PostgreSqlEngine(connectionString));
            SchemaChangeTests.Test(engine, table => ExecuteSql(connectionString, $"DROP TABLE IF EXISTS {table.ToLowerInvariant()}"));
            MigrationTests.Test(engine, table => ExecuteSql(connectionString, $"DROP TABLE IF EXISTS {table.ToLowerInvariant()}"));
            DatePrecisionTests.Test(engine);
            MigrationTests.TestKeptObjects(engine, () =>
            {
                _ = ExecuteSql(connectionString, "ALTER TABLE migrationdefaults ALTER COLUMN amount SET DEFAULT 7");
                _ = ExecuteSql(connectionString, "ALTER TABLE migrationdefaults ALTER COLUMN note SET DEFAULT 'x'");
                _ = ExecuteSql(connectionString, "ALTER TABLE migrationdefaults ALTER COLUMN removed SET DEFAULT 3");
            }, () =>
            {
                Assert.Equal(3, ExecuteScalar(connectionString, "SELECT COUNT(1) FROM information_schema.columns WHERE table_name = 'migrationdefaults' AND column_default IS NOT NULL"));
            });

            const string changeColumn = "ALTER TABLE testtypes ALTER COLUMN int32thing TYPE bigint; ALTER TABLE testtypes ALTER COLUMN int32thing DROP NOT NULL;";
            const string addColumn = "ALTER TABLE testtypes ADD dummytodrop int NOT NULL";
            const string dropColmn = "ALTER TABLE testtypes DROP COLUMN bytething";
            _ = ExecuteSql(connectionString, changeColumn);
            _ = ExecuteSql(connectionString, addColumn);
            _ = ExecuteSql(connectionString, dropColmn);

            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, modelTypes);
            RepoTest.AssertSchemaMatchesModels(engine, modelTypes);

            var sb = new StringBuilder();
            var modelDetails = ModelAnalyzer.GetModel(typeof(TestTypesModel));
            foreach (var property in modelDetails.Members)
            {
                if (property.IsIdentity || property.IsIdentityAutoGenerated || property.IsRelated)
                    continue;
                if (modelDetails.Members.Any(x => x.ForeignIdentities?.Contains(property.Name) == true))
                    continue;
                _ = sb.Append("ALTER TABLE testtypes DROP COLUMN ").Append(property.StoreName.ToLower()).Append(";\r\n");
            }
            var dropAllColumns = sb.ToString();

            _ = ExecuteSql(connectionString, dropAllColumns);

            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, modelTypes);
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
                    _ = sb.Append("ALTER TABLE testtypes ADD Junk").Append(property.StoreName).Append(' ');
                    PostgreSqlEngine.WriteSqlTypeFromModel(sb, property);
                    PostgreSqlEngine.WriteTypeEndingFromModel(sb, property);
                    _ = sb.Insert(sb.Length - 4, "NOT ");
                    _ = sb.Append(";\r\n");
                }
            }
            var addJunkColumns = sb.ToString();

            _ = ExecuteSql(connectionString, addJunkColumns);

            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, modelTypes);
            RepoTest.AssertSchemaMatchesModels(engine, modelTypes);

            DropDatabase(connectionString);
        }
    }
}
