// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;

namespace Zerra.SourceGeneration.Test
{
    //valid source with declarations the generated code can't name or use directly, the runner fails if the generated code doesn't compile
    public class GeneratedCodeCompilesTests
    {
        private static void Run(string body, string usings = "") => GeneratorRunner.Run($$"""
            using System;
            using System.Collections.Generic;
            using Zerra.Serialization.Json;
            using Zerra.Reflection;
            {{usings}}
            namespace TestApp
            {
            {{body}}
            }
            """);

        [Fact] public void FileClass() => Run("""
            file class Temp { public int V { get; set; } }
            public class Sender { public string S() => JsonSerializer.Serialize(new Temp()); }
            """);

        [Fact] public void TypeNestedInFileClass() => Run("""
            file class Outer { public class Inner { public int V { get; set; } } }
            public class Sender { public string S() => JsonSerializer.Serialize(new Outer.Inner()); }
            """);

        [Fact] public void ObsoleteErrorCommand() => Run("""
            [Obsolete("x", true)] public class OldCommand : Zerra.CQRS.ICommand { public int V { get; set; } }
            """);

        [Fact] public void ObsoleteErrorProperty() => Run("""
            [GenerateTypeDetail] public class M { [Obsolete("x", true)] public int Old { get; set; } public int V { get; set; } }
            """);

        [Fact] public void ObsoleteErrorPropertyWithDiagnosticId() => Run("""
            [GenerateTypeDetail] public class M { [Obsolete("x", true, DiagnosticId = "XX01")] public int Old { get; set; } public int V { get; set; } }
            """);

        [Fact] public void ObsoleteErrorConstructor() => Run("""
            [GenerateTypeDetail] public class M { [Obsolete("x", true)] public M() { } public int V { get; set; } }
            """);

        [Fact] public void ObsoleteErrorPropertyType() => Run("""
            [Obsolete("x", true)] public class Old { }
            [GenerateTypeDetail] public class M { [Obsolete("y")] public Old O { get; set; } public int V { get; set; } }
            """);

        [Fact] public void PointerField() => Run("""
            [GenerateTypeDetail] public unsafe class M { public int* P; public int V { get; set; } }
            """);

        [Fact] public void FunctionPointerField() => Run("""
            [GenerateTypeDetail] public unsafe class M { public delegate*<void> F; public int V { get; set; } }
            """);

        [Fact] public void KeywordNames() => Run("""
            public class @class { public int @event { get; set; } public string @string; }
            public class Sender { public string S() => JsonSerializer.Serialize(new @class()); }
            """);

        [Fact] public void TypeNestedInGeneric() => Run("""
            public class Outer<T> { public class Inner { public T V { get; set; } } }
            public class Sender { public string S() => JsonSerializer.Serialize(new Outer<int>.Inner()); }
            """);

        [Fact] public void PrivateInterfaceTypeArgument() => Run("""
            public class Model : IEquatable<Model.Secret> { private class Secret { public int V { get; set; } } bool IEquatable<Secret>.Equals(Secret s) => false; public int V { get; set; } }
            public class Sender { public string S() => JsonSerializer.Serialize(new Model()); }
            """);

        [Fact] public void DynamicMembers() => Run("""
            public class M { public dynamic D { get; set; } public List<dynamic> L { get; set; } }
            public class Sender { public string S() => JsonSerializer.Serialize(new M()); }
            """);

        [Fact] public void NamedTuples() => Run("""
            public class M { public (int a, string b) T { get; set; } public List<(int x, int y)> L { get; set; } }
            public class Sender { public string S() => JsonSerializer.Serialize(new M()); }
            """);

        [Fact] public void ConstStaticAndReadOnlyMembers() => Run("""
            [GenerateTypeDetail] public class M { public const int C = 1; public static int S; public static readonly int R = 1; public readonly int F; public int V { get; init; } }
            """);

