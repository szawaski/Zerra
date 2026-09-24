// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.T4.CSharp;

namespace Zerra.T4.Test
{
    //A file the parser fails on is skipped rather than reported, so these assert the types were found at all
    public class CSharpParserTests
    {
        [Fact]
        public void Namespaces()
        {
            var solution = GeneratorRunner.Parse("""
                namespace Outer
                {
                    public class A { }
                    namespace Inner
                    {
                        public class B { }
                    }
                }
                namespace Second.Part { public class C { } }
                """);

            Assert.Equal(["Outer", "Outer.Inner", "Second.Part"], solution.Namespaces.Select(x => x.ToString()));
            Assert.Equal("Outer", Single(solution.Classes, "A").Namespace?.ToString());
            Assert.Equal("Outer.Inner", Single(solution.Classes, "B").Namespace?.ToString());
            Assert.Equal("Second.Part", Single(solution.Classes, "C").Namespace?.ToString());
        }

        [Fact]
        public void FileScopedNamespace()
        {
            var solution = GeneratorRunner.Parse("""
                using System;

                namespace App.Models;

                public class A
                {
                    public Guid ID { get; set; }
                }

                public enum Kind { One }
                """);

            var a = Single(solution.Classes, "A");
            Assert.Equal("App.Models", a.Namespace?.ToString());
            Assert.Equal(typeof(Guid), a.Properties.Single().Type.Resolved.NativeType);
            Assert.Equal("App.Models", Assert.Single(solution.Enums).Namespace?.ToString());
        }

        [Fact]
        public void ObjectKinds()
        {
            var solution = GeneratorRunner.Parse("""
                namespace App
                {
                    public interface IThing { }
                    public abstract class Base { }
                    public static partial class Helpers { }
                    internal sealed class Hidden : Base, IThing { }
                    public class Box<T> where T : new()
                    {
                        public class Inner { }
                        public struct InnerStruct { }
                        public interface IInner { }
                        public enum InnerEnum { A }
                        public delegate void InnerDelegate();
                    }
                    public struct Point { public int X; }
                    public delegate int Handler(string value, int count);
                }
                """);

            Assert.Equal(["Base", "Helpers", "Hidden", "Box<T>"], solution.Classes.Select(x => x.Name));
            Assert.Equal(CSharpObjectType.Interface, Single(solution.Interfaces, "IThing").ObjectType);
            Assert.Equal(CSharpObjectType.Struct, Single(solution.Structs, "Point").ObjectType);

            Assert.True(Single(solution.Classes, "Base").IsAbstract);
            var helpers = Single(solution.Classes, "Helpers");
            Assert.True(helpers.IsStatic);
            Assert.True(helpers.IsPartial);
            var hidden = Single(solution.Classes, "Hidden");
            Assert.False(hidden.IsPublic);
            Assert.Equal(["Base", "IThing"], hidden.Implements.Select(x => x.Name));
            Assert.Same(Single(solution.Interfaces, "IThing"), hidden.Implements[1].Resolved.SolutionType);

            var box = Single(solution.Classes, "Box<T>");
            Assert.Equal("Inner", Assert.Single(box.InnerClasses).Name);
            Assert.Equal("InnerStruct", Assert.Single(box.InnerStructs).Name);
            Assert.Equal("IInner", Assert.Single(box.InnerInterfaces).Name);
            Assert.Equal("InnerEnum", Assert.Single(box.InnerEnums).Name);
            Assert.Equal("InnerDelegate", Assert.Single(box.InnerDelegates).Name);

            var handler = Assert.Single(solution.Delegates);
            Assert.Equal("Handler", handler.Name);
            Assert.Equal("int", handler.ReturnType.Name);
            Assert.Equal(["value", "count"], handler.Parameters.Select(x => x.Name));
        }

