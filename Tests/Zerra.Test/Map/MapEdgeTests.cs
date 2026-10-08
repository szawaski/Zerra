// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Map;
using Zerra.Test.Helpers.Models;
using Zerra.Test.Helpers.TypesModels;

namespace Zerra.Test.Map
{
    public class MapEdgeTests
    {
        public sealed class Holder<T>
        {
            public T? Value { get; set; }
        }

        private static void AssertCollection<TTarget>(Func<TTarget, IEnumerable<int>> items) where TTarget : class
        {
            Assert.Null(new Holder<IEnumerable<int>>().Map<Holder<IEnumerable<int>>, Holder<TTarget>>().Value);

            //a source that isn't a collection is counted by enumerating it
            var source = new Holder<IEnumerable<int>>() { Value = Enumerable.Range(1, 3).Select(x => x) };
            var result = source.Map<Holder<IEnumerable<int>>, Holder<TTarget>>().Value;
            Assert.NotNull(result);
            Assert.Equal([1, 2, 3], items(result).Order());
        }

        [Fact]
        public void Collections_FromNullOrAnEnumerable()
        {
            AssertCollection<IReadOnlyCollection<int>>(x => x);
            AssertCollection<IReadOnlySet<int>>(x => x);
            AssertCollection<IReadOnlyList<int>>(x => x);
            AssertCollection<IList<int>>(x => x);
            AssertCollection<ISet<int>>(x => x);
            AssertCollection<ICollection<int>>(x => x);
            AssertCollection<List<int>>(x => x);
            AssertCollection<int[]>(x => x);
            AssertCollection<TypesIListTOfTModel.CustomIList<int>>(x => x);
            AssertCollection<TypesISetTOfTModel.CustomISet<int>>(x => x);
            AssertCollection<TypesICollectionTOfTModel.CustomICollection<int>>(x => x);
        }

        public sealed class ConstructorSource { public int A { get; set; } }
        public sealed class ConstructorTarget
        {
            public int A { get; }
            public ConstructorTarget(ConstructorTarget copy) { A = copy.A; }
            public ConstructorTarget(int a, int unmatched) { A = a + unmatched; }
            public ConstructorTarget(int a) { A = a; }
        }
        public sealed class NoConstructorTarget
        {
            public int A { get; set; }
            public NoConstructorTarget(string unrelated) { }
        }

        [Fact]
        public void Target_BuiltThroughAMatchingConstructor()
        {
            //a constructor taking its own type, or an argument without a member, isn't used
            Assert.Equal(5, new ConstructorSource() { A = 5 }.Map<ConstructorSource, ConstructorTarget>().A);

            _ = Assert.Throws<InvalidOperationException>(() => new ConstructorSource() { A = 5 }.Map<ConstructorSource, NoConstructorTarget>());
            _ = Assert.Throws<NotSupportedException>(() => new SimpleModel().Map<SimpleModel, int>());
        }

        public sealed class CustomizedSource { public string? Text { get; set; } public int Value { get; set; } }
        public sealed class CustomizedTarget { public string? Text { get; set; } public int Value { get; set; } }
        private sealed class CustomizedMap : IMapDefinition<CustomizedSource, CustomizedTarget>
        {
            public void Define(IMapSetup<CustomizedSource, CustomizedTarget> map) => map.Define(x => x.Text, x => $"custom {x.Value}");
        }

        [Fact]
        public void CustomizedMember_ReplacesTheSameNamedOne()
        {
            MapDefinition.Register(new CustomizedMap());
            var target = new CustomizedSource() { Text = "plain", Value = 3 }.Map<CustomizedSource, CustomizedTarget>();
            Assert.Equal("custom 3", target.Text);
            Assert.Equal(3, target.Value);
        }

        [Fact]
        public void TypePairKey_EqualityAndText()
        {
            var key = new TypePairKey(typeof(int), typeof(string));
            Assert.Equal(typeof(int), key.Type1);
            Assert.Equal(typeof(string), key.Type2);
            Assert.Equal(new TypePairKey(typeof(int), typeof(string)), key);
            Assert.Equal(new TypePairKey(typeof(int), typeof(string)).GetHashCode(), key.GetHashCode());
            Assert.NotEqual(new TypePairKey(typeof(string), typeof(int)), key);
            Assert.False(key.Equals("System.Int32, System.String"));
            Assert.Equal("System.Int32, System.String", key.ToString());
        }

        [Fact]
        public void MapException_Messages()
        {
            var inner = new InvalidOperationException();
            Assert.Equal("message", new MapException("message").Message);
            Assert.Same(inner, new MapException("message", inner).InnerException);
        }
    }
}
