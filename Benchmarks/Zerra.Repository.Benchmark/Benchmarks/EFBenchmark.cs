// Copyright � KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Zerra.Map;
using Zerra.Repository.Benchmark.EFData;
using Zerra.Repository.Test;
using Zerra.Repository.MsSql;
using Zerra.Repository.Test.MsSql;

namespace Zerra.Repository.Benchmark.Benchmarks
{
    [MemoryDiagnoser]
    [MedianColumn, MinColumn, MaxColumn]
    [SimpleJob(warmupCount: 2, iterationCount: 5)]
    public class EFBenchmarks
    {
        private IRepo repo;
        TestTypesModel singleModel;
        EFTestTypesModel singleEF;
        EFDataContext reuseContext;
        //a different value each call, as an application's are, so a store compiles what it can't reuse
        int changing = -1_000_000;
        //the models a create of many saves, made before each iteration and deleted after it
        TestTypesModel[] createModels;
        EFTestTypesModel[] createEF;
        private const int createCount = 1000;

        [GlobalSetup]
        public void Setup()
        {
            var modelTypes = new[] { typeof(TestTypesModel), typeof(TestRelationsModel) };

            var engine = new MsSqlEngine(MsSqlTestSqlDataContext.ConnectionString);
            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, modelTypes);

            var repoSetup = Repo.New();
            repoSetup.AddProvider(new TransactStoreProvider<TestTypesModel>(engine));
            repoSetup.AddProvider(new TransactStoreProvider<TestRelationsModel>(engine));
            repo = repoSetup;

            for (var i = 0; i < 100; i++)
            {
                var relationBModel0 = TestRelationsModel.Create();
                repo.Create(relationBModel0);

                var model = TestTypesModel.Create();
                model.RelationAKey = relationBModel0.RelationAKey;
                repo.Create(model);

                for (var j = 0; j < 20; j++)
                {
                    var relationBModel = TestRelationsModel.Create();
                    relationBModel.RelationBKey = model.KeyA;
                    repo.Create(relationBModel);
                }
            }

            singleModel = repo.Query<TestTypesModel>().First();

            reuseContext = new EFDataContext();
            singleEF = reuseContext.TestTypes.First();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            reuseContext.Dispose();

