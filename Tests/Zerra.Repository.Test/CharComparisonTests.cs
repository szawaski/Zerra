// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    //C# compiles a char literal in a comparison to its number, which compares with the character the column holds
    public static class CharComparisonTests
    {
        public static void TestSequence(ITransactStoreEngine engine)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<TestTypesModel>(engine));
            repo.AddProvider(new TransactStoreProvider<TestRelationsModel>(engine));
            var model = TestTypesModel.Create();
            repo.Create(model);
            try
            {
                var c = model.CharThing;
                var q = repo.Query<TestTypesModel>().Where(x => x.KeyA == model.KeyA);
                Assert.True(q.Any(x => x.CharThing == c));
                Assert.True(q.Any(x => x.CharThing == 'Z'));
                Assert.True(q.Any(x => 'Z' == x.CharThing));
                Assert.False(q.Any(x => x.CharThing != 'Z'));
                Assert.True(q.Any(x => x.CharThing != 'Q'));
                Assert.False(q.Any(x => x.CharThing == 'Q'));
                Assert.True(q.Any(x => x.CharThing > 'A'));
                Assert.True(q.Any(x => 'A' < x.CharThing));
                Assert.True(q.Any(x => x.CharNullableThing == 'X'));
                Assert.False(q.Any(x => x.CharNullableThingNull == 'X'));
                Assert.Equal('Z', q.Select(x => x.CharThing).Single());
            }
            finally
            {
                repo.Delete(model);
            }
        }
    }

    public class CharComparisonMemoryTests
    {
        [Fact]
        public void Memory() => CharComparisonTests.TestSequence(new MemoryEngine());
    }
}