        [Fact]
        public void Records()
        {
            var solution = GeneratorRunner.Parse("""
                namespace App
                {
                    public record Point(int X, int Y);
                    public record Named(string Name, int Count = 1) : Base(Name), IThing
                    {
                        public string Extra { get; init; }
                    }
                    public record class Plain
                    {
                        public int Value { get; init; }
                    }
                    public readonly record struct Pair([property: System.Obsolete] int A, string B);
                    public record Box<T>(T Value) where T : class;
                    public class Outer
                    {
                        public record Inner(int I);
                        public record struct InnerStruct(int S);
                    }
                    public abstract record Base(string Key);
                    public interface IThing { }
                    public class After { }
                }
                """);

            Assert.Equal(["Point", "Named", "Plain", "Box<T>", "Outer", "Base", "After"], solution.Classes.Select(x => x.Name));

            var point = Single(solution.Classes, "Point");
            Assert.True(point.IsRecord);
            Assert.Equal(CSharpObjectType.Class, point.ObjectType);
            Assert.Equal(["X", "Y"], point.Properties.Select(x => x.Name));
            AssertProperty(point, "X", hasGet: true, hasSet: true, isSetPublic: true);
            Assert.Equal(typeof(int), Resolve(point, "Y").NativeType);

            //positional properties come before the ones in the body, base constructor arguments are skipped
            var named = Single(solution.Classes, "Named");
            Assert.Equal(["Name", "Count", "Extra"], named.Properties.Select(x => x.Name));
            Assert.Equal(["Base", "IThing"], named.Implements.Select(x => x.Name));
            Assert.Same(Single(solution.Classes, "Base"), named.Implements[0].Resolved.SolutionType);

            var plain = Single(solution.Classes, "Plain");
            Assert.True(plain.IsRecord);
            Assert.Equal("Value", Assert.Single(plain.Properties).Name);

            var pair = Single(solution.Structs, "Pair");
            Assert.True(pair.IsRecord);
            Assert.Equal(CSharpObjectType.Struct, pair.ObjectType);
            Assert.Equal(["A", "B"], pair.Properties.Select(x => x.Name));
            Assert.Equal("string", Single(pair.Properties, "B").Type.Name);

            Assert.Equal("Value", Assert.Single(Single(solution.Classes, "Box<T>").Properties).Name);

            var outer = Single(solution.Classes, "Outer");
            Assert.False(outer.IsRecord);
            var inner = Assert.Single(outer.InnerClasses);
            Assert.True(inner.IsRecord);
            Assert.Equal("I", Assert.Single(inner.Properties).Name);
            var innerStruct = Assert.Single(outer.InnerStructs);
            Assert.True(innerStruct.IsRecord);
            Assert.Equal("S", Assert.Single(innerStruct.Properties).Name);

            Assert.True(Single(solution.Classes, "Base").IsAbstract);
        }

        //unlike a record, a primary constructor's parameters aren't properties
        [Fact]
        public void PrimaryConstructors()
        {
            var solution = GeneratorRunner.Parse("""
                namespace App
                {
                    public class Base(int value) { }
                    public class Service(string name, int count = 2) : Base(count)
                    {
                        public string Name => name;
                        public int Count { get; } = count;
                    }
                    public struct Measure(double amount)
                    {
                        public double Amount { get; } = amount;
                    }
                    public class After { }
                }
                """);

            Assert.Equal(["Base", "Service", "After"], solution.Classes.Select(x => x.Name));
            var service = Single(solution.Classes, "Service");
            Assert.False(service.IsRecord);
            Assert.Equal(["Name", "Count"], service.Properties.Select(x => x.Name));
            Assert.Equal(["Base"], service.Implements.Select(x => x.Name));
            Assert.Equal("Amount", Assert.Single(Single(solution.Structs, "Measure").Properties).Name);
        }

