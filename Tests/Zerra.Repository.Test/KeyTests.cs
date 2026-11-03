// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Linq;

namespace Zerra.Repository.Test
{
    public class KeyTests
    {
        //a key of several members compares integers of any size alike, and bytes by their content
        [Fact]
        public void LinqKeyComparesValues()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var key = LinqKey.Of([(byte)1, (short)2, bytes, "a"], [0, 1, 2, 3])!;
            var same = LinqKey.Of([1L, (ushort)2, new byte[] { 1, 2, 3 }, "a"], [0, 1, 2, 3])!;
            Assert.Equal(key, same);
            Assert.True(key.Equals((object)same));
            Assert.Equal(key.GetHashCode(), same.GetHashCode());
            Assert.Equal(LinqKey.Of([(sbyte)5, 6u], [0, 1]), LinqKey.Of([5, 6L], [0, 1]));

            Assert.NotEqual(key, LinqKey.Of([1L, 2L, new byte[] { 1, 2, 4 }, "a"], [0, 1, 2, 3]));
            Assert.NotEqual(key, LinqKey.Of([1L, 2L, "not bytes", "a"], [0, 1, 2, 3]));
            Assert.NotEqual(key, LinqKey.Of([1L, 2L], [0, 1]));
            Assert.False(key.Equals((LinqKey?)null));
            Assert.False(key.Equals("a"));

            //the columns pick the parts, and a null part refers to nothing
            Assert.Equal(LinqKey.Of(["x", 1, "y"], [2, 0]), LinqKey.Of(["y", "x"], [0, 1]));
            Assert.Null(LinqKey.Of([1, null], [0, 1]));
        }

        [Fact]
        public void TypeKeyComparesItsParts()
        {
            var key = new TypeKey("a", typeof(int));
            Assert.Equal("a", key.Str);
            Assert.Equal(typeof(int), key.Type);
            Assert.Equal(key, new TypeKey("a", typeof(int)));
            Assert.Equal(key.GetHashCode(), new TypeKey("a", typeof(int)).GetHashCode());
            Assert.NotEqual(key, new TypeKey("b", typeof(int)));
            Assert.NotEqual(key, new TypeKey("a", typeof(long)));
            Assert.NotEqual(key, new TypeKey(typeof(int)));
            Assert.False(key.Equals(null));
            Assert.False(key.Equals("a"));
            Assert.Equal(new TypeKey(typeof(int)), new TypeKey(typeof(int)));
            Assert.Equal(new TypeKey(null, null), new TypeKey(null, null));
            Assert.Equal(new TypeKey(null, null).GetHashCode(), new TypeKey(null, null).GetHashCode());
            Assert.Contains("a", key.ToString());
            Assert.NotNull(new TypeKey(null, null).ToString());
        }
    }
}
