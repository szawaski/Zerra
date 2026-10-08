// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Memory;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    //models identified by two members
    public class CompositeIdentityTests
    {
        [Entity("CompositeParent")]
        public sealed class CompositeParent
        {
            [Identity]
            public Guid A { get; set; }
            [Identity]
            public int B { get; set; }
            public string? Name { get; set; }
        }

        [Entity("CompositeChild")]
        public sealed class CompositeChild
        {
            [Identity]
            public Guid ID { get; set; }
            public Guid ParentA { get; set; }
            public int ParentB { get; set; }
            public string? Name { get; set; }
        }

        [Fact]
        public void Identity_GetSetCompare()
        {
            var a = Guid.NewGuid();
            var parent = new CompositeParent { A = a, B = 2 };
            Assert.Equal(["A", "B"], ModelAnalyzer.GetIdentityPropertyNames(typeof(CompositeParent)));
            Assert.Equal(new object[] { a, 2 }, (object[])ModelAnalyzer.GetIdentity(typeof(CompositeParent), parent));

            ModelAnalyzer.SetIdentity(typeof(CompositeParent), parent, new object[] { a, 3 });
            Assert.Equal(3, parent.B);

            var child = new CompositeChild();
            ModelAnalyzer.SetForeignIdentity(typeof(CompositeChild), "ParentA,ParentB", child, new object[] { a, 3 });
            Assert.True(ModelAnalyzer.CompareIdentities(ModelAnalyzer.GetIdentity(typeof(CompositeParent), parent), ModelAnalyzer.GetForeignIdentity(typeof(CompositeChild), "ParentA,ParentB", child)));

            Assert.True(ModelAnalyzer.CompareIdentities(null, null));
            Assert.False(ModelAnalyzer.CompareIdentities(1, null));
            Assert.False(ModelAnalyzer.CompareIdentities(null, 1));
            Assert.False(ModelAnalyzer.CompareIdentities(new object?[] { 1 }, 1));
            Assert.False(ModelAnalyzer.CompareIdentities(new object?[] { 1 }, new object?[] { 1, 2 }));
            Assert.True(ModelAnalyzer.CompareIdentities(new object?[] { 1, null }, new object?[] { 1, null }));
            Assert.False(ModelAnalyzer.CompareIdentities(new object?[] { 1, null }, new object?[] { 1, 2 }));
            Assert.False(ModelAnalyzer.CompareIdentities(new object?[] { 1, 2 }, new object?[] { 1, null }));
            Assert.False(ModelAnalyzer.CompareIdentities(new object?[] { 1, 2 }, new object?[] { 1, 3 }));
            Assert.False(ModelAnalyzer.CompareIdentities(1, 2));

            var where = ModelAnalyzer.GetIdentityExpression<CompositeParent>(new object[] { a, 3 })!.Compile();
            Assert.True(where(parent));
            Assert.False(where(new CompositeParent { A = a, B = 4 }));
            _ = Assert.Throws<InvalidOperationException>(() => ModelAnalyzer.GetIdentityExpression<CompositeParent>(a));
            _ = Assert.Throws<Exception>(() => ModelAnalyzer.GetIdentityPropertyNames(typeof(NoIdentity)));
        }

        public sealed class NoIdentity
        {
            public int Value { get; set; }
        }

        [Fact]
        public async Task TransactStore_CreateReadUpdateDelete()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<CompositeParent>(new MemoryEngine()));
            var a = Guid.NewGuid();
            repo.Create(new CompositeParent { A = a, B = 1, Name = "one" });
            await repo.CreateAsync(new CompositeParent { A = a, B = 2, Name = "two" });

            Assert.Equal("two", repo.Single<CompositeParent>(x => x.A == a && x.B == 2)!.Name);
            repo.Update(new CompositeParent { A = a, B = 2, Name = "updated" });
            Assert.Equal(["one", "updated"], repo.Many<CompositeParent>(x => x.A == a).Select(x => x.Name).Order());
            repo.DeleteByID<CompositeParent>((object)new object[] { a, 1 }); //an object[] alone is a collection of IDs
            await repo.DeleteAsync(new CompositeParent { A = a, B = 2 });
            Assert.Empty(await repo.ManyAsync<CompositeParent>(x => x.A == a));
        }

        [Fact]
        public async Task EventStore_SavedStates()
        {
            var repo = Repo.New();
            repo.AddProvider(new EventStoreAsTransactStoreProvider<CompositeParent>(new MemoryEngine(), 2));
            var model = new CompositeParent { A = Guid.NewGuid(), B = 5, Name = "0" };
            repo.Create(model);
            for (var i = 1; i <= 4; i++)
            {
                model.Name = $"{i}";
                if (i % 2 == 0)
                    await repo.UpdateAsync(model);
                else
                    repo.Update(model);
            }

            Assert.Equal("4", repo.Single<CompositeParent>(x => x.A == model.A && x.B == 5)!.Name);
            Assert.Equal("3", repo.TemporalFirst<CompositeParent>(TemporalOrder.Newest, (ulong?)null, 3, null, null, x => x.A == model.A && x.B == 5, null, null)!.Name);
            Assert.Equal(["2", "3", "4"], repo.TemporalMany<CompositeParent>(TemporalOrder.Oldest, (ulong?)2, null, null, null, x => x.A == model.A && x.B == 5).Select(x => x.Name));
        }
    }
}
