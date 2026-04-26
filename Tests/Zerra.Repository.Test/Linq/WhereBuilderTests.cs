// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Linq;

namespace Zerra.Repository.Test.Linq
{
    public sealed class WhereBuilderTests
    {
        private sealed class SampleModel
        {
            public int Value { get; set; }
            public string? Name { get; set; }
        }

        [Fact]
        public void Build_SingleExpression_ReturnsMatchingPredicate()
        {
            var builder = new WhereBuilder<SampleModel>(x => x.Value > 10);

            var expr = builder.Build();
            var compiled = expr.Compile();

            Assert.True(compiled(new SampleModel { Value = 15 }));
            Assert.False(compiled(new SampleModel { Value = 5 }));
        }

        [Fact]
        public void Build_AndExpression_BothMustMatch()
        {
            var builder = new WhereBuilder<SampleModel>(x => x.Value > 5);
            builder.And(x => x.Value < 20);

            var expr = builder.Build();
            var compiled = expr.Compile();

            Assert.True(compiled(new SampleModel { Value = 10 }));
            Assert.False(compiled(new SampleModel { Value = 3 }));
            Assert.False(compiled(new SampleModel { Value = 25 }));
        }

        [Fact]
        public void Build_OrExpression_EitherCanMatch()
        {
            var builder = new WhereBuilder<SampleModel>(x => x.Value < 5);
            builder.Or(x => x.Value > 95);

            var expr = builder.Build();
            var compiled = expr.Compile();

            Assert.True(compiled(new SampleModel { Value = 1 }));
            Assert.True(compiled(new SampleModel { Value = 99 }));
            Assert.False(compiled(new SampleModel { Value = 50 }));
        }

        [Fact]
        public void Build_AndGroup_GroupedExpressionIsEvaluatedTogether()
        {
            // (Value > 0) AND (Value < 5 OR Value > 95)
            var builder = new WhereBuilder<SampleModel>(x => x.Value > 0);
            builder.StartGroupAnd();
            builder.And(x => x.Value < 5);
            builder.Or(x => x.Value > 95);
            builder.EndGroup();

            var expr = builder.Build();
            var compiled = expr.Compile();

            Assert.True(compiled(new SampleModel { Value = 2 }));
            Assert.True(compiled(new SampleModel { Value = 99 }));
            Assert.False(compiled(new SampleModel { Value = 50 }));
            Assert.False(compiled(new SampleModel { Value = 0 }));
        }

        [Fact]
        public void Build_OrGroup_GroupedExpressionIsEvaluatedTogether()
        {
            // (Value == 1) OR (Value > 10 AND Value < 20)
            var builder = new WhereBuilder<SampleModel>(x => x.Value == 1);
            builder.StartGroupOr();
            builder.And(x => x.Value > 10);
            builder.And(x => x.Value < 20);
            builder.EndGroup();

            var expr = builder.Build();
            var compiled = expr.Compile();

            Assert.True(compiled(new SampleModel { Value = 1 }));
            Assert.True(compiled(new SampleModel { Value = 15 }));
            Assert.False(compiled(new SampleModel { Value = 25 }));
            Assert.False(compiled(new SampleModel { Value = 5 }));
        }

        [Fact]
        public void Build_ReturnedExpressionHasSingleParameter()
        {
            var builder = new WhereBuilder<SampleModel>(x => x.Value == 42);
            var expr = builder.Build();

            Assert.Single(expr.Parameters);
        }

        [Fact]
        public void ToString_ReturnsNonEmptyString()
        {
            var builder = new WhereBuilder<SampleModel>(x => x.Value == 42);
            var str = builder.ToString();

            Assert.NotNull(str);
            Assert.NotEmpty(str);
        }

        [Fact]
        public void Build_MultipleAnds_AllMustMatch()
        {
            var builder = new WhereBuilder<SampleModel>(x => x.Value > 0);
            builder.And(x => x.Value < 100);
            builder.And(x => x.Value != 50);

            var expr = builder.Build();
            var compiled = expr.Compile();

            Assert.True(compiled(new SampleModel { Value = 25 }));
            Assert.False(compiled(new SampleModel { Value = 50 }));
            Assert.False(compiled(new SampleModel { Value = 0 }));
            Assert.False(compiled(new SampleModel { Value = 100 }));
        }

        [Fact]
        public void Build_MultipleOrs_AnyCanMatch()
        {
            var builder = new WhereBuilder<SampleModel>(x => x.Value == 1);
            builder.Or(x => x.Value == 2);
            builder.Or(x => x.Value == 3);

            var expr = builder.Build();
            var compiled = expr.Compile();

            Assert.True(compiled(new SampleModel { Value = 1 }));
            Assert.True(compiled(new SampleModel { Value = 2 }));
            Assert.True(compiled(new SampleModel { Value = 3 }));
            Assert.False(compiled(new SampleModel { Value = 4 }));
        }
    }
}