        [Fact]
        public void Properties()
        {
            var solution = GeneratorRunner.Parse("""
                using System.Collections.Generic;
                namespace App
                {
                    public class Model
                    {
                        public int Auto { get; set; }
                        public int PrivateSet { get; private set; }
                        public int GetOnly { get; }
                        public int Init { get; init; }
                        public required string Required { get; set; }
                        public int Initialized { get; set; } = 5;
                        public List<int> InitializedNew { get; set; } = new() { 1, 2 };
                        public int Expression => Auto * 2;
                        public int Bodies { get { return Auto; } set { Auto = value; } }
                        public static int Static { get; set; }
                        public virtual int Virtual { get; set; }
                        private int Private { get; set; }
                        public int Field;
                        public const int Constant = 1;
                        public int this[int index] { get { return index; } }
                        public int After { get; set; }
                    }
                }
                """);

            var model = Single(solution.Classes, "Model");
            Assert.Equal(["Auto", "PrivateSet", "GetOnly", "Init", "Required", "Initialized", "InitializedNew", "Expression", "Bodies", "Static", "Virtual", "Private", "this", "After"], model.Properties.Select(x => x.Name));

            AssertProperty(model, "Auto", hasGet: true, hasSet: true, isSetPublic: true);
            AssertProperty(model, "PrivateSet", hasGet: true, hasSet: true, isSetPublic: false);
            AssertProperty(model, "GetOnly", hasGet: true, hasSet: false, isSetPublic: false);
            AssertProperty(model, "Init", hasGet: true, hasSet: true, isSetPublic: true);
            AssertProperty(model, "Expression", hasGet: true, hasSet: false, isSetPublic: false);
            AssertProperty(model, "Bodies", hasGet: true, hasSet: true, isSetPublic: true);
            Assert.True(Single(model.Properties, "Static").IsStatic);
            Assert.True(Single(model.Properties, "Virtual").IsVirtual);
            Assert.False(Single(model.Properties, "Private").IsPublic);
            Assert.Equal("string", Single(model.Properties, "Required").Type.Name);
            Assert.Equal("List<int>", Single(model.Properties, "InitializedNew").Type.Name);
        }

        //braces, quotes and comments inside method bodies must not end the body early
        [Fact]
        public void MethodBodiesWithLiterals()
        {
            var solution = GeneratorRunner.Parse(""""
                namespace App
                {
                    public class Tricky
                    {
                        public string Brace() { return "}{"; }
                        public char Char() { return '}'; }
                        public char EscapedChar() { return '\''; }
                        public string Escaped() { return "\"}"; }
                        public string Backslash() { return "\\"; }
                        public char BackslashChar() { return '\\'; }
                        public string Verbatim() { return @"a""}"; }
                        public string Interpolated() { return $"{Brace()}}}"; }
                        public string Comments() { /* } */ return "x"; } // }
                        public string Raw() { return """ } """; }
                        #region Region
                        public void Nested() { if (true) { { } } }
                        #endregion
                        public int After { get; set; }
                    }
                    public class Next { }
                }
                """");

            var tricky = Single(solution.Classes, "Tricky");
            Assert.Equal(["Brace", "Char", "EscapedChar", "Escaped", "Backslash", "BackslashChar", "Verbatim", "Interpolated", "Comments", "Raw", "Nested"], tricky.Methods.Select(x => x.Name));
            Assert.Equal("After", Assert.Single(tricky.Properties).Name);
            _ = Single(solution.Classes, "Next");
        }

