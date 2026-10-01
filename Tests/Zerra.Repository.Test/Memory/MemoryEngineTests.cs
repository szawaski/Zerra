// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test.Memory
{
    public class MemoryEngineTests
    {
        [Fact]
        public async Task TestSequenceTransactStore()
        {
            RepoTest.TestSequenceTransactStore(MemoryDataContext.GetEngine());
            await RepoTest.TestSequenceTransactStoreAsync(MemoryDataContext.GetEngine());
        }

        [Fact]
        public async Task TestSequenceEventStore()
        {
            RepoTest.TestSequenceEventStore(MemoryDataContext.GetEngine());
            await RepoTest.TestSequenceEventStoreAsync(MemoryDataContext.GetEngine());
        }

        [Fact]
        public async Task TestSequenceAggregate()
        {
            await AggregateTest.TestSequenceAsync(MemoryDataContext.GetEngine());
        }

        [Fact]
        public async Task TestAggregateConcurrency()
        {
            await AggregateTest.TestConcurrencyAsync(MemoryDataContext.GetEngine());
        }
    }
}
