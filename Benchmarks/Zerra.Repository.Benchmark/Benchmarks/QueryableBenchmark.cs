// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Zerra.Repository.Benchmark.EFData;
using Zerra.Repository.Test;
using Zerra.Repository.MsSql;
using Zerra.Repository.Test.MsSql;

namespace Zerra.Repository.Benchmark.Benchmarks
{
    //the same query through the IRepo methods and through IRepo.Queryable, on SQL Server
    [MemoryDiagnoser]
    [MedianColumn, MinColumn, MaxColumn]
    [SimpleJob(warmupCount: 2, iterationCount: 5)]
    public class QueryableBenchmarks
    {
        public sealed class TypesRow
        {
            public Guid KeyA { get; set; }
            public int Int32Thing { get; set; }
            public string StringThing { get; set; }
            public string Related { get; set; }
        }
        public sealed class TypesRows
        {
            public Guid KeyA { get; set; }
            public List<RelationRow> Items { get; set; }
        }
        public sealed class RelationRow
        {
            public string SomeValue { get; set; }
        }
        public sealed class TypesSummary
        {
            public bool Flag { get; set; }
            public int Rows { get; set; }
            public long Total { get; set; }
        }

        private IRepo repo;

        [GlobalSetup]
        public void Setup()
        {
            var modelTypes = new[] { typeof(TestTypesModel), typeof(TestRelationsModel) };

            var engine = new MsSqlEngine(MsSqlTestSqlDataContext.ConnectionString);
            CodeFirstGeneration.Generate(engine, DataStoreGenerationType.CodeFirst, modelTypes);

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
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            var builder = new SqlConnectionStringBuilder(MsSqlTestSqlDataContext.ConnectionString);
            var testDatabase = builder.InitialCatalog;
            builder.InitialCatalog = "master";
            using (var connection = new SqlConnection(builder.ToString()))
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
        public Task<TestTypesModel[]> ManyWhere_Queryable()
            => repo.Query<TestTypesModel>().Where(x => x.Int32Thing > 0 && x.StringThing.Contains("Hello")).ToArrayAsync();

        [Benchmark]
        public Task<TestTypesModel> First_Queryable()
            => repo.Query<TestTypesModel>().FirstOrDefaultAsync(x => x.StringThing.Contains("Hello"));

        [Benchmark]
        public Task<int> Count_Queryable()
            => repo.Query<TestTypesModel>().CountAsync(x => x.Int32Thing > 0);

        [Benchmark]
        public Task<TypesRow[]> SelectJoin_Queryable()
            => repo.Query<TestTypesModel>().Where(x => x.Int32Thing > 0).Select(x => new TypesRow { KeyA = x.KeyA, Int32Thing = x.Int32Thing, StringThing = x.StringThing, Related = x.RelationA.SomeValue }).ToArrayAsync();

        [Benchmark]
        public Task<TypesRows[]> SelectCollection_Queryable()
            => repo.Query<TestTypesModel>().Select(x => new TypesRows { KeyA = x.KeyA, Items = x.RelationB.Select(r => new RelationRow { SomeValue = r.SomeValue }).ToList() }).ToArrayAsync();

        [Benchmark]
        public Task<TypesSummary[]> Group_Queryable()
            => repo.Query<TestTypesModel>().GroupBy(x => x.RelationAKey).Select(g => new TypesSummary { Rows = g.Count(), Total = g.Sum(x => x.Int64Thing) }).ToArrayAsync();

        [Benchmark]
        public async Task<List<EFTestTypesModel>> ManyWhere_EF()
        {
            using var context = new EFDataContext();
            return await EntityFrameworkQueryableExtensions.ToListAsync(context.TestTypes.AsNoTracking().Where(x => x.Int32Thing > 0 && x.StringThing.Contains("Hello")));
        }

        [Benchmark]
        public async Task<EFTestTypesModel> First_EF()
        {
            using var context = new EFDataContext();
            return await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.TestTypes.AsNoTracking(), x => x.StringThing.Contains("Hello"));
        }

        [Benchmark]
        public async Task<int> Count_EF()
        {
            using var context = new EFDataContext();
            return await EntityFrameworkQueryableExtensions.CountAsync(context.TestTypes.AsNoTracking(), x => x.Int32Thing > 0);
        }

        [Benchmark]
        public async Task<TypesRow[]> SelectJoin_EF()
        {
            using var context = new EFDataContext();
            return await EntityFrameworkQueryableExtensions.ToArrayAsync(context.TestTypes.AsNoTracking().Where(x => x.Int32Thing > 0).Select(x => new TypesRow { KeyA = x.KeyA, Int32Thing = x.Int32Thing, StringThing = x.StringThing, Related = x.RelationA.SomeValue }));
        }

        [Benchmark]
        public async Task<TypesRows[]> SelectCollection_EF()
        {
            using var context = new EFDataContext();
            return await EntityFrameworkQueryableExtensions.ToArrayAsync(context.TestTypes.AsNoTracking().Select(x => new TypesRows { KeyA = x.KeyA, Items = x.RelationB.Select(r => new RelationRow { SomeValue = r.SomeValue }).ToList() }));
        }

        [Benchmark]
        public async Task<TypesSummary[]> Group_EF()
        {
            using var context = new EFDataContext();
            return await EntityFrameworkQueryableExtensions.ToArrayAsync(context.TestTypes.AsNoTracking().GroupBy(x => x.RelationAKey).Select(g => new TypesSummary { Rows = g.Count(), Total = g.Sum(x => x.Int64Thing) }));
        }
    }
}
