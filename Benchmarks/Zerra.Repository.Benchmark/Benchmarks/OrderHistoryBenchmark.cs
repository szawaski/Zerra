// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;
using Zerra.Repository.Memory;
using Zerra.Repository.MsSql;
using Zerra.Repository.Test;
using Zerra.Repository.Test.MsSql;

namespace Zerra.Repository.Benchmark.Benchmarks
{
    //ordered pages on SQL Server and in memory, and the history of a model in a memory event store
    [MemoryDiagnoser]
    [MedianColumn, MinColumn, MaxColumn]
    [SimpleJob(warmupCount: 2, iterationCount: 5)]
    public class OrderHistoryBenchmarks
    {
        private IRepo sql = null!;
        private IRepo memory = null!;
        private IRepo history = null!;
        private Guid id;

        [GlobalSetup]
        public void Setup()
        {
            var engine = new MsSqlEngine(MsSqlTestSqlDataContext.ConnectionString);
            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, [typeof(TestTypesModel), typeof(TestRelationsModel)]);
            var sqlSetup = Repo.New();
            sqlSetup.AddProvider(new TransactStoreProvider<TestTypesModel>(engine));
            sql = sqlSetup;

            var memorySetup = Repo.New();
            memorySetup.AddProvider(new TransactStoreProvider<TestTypesModel>(new MemoryEngine()));
            memory = memorySetup;

            for (var i = 0; i < 200; i++)
            {
                sql.Create(TestTypesModel.Create());
                memory.Create(TestTypesModel.Create());
            }

            var historySetup = Repo.New();
            historySetup.AddProvider(new EventStoreAsTransactStoreProvider<TestTypesModel>(new MemoryEngine(), 100));
            history = historySetup;
            var model = TestTypesModel.Create();
            history.Create(model);
            for (var i = 0; i < 300; i++)
            {
                model.Int32Thing = i;
                history.Update(model);
            }
            id = model.KeyA;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            var builder = new SqlConnectionStringBuilder(MsSqlTestSqlDataContext.ConnectionString);
            var database = builder.InitialCatalog;
            builder.InitialCatalog = "master";
            using var connection = new SqlConnection(builder.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]";
            _ = command.ExecuteNonQuery();
        }

        [Benchmark]
        public Task<TestTypesModel[]> OrderedPageSql_Zerra() => sql.Query<TestTypesModel>().Where(x => x.Int32Thing > int.MinValue).OrderBy(x => x.Int32Thing).ThenByDescending(x => x.StringThing).Skip(10).Take(50).ToArrayAsync();

        [Benchmark]
        public Task<TestTypesModel[]> OrderedPageMemory_Zerra() => memory.Query<TestTypesModel>().Where(x => x.Int32Thing > int.MinValue).OrderBy(x => x.Int32Thing).ThenByDescending(x => x.StringThing).Skip(10).Take(50).ToArrayAsync();

        [Benchmark]
        public Task<TestTypesModel?> HistoryCurrent_Zerra() => history.Query<TestTypesModel>().FirstOrDefaultAsync(x => x.KeyA == id);

        [Benchmark]
        public async Task<int> HistoryAll_Zerra() => (await history.TemporalQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.KeyA == id).ToListAsync()).Count;

        [Benchmark]
        public Task<TestTypesModel?> HistoryAsOf_Zerra() => history.TemporalQuery<TestTypesModel>(numberTo: 250).FirstOrDefaultAsync(x => x.KeyA == id);

        [Benchmark]
        public async Task<int> HistoryNewestPage_Zerra() => (await history.TemporalQuery<TestTypesModel>().Where(x => x.KeyA == id).Take(10).ToListAsync()).Count;

        [Benchmark]
        public async Task<int> EventsAll_Zerra() => (await history.EventQuery<TestTypesModel>(TemporalOrder.Oldest).Where(x => x.Model.KeyA == id).ToListAsync()).Count;
    }
}
