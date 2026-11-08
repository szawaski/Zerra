// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection
{
    //without the source generator the members are read with NullabilityInfoContext
    public class MemberNullabilityTests
    {
#nullable enable
        public sealed class Annotated
        {
            public string Name { get; set; } = null!;
            public string? Nick { get; set; }
            public byte[] Data { get; set; } = null!;
            public byte[]? DataNull { get; set; }
            public int Count { get; set; }
            public int? Maybe { get; set; }
            public string Field = null!;
            public string? FieldNull;
        }
#nullable disable

        public sealed class Oblivious
        {
            public string Name { get; set; }
            public int Count { get; set; }
            public int? Maybe { get; set; }
        }

#nullable enable
#if !NETSTANDARD2_0
        //the netstandard2.0 build can't read annotations, its reference types allow null
        [Fact]
        public void AnnotatedMembers()
        {
            var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(Annotated));
            bool AllowsNull(string name) => typeDetail.Members.Single(x => x.Name == name).AllowsNull;

            Assert.False(AllowsNull(nameof(Annotated.Name)));
            Assert.True(AllowsNull(nameof(Annotated.Nick)));
            Assert.False(AllowsNull(nameof(Annotated.Data)));
            Assert.True(AllowsNull(nameof(Annotated.DataNull)));
            Assert.False(AllowsNull(nameof(Annotated.Count)));
            Assert.True(AllowsNull(nameof(Annotated.Maybe)));
            Assert.False(AllowsNull(nameof(Annotated.Field)));
            Assert.True(AllowsNull(nameof(Annotated.FieldNull)));
        }
#endif

        [Fact]
        public void ObliviousMembers()
        {
            var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(Oblivious));
            bool AllowsNull(string name) => typeDetail.Members.Single(x => x.Name == name).AllowsNull;

            //code without nullable annotations allows null in reference types
            Assert.True(AllowsNull(nameof(Oblivious.Name)));
            Assert.False(AllowsNull(nameof(Oblivious.Count)));
            Assert.True(AllowsNull(nameof(Oblivious.Maybe)));
        }

        [Fact]
        public void DecidedByTypeWhenNotGiven()
        {
            Assert.True(new MemberDetail(typeof(Oblivious), typeof(string), "Name", false, null, null, null, null, [], false, false).AllowsNull);
            Assert.False(new MemberDetail(typeof(Oblivious), typeof(int), "Count", false, null, null, null, null, [], false, false).AllowsNull);
            Assert.True(new MemberDetail(typeof(Oblivious), typeof(int?), "Maybe", false, null, null, null, null, [], false, false).AllowsNull);
            Assert.False(new MemberDetail(typeof(Oblivious), typeof(string), "Name", false, null, null, null, null, [], false, false, false).AllowsNull);
        }
#nullable restore
    }
}
