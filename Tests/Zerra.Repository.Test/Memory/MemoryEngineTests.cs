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
            RepoTest.TestSequenceTransactStore(new MemoryEngine());
            await RepoTest.TestSequenceTransactStoreAsync(new MemoryEngine());
        }

        [Fact]
        public async Task TestSequenceEventStore()
        {
            RepoTest.TestSequenceEventStore(new MemoryEngine());
            await RepoTest.TestSequenceEventStoreAsync(new MemoryEngine());
        }

        [Fact]
        public async Task TestSequenceAggregate()
        {
            await AggregateTest.TestSequenceAsync(new MemoryEngine());
        }

        [Fact]
        public async Task TestAggregateConcurrency()
        {
            await AggregateTest.TestConcurrencyAsync(new MemoryEngine());
        }
    }
}
