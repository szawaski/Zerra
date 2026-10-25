// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;

namespace Zerra.Repository.Test
{
    public class MemoryExpressionRewriterTests
    {
        //the interpreter is what runs expressions when dynamic code isn't supported, such as with AOT

        [Fact]
        public void ArrayContains_ValueType_RunsInterpreted()
        {
            var keys = new[] { Guid.NewGuid(), Guid.NewGuid() };
            Expression<Func<TestTypesModel, bool>> where = x => keys.Contains(x.KeyA);

            var rewritten = MemoryExpressionRewriter.Rewrite(where, false);
            Assert.DoesNotContain(nameof(MemoryExtensions), rewritten.ToString());

            var predicate = rewritten.Compile(preferInterpretation: true);
            Assert.True(predicate(new TestTypesModel() { KeyA = keys[1] }));
            Assert.False(predicate(new TestTypesModel() { KeyA = Guid.NewGuid() }));
        }

        [Fact]
        public void ArrayContains_ReferenceTypeAndNull_RunsInterpreted()
        {
            var values = new[] { "a", null };
            Expression<Func<TestTypesModel, bool>> where = x => values.Contains(x.StringThing);

            var predicate = MemoryExpressionRewriter.Rewrite(where, false).Compile(preferInterpretation: true);
            Assert.True(predicate(new TestTypesModel() { StringThing = "a" }));
            Assert.True(predicate(new TestTypesModel() { StringThing = null }));
            Assert.False(predicate(new TestTypesModel() { StringThing = "b" }));
        }

        [Fact]
        public void ArrayContains_InsideCondition_RunsInterpreted()
        {
            var numbers = new[] { 1, 2, 3 };
            Expression<Func<TestTypesModel, bool>> where = x => x.BooleanThing && !numbers.Contains(x.Int32Thing);

            var predicate = MemoryExpressionRewriter.Rewrite(where, false).Compile(preferInterpretation: true);
            Assert.True(predicate(new TestTypesModel() { BooleanThing = true, Int32Thing = 4 }));
            Assert.False(predicate(new TestTypesModel() { BooleanThing = true, Int32Thing = 2 }));
        }

        [Fact]
        public void ListContains_IsUnchanged()
        {
            var numbers = new List<int>() { 1, 2, 3 };
            Expression<Func<TestTypesModel, bool>> where = x => numbers.Contains(x.Int32Thing);

            Assert.Same(where, MemoryExpressionRewriter.Rewrite(where, false));
        }

        [Fact]
        public void StringCaseSensitivity()
        {
            var value = "hello";
            Expression<Func<TestTypesModel, bool>> where = x => value == x.StringThing;

            var predicate = MemoryExpressionRewriter.Rewrite(where, true).Compile(preferInterpretation: true);
            var predicateCaseInsensitive = MemoryExpressionRewriter.Rewrite(where, false).Compile(preferInterpretation: true);

            var model = new TestTypesModel() { StringThing = "Hello" };
            Assert.False(predicate(model));
            Assert.True(predicateCaseInsensitive(model));
        }
    }
}
