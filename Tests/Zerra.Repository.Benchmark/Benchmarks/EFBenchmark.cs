// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Zerra.Repository.Benchmark.EFData;
using Zerra.Repository.Test;

namespace Zerra.Repository.Benchmark.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 5)]
    public class EFBenchmarks
    {
        private IRepo repo;
        TestTypesModel singleModel;
        EFTestTypesModel singleEF;
        EFDataContext reuseContext;

        [GlobalSetup]
        public void Setup()
        {
            var modelTypes = new[] { typeof(TestTypesModel), typeof(TestRelationsModel) };

            CodeFirstGeneration.Generate<MsSqlTestSqlDataContext>(DataStoreGenerationType.CodeFirst, modelTypes);

            var repoSetup = Repo.New();
            repoSetup.AddProvider(new TransactStoreProvider<MsSqlTestSqlDataContext, TestTypesModel>());
            repoSetup.AddProvider(new TransactStoreProvider<MsSqlTestSqlDataContext, TestRelationsModel>());
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

            singleModel = repo.First<TestTypesModel>();

            reuseContext = new EFDataContext();
            singleEF = reuseContext.TestTypes.First();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            reuseContext.Dispose();

            var context = new MsSqlTestSqlDataContext();
            var builder = new SqlConnectionStringBuilder(context.GetConnectionString());
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
            var results = await repo.ManyAsync<TestTypesModel>();
            return results;
        }

        [Benchmark]
        public async Task<List<EFTestTypesModel>> QueryMany_EF()
        {
            using var context = new EFDataContext();
            var results = await context.TestTypes
                .AsNoTracking()
                .ToListAsync();
            return results;
        }

        [Benchmark]
        public async Task<List<EFTestTypesModel>> QueryMany_EF_Reuse()
        {
            var results = await reuseContext.TestTypes
                .AsNoTracking()
                .ToListAsync();
            return results;
        }

        [Benchmark]
        public async Task<IReadOnlyCollection<TestTypesModel>> QueryManyWhere_Zerra()
        {
            var results = await repo.ManyAsync<TestTypesModel>(x => x.Int32Thing > 0 && x.StringThing.Contains("Hello"));
            return results;
        }

        [Benchmark]
        public async Task<List<EFTestTypesModel>> QueryManyWhere_EF()
        {
            using var context = new EFDataContext();
            var results = await context.TestTypes
                .AsNoTracking()
                .Where(x => x.Int32Thing > 0 && x.StringThing.Contains("Hello"))
                .ToListAsync();
            return results;
        }

        [Benchmark]
        public async Task<TestTypesModel> QueryFirst_Zerra()
        {
            var results = await repo.FirstAsync<TestTypesModel>(x => x.StringThing.Contains("Hello"));
            return results;
        }

        [Benchmark]
        public async Task<EFTestTypesModel> QueryFirst_EF()
        {
            using var context = new EFDataContext();
            var results = await context.TestTypes
                .AsNoTracking()
                .Where(results => results.StringThing.Contains("Hello"))
                .FirstOrDefaultAsync();
            return results;
        }

        [Benchmark]
        public async Task<IReadOnlyCollection<TestTypesModel>> QueryManyIncludeOneToOne_Zerra()
        {
            var graph = new Graph<TestTypesModel>(
                true,
                x => x.RelationA
            );
            var results = await repo.ManyAsync<TestTypesModel>(graph);
            return results;
        }

        [Benchmark]
        public async Task<List<EFTestTypesModel>> QueryManyIncludeOneToOne_EF()
        {
            using var context = new EFDataContext();
            var results = await context.TestTypes
                .AsNoTracking()
                .Include(x => x.RelationA)
                .ToListAsync();
            return results;
        }

        [Benchmark]
        public async Task<IReadOnlyCollection<TestTypesModel>> QueryManyIncludeOneToMany_Zerra()
        {
            var graph = new Graph<TestTypesModel>(
                true,
                x => x.RelationB
            );
            var results = await repo.ManyAsync<TestTypesModel>(graph);
            return results;
        }

        [Benchmark]
        public async Task<List<EFTestTypesModel>> QueryManyIncludeOneToMany_EF()
        {
            using var context = new EFDataContext();
            var results = await context.TestTypes
                .AsNoTracking()
                .Include(x => x.RelationB)
                .ToListAsync();
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
    }
}