        [Fact] public void RefProperties() => Run("""
            [GenerateTypeDetail] public class M { private int x; public ref int R => ref x; public ref readonly int RR => ref x; public int V { get; set; } }
            """);

        [Fact] public void SpanProperties() => Run("""
            [GenerateTypeDetail] public class M { public Span<int> S => default; public ReadOnlySpan<char> C => default; public int V { get; set; } }
            """);

        [Fact] public void StaticAbstractInterfaceProperties() => Run("""
            public interface IHas { static abstract int Make(); }
            public class M { public IHas H { get; set; } public System.Numerics.INumber<int> N { get; set; } }
            public class Sender { public string S() => JsonSerializer.Serialize(new M()); }
            """);

        [Fact] public void StaticAbstractInterfaceModel() => Run("""
            [GenerateTypeDetail] public interface IHas { static abstract int Make(); int V { get; set; } }
            """);

        [Fact] public void PointerArrayField() => Run("""
            [GenerateTypeDetail] public unsafe class M { public int*[] P; public int V { get; set; } }
            """);

        [Fact] public void UserTypeNamedSystem() => Run("""
            public class System { }
            public class Cmd : Zerra.CQRS.ICommand { public int V { get; set; } }
            """, "");

        [Fact] public void UserTypeNamedZerra() => Run("""
            public class Zerra { }
            public class Cmd : global::Zerra.CQRS.ICommand { public int V { get; set; } }
            """);

        [Fact] public void RequiredMembers() => Run("""
            public class M { public required int V { get; set; } public required string S { get; init; } }
            public class Sender { public M S() => JsonSerializer.Deserialize<M>("{}"); }
            """);

        [Fact] public void ProtectedInternalNestedType() => Run("""
            public class Outer { protected internal class Inner { public int V { get; set; } } public string S() => JsonSerializer.Serialize(new Inner()); }
            """);

        [Fact] public void Records() => Run("""
            public readonly record struct R(int A, string B);
            public record class C(int A) { public int B { get; init; } }
            public class Sender { public string S() => JsonSerializer.Serialize(new R(1, "")) + JsonSerializer.Serialize(new C(1)); }
            """);

        [Fact] public void HiddenMember() => Run("""
            public class B { public int V { get; set; } }
            public class D : B { public new string V { get; set; } }
            public class Sender { public string S() => JsonSerializer.Serialize(new D()); }
            """);

        [Fact] public void ExperimentalType() => Run("""
            [System.Diagnostics.CodeAnalysis.Experimental("EXP01")] public class E { public int V { get; set; } }
            #pragma warning disable EXP01
            public class Sender { public string S() => JsonSerializer.Serialize(new E()); }
            #pragma warning restore EXP01
            """);

        [Fact] public void TypeNestedInGenericOfPrivateType() => Run("""
            public class Outer<T> { public class Inner { public T V { get; set; } } }
            public class Sender { private class P { } public string S() => JsonSerializer.Serialize(new Outer<P>.Inner()); }
            """);

        [Fact] public void GlobalNamespaceOnly() => GeneratorRunner.Run("""
            using Zerra.Serialization.Json;
            public class G { public int V { get; set; } }
            public class Sender { public string S() => JsonSerializer.Serialize(new G()); }
            """);

        [Fact] public void AbstractAndInterfaceTargets() => Run("""
            public abstract class A { public int V { get; set; } }
            public interface I { int V { get; set; } }
            public class Sender { public A D(string s) => JsonSerializer.Deserialize<A>(s); public I E(string s) => JsonSerializer.Deserialize<I>(s); }
            """);

        [Fact] public void RefStructModels() => Run("""
            public ref struct RS { public int V; }
            [GenerateTypeDetail] public ref struct RS2 { public int V; }
            """);

        [Fact] public void PrivateNestedCommandAndHandler() => Run("""
            public class Holder { private class Cmd : Zerra.CQRS.ICommand { public int V { get; set; } } private interface ICmdHandler : Zerra.CQRS.ICommandHandler<Cmd> { } }
            """);

