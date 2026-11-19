// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Reflection;
using Zerra.Serialization.Bytes;
using Zerra.Serialization.Json;

namespace Zerra.Test.Reflection
{
    //without the source generator the members are read with reflection, which must give the same accessors as generated code in the type's own assembly
    public class MemberAccessorTests
    {
        public class Accessors
        {
            public string? Public { get; set; }
            public string? Internal { get; internal set; }
            public string? ProtectedInternal { get; protected internal set; }
            public string? PrivateProtected { get; private protected set; }
            public string? Protected { get; protected set; }
            public string? Private { get; private set; }
            public string? GetOnly { get; }
            public string? Init { get; init; }
            public string? PrivateGet { private get; set; }
            public string? InternalGet { internal get; set; }

            private string? manual;
            public string? Manual { get => manual; set => manual = value; }
            public string? ManualPrivateSet { get => manual; private set => manual = value; }
            public string? ManualInternalSet { get => manual; internal set => manual = value; }

            public void SetAll(string value)
            {
                Internal = value;
                ProtectedInternal = value;
                PrivateProtected = value;
                Private = value;
                ManualPrivateSet = value;
            }
        }

        [Fact]
        public void PublicAndInternalAccessors()
        {
            var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(Accessors));
            MemberDetail Member(string name) => typeDetail.Members.Single(x => x.Name == name);

            Assert.True(Member(nameof(Accessors.Public)).HasGetter);
            Assert.True(Member(nameof(Accessors.Public)).HasSetter);

            Assert.True(Member(nameof(Accessors.Internal)).HasGetter);
            Assert.True(Member(nameof(Accessors.Internal)).HasSetter);

            Assert.True(Member(nameof(Accessors.ProtectedInternal)).HasGetter);
            Assert.True(Member(nameof(Accessors.ProtectedInternal)).HasSetter);

            Assert.True(Member(nameof(Accessors.PrivateProtected)).HasGetter);
            Assert.False(Member(nameof(Accessors.PrivateProtected)).HasSetter);

            Assert.True(Member(nameof(Accessors.Protected)).HasGetter);
            Assert.False(Member(nameof(Accessors.Protected)).HasSetter);

            Assert.True(Member(nameof(Accessors.Private)).HasGetter);
            Assert.False(Member(nameof(Accessors.Private)).HasSetter);

            Assert.True(Member(nameof(Accessors.GetOnly)).HasGetter);
            Assert.False(Member(nameof(Accessors.GetOnly)).HasSetter);

            Assert.True(Member(nameof(Accessors.Init)).HasGetter);
            Assert.True(Member(nameof(Accessors.Init)).HasSetter);

            Assert.False(Member(nameof(Accessors.PrivateGet)).HasGetter);
            Assert.True(Member(nameof(Accessors.PrivateGet)).HasSetter);

            Assert.True(Member(nameof(Accessors.InternalGet)).HasGetter);
            Assert.True(Member(nameof(Accessors.InternalGet)).HasSetter);

            Assert.True(Member(nameof(Accessors.Manual)).HasGetter);
            Assert.True(Member(nameof(Accessors.Manual)).HasSetter);

            Assert.True(Member(nameof(Accessors.ManualPrivateSet)).HasGetter);
            Assert.False(Member(nameof(Accessors.ManualPrivateSet)).HasSetter);

            Assert.True(Member(nameof(Accessors.ManualInternalSet)).HasGetter);
            Assert.True(Member(nameof(Accessors.ManualInternalSet)).HasSetter);

            Assert.Equal(
                [nameof(Accessors.Init), nameof(Accessors.Internal), nameof(Accessors.InternalGet), nameof(Accessors.Manual), nameof(Accessors.ManualInternalSet), nameof(Accessors.ProtectedInternal), nameof(Accessors.Public)],
                typeDetail.SerializableMembers.Select(x => x.Name).OrderBy(x => x).ToArray());
        }

        [Fact]
        public void BackingFieldAccessors()
        {
            var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(Accessors));
            var model = new Accessors() { Public = "a" };
            model.SetAll("b");

            Assert.Equal("a", typeDetail.Members.Single(x => x.Name == nameof(Accessors.Public)).GetterBoxed!(model));
            Assert.Equal("b", typeDetail.Members.Single(x => x.Name == nameof(Accessors.Private)).GetterBoxed!(model));
            typeDetail.Members.Single(x => x.Name == nameof(Accessors.Internal)).SetterBoxed!(model, "c");
            Assert.Equal("c", model.Internal);
        }

        [Fact]
        public void PublicAndInternalSettersSerialized()
        {
            var model = new Accessors() { Public = "a", Init = "i", InternalGet = "g" };
            model.SetAll("b");

            var json = JsonSerializer.Serialize(model);
            Assert.Equal("{\"Public\":\"a\",\"Internal\":\"b\",\"ProtectedInternal\":\"b\",\"Init\":\"i\",\"InternalGet\":\"g\",\"Manual\":\"b\",\"ManualInternalSet\":\"b\"}", json);

            var result = ByteSerializer.Deserialize<Accessors>(ByteSerializer.Serialize(model))!;
            Assert.Equal("a", result.Public);
            Assert.Equal("b", result.Internal);
            Assert.Equal("b", result.ProtectedInternal);
            Assert.Null(result.PrivateProtected);
            Assert.Null(result.Private);
            Assert.Equal("i", result.Init);
            Assert.Equal("b", result.Manual);
        }
    }
}
