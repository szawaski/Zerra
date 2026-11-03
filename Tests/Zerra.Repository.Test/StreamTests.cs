// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Xunit;
using Zerra.Encryption;
using Zerra.Repository.Linq;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    //a query read with await foreach, each element given as it's read
    public static class StreamTests
    {
        [Entity("StreamParent")]
        public sealed class ParentModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Number { get; set; }
            [StoreProperties(false, 100)]
            public string? Name { get; set; }
            [Relation(nameof(ChildModel.ParentID))]
            public ChildModel[]? Children { get; set; }
        }

        [Entity("StreamChild")]
        public sealed class ChildModel
        {
            [Identity]
            public Guid ID { get; set; }
            public Guid ParentID { get; set; }
            public int Quantity { get; set; }
        }

        public static readonly Type[] ModelTypes = [typeof(ParentModel), typeof(ChildModel)];

        private const int count = 300;

        //the rows of each chunk OnGet is called on
        private sealed class ChunkRules<TModel>(ITransactStoreProvider<TModel> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<TModel>, TModel>(next) where TModel : class, new()
        {
            public readonly List<int> Chunks = new();
            public override IEnumerable OnGet(IEnumerable models, Graph? graph)
            {
                var list = models.Cast<object>().ToArray();
                lock (Chunks)
                    Chunks.Add(list.Length);
                return list;
            }
        }

        private static IRepo NewRepo(ITransactStoreEngine engine, ITransactStoreEngine? childEngine, out TransactStoreProvider<ParentModel> parents, out ChunkRules<ParentModel> parentRules, out ChunkRules<ChildModel> childRules)
        {
            var repo = Repo.New();
            parents = new TransactStoreProvider<ParentModel>(engine);
            parentRules = new ChunkRules<ParentModel>(parents);
            childRules = new ChunkRules<ChildModel>(new TransactStoreProvider<ChildModel>(childEngine ?? engine));
            repo.AddProvider<ParentModel>(parentRules);
            repo.AddProvider<ChildModel>(childRules);
            return repo;
        }
        private static void Clear(IRepo repo)
        {
            _ = repo.Delete<ChildModel>(x => x.Quantity >= 0);
            _ = repo.Delete<ParentModel>(x => x.Number >= 0);
        }
        private static ParentModel[] NewParents() => Enumerable.Range(0, count).Select(i =>
        {
            var id = Guid.NewGuid();
            return new ParentModel
            {
                ID = id,
                Number = i,
                Name = new string('n', i % 50),
                Children = [new ChildModel { ID = Guid.NewGuid(), ParentID = id, Quantity = i }, new ChildModel { ID = Guid.NewGuid(), ParentID = id, Quantity = i + 1000 }],
            };
        }).ToArray();

        private static async Task<List<T>> Read<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
        {
            var elements = new List<T>();
            await foreach (var element in query.AsAsyncEnumerable(cancellationToken))
                elements.Add(element);
            return elements;
        }

        public static async Task TestSequence(ITransactStoreEngine engine, bool holdsConnection)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var repo = NewRepo(engine, null, out _, out var parentRules, out var childRules);
            Clear(repo);
            var created = NewParents();
            await repo.CreateAsync<ParentModel>(created, new Graph<ParentModel>(true, x => x.Children), cancellationToken);

            //the elements are those the list has, in the same order when ordered
            var all = await Read(repo.Query<ParentModel>(), cancellationToken);
            Assert.Equal(created.Select(x => x.ID).Order(), all.Select(x => x.ID).Order());
            Assert.Equal(created.Where(x => x.Number % 3 == 0).OrderByDescending(x => x.Number).Skip(5).Take(40).Select(x => x.Number),
                (await Read(repo.Query<ParentModel>().Where(x => x.Number % 3 == 0).OrderByDescending(x => x.Number).Skip(5).Take(40), cancellationToken)).Select(x => x.Number));
            Assert.Equal(created.OrderBy(x => x.Number).Select(x => $"{x.Number}:{x.Name!.Length}"),
                (await Read(repo.Query<ParentModel>().OrderBy(x => x.Number).Select(x => new { x.Number, Length = x.Name!.Length }), cancellationToken)).Select(x => $"{x.Number}:{x.Length}"));
            var numbers = new List<int>();
            await foreach (var number in repo.Query<ParentModel>().Where(x => x.Number < 10).Select(x => x.Number).AsAsyncEnumerable())
                numbers.Add(number);
            Assert.Equal(Enumerable.Range(0, 10), numbers.Order());

            //relations read with the rows, and a SelectMany of them
            var withChildren = await Read(repo.Query<ParentModel>(new Graph<ParentModel>(true, x => x.Children)).Where(x => x.Number < 20), cancellationToken);
            Assert.Equal(20, withChildren.Count);
            Assert.All(withChildren, x => Assert.Equal([x.Number, x.Number + 1000], x.Children!.Select(c => c.Quantity).Order()));
            Assert.Equal(created.Where(x => x.Number < 5).SelectMany(x => x.Children!).Select(c => c.Quantity).Order(),
                (await Read(repo.Query<ParentModel>().Where(x => x.Number < 5).SelectMany(x => x.Children!).Select(c => c.Quantity), cancellationToken)).Order());

            //a query looked in, and a token given only when it's read
            var few = repo.Query<ParentModel>().Where(x => x.Number < 3).Select(x => x.ID);
            Assert.Equal([0, 1, 2], (await Read(repo.Query<ParentModel>().Where(x => few.Contains(x.ID)).Select(x => x.Number), cancellationToken)).Order());
            var tokenOnly = 0;
            await foreach (var model in repo.Query<ParentModel>().Where(x => x.Number < 4).AsAsyncEnumerable().WithCancellation(cancellationToken))
                tokenOnly++;
            Assert.Equal(4, tokenOnly);

            //a set of two queries
            Assert.Equal(created.Where(x => x.Number < 5 || x.Number > 290).Select(x => x.Number).Order(),
                (await Read(repo.Query<ParentModel>().Where(x => x.Number < 5).Select(x => x.Number).Union(repo.Query<ParentModel>().Where(x => x.Number > 290).Select(x => x.Number)), cancellationToken)).Order());

            //the rules see the models in chunks that fit the budget, measured as they come
            parentRules.Chunks.Clear();
            parentRules.StreamChunkBytes = 2000;
            Assert.Equal(count, (await Read(repo.Query<ParentModel>(), cancellationToken)).Count);
            Assert.True(parentRules.Chunks.Count > 5, $"{parentRules.Chunks.Count} chunks");
            Assert.Equal(count, parentRules.Chunks.Sum());
            parentRules.Chunks.Clear();
            parentRules.StreamChunkBytes = 1024 * 1024;
            _ = await Read(repo.Query<ParentModel>(), cancellationToken);
            Assert.Equal([count], parentRules.Chunks);
            //a list sees them all at once, without measuring
            parentRules.Chunks.Clear();
            parentRules.StreamChunkBytes = 2000;
            _ = await repo.Query<ParentModel>().ToListAsync(cancellationToken);
            Assert.Equal([count], parentRules.Chunks);
            parentRules.StreamChunkBytes = 1024 * 1024;

            //stopping early closes what the stream holds, or the connections would run out
            for (var i = 0; i < 200; i++)
            {
                await foreach (var model in repo.Query<ParentModel>().AsAsyncEnumerable(cancellationToken))
                    break;
            }
            Assert.Equal(count, await repo.Query<ParentModel>().CountAsync(cancellationToken));

            //cancelled while reading
            using (var cancellation = new CancellationTokenSource())
            {
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                {
                    await foreach (var model in repo.Query<ParentModel>().AsAsyncEnumerable(cancellation.Token))
                        cancellation.Cancel();
                });
            }
            //a token given when it's read cancels it too, linked with the one it was made with
            using (var cancellation = new CancellationTokenSource())
            {
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                {
                    await foreach (var model in repo.Query<ParentModel>().AsAsyncEnumerable(cancellationToken).WithCancellation(cancellation.Token))
                        cancellation.Cancel();
                });
            }

            //in a transaction the stream holds the connection, so what runs inside it is refused until it ends; chunks of one model keep the stream reading while each is given
            parentRules.StreamChunkBytes = 1;
            using (var transaction = repo.BeginTransaction())
            {
                var added = new ParentModel { ID = Guid.NewGuid(), Number = 5000, Name = "t" };
                var read = 0;
                await foreach (var model in repo.Query<ParentModel>().Where(x => x.Number < 3).AsAsyncEnumerable(cancellationToken))
                {
                    read++;
                    if (holdsConnection)
                    {
                        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.CreateAsync(added, cancellationToken: cancellationToken));
                        _ = Assert.Throws<InvalidOperationException>(transaction.Commit);
                    }
                }
                Assert.Equal(3, read);
                await repo.CreateAsync(added, cancellationToken: cancellationToken);
                Assert.Equal(count + 1, (await Read(repo.Query<ParentModel>(), cancellationToken)).Count);
                transaction.Commit();
            }
            parentRules.StreamChunkBytes = 1024 * 1024;
            Assert.Equal(count + 1, await repo.Query<ParentModel>().CountAsync(cancellationToken));
            _ = await repo.DeleteAsync<ParentModel>(x => x.Number == 5000, cancellationToken: cancellationToken);

            TestSync(repo, created, parentRules, holdsConnection);

            Clear(repo);
        }

        //AsEnumerable streams with foreach, and enumerating the query itself reads it all at once
        private static void TestSync(IRepo repo, ParentModel[] created, ChunkRules<ParentModel> parentRules, bool holdsConnection)
        {
            Assert.Equal(created.Select(x => x.ID).Order(), repo.Query<ParentModel>().AsEnumerable().Select(x => x.ID).Order());
            Assert.Equal(created.Where(x => x.Number % 3 == 0).OrderByDescending(x => x.Number).Skip(5).Take(40).Select(x => x.Number),
                repo.Query<ParentModel>().Where(x => x.Number % 3 == 0).OrderByDescending(x => x.Number).Skip(5).Take(40).AsEnumerable().Select(x => x.Number));
            Assert.Equal(created.OrderBy(x => x.Number).Select(x => x.Number * 2), repo.Query<ParentModel>().OrderBy(x => x.Number).Select(x => x.Number * 2).AsEnumerable());
            var withChildren = repo.Query<ParentModel>(new Graph<ParentModel>(true, x => x.Children)).Where(x => x.Number < 20).AsEnumerable().ToArray();
            Assert.Equal(20, withChildren.Length);
            Assert.All(withChildren, x => Assert.Equal([x.Number, x.Number + 1000], x.Children!.Select(c => c.Quantity).Order()));
            Assert.Equal(created.Where(x => x.Number < 5 || x.Number > 290).Select(x => x.Number).Order(),
                repo.Query<ParentModel>().Where(x => x.Number < 5).Select(x => x.Number).Union(repo.Query<ParentModel>().Where(x => x.Number > 290).Select(x => x.Number)).AsEnumerable().Order());
            var few = repo.Query<ParentModel>().Where(x => x.Number < 3).Select(x => x.ID);
            Assert.Equal([0, 1, 2], repo.Query<ParentModel>().Where(x => few.Contains(x.ID)).Select(x => x.Number).AsEnumerable().Order());
            //what follows AsEnumerable runs in memory, as with LINQ's
            Assert.Equal(created.Where(x => x.Name!.Length == 3).Select(x => x.Number).Order(), repo.Query<ParentModel>().AsEnumerable().Where(x => x.Name!.Length == 3).Select(x => x.Number).Order());
            //another provider's query is given as it is
            var plain = created.AsQueryable();
            Assert.Same(plain, plain.AsEnumerable());

            //the rules see a stream in chunks, and a foreach of the query or a list all at once
            parentRules.Chunks.Clear();
            parentRules.StreamChunkBytes = 2000;
            Assert.Equal(created.Length, repo.Query<ParentModel>().AsEnumerable().Count());
            Assert.True(parentRules.Chunks.Count > 5, $"{parentRules.Chunks.Count} chunks");
            parentRules.Chunks.Clear();
            var looped = 0;
            foreach (var model in repo.Query<ParentModel>())
                looped++;
            _ = repo.Query<ParentModel>().ToList();
            _ = repo.Query<ParentModel>().ToArray();
            Assert.Equal(created.Length, looped);
            Assert.Equal([created.Length, created.Length, created.Length], parentRules.Chunks);

            //stopping early closes what the stream holds
            for (var i = 0; i < 200; i++)
            {
                foreach (var model in repo.Query<ParentModel>().AsEnumerable())
                    break;
            }
            Assert.Equal(created.Length, repo.Query<ParentModel>().Count());

            //in a transaction a stream holds the connection; a foreach of the query reads it first, so a write inside it works
            parentRules.StreamChunkBytes = 1;
            using (var transaction = repo.BeginTransaction())
            {
                var added = new ParentModel { ID = Guid.NewGuid(), Number = 5001, Name = "s" };
                var read = 0;
                foreach (var model in repo.Query<ParentModel>().Where(x => x.Number < 3).AsEnumerable())
                {
                    read++;
                    if (holdsConnection)
                    {
                        _ = Assert.Throws<InvalidOperationException>(() => repo.Create(added));
                        _ = Assert.Throws<InvalidOperationException>(transaction.Commit);
                    }
                }
                Assert.Equal(3, read);
                foreach (var model in repo.Query<ParentModel>().Where(x => x.Number < 1))
                    repo.Create(added);
                transaction.Commit();
            }
            parentRules.StreamChunkBytes = 1024 * 1024;
            Assert.Equal(1, repo.Delete<ParentModel>(x => x.Number == 5001));
        }

        //children on another engine load for each chunk of parents, the chunk sized by the parents and what their children were
        public static async Task TestAcrossEngines(ITransactStoreEngine engine, ITransactStoreEngine childEngine)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var repo = NewRepo(engine, childEngine, out var parents, out _, out var childRules);
            Clear(repo);
            var created = NewParents();
            await repo.CreateAsync<ParentModel>(created, new Graph<ParentModel>(true, x => x.Children), cancellationToken);

            parents.StreamChunkBytes = 4000;
            childRules.Chunks.Clear();
            var streamed = await Read(repo.Query<ParentModel>(new Graph<ParentModel>(true, x => x.Children)).OrderBy(x => x.Number), cancellationToken);
            Assert.Equal(created.Select(x => x.Number), streamed.Select(x => x.Number));
            Assert.All(streamed, x => Assert.Equal([x.Number, x.Number + 1000], x.Children!.Select(c => c.Quantity).Order()));
            Assert.True(childRules.Chunks.Count > 5, $"{childRules.Chunks.Count} chunks");
            Assert.Equal(count * 2, childRules.Chunks.Sum());

            //a condition through the relation runs on the other engine first
            Assert.Equal(created.Where(x => x.Number < 7).Select(x => x.Number),
                (await Read(repo.Query<ParentModel>(new Graph<ParentModel>(true, x => x.Children)).Where(x => x.Children!.Any(c => c.Quantity < 7)).OrderBy(x => x.Number), cancellationToken)).Select(x => x.Number));

            //the same without async
            childRules.Chunks.Clear();
            Assert.Equal(created.Select(x => x.Number), repo.Query<ParentModel>(new Graph<ParentModel>(true, x => x.Children)).OrderBy(x => x.Number).AsEnumerable().Select(x => x.Number));
            Assert.True(childRules.Chunks.Count > 5, $"{childRules.Chunks.Count} chunks");
            Assert.Equal(count * 2, childRules.Chunks.Sum());

            //an order through the relation runs over the models in memory, so they load first
            Assert.Equal(created.OrderByDescending(x => x.Number).Select(x => x.Number),
                (await Read(repo.Query<ParentModel>(new Graph<ParentModel>(true, x => x.Children)).OrderByDescending(x => x.Children!.Max(c => c.Quantity)), cancellationToken)).Select(x => x.Number));

            //in a transaction a chunk's relations would wait on the connection the stream holds, so the models load first and a write inside the loop works
            using (var transaction = repo.BeginTransaction())
            {
                var read = 0;
                await foreach (var model in repo.Query<ParentModel>(new Graph<ParentModel>(true, x => x.Children)).Where(x => x.Number < 3).AsAsyncEnumerable(cancellationToken))
                {
                    read++;
                    Assert.Equal(2, model.Children!.Length);
                    model.Name = "t" + model.Number;
                    await repo.UpdateAsync(model, new Graph<ParentModel>(x => x.Name), cancellationToken);
                }
                Assert.Equal(3, read);
                foreach (var model in repo.Query<ParentModel>(new Graph<ParentModel>(true, x => x.Children)).Where(x => x.Number < 3).AsEnumerable())
                    Assert.Equal("t" + model.Number, model.Name);
                await transaction.CommitAsync(cancellationToken);
            }
            Assert.Equal(["t0", "t1", "t2"], (await repo.Query<ParentModel>().Where(x => x.Number < 3).OrderBy(x => x.Number).Select(x => x.Name).ToArrayAsync(cancellationToken))!);

            //a list loads them all at once
            childRules.Chunks.Clear();
            _ = await repo.Query<ParentModel>(new Graph<ParentModel>(true, x => x.Children)).ToListAsync(cancellationToken);
            Assert.Equal([count * 2], childRules.Chunks);
            parents.StreamChunkBytes = 1024 * 1024;

            Clear(repo);
        }
    }

    public class StreamMemoryTests
    {
        [Entity("StreamLayered")]
        public sealed class LayeredModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Number { get; set; }
            public string? Text { get; set; }
        }

        private static readonly IEncryptor encryptor = new ZerraEncryptor("stream-test", SymmetricAlgorithmType.AES_GCM);
        private sealed class Encrypting(ITransactStoreProvider<LayeredModel> next) : BaseTransactStoreEncryptionProvider<ITransactStoreProvider<LayeredModel>, LayeredModel>(next)
        {
            public override IEncryptor Encryptor => encryptor;
        }
        private sealed class Compressing(ITransactStoreProvider<LayeredModel> next) : BaseTransactStoreCompressionProvider<ITransactStoreProvider<LayeredModel>, LayeredModel>(next)
        {
        }
        private sealed class Dual(ITransactStoreProvider<LayeredModel> eventStore, ITransactStoreProvider<LayeredModel> table)
            : BaseDualEventStoreProvider<ITransactStoreProvider<LayeredModel>, ITransactStoreProvider<LayeredModel>, LayeredModel>(eventStore, table)
        {
        }

        public static TheoryData<string> Providers => new() { "encryption", "compression", "event store", "dual" };

        //each layer and store gives the models as they come, the same as its list
        [Theory]
        [MemberData(nameof(Providers))]
        public async Task LayersAndStores(string kind)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var engine = new MemoryEngine();
            ITransactStoreProvider<LayeredModel> provider = kind switch
            {
                "encryption" => new Encrypting(new TransactStoreProvider<LayeredModel>(engine)),
                "compression" => new Compressing(new TransactStoreProvider<LayeredModel>(engine)),
                "event store" => new EventStoreAsTransactStoreProvider<LayeredModel>(engine),
                _ => new Dual(new EventStoreAsTransactStoreProvider<LayeredModel>(new MemoryEngine()), new TransactStoreProvider<LayeredModel>(engine)),
            };
            var repo = Repo.New();
            repo.AddProvider(provider);
            var models = Enumerable.Range(0, 50).Select(i => new LayeredModel { ID = Guid.NewGuid(), Number = i, Text = "text " + i }).ToArray();
            await repo.CreateAsync<LayeredModel>(models, cancellationToken);

            //an event store reads the models a condition names by identity
            var ids = models.Select(x => x.ID).ToArray();
            var expected = models.Where(x => x.Number % 2 == 0).OrderBy(x => x.Number).Select(x => $"{x.Number}:{x.Text}").ToArray();
            var query = repo.Query<LayeredModel>().Where(x => ids.Contains(x.ID) && x.Number % 2 == 0).OrderBy(x => x.Number);
            var streamed = new List<string>();
            await foreach (var model in query.AsAsyncEnumerable(cancellationToken))
                streamed.Add($"{model.Number}:{model.Text}");
            Assert.Equal(expected, streamed);
            Assert.Equal(expected, query.AsEnumerable().Select(x => $"{x.Number}:{x.Text}"));
            Assert.Equal(expected, (await query.ToListAsync(cancellationToken)).Select(x => $"{x.Number}:{x.Text}"));
            //a projection is given as the next provider gives it
            Assert.Equal(models.Where(x => x.Number % 2 == 0).Select(x => x.Number), query.Select(x => x.Number).AsEnumerable());
            //the condition the layer swapped in is put back, so the same query reads the same again
            Assert.Equal(expected, query.AsEnumerable().Select(x => $"{x.Number}:{x.Text}"));
        }

        //a model's columns are their fixed sizes and its text, and its relations those of the rows it has
        [Fact]
        public void StreamSize()
        {
            var size = new LinqStreamSize(Zerra.Repository.Reflection.ModelAnalyzer.GetModel(typeof(StreamTests.ParentModel)));
            var id = Guid.NewGuid();
            var parent = new StreamTests.ParentModel { ID = id, Number = 1, Name = "abc" };
            //an object, a Guid, an int, and the text as an object of two bytes a character
            Assert.Equal(24 + 16 + 4 + 24 + 6, size.Columns(parent));
            Assert.Equal(0, size.Relations(parent));
            parent.Children = [new StreamTests.ChildModel { ID = Guid.NewGuid(), ParentID = id, Quantity = 1 }, new StreamTests.ChildModel { ID = Guid.NewGuid(), ParentID = id, Quantity = 2 }];
            Assert.Equal(2 * (24 + 16 + 16 + 4), size.Relations(parent));
            Assert.Equal(size.Columns(parent) + size.Relations(parent), size.WithRelations(parent));
            parent.Name = null;
            Assert.Equal(24 + 16 + 4, size.Columns(parent));
            //before any is read, text is its declared length
            Assert.Equal(24 + 16 + 4 + 24 + 200, size.Estimate());
            Assert.Equal(24 + 16 + 16 + 4, size.EstimateRelations(new Graph<StreamTests.ParentModel>(true, x => x.Children)));
            Assert.Equal(0, size.EstimateRelations(new Graph<StreamTests.ParentModel>(true)));
            Assert.Equal(0, size.EstimateRelations(null));

            //every kind of column has its fixed size, and bytes their length
            var types = new LinqStreamSize(Zerra.Repository.Reflection.ModelAnalyzer.GetModel(typeof(TestTypesModel)));
            var model = TestTypesModel.Create();
            model.BytesThing = null;
            var withoutBytes = types.Columns(model);
            model.BytesThing = new byte[1000];
            Assert.Equal(withoutBytes + 24 + 1000, types.Columns(model));
            Assert.True(types.Estimate() > 0);
        }

        //an engine that fails when the elements are first read gives the error, and the stream closes what it opened
        [Fact]
        public async Task StreamFails()
        {
            var repo = Repo.New();
            repo.AddProvider<StreamTests.ParentModel>(new LayerOverFailing(new TransactStoreProvider<StreamTests.ParentModel>(new Zerra.Repository.MySql.MySqlEngine("Server=localhost;Port=1;Database=None;Connection Timeout=1"))));
            _ = Assert.ThrowsAny<Exception>(() => repo.Query<StreamTests.ParentModel>().AsEnumerable().ToArray());
            _ = await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await foreach (var model in repo.Query<StreamTests.ParentModel>().AsAsyncEnumerable(TestContext.Current.CancellationToken))
                {
                }
            });
        }
        private sealed class LayerOverFailing(ITransactStoreProvider<StreamTests.ParentModel> next) : BaseTransactStoreEncryptionProvider<ITransactStoreProvider<StreamTests.ParentModel>, StreamTests.ParentModel>(next)
        {
            public override IEncryptor Encryptor => encryptor;
        }

        [Fact]
        public async Task Memory()
        {
            await StreamTests.TestSequence(new MemoryEngine(), false);
            await StreamTests.TestAcrossEngines(new MemoryEngine(), new MemoryEngine());
        }
    }
}
