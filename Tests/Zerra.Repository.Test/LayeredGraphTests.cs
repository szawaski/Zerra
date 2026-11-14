// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Xunit;
using Zerra.Encryption;
using Zerra.Repository.Linq;
using Zerra.Repository.Memory;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    //a graph through a layer on a related model reads in the one statement: the layer's condition is on the relation's rows, and its OnGet treats them after
    public static class LayeredGraphTests
    {
        private sealed class Marking(ITransactStoreProvider<TestRelationsModel> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<TestRelationsModel>, TestRelationsModel>(next)
        {
            public override LambdaExpression? WhereExpression(Graph? graph) => (Expression<Func<TestRelationsModel, bool>>)(x => x.SomeValue != "hidden");
            public override IEnumerable OnGet(IEnumerable models, Graph? graph)
            {
                foreach (TestRelationsModel model in models)
                    model.SomeValue += " inner";
                return models;
            }
        }

        private sealed class Dropping(ITransactStoreProvider<TestRelationsModel> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<TestRelationsModel>, TestRelationsModel>(next)
        {
            public override LambdaExpression? WhereExpression(Graph? graph) => (Expression<Func<TestRelationsModel, bool>>)(x => x.SomeValue != "hidden too");
            public override IEnumerable OnGet(IEnumerable models, Graph? graph)
            {
                var kept = new List<TestRelationsModel>();
                foreach (TestRelationsModel model in models)
                {
                    if (model.SomeValue.StartsWith("dropped"))
                        continue;
                    model.SomeValue += " outer";
                    kept.Add(model);
                }
                return kept;
            }
        }

        private static readonly IEncryptor encryptor = new ZerraEncryptor("layered-graph-test", SymmetricAlgorithmType.AES_GCM);
        private sealed class Encrypting(ITransactStoreProvider<TestRelationsModel> next) : BaseTransactStoreEncryptionProvider<ITransactStoreProvider<TestRelationsModel>, TestRelationsModel>(next)
        {
            public override IEncryptor Encryptor => encryptor;
            public override Graph<TestRelationsModel>? Properties => new(x => x.SomeValue);
        }

        private static readonly Graph<TestTypesModel> graph = new(x => x.KeyA, x => x.RelationA, x => x.RelationB);

        private static (TestTypesModel[] Parents, TestRelationsModel[] Related) Seed(IRepo repo)
        {
            TestRelationsModel Related(string value, Guid? parent = null)
            {
                var model = TestRelationsModel.Create();
                model.SomeValue = value;
                model.RelationBKey = parent;
                repo.Create(model);
                return model;
            }
            var parents = new[] { TestTypesModel.Create(), TestTypesModel.Create(), TestTypesModel.Create() };
            var related = new List<TestRelationsModel>();
            parents[0].RelationAKey = Add(Related("shown")).RelationAKey;
            parents[1].RelationAKey = Add(Related("hidden")).RelationAKey;
            parents[2].RelationAKey = Add(Related("dropped")).RelationAKey;
            foreach (var parent in parents)
                repo.Create(parent);
            _ = Add(Related("shown", parents[0].KeyA));
            _ = Add(Related("hidden", parents[0].KeyA));
            _ = Add(Related("hidden too", parents[0].KeyA));
            _ = Add(Related("dropped", parents[0].KeyA));
            _ = Add(Related("dropped", parents[1].KeyA));
            return (parents, related.ToArray());

            TestRelationsModel Add(TestRelationsModel model)
            {
                related.Add(model);
                return model;
            }
        }

        private static void Clean(IRepo repo, TestTypesModel[] parents, TestRelationsModel[] related)
        {
            foreach (var parent in parents)
                repo.Delete(parent);
            foreach (var model in related)
                repo.Delete(model);
        }

        private static void AssertRead(TestTypesModel[] parents, TestTypesModel[] read, string suffix)
        {
            Assert.Equal(parents.Length, read.Length);
            var first = read.Single(x => x.KeyA == parents[0].KeyA);
            Assert.Equal("shown" + suffix, first.RelationA.SomeValue);
            Assert.Equal(["shown" + suffix], first.RelationB.Select(x => x.SomeValue));
            var second = read.Single(x => x.KeyA == parents[1].KeyA);
            Assert.Null(second.RelationA);
            Assert.Empty(second.RelationB);
            var third = read.Single(x => x.KeyA == parents[2].KeyA);
            Assert.Null(third.RelationA);
            Assert.Empty(third.RelationB);
        }

        public static void TestSequence(ITransactStoreEngine engine)
        {
            var counting = new CountingEngine(engine);
            var plain = Repo.New();
            plain.AddProvider(new TransactStoreProvider<TestTypesModel>(counting));
            plain.AddProvider(new TransactStoreProvider<TestRelationsModel>(counting));
            var (parents, related) = Seed(plain);
            try
            {
                var keys = parents.Select(x => x.KeyA).ToArray();
                var layered = Repo.New();
                layered.AddProvider(new TransactStoreProvider<TestTypesModel>(counting));
                layered.AddProvider<TestRelationsModel>(new Dropping(new Marking(new TransactStoreProvider<TestRelationsModel>(counting))));

                counting.Reads = 0;
                AssertRead(parents, layered.Query<TestTypesModel>(graph).Where(x => keys.Contains(x.KeyA)).ToArray(), " inner outer");
                Assert.Equal(1, counting.Reads);

                counting.Reads = 0;
                AssertRead(parents, layered.Query<TestTypesModel>(graph).Where(x => keys.Contains(x.KeyA)).AsEnumerable().ToArray(), " inner outer");
                Assert.Equal(1, counting.Reads);

                counting.Reads = 0;
                var one = layered.Query<TestTypesModel>(graph).Where(x => x.KeyA == parents[0].KeyA).FirstOrDefault();
                Assert.Equal("shown inner outer", one!.RelationA.SomeValue);
                Assert.Equal(1, counting.Reads);

                //a layer under a layer reads through, and the next provider's models are treated first
                var encrypted = Repo.New();
                encrypted.AddProvider(new TransactStoreProvider<TestTypesModel>(counting));
                encrypted.AddProvider<TestRelationsModel>(new Marking(new Encrypting(new TransactStoreProvider<TestRelationsModel>(counting))));
                var secret = TestRelationsModel.Create();
                secret.SomeValue = "secret";
                secret.RelationBKey = parents[2].KeyA;
                encrypted.Create(secret);
                related = [.. related, secret];
                counting.Reads = 0;
                var read = encrypted.Query<TestTypesModel>(graph).Where(x => x.KeyA == parents[2].KeyA).ToArray().Single();
                Assert.Equal(["secret inner"], read.RelationB.Select(x => x.SomeValue));
                Assert.Equal(1, counting.Reads);
                Assert.NotEqual("secret", plain.Query<TestRelationsModel>().Where(x => x.RelationAKey == secret.RelationAKey).Single().SomeValue);
            }
            finally
            {
                Clean(plain, parents, related);
            }
        }

        public static async Task TestSequenceAsync(ITransactStoreEngine engine)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var counting = new CountingEngine(engine);
            var plain = Repo.New();
            plain.AddProvider(new TransactStoreProvider<TestTypesModel>(counting));
            plain.AddProvider(new TransactStoreProvider<TestRelationsModel>(counting));
            var (parents, related) = Seed(plain);
            try
            {
                var keys = parents.Select(x => x.KeyA).ToArray();
                var layered = Repo.New();
                layered.AddProvider(new TransactStoreProvider<TestTypesModel>(counting));
                layered.AddProvider<TestRelationsModel>(new Dropping(new Marking(new TransactStoreProvider<TestRelationsModel>(counting))));

                counting.Reads = 0;
                AssertRead(parents, await layered.Query<TestTypesModel>(graph).Where(x => keys.Contains(x.KeyA)).ToArrayAsync(cancellationToken), " inner outer");
                Assert.Equal(1, counting.Reads);

                counting.Reads = 0;
                var streamed = new List<TestTypesModel>();
                await foreach (var model in layered.Query<TestTypesModel>(graph).Where(x => keys.Contains(x.KeyA)).AsAsyncEnumerable(cancellationToken))
                    streamed.Add(model);
                AssertRead(parents, streamed.ToArray(), " inner outer");
                Assert.Equal(1, counting.Reads);

                counting.Reads = 0;
                var one = await layered.Query<TestTypesModel>(graph).Where(x => x.KeyA == parents[1].KeyA).FirstOrDefaultAsync(cancellationToken);
                Assert.Null(one!.RelationA);
                Assert.Empty(one.RelationB);
                Assert.Equal(1, counting.Reads);
            }
            finally
            {
                Clean(plain, parents, related);
            }
        }

        //counts the reads, so a test sees how many statements a query took
        private sealed class CountingEngine(ITransactStoreEngine inner) : ITransactStoreEngine
        {
            public int Reads;
            public object? ExecuteLinq<TModel, TResult>(LinqQuery query, ModelDetail modelDetail) where TModel : class, new() { Reads++; return inner.ExecuteLinq<TModel, TResult>(query, modelDetail); }
            public Task<object?> ExecuteLinqAsync<TModel, TResult>(LinqQuery query, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new() { Reads++; return inner.ExecuteLinqAsync<TModel, TResult>(query, modelDetail, cancellationToken); }
            public IEnumerable<TResult> ExecuteLinqStream<TModel, TResult>(LinqQuery query, ModelDetail modelDetail) where TModel : class, new() { Reads++; return inner.ExecuteLinqStream<TModel, TResult>(query, modelDetail); }
            public IAsyncEnumerable<TResult> ExecuteLinqStreamAsync<TModel, TResult>(LinqQuery query, ModelDetail modelDetail, CancellationToken cancellationToken = default) where TModel : class, new() { Reads++; return inner.ExecuteLinqStreamAsync<TModel, TResult>(query, modelDetail, cancellationToken); }
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
    }

    public class LayeredGraphMemoryTests
    {
        [Fact]
        public async Task Memory()
        {
            LayeredGraphTests.TestSequence(new MemoryEngine());
            await LayeredGraphTests.TestSequenceAsync(new MemoryEngine());
        }

        [Entity("LayeredRoot")]
        public sealed class Root
        {
            [Identity]
            public Guid ID { get; set; }
            [Relation(nameof(Item.RootID))]
            public List<Item>? Items { get; set; }
        }
        [Entity("LayeredItem")]
        public sealed class Item
        {
            [Identity]
            public Guid ID { get; set; }
            public Guid RootID { get; set; }
            public int Number { get; set; }
            public Guid? TagID { get; set; }
            [Relation(nameof(TagID))]
            public Tag? Tag { get; set; }
        }
        [Entity("LayeredTag")]
        public sealed class Tag
        {
            [Identity]
            public Guid ID { get; set; }
            public string? Name { get; set; }
        }

        private sealed class ItemRules(ITransactStoreProvider<Item> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<Item>, Item>(next)
        {
            public List<string?> TagsSeen = new();
            public override LambdaExpression? WhereExpression(Graph? graph) => (Expression<Func<Item, bool>>)(x => x.Number != 2);
            public override IEnumerable OnGet(IEnumerable models, Graph? graph)
            {
                foreach (Item model in models)
                    TagsSeen.Add(model.Tag?.Name);
                return models;
            }
        }
        private sealed class TagRules(ITransactStoreProvider<Tag> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<Tag>, Tag>(next)
        {
            public override LambdaExpression? WhereExpression(Graph? graph) => (Expression<Func<Tag, bool>>)(x => x.Name != "secret");
            public override IEnumerable OnGet(IEnumerable models, Graph? graph)
            {
                //copies in place of the models read, which are matched to their parents by key
                return models.Cast<Tag>().Select(x => new Tag { ID = x.ID, Name = x.Name + "!" }).ToArray();
            }
        }

        //each level's layer applies, the deepest first, in one read, as reading each level through its layer gives it
        [Fact]
        public void Nested()
        {
            var tags = new[] { new Tag { ID = Guid.NewGuid(), Name = "a" }, new Tag { ID = Guid.NewGuid(), Name = "secret" } };
            var roots = new[] { new Root { ID = Guid.NewGuid() }, new Root { ID = Guid.NewGuid() } };
            var items = new[]
            {
                new Item { ID = Guid.NewGuid(), RootID = roots[0].ID, Number = 1, TagID = tags[0].ID },
                new Item { ID = Guid.NewGuid(), RootID = roots[0].ID, Number = 2, TagID = tags[0].ID },
                new Item { ID = Guid.NewGuid(), RootID = roots[0].ID, Number = 3, TagID = tags[1].ID },
                new Item { ID = Guid.NewGuid(), RootID = roots[1].ID, Number = 4 },
            };

            //the roots on another engine read their relations through the layers
            foreach (var acrossEngines in new[] { false, true })
            {
                var engine = new MemoryEngine();
                var repo = Repo.New();
                repo.AddProvider(new TransactStoreProvider<Root>(acrossEngines ? new MemoryEngine() : engine));
                var itemRules = new ItemRules(new TransactStoreProvider<Item>(engine));
                repo.AddProvider<Item>(itemRules);
                repo.AddProvider<Tag>(new TagRules(new TransactStoreProvider<Tag>(engine)));
                repo.CreateMany<Tag>(tags);
                repo.CreateMany<Root>(roots);
                repo.CreateMany<Item>(items);

                var graph = new Graph<Root>(true, x => x.Items!.Select(i => i.Number), x => x.Items!.Select(i => i.Tag));
                var signature = graph.GetChildGraph(nameof(Root.Items))!.Signature;
                var read = repo.Query<Root>(graph).ToArray();
                //the keys read for the layers don't change the caller's graph
                Assert.Equal(signature, graph.GetChildGraph(nameof(Root.Items))!.Signature);
                //a layer's condition on a single relation also hides the rows whose related model it hides
                var first = read.Single(x => x.ID == roots[0].ID);
                Assert.Equal("a!", Assert.Single(first.Items!).Tag!.Name);
                Assert.Equal(1, first.Items![0].Number);
                Assert.Equal([4], read.Single(x => x.ID == roots[1].ID).Items!.Select(x => x.Number));
                Assert.Equal([null, "a!"], itemRules.TagsSeen.Order());
            }

            //the relation is read in the statement, with the layer's condition on its rows
            var planned = Repo.New();
            planned.AddProvider(new TransactStoreProvider<Root>(new MemoryEngine()));
            var query = new LinqQuery<Root, Root>(planned.Query<Root>().Expression, null, null, new Graph<Root>(true, x => x.Items));
            query.Graph = new LinqLayeredGraph(query.Graph!, (type, _) => type == typeof(Item) ? (Expression<Func<Item, bool>>)(x => x.Number != 2) : null);
            var plan = LinqPlan.Read(query, ModelAnalyzer.GetModel(typeof(Root)));
            Assert.NotNull(Assert.Single(plan.Pieces).Where);
        }
    }
}
