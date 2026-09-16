// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;

namespace Zerra.Repository.Test.Memory
{
    public class MemoryEngineTests
    {
        [Fact]
        public async Task TestSequenceTransactStore()
        {
            RepoTest.TestSequenceTransactStore<MemoryTestDataContext>();
            await RepoTest.TestSequenceTransactStoreAsync<MemoryTestDataContext>();
        }

        [Fact]
        public async Task TestSequenceEventStore()
        {
            RepoTest.TestSequenceEventStore<MemoryTestDataContext>();
            await RepoTest.TestSequenceEventStoreAsync<MemoryTestDataContext>();
        }

        [Fact]
        public async Task TestSequenceAggregate()
        {
            await AggregateTest.TestSequenceAsync<MemoryTestDataContext>();
        }

        [Fact]
        public async Task TestAggregateConcurrency()
        {
            await AggregateTest.TestConcurrencyAsync<MemoryTestDataContext>();
        }
    }
}
