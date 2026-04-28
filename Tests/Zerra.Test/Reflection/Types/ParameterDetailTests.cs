// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection.Types
{
    public class ParameterDetailTests
    {
        [Fact]
        public void Constructor_SetsTypeAndName()
        {
            var detail = TestReflectionDetailFactory.MakeParameter(typeof(int), "count");

            Assert.Equal(typeof(int), detail.Type);
            Assert.Equal("count", detail.Name);
        }

        [Fact]
        public void Constructor_StringType_SetsCorrectly()
        {
            var detail = TestReflectionDetailFactory.MakeParameter(typeof(string), "value");

            Assert.Equal(typeof(string), detail.Type);
            Assert.Equal("value", detail.Name);
        }

        [Fact]
        public void Constructor_NullableType_SetsCorrectly()
        {
            var detail = TestReflectionDetailFactory.MakeParameter(typeof(int?), "maybeCount");

            Assert.Equal(typeof(int?), detail.Type);
            Assert.Equal("maybeCount", detail.Name);
        }

        [Fact]
        public void TypeDetail_ReturnsCachedTypeDetail()
        {
            var detail = TestReflectionDetailFactory.MakeParameter(typeof(int), "x");

            var td1 = detail.TypeDetail;
            var td2 = detail.TypeDetail;

            Assert.NotNull(td1);
            Assert.Same(td1, td2);
            Assert.Equal(typeof(int), td1.Type);
        }

        [Fact]
        public void TypeDetail_ComplexType_ReturnsTypeDetail()
        {
            var detail = TestReflectionDetailFactory.MakeParameter(typeof(TestReflectionClass), "obj");

            var td = detail.TypeDetail;

            Assert.NotNull(td);
            Assert.Equal(typeof(TestReflectionClass), td.Type);
        }
    }
}
