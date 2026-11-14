// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Xunit;
using Zerra.Repository.Linq;
using Zerra.Repository.Memory;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    //an engine that implements only what the interface requires, so the defaults write many models one at a time
    public class EngineDefaultsTests
    {
        [Entity("DefaultsModel")]
        public sealed class DefaultsModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Number { get; set; }
        }

        [Entity("DefaultsGenerated")]
        public sealed class GeneratedModel
        {
            [Identity(true)]
            public int ID { get; set; }
            public int Number { get; set; }
        }

        private sealed class RequiredOnly(ITransactStoreEngine inner) : ITransactStoreEngine
        {
            public object? ExecuteLinq<TModel, TResult>(LinqQuery query, ModelDetail modelDetail) where TModel : class, new() => inner.ExecuteLinq<TModel, TResult>(query, modelDetail);
            public Task<object?> ExecuteLinqAsync<TModel, TResult>(LinqQuery query, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new() => inner.ExecuteLinqAsync<TModel, TResult>(query, modelDetail, cancellationToken);
            public IEnumerable<TResult> ExecuteLinqStream<TModel, TResult>(LinqQuery query, ModelDetail modelDetail) where TModel : class, new() => inner.ExecuteLinqStream<TModel, TResult>(query, modelDetail);
            public IAsyncEnumerable<TResult> ExecuteLinqStreamAsync<TModel, TResult>(LinqQuery query, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new() => inner.ExecuteLinqStreamAsync<TModel, TResult>(query, modelDetail, cancellationToken);
            public object ExecuteInsertGetIdentities<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new() => inner.ExecuteInsertGetIdentities(model, graph, modelDetail);
            public bool ExecuteInsert<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new() => inner.ExecuteInsert(model, graph, modelDetail);
            public bool ExecuteUpdate<TModel>(TModel model, Graph? graph, ModelDetail modelDetail) where TModel : class, new() => inner.ExecuteUpdate(model, graph, modelDetail);
            public int ExecuteDelete<TModel>(ICollection ids, ModelDetail modelDetail) where TModel : class, new() => inner.ExecuteDelete<TModel>(ids, modelDetail);
            public int ExecuteUpdateWhere<TModel>(TModel model, Graph graph, LambdaExpression where, ModelDetail modelDetail) where TModel : class, new() => inner.ExecuteUpdateWhere(model, graph, where, modelDetail);
            public int ExecuteUpdateWhere<TModel>(LambdaExpression set, LambdaExpression where, ModelDetail modelDetail) where TModel : class, new() => inner.ExecuteUpdateWhere<TModel>(set, where, modelDetail);
            public int ExecuteDeleteWhere<TModel>(LambdaExpression where, ModelDetail modelDetail) where TModel : class, new() => inner.ExecuteDeleteWhere<TModel>(where, modelDetail);
            public Task<object> ExecuteInsertGetIdentitiesAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new() => inner.ExecuteInsertGetIdentitiesAsync(model, graph, modelDetail, cancellationToken);
            public Task<bool> ExecuteInsertAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new() => inner.ExecuteInsertAsync(model, graph, modelDetail, cancellationToken);
            public Task<bool> ExecuteUpdateAsync<TModel>(TModel model, Graph? graph, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new() => inner.ExecuteUpdateAsync(model, graph, modelDetail, cancellationToken);
            public Task<int> ExecuteDeleteAsync<TModel>(ICollection ids, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new() => inner.ExecuteDeleteAsync<TModel>(ids, modelDetail, cancellationToken);
            public Task<int> ExecuteUpdateWhereAsync<TModel>(TModel model, Graph graph, LambdaExpression where, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new() => inner.ExecuteUpdateWhereAsync(model, graph, where, modelDetail, cancellationToken);
            public Task<int> ExecuteUpdateWhereAsync<TModel>(LambdaExpression set, LambdaExpression where, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new() => inner.ExecuteUpdateWhereAsync<TModel>(set, where, modelDetail, cancellationToken);
            public Task<int> ExecuteDeleteWhereAsync<TModel>(LambdaExpression where, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new() => inner.ExecuteDeleteWhereAsync<TModel>(where, modelDetail, cancellationToken);
            public IDataStoreGenerationPlan BuildStoreGenerationPlan(bool createDataStore, bool columns, bool constraints, bool dropColumns, ICollection<ModelDetail> modelDetail) => inner.BuildStoreGenerationPlan(createDataStore, columns, constraints, dropColumns, modelDetail);
        }

        [Fact]
        public async Task ManyModelsOneAtATime()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var engine = new RequiredOnly(new MemoryEngine());
            Assert.False(((ITransactStoreEngine)engine).SupportsTransactions);
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<DefaultsModel>(engine));
            repo.AddProvider(new TransactStoreProvider<GeneratedModel>(engine));

            var models = Enumerable.Range(0, 5).Select(i => new DefaultsModel { ID = Guid.NewGuid(), Number = i }).ToArray();
            repo.CreateMany<DefaultsModel>(models[..3]);
            await repo.CreateManyAsync<DefaultsModel>(models[3..], cancellationToken);
            foreach (var model in models)
                model.Number += 10;
            repo.UpdateMany<DefaultsModel>(models[..3]);
            await repo.UpdateManyAsync<DefaultsModel>(models[3..], cancellationToken);
            Assert.Equal([10, 11, 12, 13, 14], repo.Query<DefaultsModel>().OrderBy(x => x.Number).Select(x => x.Number).ToArray());

            var generated = Enumerable.Range(0, 4).Select(i => new GeneratedModel { Number = i }).ToArray();
            repo.CreateMany<GeneratedModel>(generated[..2]);
            await repo.CreateManyAsync<GeneratedModel>(generated[2..], cancellationToken);
            Assert.Equal(4, generated.Select(x => x.ID).Distinct().Count());
            Assert.DoesNotContain(0, generated.Select(x => x.ID));
            Assert.Equal(generated.Select(x => x.ID).Order(), repo.Query<GeneratedModel>().Select(x => x.ID).ToArray().Order());
        }

        //members are found by name, by lower-case name, and by the name in the store
        [Fact]
        public void ModelLookups()
        {
            var model = ModelAnalyzer.GetModel(typeof(QueryParityTests.ParityModel));
            Assert.Equal("Title", model.GetMemberLower("title").Name);
            Assert.True(model.TryGetPropertyLower("number", out var number));
            Assert.Equal("Number", number!.Name);
            Assert.False(model.TryGetPropertyLower("nothing", out _));
            Assert.True(model.TryGetPropertyBySourceName("Label", out var label));
            Assert.Equal("Title", label!.Name);
            Assert.False(model.TryGetPropertyBySourceName("Title", out _));
            Assert.True(model.TryGetPropertyBySourceNameLower("label", out label));
            Assert.Equal("Title", label!.Name);
            _ = Assert.ThrowsAny<Exception>(() => model.GetMember("Nothing"));
            _ = Assert.ThrowsAny<Exception>(() => model.GetMemberLower("nothing"));
            Assert.Contains(nameof(QueryParityTests.ParityModel), model.ToString());
        }
    }
}
