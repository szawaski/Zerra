// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;

namespace Zerra.Repository.Test.Linq
{
    public sealed class LinqValueExtractorTests
    {
        [Fact]
        public void Extract_SimpleEquality_ExtractsValue()
        {
            var expectedValue = 42;
            Expression<Func<TestTypesModel, bool>> where = x => x.Int32Thing == expectedValue;

            var result = LinqValueExtractor.Extract(where, typeof(TestTypesModel), nameof(TestTypesModel.Int32Thing));

            Assert.True(result.ContainsKey(nameof(TestTypesModel.Int32Thing)));
            Assert.Single(result[nameof(TestTypesModel.Int32Thing)]);
            Assert.Equal(expectedValue, result[nameof(TestTypesModel.Int32Thing)][0]);
        }

        [Fact]
        public void Extract_EqualityReversed_ExtractsValue()
        {
            var expectedValue = 99;
            Expression<Func<TestTypesModel, bool>> where = x => expectedValue == x.Int32Thing;

            var result = LinqValueExtractor.Extract(where, typeof(TestTypesModel), nameof(TestTypesModel.Int32Thing));

            Assert.True(result.ContainsKey(nameof(TestTypesModel.Int32Thing)));
            Assert.Single(result[nameof(TestTypesModel.Int32Thing)]);
            Assert.Equal(expectedValue, result[nameof(TestTypesModel.Int32Thing)][0]);
        }

        [Fact]
        public void Extract_NullEquality_ExtractsNull()
        {
            Expression<Func<TestTypesModel, bool>> where = x => x.Int32NullableThing == null;

            var result = LinqValueExtractor.Extract(where, typeof(TestTypesModel), nameof(TestTypesModel.Int32NullableThing));

            Assert.True(result.ContainsKey(nameof(TestTypesModel.Int32NullableThing)));
            Assert.Single(result[nameof(TestTypesModel.Int32NullableThing)]);
            Assert.Null(result[nameof(TestTypesModel.Int32NullableThing)][0]);
        }

        [Fact]
        public void Extract_AndExpression_ExtractsBothValues()
        {
            var val1 = 10;
            long val2 = 20L;
            Expression<Func<TestTypesModel, bool>> where = x => x.Int32Thing == val1 && x.Int64Thing == val2;

            var result = LinqValueExtractor.Extract(where, typeof(TestTypesModel),
                nameof(TestTypesModel.Int32Thing), nameof(TestTypesModel.Int64Thing));

            Assert.Equal(val1, result[nameof(TestTypesModel.Int32Thing)][0]);
            Assert.Equal(val2, result[nameof(TestTypesModel.Int64Thing)][0]);
        }

        [Fact]
        public void Extract_PropertyNotRequested_ReturnsEmptyListForThatProperty()
        {
            Expression<Func<TestTypesModel, bool>> where = x => x.Int32Thing == 5;

            var result = LinqValueExtractor.Extract(where, typeof(TestTypesModel), nameof(TestTypesModel.Int64Thing));

            Assert.True(result.ContainsKey(nameof(TestTypesModel.Int64Thing)));
            Assert.Empty(result[nameof(TestTypesModel.Int64Thing)]);
        }

        [Fact]
        public void Extract_DecimalEquality_ExtractsDecimalValue()
        {
            var amount = 9.99m;
            Expression<Func<TestTypesModel, bool>> where = x => x.DecimalThing == amount;

            var result = LinqValueExtractor.Extract(where, typeof(TestTypesModel), nameof(TestTypesModel.DecimalThing));

            Assert.Single(result[nameof(TestTypesModel.DecimalThing)]);
            Assert.Equal(amount, result[nameof(TestTypesModel.DecimalThing)][0]);
        }

        [Fact]
        public void Extract_GuidEquality_ExtractsGuidValue()
        {
            var id = Guid.NewGuid();
            Expression<Func<TestTypesModel, bool>> where = x => x.GuidThing == id;

            var result = LinqValueExtractor.Extract(where, typeof(TestTypesModel), nameof(TestTypesModel.GuidThing));

            Assert.Single(result[nameof(TestTypesModel.GuidThing)]);
            Assert.Equal(id, result[nameof(TestTypesModel.GuidThing)][0]);
        }

        [Fact]
        public void Extract_ContainsEnumerable_ExtractsAllValues()
        {
            var ids = new List<int> { 1, 2, 3 };
            Expression<Func<TestTypesModel, bool>> where = x => ids.Contains(x.Int32Thing);

            var result = LinqValueExtractor.Extract(where, typeof(TestTypesModel), nameof(TestTypesModel.Int32Thing));

            Assert.Equal(3, result[nameof(TestTypesModel.Int32Thing)].Count);
            Assert.Equal(1, result[nameof(TestTypesModel.Int32Thing)][0]);
            Assert.Equal(2, result[nameof(TestTypesModel.Int32Thing)][1]);
            Assert.Equal(3, result[nameof(TestTypesModel.Int32Thing)][2]);
        }

        [Fact]
        public void Extract_MultipleProperties_ReturnsDictionaryWithAllKeys()
        {
            var val = 7;
            var guid = Guid.NewGuid();
            Expression<Func<TestTypesModel, bool>> where = x => x.Int32Thing == val && x.GuidThing == guid;

            var result = LinqValueExtractor.Extract(where, typeof(TestTypesModel),
                nameof(TestTypesModel.Int32Thing), nameof(TestTypesModel.GuidThing));

            Assert.True(result.ContainsKey(nameof(TestTypesModel.Int32Thing)));
            Assert.True(result.ContainsKey(nameof(TestTypesModel.GuidThing)));
            Assert.Equal(val, result[nameof(TestTypesModel.Int32Thing)][0]);
            Assert.Equal(guid, result[nameof(TestTypesModel.GuidThing)][0]);
        }
    }
}
