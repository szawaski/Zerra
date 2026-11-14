// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Zerra.Repository.Benchmark.EFData;
using Zerra.Repository.MsSql;
using Zerra.Repository.Test;
using Zerra.Repository.Test.MsSql;

namespace Zerra.Repository.Benchmark.Benchmarks
{
    //a large read as a list and as a stream, with and without a rule layer that sees the models in chunks
    [MemoryDiagnoser]
    [MedianColumn, MinColumn, MaxColumn]
    [SimpleJob(warmupCount: 2, iterationCount: 5)]
    public class StreamBenchmarks
    {
        private const int rows = 10_000;

        private IRepo repo;
        private IRepo rulesRepo;

        private sealed class PassRules(ITransactStoreProvider<TestTypesModel> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<TestTypesModel>, TestTypesModel>(next)
        {
        }

        [GlobalSetup]
        public void Setup()
        {
            var engine = new MsSqlEngine(MsSqlTestSqlDataContext.ConnectionString);
            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, [typeof(TestTypesModel), typeof(TestRelationsModel)]);

            var repoSetup = Repo.New();
            repoSetup.AddProvider(new TransactStoreProvider<TestTypesModel>(engine));
            repoSetup.AddProvider(new TransactStoreProvider<TestRelationsModel>(engine));
            repo = repoSetup;

            var rulesSetup = Repo.New();
            rulesSetup.AddProvider<TestTypesModel>(new PassRules(new TransactStoreProvider<TestTypesModel>(engine)));
            rulesSetup.AddProvider(new TransactStoreProvider<TestRelationsModel>(engine));
            rulesRepo = rulesSetup;

            repo.CreateMany<TestTypesModel>(Enumerable.Range(0, rows).Select(_ => TestTypesModel.Create()).ToArray());
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            var builder = new SqlConnectionStringBuilder(MsSqlTestSqlDataContext.ConnectionString);
            var testDatabase = builder.InitialCatalog;
            builder.InitialCatalog = "master";
            using var connection = new SqlConnection(builder.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"IF EXISTS(SELECT[dbid] FROM master.dbo.sysdatabases where[name] = '{testDatabase}')\r\nBEGIN\r\nALTER DATABASE [{testDatabase}] SET single_user WITH ROLLBACK IMMEDIATE\r\nDROP DATABASE {testDatabase}\r\nEND";
            _ = command.ExecuteNonQuery();
        }

        [Benchmark]
        public async Task<int> List_Zerra()
        {
            var models = await RepoQueryableExtensions.ToListAsync(repo.Query<TestTypesModel>());
            return models.Count;
        }

        [Benchmark]
        public async Task<int> Stream_Zerra()
        {
            var count = 0;
            await foreach (var model in RepoQueryableExtensions.AsAsyncEnumerable(repo.Query<TestTypesModel>()))
                count++;
            return count;
        }

        [Benchmark]
        public int ListSync_Zerra() => repo.Query<TestTypesModel>().ToList().Count;

        [Benchmark]
        public int StreamSync_Zerra()
        {
            var count = 0;
            foreach (var model in repo.Query<TestTypesModel>().AsEnumerable())
                count++;
            return count;
        }

        [Benchmark]
        public async Task<int> ListRules_Zerra()
        {
            var models = await RepoQueryableExtensions.ToListAsync(rulesRepo.Query<TestTypesModel>());
            return models.Count;
        }

        [Benchmark]
        public async Task<int> StreamRules_Zerra()
        {
            var count = 0;
            await foreach (var model in RepoQueryableExtensions.AsAsyncEnumerable(rulesRepo.Query<TestTypesModel>()))
                count++;
            return count;
        }

        [Benchmark]
        public async Task<int> List_EF()
        {
            using var context = new EFDataContext();
            var models = await EntityFrameworkQueryableExtensions.ToListAsync(context.TestTypes.AsNoTracking());
            return models.Count;
        }

        [Benchmark]
        public async Task<int> Stream_EF()
        {
            using var context = new EFDataContext();
            var count = 0;
            await foreach (var model in EntityFrameworkQueryableExtensions.AsAsyncEnumerable(context.TestTypes.AsNoTracking()))
                count++;
            return count;
        }
    }
}