            var builder = new SqlConnectionStringBuilder(MsSqlTestSqlDataContext.ConnectionString);
            var testDatabase = builder.InitialCatalog;
            builder.InitialCatalog = "master";
            var connectionStringForMaster = builder.ToString();
            using (var connection = new SqlConnection(connectionStringForMaster))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"IF EXISTS(SELECT[dbid] FROM master.dbo.sysdatabases where[name] = '{testDatabase}')\r\nBEGIN\r\nALTER DATABASE [{testDatabase}] SET single_user WITH ROLLBACK IMMEDIATE\r\nDROP DATABASE {testDatabase}\r\nEND";
                    _ = command.ExecuteNonQuery();
                }
            }
        }

        [Benchmark]
        public async Task<IReadOnlyCollection<TestTypesModel>> QueryMany_Zerra()
        {
            var results = await repo.Query<TestTypesModel>().ToArrayAsync();
            return results;
        }

        [Benchmark]
        public async Task<List<EFTestTypesModel>> QueryMany_EF()
        {
            using var context = new EFDataContext();
            var results = await EntityFrameworkQueryableExtensions.ToListAsync(context.TestTypes
                .AsNoTracking()
                );
            return results;
        }

        [Benchmark]
        public async Task<List<EFTestTypesModel>> QueryMany_EF_Reuse()
        {
            var results = await EntityFrameworkQueryableExtensions.ToListAsync(reuseContext.TestTypes
                .AsNoTracking()
                );
            return results;
        }

        [Benchmark]
        public async Task<IReadOnlyCollection<TestTypesModel>> QueryManyWhere_Zerra()
        {
            var results = await repo.Query<TestTypesModel>().Where(x => x.Int32Thing > 0 && x.StringThing.Contains("Hello")).ToArrayAsync();
            return results;
        }

        [Benchmark]
        public async Task<List<EFTestTypesModel>> QueryManyWhere_EF()
        {
            using var context = new EFDataContext();
            var results = await EntityFrameworkQueryableExtensions.ToListAsync(context.TestTypes
                .AsNoTracking()
                .Where(x => x.Int32Thing > 0 && x.StringThing.Contains("Hello"))
                );
            return results;
        }

        [Benchmark]
        public async Task<TestTypesModel> QueryFirst_Zerra()
        {
            var results = await repo.Query<TestTypesModel>().FirstOrDefaultAsync(x => x.StringThing.Contains("Hello"));
            return results;
        }

        [Benchmark]
        public async Task<EFTestTypesModel> QueryFirst_EF()
        {
            using var context = new EFDataContext();
            var results = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.TestTypes
                .AsNoTracking()
                .Where(results => results.StringThing.Contains("Hello"))
                );
            return results;
        }

        [Benchmark]
        public async Task<TestTypesModel> QueryFirstChanging_Zerra()
        {
            var value = changing--;
            var results = await repo.Query<TestTypesModel>().FirstOrDefaultAsync(x => x.StringThing.Contains("Hello") && x.Int32Thing != value);
            return results;
        }

        [Benchmark]
        public async Task<EFTestTypesModel> QueryFirstChanging_EF()
        {
            var value = changing--;
            using var context = new EFDataContext();
            var results = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.TestTypes
                .AsNoTracking()
                .Where(results => results.StringThing.Contains("Hello") && results.Int32Thing != value)
                );
            return results;
        }

        [Benchmark]
        public async Task<IReadOnlyCollection<TestTypesModel>> QueryManyIncludeOneToOne_Zerra()
        {
            var results = await repo.Query<TestTypesModel>(new Graph<TestTypesModel>(true, x => x.RelationA)).ToArrayAsync();
            return results;
        }

        [Benchmark]
        public async Task<List<EFTestTypesModel>> QueryManyIncludeOneToOne_EF()
        {
            using var context = new EFDataContext();
            var results = await EntityFrameworkQueryableExtensions.ToListAsync(context.TestTypes
                .AsNoTracking()
                .Include(x => x.RelationA)
                );
            return results;
        }

        [Benchmark]
        public async Task<IReadOnlyCollection<TestTypesModel>> QueryManyIncludeOneToMany_Zerra()
        {
            var results = await repo.Query<TestTypesModel>(new Graph<TestTypesModel>(true, x => x.RelationB)).ToArrayAsync();
            return results;
        }

        [Benchmark]
        public async Task<List<EFTestTypesModel>> QueryManyIncludeOneToMany_EF()
        {
            using var context = new EFDataContext();
            var results = await EntityFrameworkQueryableExtensions.ToListAsync(context.TestTypes
                .AsNoTracking()
                .Include(x => x.RelationB)
                );
            return results;
        }

        [Benchmark]
        public async Task<List<EFTestTypesModel>> QueryManyIncludeOneToMany_EF_Split()
        {
            using var context = new EFDataContext();
            var results = await EntityFrameworkQueryableExtensions.ToListAsync(context.TestTypes
                .AsNoTracking()
                .Include(x => x.RelationB)
                .AsSplitQuery()
                );
            return results;
        }

        [Benchmark]
        public async Task Update_Zerra()
        {
            await repo.UpdateAsync(singleModel);
        }

        [Benchmark]
        public async Task Update_EF()
        {
            using var context = new EFDataContext();
            context.Attach(singleEF).State = EntityState.Modified;
            _ = await context.SaveChangesAsync();
        }

        [Benchmark]
        public async Task UpdateChanging_Zerra()
        {
            singleModel.Int32Thing = changing--;
            await repo.UpdateAsync(singleModel);
        }

        [Benchmark]
        public async Task UpdateChanging_EF()
        {
            singleEF.Int32Thing = changing--;
            using var context = new EFDataContext();
            context.Attach(singleEF).State = EntityState.Modified;
            _ = await context.SaveChangesAsync();
        }

        [IterationSetup(Target = nameof(CreateMany_Zerra))]
        public void SetupCreateMany() => createModels = Enumerable.Range(0, createCount).Select(_ => TestTypesModel.Create()).ToArray();
        [IterationCleanup(Target = nameof(CreateMany_Zerra))]
        public void CleanupCreateMany()
        {
            var keys = createModels.Select(x => x.KeyA).ToArray();
            _ = repo.Delete<TestTypesModel>(x => keys.Contains(x.KeyA));
        }
        [Benchmark]
        public async Task CreateMany_Zerra()
        {
            await repo.CreateAsync<TestTypesModel>(createModels);
        }

        [IterationSetup(Target = nameof(CreateMany_EF))]
        public void SetupCreateManyEF() => createEF = Enumerable.Range(0, createCount).Select(_ => TestTypesModel.Create().Map<TestTypesModel, EFTestTypesModel>()).ToArray();
        [IterationCleanup(Target = nameof(CreateMany_EF))]
        public void CleanupCreateManyEF()
        {
            var keys = createEF.Select(x => x.KeyA).ToArray();
            _ = repo.Delete<TestTypesModel>(x => keys.Contains(x.KeyA));
        }
        [Benchmark]
        public async Task CreateMany_EF()
        {
            using var context = new EFDataContext();
            context.TestTypes.AddRange(createEF);
            _ = await context.SaveChangesAsync();
        }
    }
}