        [Fact] public void PublicTypesNestedInPrivateClass() => Run("""
            public class Holder { private class Inner { public class Cmd : Zerra.CQRS.ICommand { public int V { get; set; } } public interface ICmdHandler : Zerra.CQRS.ICommandHandler<Cmd> { } public enum E { A, B } } }
            """);

        [Fact] public void FileCommandHandlerEnumAndQuery() => Run("""
            file class Cmd : Zerra.CQRS.ICommand { public int V { get; set; } }
            file interface ICmdHandler : Zerra.CQRS.ICommandHandler<Cmd> { }
            file enum E { A, B }
            file interface IQ : Zerra.CQRS.IQueryHandler { int Get(); }
            """);

        [Fact] public void PrivateNestedQuery() => Run("""
            public class Holder { private interface IQ : Zerra.CQRS.IQueryHandler { int Get(); } }
            """);

        [Fact] public void ObsoleteErrorEnum() => Run("""
            [Obsolete("x", true)] public enum E { A, B }
            """);

        [Fact] public void ObsoleteErrorQueryAndMethod() => Run("""
            [Obsolete("x", true)] public interface IQ : Zerra.CQRS.IQueryHandler { int Get(); }
            public interface IQ2 : Zerra.CQRS.IQueryHandler { [Obsolete("x", true)] int Get(); int Other(); }
            """);

        [Fact] public void ObsoleteErrorCommandHandler() => Run("""
            [Obsolete("x", true)] public class Cmd : Zerra.CQRS.ICommand { public int V { get; set; } }
            [Obsolete("x", true)] public interface ICmdHandler : Zerra.CQRS.ICommandHandler<Cmd> { }
            """);

        [Fact] public void InterfaceNestedInPrivateClass() => Run("""
            public class Holder { private class Inner { public interface IHas { int V { get; set; } } } public class M : Inner.IHas { public int V { get; set; } } }
            public class Sender { public string S() => JsonSerializer.Serialize(new Holder.M()); }
            """);

        [Fact] public void AttributeNestedInPrivateClass() => Run("""
            public class Holder { private class Inner { public class MarkAttribute : Attribute { } } public class M { [Inner.Mark] public int V { get; set; } } }
            public class Sender { public string S() => JsonSerializer.Serialize(new Holder.M()); }
            """);

        [Fact] public void KeywordNameInGeneric() => Run("""
            public class @class { public int V { get; set; } }
            public class M { public List<@class> L { get; set; } public Dictionary<string, @class> D { get; set; } }
            public class Sender { public string S() => JsonSerializer.Serialize(new M()); }
            """);

        //a generic of arrays is the closed type, not the open List<>
        [Fact]
        public void ArrayInGeneric()
        {
            var output = GeneratorRunner.Run("""
                using System.Collections.Generic;
                using Zerra.Serialization.Json;
                namespace TestApp
                {
                    public class M { public List<int[]> L { get; set; } public List<int[][,]> J { get; set; } public List<dynamic[]> Y { get; set; } public Dictionary<string, M[]> D { get; set; } }
                    public class Sender { public string S() => JsonSerializer.Serialize(new M()); }
                }
                """);
            output.AssertInitializerContains("typeof(global::System.Collections.Generic.List<global::System.Int32[]>)");
            output.AssertInitializerContains("typeof(global::System.Collections.Generic.List<global::System.Int32[][,]>)");
            output.AssertInitializerContains("typeof(global::System.Collections.Generic.List<global::System.Object[]>)");
            output.AssertInitializerContains("typeof(global::System.Collections.Generic.KeyValuePair<global::System.String,global::TestApp.M[]>)");
            output.AssertInitializerDoesNotContain("List<>)");
            output.AssertInitializerDoesNotContain("KeyValuePair<,>)");
        }

