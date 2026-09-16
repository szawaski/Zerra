// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;

namespace Zerra.Repository.Test.Memory
{
    public class MemoryEngineTests
    {
        [Fact]
        public async Task TestSequence()
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
    }
}
