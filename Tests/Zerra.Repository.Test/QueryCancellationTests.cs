// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Reflection;
using Xunit;
using Zerra.Encryption;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    public class QueryCancellationTests
    {
        [Entity("CancelParent")]
        public sealed class Parent
        {
            [Identity]
            public Guid ID { get; set; }
            public int Number { get; set; }
            [StoreProperties(false, 200)]
            public string? Note { get; set; }
            [Relation(nameof(Child.ParentID))]
            public Child[]? Children { get; set; }
        }

        [Entity("CancelChild")]
        public sealed class Child
        {
            [Identity]
            public Guid ID { get; set; }
            public Guid ParentID { get; set; }
        }

        //a provider on no engine, so a relation goes through it and a layer calls it, keeping what it was given
        private sealed class Recording<TModel> : ITransactStoreProvider<TModel> where TModel : class, new()
        {
            public LinqQuery? Query { get; private set; }
            public CancellationToken QueryToken { get; private set; }
            public Persist? Persist { get; private set; }
            public CancellationToken PersistToken { get; private set; }
            object? ITransactStoreProvider.Query(LinqQuery query) => throw new NotSupportedException();
            Task<object?> ITransactStoreProvider.QueryAsync(LinqQuery query, CancellationToken cancellationToken)
            {
                Query = query;
                QueryToken = cancellationToken;
                return Task.FromResult<object?>(Array.Empty<TModel>());
            }
            void ITransactStoreProvider.Persist(Persist persist) => throw new NotSupportedException();
            Task ITransactStoreProvider.PersistAsync(Persist persist, CancellationToken cancellationToken)
            {
                Persist = persist;
                PersistToken = cancellationToken;
                return Task.CompletedTask;
            }
        }

        private sealed class Rules(ITransactStoreProvider<Parent> next) : BaseTransactStoreRuleProvider<ITransactStoreProvider<Parent>, Parent>(next)
        {
        }
        private sealed class Encrypting(ITransactStoreProvider<Parent> next) : BaseTransactStoreEncryptionProvider<ITransactStoreProvider<Parent>, Parent>(next)
        {
            public override IEncryptor Encryptor { get; } = new ZerraEncryptor("cancel-test", SymmetricAlgorithmType.AES_GCM);
            public override Graph<Parent>? Properties => new(x => x.Note);
        }
        private sealed class Compressing(ITransactStoreProvider<Parent> next) : BaseTransactStoreCompressionProvider<ITransactStoreProvider<Parent>, Parent>(next)
        {
            public override Graph<Parent>? Properties => new(x => x.Note);
        }

        //every asynchronous ending, history query, and change takes the token, last as handlers do
        [Fact]
        public void EveryAsyncQueryTakesAToken()
        {
            var endings = typeof(RepoQueryableExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(x => x.Name.EndsWith("Async", StringComparison.Ordinal)).ToArray();
            var history = typeof(IRepo).GetMethods().Where(x => x.Name.EndsWith("Async", StringComparison.Ordinal)).ToArray();
            Assert.NotEmpty(endings);
            Assert.NotEmpty(history);
            foreach (var method in endings.Concat(history))
            {
                var last = method.GetParameters()[^1];
                Assert.True(last.ParameterType == typeof(CancellationToken) && last.IsOptional, method.ToString());
            }
        }

        [Fact]
        public async Task CancelledQueryThrows()
        {
            var engine = new MemoryEngine();
            var plain = Repo.New();
            plain.AddProvider(new TransactStoreProvider<Parent>(engine));
            var events = Repo.New();
            events.AddProvider(new EventStoreAsTransactStoreProvider<Parent>(new MemoryEngine()));
            var parent = new Parent { ID = Guid.NewGuid(), Number = 1 };
            plain.Create(parent);
            events.Create(parent);

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            var token = cancelled.Token;
            foreach (var repo in new[] { plain, events })
            {
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.Query<Parent>().Where(x => x.ID == parent.ID).ToArrayAsync(token));
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.Query<Parent>().Where(x => x.ID == parent.ID).ToListAsync(token));
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.Query<Parent>().FirstOrDefaultAsync(x => x.ID == parent.ID, token));
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.Query<Parent>().SingleOrDefaultAsync(x => x.ID == parent.ID, token));
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.Query<Parent>().CountAsync(x => x.ID == parent.ID, token));
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.Query<Parent>().AnyAsync(x => x.ID == parent.ID, token));
            }
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => plain.Query<Parent>().AllAsync(x => x.ID == parent.ID, token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => events.EventManyAsync<Parent>(TemporalOrder.Oldest, (DateTime?)null, null, null, null, x => x.ID == parent.ID, cancellationToken: token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => events.TemporalManyAsync<Parent>(TemporalOrder.Oldest, (DateTime?)null, null, null, null, x => x.ID == parent.ID, cancellationToken: token));

            //a change doesn't run either
            var added = new Parent { ID = Guid.NewGuid(), Number = 2 };
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => plain.CreateAsync(added, token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => events.CreateAsync(added, token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => plain.UpdateAsync(new Parent { ID = parent.ID, Number = 9 }, token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => events.UpdateAsync(new Parent { ID = parent.ID, Number = 9 }, token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => plain.UpdateAsync<Parent>(x => x.ID == parent.ID, new Parent { Number = 9 }, new Graph<Parent>(x => x.Number), token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => plain.UpdateAsync<Parent>(x => x.ID == parent.ID, x => new Parent { Number = x.Number + 1 }, token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => plain.DeleteAsync(parent, token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => events.DeleteAsync(parent, token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => plain.DeleteAsync<Parent>(x => x.ID == parent.ID, token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => plain.DeleteByIDAsync<Parent>(parent.ID, token));
            var kept = plain.Query<Parent>().ToArray().Single();
            Assert.Equal(1, kept.Number);
            Assert.Equal(parent.ID, kept.ID);

            //without one nothing changes
            Assert.Single((await plain.Query<Parent>().ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)));
            Assert.True(await plain.Query<Parent>().AnyAsync(cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task RelationLoadCarriesTheToken()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<Parent>(new MemoryEngine()));
            var children = new Recording<Child>();
            repo.AddProvider(children);
            repo.Create(new Parent { ID = Guid.NewGuid(), Number = 1 });

            using var source = new CancellationTokenSource();
            _ = await repo.Query<Parent>(new Graph<Parent>(true, x => x.Children)).ToArrayAsync(source.Token);
            Assert.Equal(source.Token, children.QueryToken);
        }

        [Fact]
        public async Task PersistAsyncTakesTheToken()
        {
            var next = new Recording<Parent>();
            var repo = Repo.New();
            repo.AddProvider(next);
            using var first = new CancellationTokenSource();
            using var second = new CancellationTokenSource();
            var persist = new Persist<Parent>(PersistOperation.Create, null, null, [new Parent { ID = Guid.NewGuid() }], null, null);

            //the token belongs to the call, so the same persist can run again with another
            await ((IRepoInternal)repo).PersistAsync(persist, first.Token);
            Assert.Equal(first.Token, next.PersistToken);
            await ((IRepoInternal)repo).PersistAsync(persist, second.Token);
            Assert.Equal(second.Token, next.PersistToken);
            await ((IRepoInternal)repo).PersistAsync(persist, TestContext.Current.CancellationToken);
            Assert.Equal(TestContext.Current.CancellationToken, next.PersistToken);
        }

        [Fact]
        public async Task RelationChangeCarriesTheToken()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<Parent>(new MemoryEngine()));
            var children = new Recording<Child>();
            repo.AddProvider(children);
            var parent = new Parent { ID = Guid.NewGuid(), Number = 1, Children = [new Child { ID = Guid.NewGuid() }] };
            var graph = new Graph<Parent>(true, x => x.Children);

            using var source = new CancellationTokenSource();
            await repo.CreateAsync(parent, graph, source.Token);
            Assert.Equal(source.Token, children.PersistToken);

            await repo.UpdateAsync(parent, graph, source.Token);
            Assert.Equal(source.Token, children.QueryToken);
        }

        //a layer makes its own persist for the next provider, which keeps the token
        [Fact]
        public async Task LayersCarryTheToken()
        {
            using var source = new CancellationTokenSource();
            var token = source.Token;
            foreach (var kind in new[] { 0, 1, 2 })
            {
                var next = new Recording<Parent>();
                ITransactStoreProvider<Parent> layer = kind switch
                {
                    0 => new Rules(next),
                    1 => new Encrypting(next),
                    _ => new Compressing(next),
                };
                var repo = Repo.New();
                repo.AddProvider(layer);
                var model = new Parent { ID = Guid.NewGuid(), Number = 1, Note = "note" };

                await repo.CreateAsync(model, token);
                Assert.Equal(token, next.PersistToken);
                await repo.UpdateAsync(model, token);
                Assert.Equal(token, next.PersistToken);
                await repo.DeleteAsync(model, token);
                Assert.Equal(token, next.PersistToken);
                _ = await repo.UpdateAsync<Parent>(x => x.Number == 1, new Parent { Number = 2 }, new Graph<Parent>(x => x.Number), token);
                Assert.Equal(token, next.PersistToken);
                _ = await repo.UpdateAsync<Parent>(x => x.Number == 1, x => new Parent { Number = x.Number + 1 }, token);
                Assert.Equal(token, next.PersistToken);
                _ = await repo.DeleteAsync<Parent>(x => x.Number == 1, token);
                Assert.Equal(token, next.PersistToken);
                _ = await repo.Query<Parent>().ToArrayAsync(token);
                Assert.Equal(token, next.QueryToken);
            }
        }

        [Fact]
        public async Task AggregateTakesTheToken()
        {
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            var aggregate = new TestAggregate(Guid.NewGuid(), new MemoryEngine());
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => aggregate.Append(new TestAggregateCreated { Name = "n", Amount = 1 }, false, cancelled.Token));
            Assert.False(await aggregate.Rebuild(cancellationToken: TestContext.Current.CancellationToken));
            await aggregate.Append(new TestAggregateCreated { Name = "n", Amount = 1 }, cancellationToken: TestContext.Current.CancellationToken);
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => aggregate.Rebuild(null, null, cancelled.Token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => aggregate.RebuildOneEvent(cancelled.Token));
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => aggregate.Delete(new TestAggregateRemoved { Reason = "r" }, false, cancelled.Token));
            Assert.False(aggregate.IsDeleted);
        }

        //a persist is what a provider runs, not what a handler calls
        [Fact]
        public void PersistIsNotOnTheRepo()
        {
            Assert.DoesNotContain(typeof(IRepo).GetMethods(), x => x.Name is "Persist" or "PersistAsync");
            Assert.Contains(typeof(IRepoInternal).GetMethods(), x => x.Name == "PersistAsync");
        }
    }
}
