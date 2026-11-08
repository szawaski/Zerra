// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    //a column is NOT NULL when the member can't be null, unless [StoreProperties] sets NotNull
    public class NotNullTests
    {
#nullable enable
        [Entity("NotNullModel")]
        public sealed class AnnotatedModel
        {
            [Identity]
            public int ID { get; set; }
            public int Count { get; set; }
            public int? Maybe { get; set; }
            public string Name { get; set; } = null!;
            public string? Nick { get; set; }
            public byte[] Data { get; set; } = null!;
            public byte[]? DataNull { get; set; }

            [StoreProperties(64)]
            public string Code { get; set; } = null!;
            [StoreProperties(18, 2)]
            public decimal Price { get; set; }
            [StoreProperties(18, 2)]
            public decimal? PriceNull { get; set; }
            [StoreProperties(StoreTextEncoding.NonUnicode, 10)]
            public string? Short { get; set; }

            [StoreProperties(true, 64)]
            public string? ForcedNotNull { get; set; }
            [StoreProperties(64, NotNull = false)]
            public string ForcedNull { get; set; } = null!;
            [StoreProperties(false)]
            public int ForcedNullValue { get; set; }
        }
#nullable disable

        [Entity("NotNullOblivious")]
        public sealed class ObliviousModel
        {
            [Identity]
            public int ID { get; set; }
            public string Name { get; set; }
            public byte[] Data { get; set; }
            [StoreProperties(18, 2)]
            public decimal Price { get; set; }
        }

        [Fact]
        public void FromNullability()
        {
            var model = ModelAnalyzer.GetModel(typeof(AnnotatedModel));
            bool NotNull(string name) => model.GetMember(name).IsDataSourceNotNull;

            Assert.True(NotNull(nameof(AnnotatedModel.ID)));
            Assert.True(NotNull(nameof(AnnotatedModel.Count)));
            Assert.False(NotNull(nameof(AnnotatedModel.Maybe)));
            Assert.True(NotNull(nameof(AnnotatedModel.Name)));
            Assert.False(NotNull(nameof(AnnotatedModel.Nick)));
            Assert.True(NotNull(nameof(AnnotatedModel.Data)));
            Assert.False(NotNull(nameof(AnnotatedModel.DataNull)));

            //an attribute that doesn't say NotNull leaves it to the member
            Assert.True(NotNull(nameof(AnnotatedModel.Code)));
            Assert.True(NotNull(nameof(AnnotatedModel.Price)));
            Assert.False(NotNull(nameof(AnnotatedModel.PriceNull)));
            Assert.False(NotNull(nameof(AnnotatedModel.Short)));

            //one that does wins
            Assert.True(NotNull(nameof(AnnotatedModel.ForcedNotNull)));
            Assert.False(NotNull(nameof(AnnotatedModel.ForcedNull)));
            Assert.False(NotNull(nameof(AnnotatedModel.ForcedNullValue)));
        }

        [Fact]
        public void WithoutAnnotations()
        {
            var model = ModelAnalyzer.GetModel(typeof(ObliviousModel));

            //reference types allow null in code without nullable annotations
            Assert.False(model.GetMember(nameof(ObliviousModel.Name)).IsDataSourceNotNull);
            Assert.False(model.GetMember(nameof(ObliviousModel.Data)).IsDataSourceNotNull);
            Assert.True(model.GetMember(nameof(ObliviousModel.Price)).IsDataSourceNotNull);
        }

        [Fact]
        public void AttributeRemembersWhetherNotNullWasSet()
        {
            Assert.False(new StorePropertiesAttribute(64).IsNotNullSet);
            Assert.False(new StorePropertiesAttribute(18, 2).IsNotNullSet);
            Assert.False(new StorePropertiesAttribute(StoreTextEncoding.NonUnicode).IsNotNullSet);
            Assert.False(new StorePropertiesAttribute(StoreDatePart.Date).IsNotNullSet);
            Assert.True(new StorePropertiesAttribute(true).IsNotNullSet);
            Assert.True(new StorePropertiesAttribute(false, 64).IsNotNullSet);
            Assert.True(new StorePropertiesAttribute(true, 18, 2).IsNotNullSet);
            Assert.True(new StorePropertiesAttribute(64) { NotNull = false }.IsNotNullSet);
        }
    }
}
