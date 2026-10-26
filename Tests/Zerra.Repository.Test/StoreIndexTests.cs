// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Memory;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    public class StoreIndexTests
    {
        [Entity("IndexModel")]
        public sealed class IndexModel
        {
            [Identity]
            public Guid ID { get; set; }
            [StoreIndex]
            [StoreName("NumberColumn")]
            public int Number { get; set; }
            [StoreIndex("Combined", Order = 2)]
            public int Third { get; set; }
            [StoreIndex("Combined", Order = 1, Descending = true)]
            public DateTime Second { get; set; }
            [StoreIndex("combined", Unique = false)]
            [StoreIndex(Unique = true)]
            [StoreProperties(20)]
            public string? First { get; set; }
            [StoreIndex]
            public int? Nullable { get; set; }
            public int NotIndexed { get; set; }
        }

        [Fact]
        public void IndexesFromAttributes()
        {
            var model = ModelAnalyzer.GetModel(typeof(IndexModel));
            Assert.Equal(4, model.Indexes.Count);

            var number = Assert.Single(model.Indexes, x => x.StoreName == "IX_IndexModel_NumberColumn");
            Assert.False(number.IsUnique);
            Assert.Equal("Number", Assert.Single(number.Members).Name);

            var combined = Assert.Single(model.Indexes, x => x.StoreName == "IX_IndexModel_Combined");
            Assert.False(combined.IsUnique);
            Assert.Equal(["First", "Second", "Third"], combined.Members.Select(x => x.Name));
            Assert.Equal([false, true, false], combined.Descending);

            var first = Assert.Single(model.Indexes, x => x.StoreName == "IX_IndexModel_First");
            Assert.True(first.IsUnique);

            var nullable = Assert.Single(model.Indexes, x => x.StoreName == "IX_IndexModel_Nullable");
            Assert.False(Assert.Single(nullable.Members).IsDataSourceNotNull);
        }

        public sealed class NotEntityModel
        {
            [StoreIndex]
            public int Number { get; set; }
        }

        [Fact]
        public void NoIndexesWithoutEntity()
        {
            Assert.Empty(ModelAnalyzer.GetModel(typeof(NotEntityModel)).Indexes);
        }

        [Entity("IndexStringNoLength")]
        public sealed class StringNoLengthModel
        {
            [Identity]
            public Guid ID { get; set; }
            [StoreIndex]
            public string? Text { get; set; }
        }

        [Entity("IndexBytesNoLength")]
        public sealed class BytesNoLengthModel
        {
            [Identity]
            public Guid ID { get; set; }
            [StoreIndex]
            public byte[]? Bytes { get; set; }
        }

        [Entity("IndexUniqueMismatch")]
        public sealed class UniqueMismatchModel
        {
            [Identity]
            public Guid ID { get; set; }
            [StoreIndex("Both", Unique = true)]
            public int A { get; set; }
            [StoreIndex("Both")]
            public int B { get; set; }
        }

        [Entity("IndexTwice")]
        public sealed class TwiceModel
        {
            [Identity]
            public Guid ID { get; set; }
            [StoreIndex("Same")]
            [StoreIndex("Same")]
            public int A { get; set; }
        }

        [Entity("IndexRelation")]
        public sealed class RelationModel
        {
            [Identity]
            public Guid ID { get; set; }
            public Guid OtherID { get; set; }
            [StoreIndex]
            [Relation(nameof(OtherID))]
            public NotEntityModel? Other { get; set; }
        }

        [Entity("IndexNameLongerThanSixtyThreeCharactersWhenPrefixed")]
        public sealed class LongNameModel
        {
            [Identity]
            public Guid ID { get; set; }
            [StoreIndex("AlsoQuiteALongIndexName")]
            public int A { get; set; }
        }

        [Fact]
        public void InvalidIndexesThrow()
        {
            _ = Assert.Throws<Exception>(() => ModelAnalyzer.GetModel(typeof(StringNoLengthModel)));
            _ = Assert.Throws<Exception>(() => ModelAnalyzer.GetModel(typeof(BytesNoLengthModel)));
            _ = Assert.Throws<Exception>(() => ModelAnalyzer.GetModel(typeof(UniqueMismatchModel)));
            _ = Assert.Throws<Exception>(() => ModelAnalyzer.GetModel(typeof(TwiceModel)));
            _ = Assert.Throws<Exception>(() => ModelAnalyzer.GetModel(typeof(RelationModel)));
            _ = Assert.Throws<Exception>(() => ModelAnalyzer.GetModel(typeof(LongNameModel)));
            _ = Assert.Throws<ArgumentException>(() => new StoreIndexAttribute("bad name"));
            _ = Assert.Throws<ArgumentException>(() => new StoreIndexAttribute(""));
        }

        [Fact]
        public void MemoryEngineIgnoresIndexes()
        {
            var engine = new MemoryEngine();
            CodeFirstGeneration.Generate(engine, DataStoreGenerationType.CodeFirst, [typeof(IndexModel)]);
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<IndexModel>(engine));
            repo.Create(new IndexModel() { ID = Guid.NewGuid(), First = "a" });
            repo.Create(new IndexModel() { ID = Guid.NewGuid(), First = "a" });
            Assert.Equal(2L, repo.Count<IndexModel>());
        }
    }
}