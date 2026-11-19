// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection
{
    //without the source generator the members are read from the compiled nullable attributes
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

        public sealed class Generic<T>
        {
            public T Value { get; set; } = default!;
            public T? Maybe { get; set; }
            public string Name { get; set; } = null!;
        }
#nullable disable

        public sealed class Oblivious
        {
            public string Name { get; set; }
            public int Count { get; set; }
            public int? Maybe { get; set; }
        }

#nullable enable
        //the annotations are read from the compiled attributes, the netstandard2.0 build on .NET Framework included
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

        //a type argument's ? isn't kept at runtime, so a generic member uses its declared annotation like generated code does
        [Fact]
        public void GenericMembers()
        {
            var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(Generic<string>));
            bool AllowsNull(string name) => typeDetail.Members.Single(x => x.Name == name).AllowsNull;

            Assert.False(AllowsNull(nameof(Generic<string>.Value)));
            Assert.True(AllowsNull(nameof(Generic<string>.Maybe)));
            Assert.False(AllowsNull(nameof(Generic<string>.Name)));
            Assert.False(TypeAnalyzer.GetTypeDetail(typeof(Generic<int>)).Members.Single(x => x.Name == nameof(Generic<int>.Value)).AllowsNull);
        }

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
            Assert.True(new MemberDetail(typeof(Oblivious), typeof(string), "Name", false, null, null, null, null, [], false).AllowsNull);
            Assert.False(new MemberDetail(typeof(Oblivious), typeof(int), "Count", false, null, null, null, null, [], false).AllowsNull);
            Assert.True(new MemberDetail(typeof(Oblivious), typeof(int?), "Maybe", false, null, null, null, null, [], false).AllowsNull);
            Assert.False(new MemberDetail(typeof(Oblivious), typeof(string), "Name", false, null, null, null, null, [], false, false).AllowsNull);
        }
#nullable restore
    }
}
