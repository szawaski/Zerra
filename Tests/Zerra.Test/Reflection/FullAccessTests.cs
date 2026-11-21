// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Reflection;
using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection
{
    public class FullAccessTests
    {
        public interface IExplicit
        {
            int Explicit { get; }
            int Add(int a);
        }

        public class FullAccessBase
        {
            private int hidden = 1;
            protected virtual int Overridden { get; set; }
            public int GetBaseHidden() => hidden;
        }

        public class FullAccessModel : FullAccessBase, IExplicit
        {
            private int hidden = 2;
            private string? secret;
            private readonly int readOnly = 3;
            internal int Internal { get; set; }

            public FullAccessModel() { }
            private FullAccessModel(string secret) { this.secret = secret; }
            public FullAccessModel(out int value) => throw new InvalidOperationException("constructor");

            public int Public { get; set; }
            public int PrivateSet { get; private set; }
            public int GetOnly { get; }
            protected override int Overridden { get; set; }

            int IExplicit.Explicit => 7;
            int IExplicit.Add(int a) => a + 7;

            public int Run(int a) => a;
            private static int Twice(int a) => a * 2;
            public void Double(ref int value) => value *= 2;
            public bool TryRead(out int value) { value = 9; return true; }
            public bool TryFail(out int value) => throw new InvalidOperationException("method");

            public int GetHidden() => hidden;
            public string? GetSecret() => secret;
            public int GetReadOnly() => readOnly;
        }

        [Fact]
        public void SharesCompleteDetails()
        {
            var detail = TypeAnalyzer.GetTypeDetail(typeof(FullAccessModel));
            var full = detail.GetFullAccess();

            Assert.Same(full, detail.GetFullAccess());
            Assert.Same(detail.GetMember(nameof(FullAccessModel.Public)), full.GetMember(nameof(FullAccessModel.Public)));
            Assert.Same(detail.Methods.Single(x => x.Name == nameof(FullAccessModel.Run)), full.GetMethods(nameof(FullAccessModel.Run)).Single());
            Assert.Same(detail.Constructors.Single(x => x.Parameters.Count == 0), full.Constructors.Single(x => x.Parameters.Count == 0));

            //a restricted accessor gets its own detail, the type's detail is left as it was
            Assert.NotSame(detail.GetMember(nameof(FullAccessModel.PrivateSet)), full.GetMember(nameof(FullAccessModel.PrivateSet)));
            Assert.False(detail.GetMember(nameof(FullAccessModel.PrivateSet)).HasSetter);
            Assert.DoesNotContain(detail.Members, x => x.Name == "secret");
        }

        [Fact]
        public void PrivateMembers()
        {
            var full = TypeAnalyzer.GetTypeDetail(typeof(FullAccessModel)).GetFullAccess();
            var model = new FullAccessModel();

            full.GetMember(nameof(FullAccessModel.PrivateSet)).SetterBoxed!(model, 4);
            Assert.Equal(4, model.PrivateSet);

            //a get-only auto-property is set through its backing field
            full.GetMember(nameof(FullAccessModel.GetOnly)).SetterBoxed!(model, 5);
            Assert.Equal(5, model.GetOnly);

            full.GetMember("secret").SetterBoxed!(model, "s");
            Assert.Equal("s", model.GetSecret());
            Assert.Equal("s", full.GetMember("secret").GetterBoxed!(model));

            full.GetMember("readOnly").SetterBoxed!(model, 6);
            Assert.Equal(6, model.GetReadOnly());

            full.GetMember(nameof(FullAccessModel.Internal)).SetterBoxed!(model, 8);
            Assert.Equal(8, model.Internal);

            //compiler-generated backing fields aren't listed
            Assert.DoesNotContain(full.Members, x => x.Name.StartsWith("<"));
        }

        [Fact]
        public void BasePrivateMembers()
        {
            var full = TypeAnalyzer.GetTypeDetail(typeof(FullAccessModel)).GetFullAccess();
            var model = new FullAccessModel();

            //a base type's private members are on the base type's own detail, so a name is never listed twice
            Assert.Single(full.Members, x => x.Name == "hidden");
            full.GetMember("hidden").SetterBoxed!(model, 20);
            Assert.Equal(20, model.GetHidden());

            TypeAnalyzer.GetTypeDetail(typeof(FullAccessBase)).GetFullAccess().GetMember("hidden").SetterBoxed!(model, 10);
            Assert.Equal(10, model.GetBaseHidden());

            //an override is listed once
            Assert.Single(full.Members, x => x.Name == "Overridden");
        }

        [Fact]
        public void ExplicitInterfaceMembers()
        {
            var full = TypeAnalyzer.GetTypeDetail(typeof(FullAccessModel)).GetFullAccess();
            var model = new FullAccessModel();

            var property = Assert.Single(full.Members, x => x.Name.EndsWith("IExplicit.Explicit"));
            Assert.Equal(7, property.GetterBoxed!(model));

            var method = Assert.Single(full.Methods, x => x.Name.EndsWith("IExplicit.Add"));
            Assert.Equal(8, method.CallerBoxed!(model, [1]));
        }

        [Fact]
        public void Methods()
        {
            var full = TypeAnalyzer.GetTypeDetail(typeof(FullAccessModel)).GetFullAccess();
            var model = new FullAccessModel();

            Assert.Equal(6, full.GetMethods("Twice").Single().CallerBoxed!(null, [3]));

            //ref and out results are written back into the arguments
            var args = new object?[] { 4 };
            _ = full.GetMethods(nameof(FullAccessModel.Double)).Single().CallerBoxed!(model, args);
            Assert.Equal(8, args[0]);

            args = [null];
            Assert.Equal(true, full.GetMethods(nameof(FullAccessModel.TryRead)).Single().CallerBoxed!(model, args));
            Assert.Equal(9, args[0]);

            //property accessors are methods here
            Assert.Single(full.GetMethods("get_" + nameof(FullAccessModel.PrivateSet)));
            Assert.Single(full.GetMethods("set_" + nameof(FullAccessModel.PrivateSet)));
            Assert.DoesNotContain(full.Methods, x => x.Name.StartsWith("<"));
        }

        [Fact]
        public void RefAndOutThrowTheOriginalException()
        {
            var full = TypeAnalyzer.GetTypeDetail(typeof(FullAccessModel)).GetFullAccess();

            var method = full.GetMethods(nameof(FullAccessModel.TryFail)).Single();
            Assert.Equal("method", Assert.Throws<InvalidOperationException>(() => method.CallerBoxed!(new FullAccessModel(), [null])).Message);

            var constructor = Assert.Single(full.Constructors, x => x.Parameters.Count == 1 && x.Parameters[0].Type.IsByRef);
            Assert.Equal("constructor", Assert.Throws<InvalidOperationException>(() => constructor.CreatorBoxed([null])).Message);
        }

        [Fact]
        public void PrivateConstructor()
        {
            var full = TypeAnalyzer.GetTypeDetail(typeof(FullAccessModel)).GetFullAccess();

            var constructor = Assert.Single(full.Constructors, x => x.Parameters.Count == 1 && x.Parameters[0].Type == typeof(string));
            var model = (FullAccessModel)constructor.CreatorBoxed(["s"]);
            Assert.Equal("s", model.GetSecret());
        }

        [Fact]
        public void Typed()
        {
            var detail = TypeAnalyzer<FullAccessModel>.GetTypeDetail();
            var full = detail.GetFullAccess();
            Assert.Same(full, ((TypeDetail)detail).GetFullAccess());

            var constructor = Assert.Single(full.Constructors, x => x.Parameters.Count == 1 && x.Parameters[0].Type == typeof(string));
            FullAccessModel model = constructor.Creator(["s"]);
            Assert.Equal("s", model.GetSecret());
        }

        [Fact]
        public void ReflectionInfo()
        {
            var detail = TypeAnalyzer.GetTypeDetail(typeof(FullAccessModel));
            var full = detail.GetFullAccess();

            var property = Assert.IsAssignableFrom<PropertyInfo>(full.GetMember(nameof(FullAccessModel.PrivateSet)).MemberInfo);
            Assert.True(property.SetMethod!.IsPrivate);

            var field = Assert.IsAssignableFrom<FieldInfo>(full.GetMember("hidden").MemberInfo);
            Assert.Equal(typeof(FullAccessModel), field.DeclaringType);
            Assert.True(field.IsPrivate);

            var constructor = full.Constructors.Single(x => x.Parameters.Count == 1 && x.Parameters[0].Type == typeof(string)).GetConstructorInfo;
            Assert.True(constructor.IsPrivate);

            Assert.Equal(nameof(FullAccessModel.Public), detail.GetMember(nameof(FullAccessModel.Public)).MemberInfo.Name);
        }

        [Fact]
        public void DetailReflectionInfo()
        {
            var full = TypeAnalyzer.GetTypeDetail(typeof(FullAccessModel)).GetFullAccess();

            //a method's own MethodInfo finds private, static, and explicit interface methods
            var twice = full.GetMethods("Twice").Single().MethodInfo;
            Assert.True(twice.IsPrivate && twice.IsStatic);
            Assert.True(full.Methods.Single(x => x.Name.EndsWith("IExplicit.Add")).MethodInfo.IsPrivate);
            Assert.Equal(nameof(FullAccessModel.Double), full.GetMethods(nameof(FullAccessModel.Double)).Single().MethodInfo.Name);

            Assert.True(((FieldInfo)full.GetMember("secret").MemberInfo).IsPrivate);

            //an interface's members and methods include those of the interfaces it extends
            var list = TypeAnalyzer.GetTypeDetail(typeof(IList<int>));
            Assert.Equal(typeof(ICollection<int>), list.GetMember(nameof(IList<int>.Count)).MemberInfo.DeclaringType);
            Assert.Equal(typeof(ICollection<int>), list.Methods.First(x => x.Name == nameof(IList<int>.Add)).MethodInfo.DeclaringType);
        }

        [Fact]
        public void Lookups()
        {
            var full = TypeAnalyzer.GetTypeDetail(typeof(FullAccessModel)).GetFullAccess();

            Assert.True(full.TryGetMember("secret", out var member));
            Assert.Equal("secret", member.Name);
            Assert.False(full.TryGetMember("Missing", out _));
            _ = Assert.Throws<MissingMemberException>(() => full.GetMember("Missing"));

            Assert.True(full.TryGetMethod("Twice", out var method));
            Assert.Same(method, full.GetMethod("Twice"));
            Assert.Same(method, full.GetMethod("Twice", 1));
            Assert.Same(method, full.GetMethod("Twice", 0, 1));
            Assert.Same(method, full.GetMethod("Twice", [typeof(int)]));
            Assert.Same(method, full.GetMethod("Twice", 0, [typeof(int)]));
            Assert.False(full.TryGetMethod("Missing", out _));
            Assert.Empty(full.GetMethods("Missing"));
            _ = Assert.Throws<MissingMethodException>(() => full.GetMethod("Missing"));
            _ = Assert.Throws<MissingMethodException>(() => full.GetMethod("Twice", 2));

            Assert.Empty(full.GetConstructor().Parameters);
            Assert.Equal(typeof(string), full.GetConstructor([typeof(string)]).Parameters[0].Type);
            Assert.Equal(typeof(int).MakeByRefType(), full.GetConstructor([typeof(int).MakeByRefType()]).Parameters[0].Type);
            _ = Assert.Throws<InvalidOperationException>(() => full.GetConstructor(1));
            _ = Assert.Throws<MissingMethodException>(() => full.GetConstructor(3));
        }

        [Fact]
        public void InterfaceType()
        {
            var full = TypeAnalyzer.GetTypeDetail(typeof(IList<int>)).GetFullAccess();

            Assert.Contains(full.Members, x => x.Name == nameof(IList<int>.Count));
            Assert.Contains(full.Methods, x => x.Name == nameof(IList<int>.Add));
            Assert.Empty(full.Constructors);
        }
    }
}