        [Fact]
        public void Methods()
        {
            var solution = GeneratorRunner.Parse("""
                using System.Threading.Tasks;
                namespace App
                {
                    public interface IThing
                    {
                        Task<int> Go(ref int a, out string b, in long c, int d = 5, string e = "x,(y", char f = ',');
                        T Generic<T>(T value) where T : class;
                        private void Hidden();
                    }
                    public abstract class Base
                    {
                        public Base(int x) { }
                        public abstract void Abstract();
                        public virtual int Virtual() => 1;
                        public static void Static() { }
                        protected void Protected() { }
                    }
                    public class Derived : Base
                    {
                        public Derived() : base(1) { }
                        public T Generic<T>(T value) where T : class => value;
                        public override void Abstract() { }
                    }
                }
                """);

            var thing = Single(solution.Interfaces, "IThing");
            //interface members are public without saying so
            Assert.True(Single(thing.Methods, "Go").IsPublic);
            Assert.False(Single(thing.Methods, "Hidden").IsPublic);
            Assert.False(Single(thing.Methods, "Go").IsImplemented);

            var go = Single(thing.Methods, "Go");
            Assert.Equal("Task<int>", go.ReturnType.Name);
            Assert.Equal(["a", "b", "c", "d", "e", "f"], go.Parameters.Select(x => x.Name));
            Assert.True(go.Parameters[0].IsRef);
            Assert.True(go.Parameters[1].IsOut);
            Assert.True(go.Parameters[2].IsIn);
            Assert.Equal("long", go.Parameters[2].Type.Name);
            Assert.Null(go.Parameters[2].DefaultValue);
            Assert.Equal("5", go.Parameters[3].DefaultValue);
            Assert.Equal("\"x,(y\"", go.Parameters[4].DefaultValue);
            Assert.Equal("','", go.Parameters[5].DefaultValue);

            Assert.Equal("Generic<T>", Assert.Single(thing.Methods, x => x.Name.StartsWith("Generic")).Name);

            var baseClass = Single(solution.Classes, "Base");
            Assert.Equal("Base", Assert.Single(baseClass.Constructors).Name);
            var abstractMethod = Single(baseClass.Methods, "Abstract");
            Assert.True(abstractMethod.IsAbstract);
            Assert.False(abstractMethod.IsImplemented);
            var virtualMethod = Single(baseClass.Methods, "Virtual");
            Assert.True(virtualMethod.IsVirtual);
            Assert.True(virtualMethod.IsImplemented);
            Assert.True(Single(baseClass.Methods, "Static").IsStatic);
            Assert.False(Single(baseClass.Methods, "Protected").IsPublic);

            var derived = Single(solution.Classes, "Derived");
            Assert.Equal("Derived", Assert.Single(derived.Constructors).Name);
            Assert.True(Assert.Single(derived.Methods, x => x.Name.StartsWith("Generic")).IsImplemented);
            Assert.True(Single(derived.Methods, "Abstract").IsImplemented);
        }

        [Fact]
        public void EnumValues()
        {
            var solution = GeneratorRunner.Parse("""
                namespace App
                {
                    public enum Plain { A, B, C }
                    public enum Explicit : byte { Red, Green = 5, Blue, Old = 2, Next }
                    public enum Big : long { Max = 9223372036854775807 }
                }
                """);

            var plain = Single(solution.Enums, "Plain");
            Assert.Equal(typeof(int), plain.Type);
            Assert.Equal([("A", 0L), ("B", 1L), ("C", 2L)], plain.Values.Select(x => (x.Name, x.Value)));

            //a value without an initializer follows the one before it, not the largest
            var explicitEnum = Single(solution.Enums, "Explicit");
            Assert.Equal(typeof(byte), explicitEnum.Type);
            Assert.Equal([("Red", 0L), ("Green", 5L), ("Blue", 6L), ("Old", 2L), ("Next", 3L)], explicitEnum.Values.Select(x => (x.Name, x.Value)));

            var big = Single(solution.Enums, "Big");
            Assert.Equal(typeof(long), big.Type);
            Assert.Equal(Int64.MaxValue, Assert.Single(big.Values).Value);
        }

