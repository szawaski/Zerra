// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection
{
    public class MemberAndParameterNameComparerTests
    {
        private static readonly MemberAndParameterNameComparer comparer = MemberAndParameterNameComparer.Instance;

        [Fact]
        public void Equals_SameString_ReturnsTrue()
        {
            Assert.True(comparer.Equals("myProperty", "myProperty"));
        }

        [Fact]
        public void Equals_DifferentCase_ReturnsTrue()
        {
            Assert.True(comparer.Equals("MyProperty", "myProperty"));
            Assert.True(comparer.Equals("myProperty", "MyProperty"));
            Assert.True(comparer.Equals("MYPROPERTY", "myproperty"));
        }

        [Fact]
        public void Equals_UnderscorePrefix_ReturnsTrue()
        {
            Assert.True(comparer.Equals("_myProperty", "myProperty"));
            Assert.True(comparer.Equals("myProperty", "_myProperty"));
            Assert.True(comparer.Equals("_myProperty", "_myProperty"));
        }

        [Fact]
        public void Equals_UnderscorePrefixWithDifferentCase_ReturnsTrue()
        {
            Assert.True(comparer.Equals("_myProperty", "MyProperty"));
            Assert.True(comparer.Equals("MyProperty", "_myProperty"));
            Assert.True(comparer.Equals("_MyProperty", "myProperty"));
        }

        [Fact]
        public void Equals_DifferentStrings_ReturnsFalse()
        {
            Assert.False(comparer.Equals("myProperty", "otherProperty"));
            Assert.False(comparer.Equals("myProp", "myProperty"));
        }

        [Fact]
        public void Equals_BothNull_ReturnsTrue()
        {
            Assert.True(comparer.Equals(null, null));
        }

        [Fact]
        public void Equals_OneNull_ReturnsFalse()
        {
            Assert.False(comparer.Equals(null, "myProperty"));
            Assert.False(comparer.Equals("myProperty", null));
        }

        [Fact]
        public void Equals_EmptyStrings_ReturnsTrue()
        {
            Assert.True(comparer.Equals(string.Empty, string.Empty));
        }

        [Fact]
        public void Equals_UnderscoreAndEmpty_ReturnsTrue()
        {
            // A lone underscore strips to empty, so it equals an empty string
            Assert.True(comparer.Equals("_", string.Empty));
            Assert.True(comparer.Equals(string.Empty, "_"));
        }

        [Fact]
        public void GetHashCode_SameString_SameHash()
        {
            var h1 = comparer.GetHashCode("myProperty");
            var h2 = comparer.GetHashCode("myProperty");
            Assert.Equal(h1, h2);
        }

        [Fact]
        public void GetHashCode_DifferentCase_SameHash()
        {
            var h1 = comparer.GetHashCode("MyProperty");
            var h2 = comparer.GetHashCode("myProperty");
            Assert.Equal(h1, h2);
        }

        [Fact]
        public void GetHashCode_UnderscorePrefix_SameHash()
        {
            var h1 = comparer.GetHashCode("_myProperty");
            var h2 = comparer.GetHashCode("myProperty");
            Assert.Equal(h1, h2);
        }

        [Fact]
        public void GetHashCode_UnderscorePrefixDifferentCase_SameHash()
        {
            var h1 = comparer.GetHashCode("_MyProperty");
            var h2 = comparer.GetHashCode("myProperty");
            Assert.Equal(h1, h2);
        }

        [Fact]
        public void GetHashCode_DifferentStrings_DifferentHash()
        {
            var h1 = comparer.GetHashCode("myProperty");
            var h2 = comparer.GetHashCode("otherProperty");
            Assert.NotEqual(h1, h2);
        }

        [Fact]
        public void GetHashCode_LongString_NoException()
        {
            var longString = new string('a', 200);
            var hash = comparer.GetHashCode(longString);
            Assert.IsType<int>(hash);
        }

        [Fact]
        public void UsedAsDictionaryKey_MatchesEquivalentNames()
        {
            var dict = new Dictionary<string, int>(comparer)
            {
                ["myProperty"] = 42
            };

            Assert.Equal(42, dict["MyProperty"]);
            Assert.Equal(42, dict["_myProperty"]);
            Assert.Equal(42, dict["_MyProperty"]);
            Assert.Equal(42, dict["myProperty"]);
        }
    }
}