        [Fact]
        public void ExperimentalInterfaceModel()
        {
            var output = GeneratorRunner.Run("""
                using Zerra.Serialization.Json;
                #pragma warning disable EXP02
                namespace TestApp
                {
                    [System.Diagnostics.CodeAnalysis.Experimental("EXP02")] public interface IExp<T> { T V { get; set; } }
                    public class Sender { public IExp<int> D(string s) => JsonSerializer.Deserialize<IExp<int>>(s); }
                }
                """);
            output.AssertInitializerContains("Register.EmptyImplementation(typeof(global::TestApp.IExp<global::System.Int32>)");
        }

        [Fact] public void ExperimentalQuery() => Run("""
            #pragma warning disable EXP03
            [System.Diagnostics.CodeAnalysis.Experimental("EXP03")] public class Thing { public int V { get; set; } }
            [System.Diagnostics.CodeAnalysis.Experimental("EXP03")] public interface IQ : Zerra.CQRS.IQueryHandler { Thing Get(); }
            public interface IQ2 : Zerra.CQRS.IQueryHandler { Thing Get(); }
            """);

        [Fact] public void ObsoleteInterfaceModelAndQuery() => Run("""
            #pragma warning disable CS0612, CS0618
            [Obsolete("x", DiagnosticId = "OBS01")] public interface IOld { int V { get; set; } }
            [Obsolete("x")] public interface IQ : Zerra.CQRS.IQueryHandler { IOld Get(); }
            public class Sender { public IOld D(string s) => JsonSerializer.Deserialize<IOld>(s); }
            """);

        [Fact] public void ObsoleteErrorBaseType() => Run("""
            [Obsolete("x", true)] public class B { public int V { get; set; } }
            [Obsolete("y")] public class D : B { }
            #pragma warning disable CS0612, CS0618
            public class Sender { public string S() => JsonSerializer.Serialize(new D()); }
            #pragma warning restore CS0612, CS0618
            """);

        [Fact] public void OtherAssemblyInternalType() => GeneratorRunner.Run("""
            using Zerra.Serialization.Json;
            namespace TestApp
            {
                public class Sender { public string S() => JsonSerializer.Serialize(new Lib.LibModel()); }
            }
            """, librarySource: """
            namespace Lib
            {
                internal interface IHidden<T> { }
                internal class Hidden { public int V { get; set; } }
                public class LibModel : IHidden<Hidden> { public int V { get; set; } }
            }
            """);

        [Fact] public void OtherAssemblyInternalTypeWithInternalsVisibleTo() => GeneratorRunner.Run("""
            using Zerra.Serialization.Json;
            namespace TestApp
            {
                public class Sender { public string S() => JsonSerializer.Serialize(new Lib.LibModel()); }
            }
            """, librarySource: """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ZerraSourceGenerationTestAssembly")]
            namespace Lib
            {
                internal interface IHidden<T> { }
                internal class Hidden { public int V { get; set; } }
                public class LibModel : IHidden<Hidden> { public int V { get; set; } }
            }
            """).AssertInitializerContains("TypeDetail<global::Lib.Hidden>");

        [Fact] public void QueryTypesShadowedInGeneratedNamespace() => Run("""
            public interface IQ : Zerra.CQRS.IQueryHandler { Other.Thing Get(); Other.Thing Prop { get; } }
            }
            namespace TestApp.Other { public class Thing { public int V { get; set; } } }
            namespace TestApp.SourceGeneration.TestApp { public class Thing { }
            """);

        [Fact] public void InlineArrayAndFixedBuffer() => Run("""
            [System.Runtime.CompilerServices.InlineArray(4)] public struct Buf { private int e; }
            public unsafe struct Fixed { public fixed int F[4]; public int V; }
            [GenerateTypeDetail] public class M { public Buf B { get; set; } public Fixed X { get; set; } }
            """);
    }
}