        [Fact]
        public void Attributes()
        {
            var solution = GeneratorRunner.Parse("""
                namespace App
                {
                    [Serializable]
                    [Custom("a", Count = 2)]
                    public class Model
                    {
                        [Obsolete("old")]
                        public int Value { get; set; }
                        [Route("x")]
                        public void Method() { }
                    }
                    public enum Kind
                    {
                        [Display(Name = "First")] One,
                        Two
                    }
                }
                """);

            var model = Single(solution.Classes, "Model");
            Assert.Equal(["Serializable", "Custom"], model.Attributes.Select(x => x.Name));
            Assert.Equal(["\"a\"", " Count = 2"], model.Attributes[1].Arguments);
            var obsolete = Assert.Single(model.Properties.Single().Attributes);
            Assert.Equal("Obsolete", obsolete.Name);
            Assert.Equal(["\"old\""], obsolete.Arguments);
            Assert.Equal("Route", Assert.Single(model.Methods.Single().Attributes).Name);

            var kind = Single(solution.Enums, "Kind");
            Assert.Equal("Display", Assert.Single(kind.Values[0].Attributes).Name);
            Assert.Empty(kind.Values[1].Attributes);
        }

        [Fact]
        public void TypeResolution()
        {
            var solution = GeneratorRunner.Parse("""
                using System;
                using System.Collections.Generic;
                using Other;
                namespace App
                {
                    public class Holder
                    {
                        public int Int { get; set; }
                        public String SystemName { get; set; }
                        public Guid Guid { get; set; }
                        public DateTime? Nullable { get; set; }
                        public int[] Array { get; set; }
                        public List<Ref> List { get; set; }
                        public Dictionary<string, Ref> Dictionary { get; set; }
                        public Ref[] RefArray { get; set; }
                        public Local Local { get; set; }
                        public Other.Ref Qualified { get; set; }
                        public global::Other.Ref Global { get; set; }
                        public Unknown Unknown { get; set; }
                    }
                    public class Local { }
                }
                namespace Other { public class Ref { } }
                namespace NotImported { public class Hidden { } }
                """);

            var holder = Single(solution.Classes, "Holder");
            var reference = Single(solution.Classes, "Ref");
            var local = Single(solution.Classes, "Local");

            Assert.Equal(typeof(int), Resolve(holder, "Int").NativeType);
            Assert.Equal(typeof(string), Resolve(holder, "SystemName").NativeType);
            Assert.Equal(typeof(Guid), Resolve(holder, "Guid").NativeType);

            var nullable = Resolve(holder, "Nullable");
            Assert.Equal("Nullable`1", nullable.Name);
            Assert.Equal(typeof(DateTime), Assert.Single(nullable.GenericArguments).NativeType);

            var array = Resolve(holder, "Array");
            Assert.Equal("Array`1", array.Name);
            Assert.Equal(typeof(int), Assert.Single(array.GenericArguments).NativeType);

            var list = Resolve(holder, "List");
            Assert.Equal(typeof(List<>), list.NativeType);
            Assert.Same(reference, Assert.Single(list.GenericArguments).SolutionType);

            var dictionary = Resolve(holder, "Dictionary");
            Assert.Equal(typeof(Dictionary<,>), dictionary.NativeType);
            Assert.Equal(typeof(string), dictionary.GenericArguments[0].NativeType);
            Assert.Same(reference, dictionary.GenericArguments[1].SolutionType);

            Assert.Same(reference, Assert.Single(Resolve(holder, "RefArray").GenericArguments).SolutionType);
            Assert.Same(local, Resolve(holder, "Local").SolutionType);
            Assert.Same(reference, Resolve(holder, "Qualified").SolutionType);
            Assert.Same(reference, Resolve(holder, "Global").SolutionType);

            var unknown = Resolve(holder, "Unknown");
            Assert.Null(unknown.NativeType);
            Assert.Null(unknown.SolutionType);
        }

        [Fact]
        public void TypeOutOfScope()
        {
            var solution = GeneratorRunner.Parse("""
                namespace App
                {
                    public class Holder
                    {
                        public Hidden Hidden { get; set; }
                    }
                }
                namespace NotImported { public class Hidden { } }
                """);

            Assert.Null(Resolve(Single(solution.Classes, "Holder"), "Hidden").SolutionType);
        }

