// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Zerra.Repository.MongoDB;

namespace Zerra.Repository.Test.MongoDB
{
    public class MongoDBEngineTests
    {
        private static void DropDatabase(string connectionString)
        {
            var url = MongoUrl.Create(connectionString);
            new MongoClient(url).DropDatabase(url.DatabaseName);
        }

        //MongoDB reads never wait on a lock, so the commands are held by a failpoint, which fails them after the wait so nothing is written;
        //turning it off waits for the commands it holds, so the wait is only long enough to cancel in
        private static IDisposable HoldParityCollection(string connectionString)
        {
            var admin = new MongoClient(MongoUrl.Create(connectionString)).GetDatabase("admin");
            _ = admin.RunCommand<BsonDocument>(new BsonDocument
            {
                { "configureFailPoint", "failCommand" },
                { "mode", "alwaysOn" },
                { "data", new BsonDocument
                    {
                        { "failCommands", new BsonArray { "aggregate", "insert", "update", "delete", "findAndModify" } },
                        { "blockConnection", true },
                        { "blockTimeMS", 2000 },
                        { "errorCode", 11601 }
                    }
                }
            });
            return new QueryParityTests.HeldTable(() => admin.RunCommand<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } }));
        }

        [Fact]
        public async Task TestSequence()
        {
            var connectionString = MongoDBTestDataContext.ConnectionString;
            var engine = new MongoDBEngine(connectionString);

            DropDatabase(connectionString);

            var modelTypes = new[] { typeof(TestTypesModel), typeof(TestRelationsModel) }.Concat(RelationPersistTests.ModelTypes).Concat(QueryParityTests.ModelTypes).Concat(TransactionTests.ModelTypes).Concat(BatchWriteTests.ModelTypes).Concat(CompositeRelationTests.ModelTypes).Concat(StreamTests.ModelTypes).Concat(SchemaTests.ModelTypes).ToArray();

            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, modelTypes);
            RepoTest.AssertSchemaMatchesModels(engine, modelTypes);

            RepoTest.TestSequenceTransactStore(engine);
            await RepoTest.TestSequenceTransactStoreAsync(engine);
            RelationPersistTests.TestSequence(engine);
            await RelationPersistTests.TestSequenceAsync(engine);
            QueryParityTests.TestSequence(engine);
            await QueryParityTests.TestSequenceAsync(engine);
            await QueryParityTests.TestCancellationAsync(engine, () => HoldParityCollection(connectionString));
            TransactionTests.TestSequence(engine, false);
            await TransactionTests.TestSequenceAsync(engine, false);
            BatchWriteTests.TestSequence(engine, true);
            await BatchWriteTests.TestSequenceAsync(engine, true);
            CompositeRelationTests.TestSequence(engine, false);
            CompositeRelationTests.TestAcrossEngines(engine, new Zerra.Repository.Memory.MemoryEngine());
            await CompositeRelationTests.TestSequenceAsync(engine);
            await StreamTests.TestSequence(engine, false);
            CharComparisonTests.TestSequence(engine);
            SchemaTests.TestSequence(engine);
            await SchemaTests.TestSequenceAsync(engine);
            await StreamTests.TestAcrossEngines(engine, new Zerra.Repository.Memory.MemoryEngine());
            TransactionTests.TestAcrossEngines(engine);
            TransactionTests.TestSameStore(engine, new MongoDBEngine(connectionString));
            SchemaChangeTests.Test(engine, collection => new MongoClient(MongoUrl.Create(connectionString)).GetDatabase(MongoUrl.Create(connectionString).DatabaseName).DropCollection(collection));

            DropDatabase(connectionString);
        }
    }
}
