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
            await repo.CreateAsync(second);
            Assert.NotEqual((first.A, first.B), (second.A, second.B));
            Assert.Equal("second", repo.Single<TwoGeneratedModel>(x => x.A == second.A && x.B == second.B)!.Name);

            var mixed = new GeneratedAndSetModel { Set = Guid.NewGuid(), Name = "mixed" };
            repo.Create(mixed);
            var mixedAsync = new GeneratedAndSetModel { Set = Guid.NewGuid(), Name = "mixed async" };
            await repo.CreateAsync(mixedAsync);
            Assert.NotEqual(0, mixed.Generated);
            Assert.NotEqual(mixed.Generated, mixedAsync.Generated);
            Assert.Equal("mixed async", repo.Single<GeneratedAndSetModel>(x => x.Generated == mixedAsync.Generated)!.Name);
        }

        [Fact]
        public async Task MissingModel_UpdateAndDeleteThrow()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<TwoGeneratedModel>(new MemoryEngine()));
            var missing = new TwoGeneratedModel { A = 5, B = 5 };

            Assert.Contains("No rows affected", Assert.Throws<Exception>(() => repo.Update(missing)).Message);
            Assert.Contains("No rows affected", (await Assert.ThrowsAsync<Exception>(() => repo.UpdateAsync(missing))).Message);
            Assert.Contains("No rows affected", Assert.Throws<Exception>(() => repo.Delete(missing)).Message);
            Assert.Contains("No rows affected", (await Assert.ThrowsAsync<Exception>(() => repo.DeleteAsync(missing))).Message);
        }

        [Fact]
        public async Task TemporalAndEventQueries_NotSupported()
        {
            _ = Assert.Throws<ArgumentNullException>(() => new TransactStoreProvider<TwoGeneratedModel>(null!));
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<TwoGeneratedModel>(new MemoryEngine()));

            _ = Assert.Throws<NotSupportedException>(() => repo.TemporalMany<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1));
            _ = Assert.Throws<NotSupportedException>(() => repo.TemporalFirst<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1, null, null));
            _ = Assert.Throws<NotSupportedException>(() => repo.TemporalCount<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1));
            _ = Assert.Throws<NotSupportedException>(() => repo.TemporalAny<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1));
            _ = Assert.Throws<NotSupportedException>(() => repo.EventMany<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1));
            _ = Assert.Throws<NotSupportedException>(() => repo.EventFirst<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1));
            _ = Assert.Throws<NotSupportedException>(() => repo.EventSingle<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1));
            _ = Assert.Throws<NotSupportedException>(() => repo.EventCount<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1));
            _ = Assert.Throws<NotSupportedException>(() => repo.EventAny<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1));

            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.TemporalManyAsync<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.TemporalFirstAsync<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1, null));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.TemporalCountAsync<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.TemporalAnyAsync<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.EventManyAsync<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.EventFirstAsync<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.EventSingleAsync<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.EventCountAsync<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() => repo.EventAnyAsync<TwoGeneratedModel>(TemporalOrder.Newest, (ulong?)null, null, null, null, x => x.A == 1));
        }
    }
}