        [Fact]
        public void GlobalUsings()
        {
            var solution = GeneratorRunner.ParseFiles(
                ("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>"),
                ("App/GlobalUsings.cs", "global using System;\r\nglobal using global::Models;"),
                ("App/Holder.cs", """
                    namespace App
                    {
                        public class Holder
                        {
                            public Guid Guid { get; set; }
                            public Ref Ref { get; set; }
                        }
                    }
                    """),
                ("App/Models/Ref.cs", "namespace Models { public class Ref { } }"),
                //a different project doesn't get them
                ("Other/Other.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>"),
                ("Other/Holder2.cs", "namespace Other { public class Holder2 { public Guid Guid { get; set; } } }"));

            var holder = Single(solution.Classes, "Holder");
            Assert.Equal(typeof(Guid), Resolve(holder, "Guid").NativeType);
            Assert.Same(Single(solution.Classes, "Ref"), Resolve(holder, "Ref").SolutionType);
            Assert.Null(Resolve(Single(solution.Classes, "Holder2"), "Guid").NativeType);
        }

        [Theory]
        [InlineData("enable", true)]
        [InlineData("true", true)]
        [InlineData("disable", false)]
        [InlineData(null, false)]
        public void ImplicitUsings(string? setting, bool expected)
        {
            var project = setting is null
                ? "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>"
                : $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><ImplicitUsings>{setting}</ImplicitUsings></PropertyGroup></Project>";
            var solution = GeneratorRunner.ParseFiles(
                ("App/App.csproj", project),
                //files in sub folders belong to the project above them
                ("App/Models/Holder.cs", """
                    namespace App.Models
                    {
                        public class Holder
                        {
                            public Guid Guid { get; set; }
                            public List<int> List { get; set; }
                            public Task<int> Task { get; set; }
                        }
                    }
                    """));

            var holder = Single(solution.Classes, "Holder");
            Assert.Equal(expected ? typeof(Guid) : null, Resolve(holder, "Guid").NativeType);
            Assert.Equal(expected ? typeof(Task<>) : null, Resolve(holder, "Task").NativeType);
            //System.Collections.Generic is always tried
            Assert.Equal(typeof(List<>), Resolve(holder, "List").NativeType);
        }

        [Fact]
        public void SkipsBuildOutput()
        {
            var solution = GeneratorRunner.ParseFiles(
                ("App/Model.cs", "namespace App { public class Model { } }"),
                ("App/bin/Debug/Generated.cs", "namespace App { public class FromBin { } }"),
                ("App/obj/Debug/Generated.cs", "namespace App { public class FromObj { } }"));

            Assert.Equal("Model", Assert.Single(solution.Classes).Name);
        }

        [Fact]
        public void BadFileDoesNotStopOthers()
        {
            var solution = GeneratorRunner.Parse(
                "namespace App { public class Good { } }",
                "namespace App { public class Broken : { ",
                "namespace App { public class AlsoGood { } }");

            _ = Single(solution.Classes, "Good");
            _ = Single(solution.Classes, "AlsoGood");
        }

        private static CSharpObject Single(IEnumerable<CSharpObject> items, string name) => Assert.Single(items, x => x.Name == name);
        private static CSharpEnum Single(IEnumerable<CSharpEnum> items, string name) => Assert.Single(items, x => x.Name == name);
        private static CSharpMethod Single(IEnumerable<CSharpMethod> items, string name) => Assert.Single(items, x => x.Name == name);
        private static CSharpProperty Single(IEnumerable<CSharpProperty> items, string name) => Assert.Single(items, x => x.Name == name);
        private static CSharpType Resolve(CSharpObject model, string property) => Single(model.Properties, property).Type.Resolved;

        private static void AssertProperty(CSharpObject model, string name, bool hasGet, bool hasSet, bool isSetPublic)
        {
            var property = Single(model.Properties, name);
            Assert.True(property.IsPublic);
            Assert.Equal(hasGet, property.HasGet);
            Assert.Equal(hasSet, property.HasSet);
            Assert.Equal(isSetPublic, property.IsSetPublic);
        }
    }
}
