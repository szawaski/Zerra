// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    public class TransactStoreProviderTests
    {
        [Entity("TwoGeneratedModel")]
        public sealed class TwoGeneratedModel
        {
            [Identity(true)]
            public int A { get; set; }
            [Identity(true)]
            public long B { get; set; }
            public string? Name { get; set; }
        }

        [Entity("GeneratedAndSetModel")]
        public sealed class GeneratedAndSetModel
        {
            [Identity(true)]
            public int Generated { get; set; }
            [Identity]
            public Guid Set { get; set; }
            public string? Name { get; set; }
        }

        [Fact]
        public async Task GeneratedIdentities_AreSetOnTheModel()
        {
            var engine = new MemoryEngine();
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<TwoGeneratedModel>(engine));
            repo.AddProvider(new TransactStoreProvider<GeneratedAndSetModel>(engine));

            var first = new TwoGeneratedModel { Name = "first" };
            repo.Create(first);
            var second = new TwoGeneratedModel { Name = "second" };
            await repo.CreateAsync(second, TestContext.Current.CancellationToken);
            Assert.NotEqual((first.A, first.B), (second.A, second.B));
            Assert.Equal("second", repo.Query<TwoGeneratedModel>().SingleOrDefault(x => x.A == second.A && x.B == second.B)!.Name);

            var mixed = new GeneratedAndSetModel { Set = Guid.NewGuid(), Name = "mixed" };
            repo.Create(mixed);
            var mixedAsync = new GeneratedAndSetModel { Set = Guid.NewGuid(), Name = "mixed async" };
            await repo.CreateAsync(mixedAsync, TestContext.Current.CancellationToken);
            Assert.NotEqual(0, mixed.Generated);
            Assert.NotEqual(mixed.Generated, mixedAsync.Generated);
            Assert.Equal("mixed async", repo.Query<GeneratedAndSetModel>().SingleOrDefault(x => x.Generated == mixedAsync.Generated)!.Name);
        }

        [Fact]
        public async Task MissingModel_UpdateAndDeleteThrow()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<TwoGeneratedModel>(new MemoryEngine()));
            var missing = new TwoGeneratedModel { A = 5, B = 5 };

            Assert.Contains("No rows affected", Assert.Throws<Exception>(() => repo.Update(missing)).Message);
            Assert.Contains("No rows affected", (await Assert.ThrowsAsync<Exception>(() => repo.UpdateAsync(missing, TestContext.Current.CancellationToken))).Message);
            Assert.Contains("No rows affected", Assert.Throws<Exception>(() => repo.Delete(missing)).Message);
            Assert.Contains("No rows affected", (await Assert.ThrowsAsync<Exception>(() => repo.DeleteAsync(missing, TestContext.Current.CancellationToken))).Message);
        }

        [Fact]
        public async Task TemporalAndEventQueries_NotSupported()
        {
            _ = Assert.Throws<ArgumentNullException>(() => new TransactStoreProvider<TwoGeneratedModel>(null!));
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<TwoGeneratedModel>(new MemoryEngine()));

            _ = Assert.Throws<NotSupportedException>(() => repo.TemporalQuery<TwoGeneratedModel>().Where(x => x.A == 1).ToList());
            _ = Assert.Throws<NotSupportedException>(() => repo.TemporalQuery<TwoGeneratedModel>().Where(x => x.A == 1).FirstOrDefault());
            _ = Assert.Throws<NotSupportedException>(() => repo.TemporalQuery<TwoGeneratedModel>().Where(x => x.A == 1).LongCount());
            _ = Assert.Throws<NotSupportedException>(() => repo.TemporalQuery<TwoGeneratedModel>().Where(x => x.A == 1).Any());
            _ = Assert.Throws<NotSupportedException>(() => repo.EventQuery<TwoGeneratedModel>().Where(x => x.Model.A == 1).ToList());
            _ = Assert.Throws<NotSupportedException>(() => repo.EventQuery<TwoGeneratedModel>().Where(x => x.Model.A == 1).FirstOrDefault());
            _ = Assert.Throws<NotSupportedException>(() => repo.EventQuery<TwoGeneratedModel>().Where(x => x.Model.A == 1).SingleOrDefault());
            _ = Assert.Throws<NotSupportedException>(() => repo.EventQuery<TwoGeneratedModel>().Where(x => x.Model.A == 1).LongCount());
            _ = Assert.Throws<NotSupportedException>(() => repo.EventQuery<TwoGeneratedModel>().Where(x => x.Model.A == 1).Any());

            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.TemporalQuery<TwoGeneratedModel>().Where(x => x.A == 1).ToListAsync(cancellationToken: TestContext.Current.CancellationToken));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.TemporalQuery<TwoGeneratedModel>().Where(x => x.A == 1).FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.TemporalQuery<TwoGeneratedModel>().Where(x => x.A == 1).LongCountAsync(cancellationToken: TestContext.Current.CancellationToken));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.TemporalQuery<TwoGeneratedModel>().Where(x => x.A == 1).AnyAsync(cancellationToken: TestContext.Current.CancellationToken));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.EventQuery<TwoGeneratedModel>().Where(x => x.Model.A == 1).ToListAsync(cancellationToken: TestContext.Current.CancellationToken));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.EventQuery<TwoGeneratedModel>().Where(x => x.Model.A == 1).FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.EventQuery<TwoGeneratedModel>().Where(x => x.Model.A == 1).SingleOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.EventQuery<TwoGeneratedModel>().Where(x => x.Model.A == 1).LongCountAsync(cancellationToken: TestContext.Current.CancellationToken));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.EventQuery<TwoGeneratedModel>().Where(x => x.Model.A == 1).AnyAsync(cancellationToken: TestContext.Current.CancellationToken));
        }
    }
}
