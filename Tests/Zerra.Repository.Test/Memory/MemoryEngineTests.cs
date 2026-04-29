// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;

namespace Zerra.Repository.Test
{
    public class MemoryEngineTests
    {
        [Fact]
        public async Task TestSequence()
        {
            RepoTest.TestSequence<MemoryTestDataContext>();
            await RepoTest.TestSequenceAsync<MemoryTestDataContext>();
        }
    }
}
