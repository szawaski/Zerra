// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using System.Reflection;
using Xunit;
using Zerra.Repository.Linq;

namespace Zerra.Repository.Test.Linq
{
    public sealed class LinqAppenderTests
    {
        private sealed class SimpleModel
        {
            public int Value { get; set; }
            public string Name { get; set; }
            public List<int> Items { get; set; }
            public ChildModel Child { get; set; }
        }

        private sealed class ChildModel
        {
            public int ChildValue { get; set; }
        }

        [Fact]
        public void AppendAnd_CombinesTwoPredicatesWithAnd()
        {
            Expression<Func<SimpleModel, bool>> first = x => x.Value > 5;
            Expression<Func<SimpleModel, bool>> second = x => x.Value < 20;

            var combined = LinqAppender.AppendAnd<SimpleModel>(first, second);

            var compiled = combined.Compile();
            Assert.True(compiled(new SimpleModel { Value = 10 }));
            Assert.False(compiled(new SimpleModel { Value = 3 }));
            Assert.False(compiled(new SimpleModel { Value = 25 }));
        }

        [Fact]
        public void AppendAnd_ResultIsAndAlsoNode()
        {
            Expression<Func<SimpleModel, bool>> first = x => x.Value > 0;
            Expression<Func<SimpleModel, bool>> second = x => x.Value < 100;

            var combined = LinqAppender.AppendAnd<SimpleModel>(first, second);

            Assert.Equal(ExpressionType.AndAlso, combined.Body.NodeType);
        }

        [Fact]
        public void AppendAnd_WithAlwaysFalseFirst_ReturnsFalse()
        {
            Expression<Func<SimpleModel, bool>> first = x => false;
            Expression<Func<SimpleModel, bool>> second = x => x.Value > 0;

            var combined = LinqAppender.AppendAnd<SimpleModel>(first, second);
            var compiled = combined.Compile();

            Assert.False(compiled(new SimpleModel { Value = 10 }));
        }

        [Fact]
        public void AppendExpressionOnMember_ScalarProperty_AppliesPredicateWithNullGuard()
        {
            Expression<Func<SimpleModel, bool>> it = x => true;
            var member = typeof(SimpleModel).GetProperty(nameof(SimpleModel.Child))!;
            Expression<Func<ChildModel, bool>> childPredicate = c => c.ChildValue == 42;

            var combined = LinqAppender.AppendExpressionOnMember<SimpleModel>(it, member, childPredicate);
            var compiled = combined.Compile();

            // null child => null guard short-circuits to true
            Assert.True(compiled(new SimpleModel { Child = null }));
            // matching child value
            Assert.True(compiled(new SimpleModel { Child = new ChildModel { ChildValue = 42 } }));
            // non-matching child value
            Assert.False(compiled(new SimpleModel { Child = new ChildModel { ChildValue = 99 } }));
        }

        [Fact]
        public void AppendExpressionOnMember_CollectionProperty_UsesAnyCheck()
        {
            Expression<Func<SimpleModel, bool>> it = x => true;
            var member = typeof(SimpleModel).GetProperty(nameof(SimpleModel.Items))!;
            Expression<Func<int, bool>> itemPredicate = i => i > 5;

            var combined = LinqAppender.AppendExpressionOnMember<SimpleModel>(it, member, itemPredicate);
            var compiled = combined.Compile();

            // empty collection => Any() is false, so emptyCheck (Not(Any1) || Any2) is true
            Assert.True(compiled(new SimpleModel { Items = [] }));
            // collection with matching item
            Assert.True(compiled(new SimpleModel { Items = [10] }));
            // collection with no matching item
            Assert.False(compiled(new SimpleModel { Items = [1, 2] }));
        }

        [Fact]
        public void AppendExpressionOnMember_InvalidMember_ThrowsArgumentException()
        {
            Expression<Func<SimpleModel, bool>> it = x => true;
            var method = typeof(SimpleModel).GetMethod(nameof(object.ToString))!;
            Expression<Func<object, bool>> pred = o => true;

            Assert.Throws<ArgumentException>(() =>
                LinqAppender.AppendExpressionOnMember<SimpleModel>(it, method, pred));
        }
    }
}
