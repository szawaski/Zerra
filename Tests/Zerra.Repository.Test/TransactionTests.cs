// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;
using Zerra.Repository.Reflection;
using Zerra.CQRS.Reflection;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    public static class TransactionTests
    {
        [Entity("TransactionParent")]
        public sealed class ParentModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Number { get; set; }
            [StoreProperties(false, 50)]
            public string? Name { get; set; }
            [Relation(nameof(ChildModel.ParentID))]
            public ChildModel[]? Children { get; set; }
        }

        [Entity("TransactionChild")]
        public sealed class ChildModel
        {
            [Identity]
            public Guid ID { get; set; }
            public Guid ParentID { get; set; }
            public int Quantity { get; set; }
            [Relation(nameof(ParentID))]
            public ParentModel? Parent { get; set; }
        }

        [Entity("TransactionOther")]
        public sealed class OtherModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Number { get; set; }
        }

        [Entity("TransactionGenerated")]
        public sealed class GeneratedModel
        {
            [Identity(true)]
            public int ID { get; set; }
            public int Number { get; set; }
        }

        public static readonly Type[] ModelTypes = [typeof(ParentModel), typeof(ChildModel)];

        private static IRepo NewRepo(ITransactStoreEngine engine)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<ParentModel>(engine));
            repo.AddProvider(new TransactStoreProvider<ChildModel>(engine));
            return repo;
        }
        private static void Clear(IRepo repo)
        {
            _ = repo.Delete<ChildModel>(x => x.Quantity >= 0);
            _ = repo.Delete<ParentModel>(x => x.Number >= 0);
        }
        //a thread that doesn't take this flow's values, so it isn't in the transaction
        private static T Outside<T>(Func<T> read)
        {
            T result = default!;
            Exception? error = null;
            var thread = new Thread(() =>
            {
                try
                {
                    result = read();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            });
            thread.UnsafeStart();
            thread.Join();
            if (error is not null)
                throw new InvalidOperationException("The read outside the transaction failed", error);
            return result;
        }
        private static ParentModel Parent(int number) => new() { ID = Guid.NewGuid(), Number = number, Name = $"n{number}" };

        //SQL Server makes a reader wait for rows a transaction wrote, where the others read what was committed
        public static void TestSequence(ITransactStoreEngine engine, bool readsWait)
        {
            var repo = NewRepo(engine);
            Clear(repo);
            Assert.Null(RepoTransaction.Current);

            //commit, reading its own writes
            var parent = Parent(1);
            var child = new ChildModel { ID = Guid.NewGuid(), ParentID = parent.ID, Quantity = 5 };
            using (var transaction = repo.BeginTransaction())
            {
                repo.Create(parent);
                repo.Create(child);
                Assert.Equal(1, repo.Query<ParentModel>().Count());
                Assert.Equal(5, repo.Query<ParentModel>().Select(x => x.Children!.Sum(c => c.Quantity)).Single());
                if (!readsWait)
                    Assert.Equal(0, Outside(() => repo.Query<ParentModel>().Count()));
                transaction.Commit();
                //the flow goes on outside it
                Assert.Null(RepoTransaction.Current);
                Assert.Equal(1, repo.Query<ParentModel>().Count());
                _ = Assert.Throws<InvalidOperationException>(transaction.Commit);
            }
            Assert.Equal(1, Outside(() => repo.Query<ParentModel>().Count()));
            Assert.Null(RepoTransaction.Current);

            //disposing rolls back every kind of write
            using (repo.BeginTransaction())
            {
                repo.Create(Parent(2));
                repo.Update(new ParentModel { ID = parent.ID, Number = 100, Name = "changed" });
                Assert.Equal(1, repo.Update<ParentModel>(x => x.ID == parent.ID, x => new ParentModel { Number = x.Number + 1 }));
                Assert.Equal(101, repo.Query<ParentModel>().Single(x => x.ID == parent.ID).Number);
                Assert.Equal(1, repo.Delete<ChildModel>(x => x.ID == child.ID));
                Assert.Equal(0, repo.Query<ChildModel>().Count());
                Assert.Equal(2, repo.Query<ParentModel>().Count());
            }
            Assert.Null(RepoTransaction.Current);
            var kept = repo.Query<ParentModel>().Single();
            Assert.Equal(1, kept.Number);
            Assert.Equal("n1", kept.Name);
            Assert.Equal(1, repo.Query<ChildModel>().Count());

            //an exception rolls back
            _ = Assert.Throws<ArithmeticException>((Action)(() =>
            {
                using var transaction = repo.BeginTransaction();
                repo.Create(Parent(3));
                throw new ArithmeticException();
            }));
            Assert.Equal(1, repo.Query<ParentModel>().Count());

            //every kind of write commits
            var added = Parent(4);
            using (var transaction = repo.BeginTransaction(IsolationLevel.Serializable))
            {
                repo.Create(added);
                repo.Update(new ParentModel { ID = parent.ID, Number = 10, Name = "first" });
                Assert.Equal(1, repo.Update<ParentModel>(x => x.ID == parent.ID, x => new ParentModel { Number = x.Number + 1 }));
                Assert.Equal(1, repo.Update(x => x.ID == parent.ID, new ParentModel { Name = "second" }, new Graph<ParentModel>(x => x.Name)));
                repo.Delete(child);
                transaction.Commit();
            }
            var saved = repo.Query<ParentModel>().Single(x => x.ID == parent.ID);
            Assert.Equal(11, saved.Number);
            Assert.Equal("second", saved.Name);
            Assert.Equal(0, repo.Query<ChildModel>().Count());
            Assert.Equal(2, repo.Query<ParentModel>().Count());
            Assert.Equal(1, repo.Delete<ParentModel>(x => x.ID == added.ID));

            //an explicit rollback undoes the work so far, and what comes next is new work in the same transaction
            var before = repo.Query<ParentModel>().Count();
            var afterRollback = Parent(6);
            using (var transaction = repo.BeginTransaction())
            {
                repo.Create(Parent(7));
                transaction.Rollback();
                Assert.NotNull(RepoTransaction.Current);
                Assert.Equal(before, repo.Query<ParentModel>().Count());
                repo.Create(Parent(8));
                transaction.Rollback();
                transaction.Rollback();
                repo.Create(afterRollback);
                Assert.Equal(before + 1, repo.Query<ParentModel>().Count());
                transaction.Commit();
                _ = Assert.Throws<InvalidOperationException>(transaction.Rollback);
            }
            Assert.Equal(before + 1, repo.Query<ParentModel>().Count());
            Assert.Equal(1, repo.Query<ParentModel>().Count(x => x.ID == afterRollback.ID));
            Assert.Equal(1, repo.Delete<ParentModel>(x => x.ID == afterRollback.ID));

            //disposing after a rollback rolls back only what came after it
            using (var transaction = repo.BeginTransaction())
            {
                repo.Create(Parent(9));
                transaction.Rollback();
                repo.Create(Parent(10));
            }
            Assert.Equal(before, repo.Query<ParentModel>().Count());

            //one open at a time
            using (var transaction = repo.BeginTransaction())
                _ = Assert.Throws<InvalidOperationException>(() => repo.BeginTransaction());

            //a thread still using it after it finished gets an error instead of writing on its own
            var finished = new ManualResetEventSlim();
            Exception? late = null;
            var lateThread = (Thread?)null;
            using (var transaction = repo.BeginTransaction())
            {
                _ = repo.Query<ParentModel>().Count();
                lateThread = new Thread(() =>
                {
                    finished.Wait();
                    try
                    {
                        repo.Create(Parent(5));
                    }
                    catch (Exception ex)
                    {
                        late = ex;
                    }
                });
                lateThread.Start();
                transaction.Commit();
            }
            finished.Set();
            lateThread.Join();
            _ = Assert.IsType<InvalidOperationException>(late);
            Assert.Equal(1, repo.Query<ParentModel>().Count());

            Clear(repo);
        }

        public static async Task TestSequenceAsync(ITransactStoreEngine engine, bool readsWait)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var repo = NewRepo(engine);
            Clear(repo);

            var parent = Parent(1);
            await using (var transaction = repo.BeginTransaction())
            {
                await repo.CreateAsync(parent, cancellationToken);
                Assert.Equal(1, await repo.Query<ParentModel>().CountAsync());
                if (!readsWait)
                    Assert.Equal(0, Outside(() => repo.Query<ParentModel>().Count()));
                await transaction.CommitAsync(cancellationToken);
                Assert.Null(RepoTransaction.Current);
            }
            Assert.Null(RepoTransaction.Current);
            Assert.Equal(1, await repo.Query<ParentModel>().CountAsync());

            //disposing rolls back, and the flow goes on outside it
            await using (repo.BeginTransaction())
            {
                await repo.CreateAsync(Parent(2), cancellationToken);
                Assert.Equal(1, await repo.UpdateAsync<ParentModel>(x => x.ID == parent.ID, x => new ParentModel { Number = 50 }, cancellationToken));
                Assert.Equal(2, await repo.Query<ParentModel>().CountAsync());
            }
            Assert.Null(RepoTransaction.Current);
            Assert.Equal(1, await repo.Query<ParentModel>().CountAsync());
            Assert.Equal(1, (await repo.Query<ParentModel>().SingleAsync()).Number);

            //it stays open across awaits, and parallel work takes turns on the one connection
            await using (var transaction = repo.BeginTransaction())
            {
                await Task.Yield();
                var work = new List<Task>();
                for (var i = 10; i < 20; i++)
                {
                    work.Add(repo.CreateAsync(Parent(i), cancellationToken));
                    work.Add(repo.Query<ParentModel>().CountAsync());
                }
                await Task.WhenAll(work);
                Assert.Equal(11, await repo.Query<ParentModel>().CountAsync());
                await transaction.CommitAsync(cancellationToken);
            }
            Assert.Equal(11, await repo.Query<ParentModel>().CountAsync());

            //every kind of write through the async path
            await using (var transaction = repo.BeginTransaction())
            {
                Assert.Equal(10, await repo.DeleteAsync<ParentModel>(x => x.Number >= 10, cancellationToken));
                await repo.UpdateAsync(new ParentModel { ID = parent.ID, Number = 7, Name = "seven" }, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            Assert.Equal(7, (await repo.Query<ParentModel>().SingleAsync()).Number);

            //a rollback undoes every kind of async write
            var otherChild = new ChildModel { ID = Guid.NewGuid(), ParentID = parent.ID, Quantity = 3 };
            await repo.CreateAsync(otherChild, cancellationToken);
            await using (var transaction = repo.BeginTransaction())
            {
                await repo.CreateAsync(Parent(40), cancellationToken);
                await repo.UpdateAsync(new ParentModel { ID = parent.ID, Number = 41, Name = "changed" }, cancellationToken);
                Assert.Equal(1, await repo.UpdateAsync<ParentModel>(x => x.ID == parent.ID, x => new ParentModel { Number = x.Number + 1 }, cancellationToken));
                Assert.Equal(1, await repo.UpdateAsync(x => x.ID == parent.ID, new ParentModel { Name = "again" }, new Graph<ParentModel>(x => x.Name), cancellationToken));
                Assert.Equal(42, (await repo.Query<ParentModel>().SingleAsync(x => x.ID == parent.ID)).Number);
                await repo.DeleteAsync(otherChild, cancellationToken);
                Assert.Equal(0, await repo.Query<ChildModel>().CountAsync());
                await transaction.RollbackAsync();
                var kept = await repo.Query<ParentModel>().SingleAsync();
                Assert.Equal(7, kept.Number);
                Assert.Equal("seven", kept.Name);
                Assert.Equal(1, await repo.Query<ChildModel>().CountAsync());
            }
            Assert.Equal(1, await repo.Query<ChildModel>().CountAsync());

            //a commit in an async method it calls leaves this flow holding it, using it says so and disposing ends that
            await using (var transaction = repo.BeginTransaction())
            {
                await repo.CreateAsync(Parent(50), cancellationToken);
                await CommitElsewhere(transaction, cancellationToken);
                Assert.NotNull(RepoTransaction.Current);
                _ = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.Query<ParentModel>().CountAsync());
            }
            Assert.Null(RepoTransaction.Current);
            Assert.Equal(2, await repo.Query<ParentModel>().CountAsync());
            Assert.Equal(1, await repo.DeleteAsync<ParentModel>(x => x.Number == 50, cancellationToken));

            //an explicit rollback undoes the work so far, and what comes next is new work in the same transaction
            var before = await repo.Query<ParentModel>().CountAsync();
            var afterRollback = Parent(30);
            await using (var transaction = repo.BeginTransaction())
            {
                await repo.CreateAsync(Parent(31), cancellationToken);
                await transaction.RollbackAsync();
                Assert.NotNull(RepoTransaction.Current);
                Assert.Equal(before, await repo.Query<ParentModel>().CountAsync());
                await repo.CreateAsync(afterRollback, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                _ = await Assert.ThrowsAsync<InvalidOperationException>(transaction.RollbackAsync);
            }
            Assert.Equal(before + 1, await repo.Query<ParentModel>().CountAsync());
            Assert.Equal(1, await repo.DeleteAsync<ParentModel>(x => x.ID == afterRollback.ID, cancellationToken));

            //a cancelled commit doesn't save
            await using (var transaction = repo.BeginTransaction())
            {
                await repo.CreateAsync(Parent(8), cancellationToken);
                using var cancelled = new CancellationTokenSource();
                await cancelled.CancelAsync();
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transaction.CommitAsync(cancelled.Token));
            }
            Assert.Equal(1, await repo.Query<ParentModel>().CountAsync());

            Clear(repo);
        }

        private static async Task CommitElsewhere(IRepoTransaction transaction, CancellationToken cancellationToken)
        {
            await Task.Yield();
            await transaction.CommitAsync(cancellationToken);
        }

        //two engines on one database share the transaction, on a second connection the read of the first's row would wait forever on SQL Server
        public static void TestSameStore(ITransactStoreEngine engine, ITransactStoreEngine sameStore)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<ParentModel>(engine));
            repo.AddProvider(new TransactStoreProvider<ChildModel>(sameStore));
            Clear(repo);
            var parent = Parent(1);
            using (repo.BeginTransaction())
            {
                repo.Create(parent);
                repo.Create(new ChildModel { ID = Guid.NewGuid(), ParentID = parent.ID, Quantity = 1 });
                Assert.Equal(1, repo.Query<ChildModel>().Select(x => x.Parent!.Number).Single());
            }
            Assert.Equal(0, repo.Query<ParentModel>().Count());
            Assert.Equal(0, repo.Query<ChildModel>().Count());
        }

        //the other engine is in memory, as what matters is two engines
        public static void TestAcrossEngines(ITransactStoreEngine engine)
        {
            var repo = NewRepo(engine);
            ((IRepoSetup)repo).AddProvider(new TransactStoreProvider<OtherModel>(new MemoryEngine()));
            Clear(repo);

            using (repo.BeginTransaction())
            {
                repo.Create(Parent(1));
                repo.Create(new OtherModel { ID = Guid.NewGuid(), Number = 1 });
                Assert.Equal(1, repo.Query<ParentModel>().Count());
                Assert.Equal(1, repo.Query<OtherModel>().Count());
            }
            Assert.Equal(0, repo.Query<ParentModel>().Count());
            Assert.Equal(0, repo.Query<OtherModel>().Count());

            using (var transaction = repo.BeginTransaction())
            {
                //the order they join doesn't matter
                repo.Create(new OtherModel { ID = Guid.NewGuid(), Number = 2 });
                repo.Create(Parent(2));
                transaction.Commit();
            }
            Assert.Equal(2, repo.Query<ParentModel>().Single().Number);
            Assert.Equal(2, repo.Query<OtherModel>().Single().Number);

            //a rollback undoes both, and what comes next can use either or both again
            using (var transaction = repo.BeginTransaction())
            {
                repo.Create(Parent(3));
                repo.Create(new OtherModel { ID = Guid.NewGuid(), Number = 3 });
                transaction.Rollback();
                Assert.Equal(1, repo.Query<ParentModel>().Count());
                Assert.Equal(1, repo.Query<OtherModel>().Count());
                repo.Create(new OtherModel { ID = Guid.NewGuid(), Number = 4 });
                transaction.Rollback();
                repo.Create(Parent(5));
                repo.Create(new OtherModel { ID = Guid.NewGuid(), Number = 5 });
                transaction.Commit();
            }
            Assert.Equal(2, repo.Query<ParentModel>().Count());
            Assert.Equal(2, repo.Query<OtherModel>().Count());
            Assert.Equal(0, repo.Query<OtherModel>().Count(x => x.Number == 3 || x.Number == 4));

            Clear(repo);
        }

        public interface ITestHandler
        {
            bool InTransaction();
            Task<bool> InTransactionAsync();
        }
        private sealed class TestHandler : ITestHandler
        {
            public bool InTransaction() => RepoTransaction.Current is not null;
            public async Task<bool> InTransactionAsync()
            {
                await Task.Yield();
                return RepoTransaction.Current is not null;
            }
        }

        //an engine that only says it can't take part, a write must be refused before reaching it
        public class NoTransactionsEngine : DispatchProxy
        {
            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                if (targetMethod?.Name == "get_SupportsTransactions")
                    return false;
                throw new InvalidOperationException($"{targetMethod?.Name} reached the engine");
            }
        }

        private sealed class FailingRollbackPart : RepoTransactionPart
        {
            public bool WasClosed;
            public override void Commit() { }
            public override Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public override void Rollback() => throw new InvalidDataException("failed");
            public override Task RollbackAsync() => throw new InvalidDataException("failed");
            public override void Close() => WasClosed = true;
        }

        private sealed class TestPart : RepoTransactionPart
        {
            private readonly bool fail;
            private readonly bool last;
            public readonly List<string> Calls;
            public override bool CommitsLast => last;
            public TestPart(List<string> calls, bool fail, bool last = false)
            {
                this.Calls = calls;
                this.fail = fail;
                this.last = last;
            }
            public override void Commit()
            {
                Calls.Add("commit");
                if (fail)
                    throw new InvalidDataException("failed");
            }
            public override Task CommitAsync(CancellationToken cancellationToken)
            {
                Commit();
                return Task.CompletedTask;
            }
            public override void Rollback() => Calls.Add("rollback");
            public override Task RollbackAsync()
            {
                Rollback();
                return Task.CompletedTask;
            }
            public override void Close() => Calls.Add("close");
        }

        public sealed class Facts
        {
            [Fact]
            public void Memory() => TestSequence(new MemoryEngine(), false);

            [Fact]
            public Task MemoryAsync() => TestSequenceAsync(new MemoryEngine(), false);

            [Fact]
            public void MemoryAcrossEngines() => TestAcrossEngines(new MemoryEngine());

            [Fact]
            public void MemoryRelationsSeeTheTransaction()
            {
                var repo = NewRepo(new MemoryEngine());
                var parent = Parent(1);
                repo.Create(parent);
                repo.Create(new ChildModel { ID = Guid.NewGuid(), ParentID = parent.ID, Quantity = 1 });
                using (repo.BeginTransaction())
                {
                    repo.Create(new ChildModel { ID = Guid.NewGuid(), ParentID = parent.ID, Quantity = 2 });
                    Assert.Equal(3, repo.Query<ParentModel>().Select(x => x.Children!.Sum(c => c.Quantity)).Single());
                    Assert.Equal(1, Outside(() => repo.Query<ParentModel>().Select(x => x.Children!.Sum(c => c.Quantity)).Single()));
                }
                Assert.Equal(1, repo.Query<ParentModel>().Select(x => x.Children!.Sum(c => c.Quantity)).Single());
            }

            [Fact]
            public void MemoryCommitKeepsOtherWriters()
            {
                var repo = NewRepo(new MemoryEngine());
                using (var transaction = repo.BeginTransaction())
                {
                    repo.Create(Parent(1));
                    _ = Outside(() =>
                    {
                        repo.Create(Parent(2));
                        return true;
                    });
                    transaction.Commit();
                }
                Assert.Equal(2, repo.Query<ParentModel>().Count());
            }

            [Fact]
            public void AnEngineWithoutTransactionsIsRefused()
            {
                var repo = Repo.New();
                ((IRepoSetup)repo).AddProvider(new TransactStoreProvider<OtherModel>(DispatchProxy.Create<ITransactStoreEngine, NoTransactionsEngine>()));
                using (repo.BeginTransaction())
                    _ = Assert.Throws<NotSupportedException>(() => repo.Create(new OtherModel { ID = Guid.NewGuid(), Number = 1 }));
            }

            [Fact]
            public async Task AnEngineWithoutTransactionsIsRefusedAsync()
            {
                var repo = Repo.New();
                ((IRepoSetup)repo).AddProvider(new TransactStoreProvider<OtherModel>(DispatchProxy.Create<ITransactStoreEngine, NoTransactionsEngine>()));
                await using (repo.BeginTransaction())
                    _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.CreateAsync(new OtherModel { ID = Guid.NewGuid(), Number = 1 }, TestContext.Current.CancellationToken));
            }

            //nothing is applied when another writer took a generated identity
            [Fact]
            public void MemoryCommitFailsWhenAnIdentityWasTaken()
            {
                var repo = Repo.New();
                var engine = new MemoryEngine();
                ((IRepoSetup)repo).AddProvider(new TransactStoreProvider<GeneratedModel>(engine));
                ((IRepoSetup)repo).AddProvider(new TransactStoreProvider<ParentModel>(engine));
                var transaction = repo.BeginTransaction();
                var mine = new GeneratedModel { Number = 1 };
                repo.Create(mine);
                repo.Create(Parent(1));
                Assert.Equal(1, mine.ID);
                _ = Outside(() =>
                {
                    repo.Create(new GeneratedModel { Number = 2 });
                    return true;
                });
                _ = Assert.Throws<InvalidOperationException>(transaction.Commit);
                transaction.Dispose();
                Assert.Equal(2, repo.Query<GeneratedModel>().Single().Number);
                Assert.Equal(0, repo.Query<ParentModel>().Count());
            }

            [Fact]
            public void MemoryRollbackSeesWhatOthersWrote()
            {
                var repo = NewRepo(new MemoryEngine());
                using var transaction = repo.BeginTransaction();
                repo.Create(Parent(1));
                Assert.Equal(1, repo.Query<ParentModel>().Count());
                _ = Outside(() =>
                {
                    repo.Create(Parent(2));
                    return true;
                });
                Assert.Equal(1, repo.Query<ParentModel>().Count());
                transaction.Rollback();
                Assert.Equal(2, repo.Query<ParentModel>().Single().Number);
            }

            //beginning in an async method doesn't reach its caller, which is why the docs say to begin and commit in one method
            [Fact]
            public async Task BeginningInAnAsyncMethodDoesNotReachTheCaller()
            {
                var repo = NewRepo(new MemoryEngine());
                var transaction = await BeginElsewhere(repo);
                Assert.Null(RepoTransaction.Current);
                transaction.Dispose();
            }
            private static async Task<IRepoTransaction> BeginElsewhere(IRepo repo)
            {
                await Task.Yield();
                return repo.BeginTransaction();
            }

            //a rollback that fails still closes, which rolls it back in the store, and lets the work go on
            [Fact]
            public void AFailedRollbackStillCloses()
            {
                var repo = NewRepo(new MemoryEngine());
                using var transaction = repo.BeginTransaction();
                var part = RepoTransaction.Current!.Enlist("part", "Part", _ => new FailingRollbackPart());
                transaction.Rollback();
                Assert.True(part.WasClosed);
                Assert.NotSame(part, RepoTransaction.Current!.Enlist("part", "Part", _ => new FailingRollbackPart()));
            }

            [Fact]
            public async Task AFailedRollbackStillClosesAsync()
            {
                var repo = NewRepo(new MemoryEngine());
                await using var transaction = repo.BeginTransaction();
                var part = RepoTransaction.Current!.Enlist("part", "Part", _ => new FailingRollbackPart());
                await transaction.RollbackAsync();
                Assert.True(part.WasClosed);
            }

            [Fact]
            public void AFailedRollbackOnDisposeStillCloses()
            {
                var repo = NewRepo(new MemoryEngine());
                FailingRollbackPart part;
                using (repo.BeginTransaction())
                    part = RepoTransaction.Current!.Enlist("part", "Part", _ => new FailingRollbackPart());
                Assert.True(part.WasClosed);
                Assert.Null(RepoTransaction.Current);
            }

            [Fact]
            public async Task FirstCommitFailureRollsBackTheRestAsync()
            {
                var repo = NewRepo(new MemoryEngine());
                var first = new List<string>();
                var second = new List<string>();
                await using var transaction = repo.BeginTransaction();
                _ = RepoTransaction.Current!.Enlist("first", "First", _ => new TestPart(first, true));
                _ = RepoTransaction.Current!.Enlist("second", "Second", _ => new TestPart(second, false));
                _ = await Assert.ThrowsAsync<InvalidDataException>(() => transaction.CommitAsync(TestContext.Current.CancellationToken));
                Assert.Equal(["commit", "rollback", "close"], first);
                Assert.Equal(["rollback", "close"], second);
            }

            [Fact]
            public async Task DisposingAsyncRollsBackEveryPart()
            {
                var repo = NewRepo(new MemoryEngine());
                var calls = new List<string>();
                await using (repo.BeginTransaction())
                    _ = RepoTransaction.Current!.Enlist("part", "Part", _ => new TestPart(calls, false));
                Assert.Equal(["rollback", "close"], calls);
                Assert.Null(RepoTransaction.Current);
            }

            [Fact]
            public async Task AWaitingOperationGoesOnAfterRollbackAsync()
            {
                var repo = NewRepo(new MemoryEngine());
                var cancellationToken = TestContext.Current.CancellationToken;
                await using var transaction = repo.BeginTransaction();
                var calls = new List<string>();
                var current = RepoTransaction.Current!;
                Task<TestPart> Begin(IsolationLevel level, CancellationToken token) => Task.FromResult(new TestPart(calls, false));
                var held = await current.EnterAsync("part", "Part", Begin, cancellationToken);
                var waiter = current.EnterAsync("part", "Part", Begin, cancellationToken);
                var rollback = transaction.RollbackAsync();
                await Task.Delay(50, cancellationToken);
                Assert.False(waiter.IsCompleted);
                held.Exit();
                //first in line it gets its turn on the open part, the rollback then waits for it
                var next = await waiter.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
                Assert.False(rollback.IsCompleted);
                next.Exit();
                await rollback.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
                //a part it holds after that is a new one
                var after = await current.EnterAsync("part", "Part", Begin, cancellationToken);
                Assert.NotSame(next, after);
                after.Exit();
            }

            [Fact]
            public async Task AWaitingOperationGetsAnErrorAfterCommitAsync()
            {
                var repo = NewRepo(new MemoryEngine());
                var cancellationToken = TestContext.Current.CancellationToken;
                await using var transaction = repo.BeginTransaction();
                var calls = new List<string>();
                var current = RepoTransaction.Current!;
                Task<TestPart> Begin(IsolationLevel level, CancellationToken token) => Task.FromResult(new TestPart(calls, false));
                var held = await current.EnterAsync("part", "Part", Begin, cancellationToken);
                var waiter = current.EnterAsync("part", "Part", Begin, cancellationToken);
                var commit = transaction.CommitAsync(cancellationToken);
                await Task.Delay(50, cancellationToken);
                held.Exit();
                _ = await Assert.ThrowsAsync<InvalidOperationException>(() => waiter.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken));
                await commit.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }

            [Fact]
            public void EventStoresAreRefused()
            {
                var repo = Repo.New();
                ((IRepoSetup)repo).AddProvider(new EventStoreAsTransactStoreProvider<OtherModel>(new MemoryEngine()));
                var id = Guid.NewGuid();
                using (repo.BeginTransaction())
                    _ = Assert.Throws<NotSupportedException>(() => repo.Create(new OtherModel { ID = id, Number = 1 }));
                Assert.Equal(0, repo.Query<OtherModel>().Count(x => x.ID == id));
                repo.Create(new OtherModel { ID = id, Number = 1 });
                Assert.Equal(1, repo.Query<OtherModel>().Count(x => x.ID == id));
            }

            [Fact]
            public async Task AggregatesAreRefused()
            {
                var aggregate = new TestAggregate(Guid.NewGuid(), new MemoryEngine());
                var repo = NewRepo(new MemoryEngine());
                await using (repo.BeginTransaction())
                    _ = await Assert.ThrowsAsync<NotSupportedException>(() => aggregate.Append(new TestAggregateCreated() { Name = "First", Amount = 10 }, cancellationToken: TestContext.Current.CancellationToken));
            }

            [Fact]
            public void FirstCommitFailureRollsBackTheRest()
            {
                var repo = NewRepo(new MemoryEngine());
                var first = new List<string>();
                var second = new List<string>();
                var transaction = repo.BeginTransaction();
                _ = RepoTransaction.Current!.Enlist("first", "First", _ => new TestPart(first, true));
                _ = RepoTransaction.Current!.Enlist("second", "Second", _ => new TestPart(second, false));
                _ = Assert.Throws<InvalidDataException>(transaction.Commit);
                Assert.Equal(["commit", "rollback", "close"], first);
                Assert.Equal(["rollback", "close"], second);
                transaction.Dispose();
                Assert.Null(RepoTransaction.Current);
            }

            [Fact]
            public async Task LaterCommitFailureSaysWhatWasSaved()
            {
                var repo = NewRepo(new MemoryEngine());
                foreach (var isAsync in new[] { false, true })
                {
                    var last = new List<string>();
                    var first = new List<string>();
                    var failing = new List<string>();
                    var after = new List<string>();
                    var transaction = repo.BeginTransaction();
                    //one that commits last goes after the others however early it joined
                    _ = RepoTransaction.Current!.Enlist("last", "Last", _ => new TestPart(last, false, true));
                    _ = RepoTransaction.Current!.Enlist("first", "First", _ => new TestPart(first, false));
                    _ = RepoTransaction.Current!.Enlist("failing", "Failing", _ => new TestPart(failing, true));
                    _ = RepoTransaction.Current!.Enlist("after", "After", _ => new TestPart(after, false));
                    RepoPartialCommitException ex;
                    if (isAsync)
                        ex = await Assert.ThrowsAsync<RepoPartialCommitException>(() => transaction.CommitAsync(TestContext.Current.CancellationToken));
                    else
                        ex = Assert.Throws<RepoPartialCommitException>(transaction.Commit);
                    Assert.Equal(["First"], ex.Committed);
                    Assert.Equal(["Failing", "After", "Last"], ex.NotCommitted);
                    _ = Assert.IsType<InvalidDataException>(ex.InnerException);
                    Assert.Equal(["commit", "close"], first);
                    Assert.Equal(["rollback", "close"], last);
                    Assert.Equal(["rollback", "close"], after);
                    transaction.Dispose();
                    Assert.Null(RepoTransaction.Current);
                }
            }

            [Fact]
            public void RollbackClosesThePartsAndTheNextUseStartsNewOnes()
            {
                var repo = NewRepo(new MemoryEngine());
                var calls = new List<string>();
                var started = 0;
                TestPart Begin(IsolationLevel level)
                {
                    started++;
                    return new TestPart(calls, false);
                }
                using var transaction = repo.BeginTransaction();
                var first = RepoTransaction.Current!.Enlist("part", "Part", Begin);
                transaction.Rollback();
                var second = RepoTransaction.Current!.Enlist("part", "Part", Begin);
                Assert.NotSame(first, second);
                Assert.Equal(2, started);
                Assert.Equal(["rollback", "close"], calls);
                transaction.Commit();
                Assert.Equal(["rollback", "close", "commit", "close"], calls);
            }

            //an operation waiting its turn when the part closes must go on, not wait forever
            [Fact]
            public void AWaitingOperationGoesOnAfterRollback()
            {
                var repo = NewRepo(new MemoryEngine());
                using var transaction = repo.BeginTransaction();
                var calls = new List<string>();
                var held = RepoTransaction.Current!.Enter("part", "Part", _ => new TestPart(calls, false));
                TestPart? next = null;
                var waiter = new Thread(() =>
                {
                    next = RepoTransaction.Current!.Enter("part", "Part", _ => new TestPart(calls, false));
                    next.Exit();
                });
                waiter.Start();
                var rollback = new Thread(transaction.Rollback);
                rollback.Start();
                Thread.Sleep(100);
                held.Exit();
                Assert.True(rollback.Join(TimeSpan.FromSeconds(30)));
                Assert.True(waiter.Join(TimeSpan.FromSeconds(30)));
                Assert.NotNull(next);
            }

            [Fact]
            public void AWaitingOperationGetsAnErrorAfterCommit()
            {
                var repo = NewRepo(new MemoryEngine());
                using var transaction = repo.BeginTransaction();
                var calls = new List<string>();
                var held = RepoTransaction.Current!.Enter("part", "Part", _ => new TestPart(calls, false));
                var current = RepoTransaction.Current;
                Exception? error = null;
                var waiter = new Thread(() =>
                {
                    try
                    {
                        current.Enter("part", "Part", _ => new TestPart(calls, false)).Exit();
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                });
                waiter.Start();
                var commit = new Thread(transaction.Commit);
                commit.Start();
                Thread.Sleep(100);
                held.Exit();
                Assert.True(commit.Join(TimeSpan.FromSeconds(30)));
                Assert.True(waiter.Join(TimeSpan.FromSeconds(30)));
                _ = Assert.IsType<InvalidOperationException>(error);
            }

            [Fact]
            public void EnginesOnOneStoreShareAPart()
            {
                var repo = NewRepo(new MemoryEngine());
                using var transaction = repo.BeginTransaction();
                var calls = new List<string>();
                var first = RepoTransaction.Current!.Enlist("same", "One", _ => new TestPart(calls, false));
                var second = RepoTransaction.Current!.Enlist("same", "Two", _ => new TestPart(calls, false));
                Assert.Same(first, second);
            }

            [Fact]
            public async Task AHandlerIsItsOwnUnitOfWork()
            {
                var repo = NewRepo(new MemoryEngine());
                var handler = new TestHandler();
                await using var transaction = repo.BeginTransaction();
                Assert.False((bool)BusHandlers.Invoke(typeof(ITestHandler), handler, nameof(ITestHandler.InTransaction), [])!);
                Assert.False(await (Task<bool>)BusHandlers.Invoke(typeof(ITestHandler), handler, nameof(ITestHandler.InTransactionAsync), [])!);
                //the caller still has it
                Assert.NotNull(RepoTransaction.Current);
            }
        }
    }
}
