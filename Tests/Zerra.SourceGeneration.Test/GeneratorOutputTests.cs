// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Xunit;

namespace Zerra.SourceGeneration.Test
{
    //Each test compiles the generated code against the real Zerra assemblies, so a generated line that no longer
    //matches the Register API or the input type fails here. The asserts only spot check what was registered,
    //the Demo covers the generated code actually running.
    public class GeneratorOutputTests
    {
        [Fact]
        public void NothingToGenerate()
        {
            var output = GeneratorRunner.Run("""
                namespace TestApp
                {
                    public class Plain
                    {
                        public int Value { get; set; }
                    }
                }
                """);

            _ = Assert.Single(output.Sources);
            output.AssertInitializerContains("namespace TestApp.SourceGeneration");
            output.AssertInitializerDoesNotContain("global::Zerra.Reflection.Register.");
        }

        //native AOT flags the Expression.Bind an initializer in an expression tree compiles to, the type is generated to keep its members
        //and only the method holding the expression tree is suppressed
        [Fact]
        public void ExpressionTreeInitializers()
        {
            var output = GeneratorRunner.Run("""
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Linq.Expressions;
                using System.Threading.Tasks;

                namespace TestApp
                {
                    public class Source
                    {
                        public int A { get; set; }
                        public int B { get; set; }
                    }

                    public class Projected { public int Value { get; set; } }
                    public class ProjectedAsync { public int Value { get; set; } }
                    public class ProjectedImplicit { public int Value { get; set; } }
                    public class ProjectedLocal { public int Value { get; set; } }
                    public class ProjectedNested { public int Value { get; set; } }
                    public class ProjectedGeneric<T> { public T? Value { get; set; } }
                    public class NotExpression { public int Value { get; set; } }

                    public class Queries
                    {
                        private class PrivateProjected { public int Value { get; set; } }

                        public IQueryable<Projected> Select(IQueryable<Source> query) => query.Select(x => new Projected { Value = x.A });

                        public async Task<ProjectedAsync[]> SelectAsync(IQueryable<Source> query, int add)
                        {
                            await Task.Yield();
                            return query.Select(x => new ProjectedAsync { Value = x.A + add }).ToArray();
                        }

                        public Expression<Func<Source, ProjectedImplicit>> Implicit() => x => new() { Value = x.B };

                        public IQueryable<ProjectedLocal> Local(IQueryable<Source> query)
                        {
                            return Run();
                            IQueryable<ProjectedLocal> Run() => query.Select(x => new ProjectedLocal { Value = x.A });
                        }

                        public IQueryable<int> Nested(IQueryable<IGrouping<int, Source>> query) => query.Select(g => g.Select(x => new ProjectedNested { Value = x.A }).Count());

                        public IQueryable<ProjectedGeneric<int>> Generic(IQueryable<Source> query) => query.Select(x => new ProjectedGeneric<int> { Value = x.A });

                        public IEnumerable<NotExpression> Enumerable(IEnumerable<Source> items) => items.Select(x => new NotExpression { Value = x.A });

                        public int Private(IQueryable<Source> query) => query.Select(x => new PrivateProjected { Value = x.A }).Count();
                    }
                }
                """);

            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.Projected,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.ProjectedAsync,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.ProjectedImplicit,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.ProjectedLocal,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.ProjectedNested,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.ProjectedGeneric<int>,");

            const string prefix = "[module: global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"IL2026\", Justification = \"An object initializer in an expression tree, its type is generated so its members are kept.\", Scope = \"member\", Target = \"";
            output.AssertInitializerContains(prefix + "M:TestApp.Queries.Select(System.Linq.IQueryable{TestApp.Source})\")]");
            output.AssertInitializerContains(prefix + "M:TestApp.Queries.SelectAsync(System.Linq.IQueryable{TestApp.Source},System.Int32)\")]");
            output.AssertInitializerContains(prefix + "M:TestApp.Queries.Implicit\")]");
            output.AssertInitializerContains(prefix + "M:TestApp.Queries.Local(System.Linq.IQueryable{TestApp.Source})\")]");
            output.AssertInitializerContains(prefix + "M:TestApp.Queries.Nested(System.Linq.IQueryable{System.Linq.IGrouping{System.Int32,TestApp.Source}})\")]");
            output.AssertInitializerContains(prefix + "M:TestApp.Queries.Generic(System.Linq.IQueryable{TestApp.Source})\")]");

            //a delegate lambda compiles to plain code, and a type that is not generated has nothing keeping its members
            output.AssertInitializerDoesNotContain("global::TestApp.NotExpression");
            output.AssertInitializerDoesNotContain("M:TestApp.Queries.Enumerable");
            output.AssertInitializerDoesNotContain("PrivateProjected");
            output.AssertInitializerDoesNotContain("M:TestApp.Queries.Private");
        }

        //an array brings its generic interfaces, whose element has no namespace to name here
        [Fact]
        public void ArrayOfGlobalNamespaceType()
        {
            var output = GeneratorRunner.Run("""
                using Zerra.Reflection;

                public class GlobalItem
                {
                    public int Value { get; set; }
                }

                [GenerateTypeDetail]
                public class GlobalModel
                {
                    public GlobalItem[]? Items { get; set; }
                }
                """);

            output.AssertInitializerContains("global::GlobalItem[]");
            output.AssertInitializerDoesNotContain("<global namespace>");
        }

        [Fact]
        public void QueryHandler_ParametersNamedLikeCallerFields()
        {
            //the caller's fields are read through this so parameters with the same names don't hide them, the int types fail to compile if they do
            var output = GeneratorRunner.Run("""
                using System.Threading.Tasks;
                using Zerra.CQRS;

                namespace TestApp
                {
                    public interface IShadowQueryProvider : IQueryHandler
                    {
                        Task<int> Get(int bus, int source, int remote, int overrideServiceUrl, int overrideEncryptor);
                    }
                }
                """);

            var caller = output.Sources["Caller_IShadowQueryProvider.cs"];
            Assert.Contains("this.bus._CallMethodTaskGeneric<", caller);
            Assert.Contains("[@bus, @source, @remote, @overrideServiceUrl, @overrideEncryptor], this.source, this.remote, this.overrideServiceUrl, this.overrideEncryptor);", caller);
        }

        [Fact]
        public void QueryHandler()
        {
            var output = GeneratorRunner.Run("""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using Zerra.CQRS;

                namespace TestApp
                {
                    public class Widget
                    {
                        public int ID { get; set; }
                        public string? Name { get; set; }
                    }

                    public interface IWidgetQueryProvider : IQueryHandler
                    {
                        Task<Widget?> GetWidget(int id);
                        Task<List<Widget>> GetWidgets(string? search, int[] ids);
                        Task Ping();
                        int Count();
                        void Touch(Widget widget);
                        Task<T> Generic<T>(T value) where T : class, new();
                        Task<T> GenericMany<T, U, V>(T value, U other, V? last) where T : Widget, new() where U : unmanaged where V : notnull;
                        IAsyncEnumerable<Widget> Stream();
                    }
                }
                """);

            Assert.Contains("Caller_IWidgetQueryProvider.cs", output.Sources.Keys);
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Router(typeof(global::TestApp.IWidgetQueryProvider), static (global::Zerra.CQRS.IBusInternal bus, string source, bool remote, string? overrideServiceUrl, global::Zerra.Encryption.IEncryptor? overrideEncryptor) => new global::TestApp.SourceGeneration.Caller_IWidgetQueryProvider(bus, source, remote, overrideServiceUrl, overrideEncryptor));");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Handler(typeof(global::TestApp.IWidgetQueryProvider), \"GetWidget\", true, typeof(global::TestApp.Widget)");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Handler(typeof(global::TestApp.IWidgetQueryProvider), \"Ping\", true, null");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Handler(typeof(global::TestApp.IWidgetQueryProvider), \"Count\", false, null");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Handler(typeof(global::TestApp.IWidgetQueryProvider), \"Touch\", false, null");
            //generic methods can only go through the caller, there is no closed method to register
            output.AssertInitializerDoesNotContain("\"Generic\"");
            output.AssertInitializerDoesNotContain("global::Zerra.Reflection.Register.Finder(typeof(global::TestApp.IWidgetQueryProvider));");
            //models reached through the interface
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.Widget,");
        }

        [Fact]
        public void CommandAndEventHandlers()
        {
            var output = GeneratorRunner.Run("""
                using System.Threading;
                using System.Threading.Tasks;
                using Zerra.CQRS;

                namespace TestApp
                {
                    public class CreateWidgetCommand : ICommand
                    {
                        public string? Name { get; set; }
                    }

                    public class PriceWidgetCommand : ICommand<decimal>
                    {
                        public int ID { get; set; }
                    }

                    public class WidgetCreatedEvent : IEvent
                    {
                        public int ID { get; set; }
                    }

                    public interface IWidgetCommandHandler :
                        ICommandHandler<CreateWidgetCommand>,
                        ICommandHandler<PriceWidgetCommand, decimal>
                    {
                    }

                    public interface IWidgetEventHandler :
                        IEventHandler<WidgetCreatedEvent>
                    {
                    }
                }
                """);

            //no callers, commands and events route by message type
            Assert.DoesNotContain(output.Sources.Keys, x => x.StartsWith("Caller_"));

            output.AssertInitializerContains("global::Zerra.Reflection.Register.CommandOrEventInfo(typeof(global::TestApp.IWidgetCommandHandler), \"IWidgetCommandHandler\", [typeof(global::TestApp.CreateWidgetCommand), typeof(global::TestApp.PriceWidgetCommand)], []);");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.CommandOrEventInfo(typeof(global::TestApp.IWidgetEventHandler), \"IWidgetEventHandler\", [], [typeof(global::TestApp.WidgetCreatedEvent)]);");

            output.AssertInitializerContains("global::Zerra.Reflection.Register.Handler(typeof(global::TestApp.IWidgetCommandHandler), \"Handle-CreateWidgetCommand\", true, null");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Handler(typeof(global::TestApp.IWidgetCommandHandler), \"Handle-PriceWidgetCommand\", true, typeof(decimal)");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Handler(typeof(global::TestApp.IWidgetEventHandler), \"Handle-WidgetCreatedEvent\", true, null");

            //only a command with a result needs a typed dispatcher
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Router(typeof(global::TestApp.PriceWidgetCommand), ");
            output.AssertInitializerDoesNotContain("global::Zerra.Reflection.Register.Router(typeof(global::TestApp.CreateWidgetCommand), ");

            output.AssertInitializerContains("global::Zerra.Reflection.Register.Finder(typeof(global::TestApp.CreateWidgetCommand));");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Finder(typeof(global::TestApp.PriceWidgetCommand));");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Finder(typeof(global::TestApp.WidgetCreatedEvent));");
            output.AssertInitializerDoesNotContain("global::Zerra.Reflection.Register.Finder(typeof(global::TestApp.IWidgetCommandHandler));");
        }

        [Fact]
        public void Enums()
        {
            var output = GeneratorRunner.Run("""
                using System;

                namespace TestApp
                {
                    public enum ByteEnum : byte { A, B }
                    public enum SByteEnum : sbyte { A = -1, B }
                    public enum ShortEnum : short { A, B }
                    public enum UShortEnum : ushort { A, B }
                    public enum IntEnum { A, B }
                    public enum UIntEnum : uint { A, B }
                    public enum LongEnum : long { A, B }
                    public enum ULongEnum : ulong { A, B = ulong.MaxValue }

                    [Flags]
                    public enum FlagsEnum
                    {
                        None = 0,
                        [EnumName("First \"Flag\"")]
                        First = 1,
                        Second = 2
                    }

                    internal enum InternalEnum { A }
                }
                """);

            output.AssertInitializerContains("global::Zerra.Reflection.Register.Enum(typeof(global::TestApp.ByteEnum), global::Zerra.Reflection.CoreEnumType.Byte, false");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Enum(typeof(global::TestApp.SByteEnum), global::Zerra.Reflection.CoreEnumType.SByte, false");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Enum(typeof(global::TestApp.ShortEnum), global::Zerra.Reflection.CoreEnumType.Int16, false");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Enum(typeof(global::TestApp.UShortEnum), global::Zerra.Reflection.CoreEnumType.UInt16, false");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Enum(typeof(global::TestApp.IntEnum), global::Zerra.Reflection.CoreEnumType.Int32, false");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Enum(typeof(global::TestApp.UIntEnum), global::Zerra.Reflection.CoreEnumType.UInt32, false");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Enum(typeof(global::TestApp.LongEnum), global::Zerra.Reflection.CoreEnumType.Int64, false");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Enum(typeof(global::TestApp.ULongEnum), global::Zerra.Reflection.CoreEnumType.UInt64, false");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Enum(typeof(global::TestApp.FlagsEnum), global::Zerra.Reflection.CoreEnumType.Int32, true");
            output.AssertInitializerContains("new global::EnumName.EnumFieldInfo(\"First\", \"First \\\"Flag\\\"\", global::TestApp.FlagsEnum.@First)");
            output.AssertInitializerDoesNotContain("InternalEnum");
        }

        [Fact]
        public void NullableEnums_HaveNullableUnderlyingType()
        {
            //the same as the runtime TypeDetail, a nullable enum's underlying type is the nullable one
            var output = GeneratorRunner.Run("""
                using Zerra.Reflection;

                namespace TestApp
                {
                    public enum ByteEnum : byte { A, B }
                    public enum LongEnum : long { A, B }

                    [GenerateTypeDetail]
                    public class Model
                    {
                        public ByteEnum? ByteValue { get; set; }
                        public LongEnum? LongValue { get; set; }
                    }
                }
                """);

            output.AssertInitializerContains("global::Zerra.Reflection.CoreEnumType.ByteNullable");
            output.AssertInitializerContains("global::Zerra.Reflection.CoreEnumType.Int64Nullable");
        }

        [Fact]
        public void TypeDetailModels()
        {
            var output = GeneratorRunner.Run("""
                using System;
                using System.Collections.Generic;
                using Zerra.Reflection;

                namespace TestApp
                {
                    [GenerateTypeDetail]
                    public class Model
                    {
                        public int ID { get; set; }
                        public string? Name { get; private set; }
                        public required string Code { get; init; }
                        public DateTime? When { get; set; }
                        public Guid Key;
                        public readonly int ReadOnlyField;
                        private int privateField;
                        public int Private { get => privateField; set => privateField = value; }

                        public Child[] Array { get; set; } = [];
                        public List<Child> List { get; set; } = new();
                        public IReadOnlyCollection<Child>? Collection { get; set; }
                        public Dictionary<string, Child> Dictionary { get; set; } = new();
                        public IReadOnlyDictionary<int, List<Child>>? NestedDictionary { get; set; }
                        public HashSet<StructModel> Set { get; set; } = new();
                        public Generic<Child>? Generic { get; set; }
                        public IHolder<Child>? Interface { get; set; }
                        public RecordModel? Record { get; set; }
                        public Model? Self { get; set; }

                        public Model() { }
                        public Model(int id) { ID = id; }

                        public string Describe(int count, Child? child) => $"{Name}{count}{child}";
                    }

                    public class Child
                    {
                        public int Value { get; set; }
                    }

                    public struct StructModel
                    {
                        public int X { get; set; }
                        public int Y;
                    }

                    public record RecordModel(int A, string B);

                    public class Generic<T>
                    {
                        public T? Value { get; set; }
                    }

                    public interface IHolder<T> where T : class, new()
                    {
                        int Value { get; set; }
                        string Name { get; }
                        T Get(int index);
                        void DoNothing();
                        System.Threading.Tasks.Task<int> GetAsync();
                    }

                    [GenerateTypeDetail]
                    public abstract class AbstractModel
                    {
                        public int Value { get; set; }
                    }

                    [GenerateTypeDetail]
                    public static class StaticModel
                    {
                        public static int Value { get; set; }
                    }

                    [GenerateTypeDetail]
                    [IgnoreGenerateTypeDetail]
                    public class IgnoredModel
                    {
                        public int Value { get; set; }
                    }
                }
                """);

            output.AssertInitializerContains("global::Zerra.Reflection.Register.Type(");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.Model,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.Child,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.StructModel,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.RecordModel,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.Generic<global::TestApp.Child>,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::System.Collections.Generic.List<global::TestApp.Child>,global::TestApp.Child,object,object>();");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::System.Collections.Generic.Dictionary<string, global::TestApp.Child>,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.AbstractModel,");
            output.AssertInitializerDoesNotContain("global::TestApp.IgnoredModel");

            //generic dictionaries are SpecialType.Dictionary, as the runtime TypeDetail reports (the lookup is by metadata name, Dictionary`2)
            output.AssertInitializerContains("global::Zerra.Reflection.SpecialType.Dictionary");

            //closed generic interfaces reached by a model get an empty implementation
            Assert.Contains("Empty_IHolder_TestApp_Child_.cs", output.Sources.Keys);
            output.AssertInitializerContains("global::Zerra.Reflection.Register.EmptyImplementation(typeof(global::TestApp.IHolder<global::TestApp.Child>), typeof(Empty_IHolder_TestApp_Child_));");
        }

        [Fact]
        public void InheritedMembers()
        {
            var output = GeneratorRunner.Run("""
                using System.Collections.Generic;
                using Zerra.Reflection;

                namespace InheritedTestApp
                {
                    public abstract class Endpoint
                    {
                        public string? ProviderType { get; set; }
                        public Dictionary<string, string>? Parameters { get; set; }
                        public int Port;
                        public virtual string? Label { get; set; }
                        public string? Hidden { get; set; }
                        public static int StaticValue { get; set; }
                        protected int ProtectedValue { get; set; }
                    }

                    public class Generic<T> : Endpoint
                    {
                        public T? Value { get; set; }
                    }

                    [GenerateTypeDetail]
                    public class Replica : Generic<int>
                    {
                        public string? Name { get; set; }
                        public override string? Label { get; set; }
                        public new int Hidden { get; set; }
                    }

                    public class RequiredBase
                    {
                        public required string Code { get; set; }
                    }

                    [GenerateTypeDetail]
                    public class RequiredChild : RequiredBase
                    {
                    }
                }
                """);

            string Member(string type, string name) => Assert.Single(output.Lines(x => x.Contains($"MemberDetail<{type}>(typeof(global::InheritedTestApp.Replica)") && x.Contains($"\"{name}\",")));
            _ = Member("string", "Name");
            _ = Member("string", "ProviderType");
            _ = Member("global::System.Collections.Generic.Dictionary<string, string>", "Parameters");
            _ = Member("int", "Port");
            _ = Member("int", "Value");
            _ = Member("string", "Label");
            _ = Member("int", "Hidden");
            Assert.Empty(output.Lines(x => x.Contains("MemberDetail<string>(typeof(global::InheritedTestApp.Replica)") && x.Contains("\"Hidden\",")));
            Assert.Empty(output.Lines(x => x.Contains("(typeof(global::InheritedTestApp.Replica)") && (x.Contains("\"StaticValue\",") || x.Contains("\"ProtectedValue\","))));

            //an inherited required member is set by the constructor detail, and there is no parameterless creator
            output.AssertInitializerContains("new global::InheritedTestApp.RequiredChild(){ @Code = ");
            output.AssertInitializerDoesNotContain("static () => new global::InheritedTestApp.RequiredChild()");

            var assembly = output.Load();
            var type = assembly.GetType("InheritedTestApp.Replica")!;
            var model = Activator.CreateInstance(type)!;
            type.GetProperty("Name")!.SetValue(model, "Replica1");
            type.GetProperty("ProviderType")!.SetValue(model, "Folder");
            type.GetProperty("Parameters")!.SetValue(model, new Dictionary<string, string>() { { "Path", "C:\\Data" } });
            type.GetField("Port")!.SetValue(model, 8080);
            type.GetProperty("Value")!.SetValue(model, 5);

            var json = Zerra.Serialization.Json.JsonSerializer.Serialize(model, type);
            var jsonResult = Zerra.Serialization.Json.JsonSerializer.Deserialize(json.AsSpan(), type)!;
            AssertReplica(type, jsonResult);

            var bytes = Zerra.Serialization.Bytes.ByteSerializer.Serialize(model, type);
            var bytesResult = Zerra.Serialization.Bytes.ByteSerializer.Deserialize(bytes, type)!;
            AssertReplica(type, bytesResult);

            static void AssertReplica(Type type, object result)
            {
                Assert.Equal("Replica1", type.GetProperty("Name")!.GetValue(result));
                Assert.Equal("Folder", type.GetProperty("ProviderType")!.GetValue(result));
                Assert.Equal("C:\\Data", ((Dictionary<string, string>)type.GetProperty("Parameters")!.GetValue(result)!)["Path"]);
                Assert.Equal(8080, type.GetField("Port")!.GetValue(result));
                Assert.Equal(5, type.GetProperty("Value")!.GetValue(result));
            }
        }

        //special types such as Type and Task never get type detail, so a collection of one can't register serializers and a map, which look up the element's detail when registered
        //an Exception reaches Type[] through TargetSite, and native AOT can't make the missing detail at startup
        [Fact]
        public void CollectionsOfNotGeneratedSpecialTypes()
        {
            var output = GeneratorRunner.Run("""
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using Zerra.Reflection;

                namespace SpecialTestApp
                {
                    [GenerateTypeDetail]
                    public class AppException : Exception
                    {
                        public AppException(string message) : base(message) { }
                        public int Code { get; set; }
                    }

                    [GenerateTypeDetail]
                    public class Holder
                    {
                        public Type? Single { get; set; }
                        public Type[]? Types { get; set; }
                        public List<Type>? TypeList { get; set; }
                        public Dictionary<Type, string>? TypeKeys { get; set; }
                        public Dictionary<string, Type>? TypeValues { get; set; }
                        public Task[]? Tasks { get; set; }
                        public string[]? Names { get; set; }
                    }
                }
                """);

            //the collections are still found and get type detail, Type itself does not
            output.AssertInitializerContains("TypeDetail<global::System.Type[]>");
            output.AssertInitializerDoesNotContain("TypeDetail<global::System.Type>");

            var serializersAndMaps = new List<string>(output.Lines(x => x.Contains("Register.SerializersAndMap<")));
            Assert.DoesNotContain(serializersAndMaps, x => System.Text.RegularExpressions.Regex.IsMatch(x, @",global::System\.(Type|Threading\.Tasks\.Task)[,>]"));
            Assert.Contains(serializersAndMaps, x => x.Contains("Register.SerializersAndMap<global::SpecialTestApp.Holder,"));
            Assert.Contains(serializersAndMaps, x => x.Contains("Register.SerializersAndMap<global::SpecialTestApp.AppException,"));
            Assert.Contains(serializersAndMaps, x => x.Contains("Register.SerializersAndMap<string[],string,object,object>"));
            AssertRegisteredTypeArgumentsGenerated(output);
        }

        [Fact]
        public void CollectionsOfNotGeneratedSpecialTypesEveryKind()
        {
            var output = GeneratorRunner.Run("""
                using System;
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;
                using Zerra.Reflection;

                namespace SpecialKindsTestApp
                {
                    public class TypeList : List<Type> { }

                    public class TypeLookup : Dictionary<string, Type> { }

                    [GenerateTypeDetail]
                    public class Holder
                    {
                        public Task<int>[]? GenericTasks { get; set; }
                        public IEnumerable<Task>? TaskEnumerable { get; set; }
                        public CancellationToken[]? Tokens { get; set; }
                        public IntPtr[]? Pointers { get; set; }
                        public HashSet<Type>? TypeSet { get; set; }
                        public IReadOnlyDictionary<string, Type>? ReadOnlyTypeValues { get; set; }
                        public IDictionary<Type, int>? InterfaceTypeKeys { get; set; }
                        public TypeList? CustomTypeList { get; set; }
                        public TypeLookup? CustomTypeLookup { get; set; }

                        public object[]? Objects { get; set; }
                        public List<object>? ObjectList { get; set; }
                        public Dictionary<string, object>? ObjectValues { get; set; }
                        public Dictionary<string, Dictionary<string, int>>? NestedDictionaries { get; set; }
                        public List<Type[]>? TypeArrays { get; set; }
                        public List<int>? Numbers { get; set; }
                        public string? Name { get; set; }
                    }
                }
                """);

            var serializersAndMaps = new List<string>(output.Lines(x => x.Contains("Register.SerializersAndMap<")));
            void Registered(string type) => Assert.Contains(serializersAndMaps, x => x.Contains($"Register.SerializersAndMap<{type},"));
            void NotRegistered(string type) => Assert.DoesNotContain(serializersAndMaps, x => x.Contains($"Register.SerializersAndMap<{type},"));

            //an element, key, or value without type detail skips the registration, by array, interface, or a class deriving from a collection
            NotRegistered("global::System.Threading.Tasks.Task<int>[]");
            NotRegistered("global::System.Collections.Generic.IEnumerable<global::System.Threading.Tasks.Task>");
            NotRegistered("global::System.Threading.CancellationToken[]");
            NotRegistered("global::System.IntPtr[]");
            NotRegistered("nint[]");
            NotRegistered("global::System.Collections.Generic.HashSet<global::System.Type>");
            NotRegistered("global::System.Collections.Generic.IReadOnlyDictionary<string, global::System.Type>");
            NotRegistered("global::System.Collections.Generic.IDictionary<global::System.Type, int>");
            NotRegistered("global::SpecialKindsTestApp.TypeList");
            NotRegistered("global::SpecialKindsTestApp.TypeLookup");
            Assert.DoesNotContain(serializersAndMaps, x => System.Text.RegularExpressions.Regex.IsMatch(x, @",global::System\.(Type|Threading\.Tasks\.Task(<int>)?|Threading\.CancellationToken|IntPtr)[,>]"));

            //object and dictionaries have type detail, and so does an array of Type, so collections of them are still registered
            Registered("global::SpecialKindsTestApp.Holder");
            Registered("object[]");
            Registered("global::System.Collections.Generic.List<object>");
            Registered("global::System.Collections.Generic.Dictionary<string, object>");
            Registered("global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.Dictionary<string, int>>");
            Registered("global::System.Collections.Generic.List<global::System.Type[]>");
            Registered("global::System.Collections.Generic.List<int>");
            AssertRegisteredTypeArgumentsGenerated(output);

            //the generated registrations run and the model still serializes
            var assembly = output.Load();
            var type = assembly.GetType("SpecialKindsTestApp.Holder")!;
            var model = Activator.CreateInstance(type)!;
            type.GetProperty("Name")!.SetValue(model, "Holder1");
            type.GetProperty("Numbers")!.SetValue(model, new List<int>() { 1, 2, 3 });
            type.GetProperty("NestedDictionaries")!.SetValue(model, new Dictionary<string, Dictionary<string, int>>() { { "Outer", new Dictionary<string, int>() { { "Inner", 4 } } } });

            var json = Zerra.Serialization.Json.JsonSerializer.Serialize(model, type);
            var jsonResult = Zerra.Serialization.Json.JsonSerializer.Deserialize(json.AsSpan(), type)!;
            AssertHolder(type, jsonResult);

            var bytes = Zerra.Serialization.Bytes.ByteSerializer.Serialize(model, type);
            var bytesResult = Zerra.Serialization.Bytes.ByteSerializer.Deserialize(bytes, type)!;
            AssertHolder(type, bytesResult);

            static void AssertHolder(Type type, object result)
            {
                Assert.Equal("Holder1", type.GetProperty("Name")!.GetValue(result));
                Assert.Equal([1, 2, 3], (List<int>)type.GetProperty("Numbers")!.GetValue(result)!);
                Assert.Equal(4, ((Dictionary<string, Dictionary<string, int>>)type.GetProperty("NestedDictionaries")!.GetValue(result)!)["Outer"]["Inner"]);
            }
        }

        //a model of the user's that shares a special type's name is an ordinary model
        [Fact]
        public void ModelsNamedLikeSpecialTypes()
        {
            var output = GeneratorRunner.Run("""
                using System.Collections.Generic;
                using Zerra.Reflection;

                namespace NamedTestApp
                {
                    public class Type
                    {
                        public string? Name { get; set; }
                    }

                    public class Task
                    {
                        public int Id { get; set; }
                    }

                    public class CancellationToken
                    {
                        public bool Cancelled { get; set; }
                    }

                    [GenerateTypeDetail]
                    public class Holder
                    {
                        public Type? Single { get; set; }
                        public List<Type>? Types { get; set; }
                        public Task[]? Tasks { get; set; }
                        public Dictionary<string, CancellationToken>? Tokens { get; set; }
                    }
                }
                """);

            output.AssertInitializerContains("TypeDetail<global::NamedTestApp.Type>");
            output.AssertInitializerContains("TypeDetail<global::NamedTestApp.Task>");
            output.AssertInitializerContains("TypeDetail<global::NamedTestApp.CancellationToken>");

            var serializersAndMaps = new List<string>(output.Lines(x => x.Contains("Register.SerializersAndMap<")));
            void Registered(string type) => Assert.Contains(serializersAndMaps, x => x.Contains($"Register.SerializersAndMap<{type},"));
            Registered("global::NamedTestApp.Type");
            Registered("global::NamedTestApp.Task");
            Registered("global::NamedTestApp.CancellationToken");
            Registered("global::System.Collections.Generic.List<global::NamedTestApp.Type>");
            Registered("global::NamedTestApp.Task[]");
            Registered("global::System.Collections.Generic.Dictionary<string, global::NamedTestApp.CancellationToken>");
            AssertRegisteredTypeArgumentsGenerated(output);

            var assembly = output.Load();
            var holderType = assembly.GetType("NamedTestApp.Holder")!;
            var typeType = assembly.GetType("NamedTestApp.Type")!;
            var taskType = assembly.GetType("NamedTestApp.Task")!;

            //the runtime lookup agrees with the generator
            Assert.False(Zerra.Reflection.TypeLookup.GetSpecialType(typeType, out _));
            Assert.False(Zerra.Reflection.TypeLookup.GetSpecialType(taskType, out _));
            Assert.True(Zerra.Reflection.TypeLookup.GetSpecialType(typeof(Type), out var specialType));
            Assert.Equal(Zerra.Reflection.SpecialType.Type, specialType);
            Assert.True(Zerra.Reflection.TypeLookup.GetSpecialType(typeof(System.Threading.Tasks.Task<int>), out specialType));
            Assert.Equal(Zerra.Reflection.SpecialType.Task, specialType);
            Assert.True(Zerra.Reflection.TypeLookup.GetSpecialType(typeof(Zerra.Collections.ConcurrentFactoryDictionary<int, int>), out specialType));
            Assert.Equal(Zerra.Reflection.SpecialType.Dictionary, specialType);
            Assert.Null(Zerra.Reflection.TypeAnalyzer.GetTypeDetail(typeType).SpecialType);

            var model = Activator.CreateInstance(holderType)!;
            var single = Activator.CreateInstance(typeType)!;
            typeType.GetProperty("Name")!.SetValue(single, "Single1");
            holderType.GetProperty("Single")!.SetValue(model, single);
            var task = Activator.CreateInstance(taskType)!;
            taskType.GetProperty("Id")!.SetValue(task, 7);
            var tasks = Array.CreateInstance(taskType, 1);
            tasks.SetValue(task, 0);
            holderType.GetProperty("Tasks")!.SetValue(model, tasks);

            var json = Zerra.Serialization.Json.JsonSerializer.Serialize(model, holderType);
            var jsonResult = Zerra.Serialization.Json.JsonSerializer.Deserialize(json.AsSpan(), holderType)!;
            AssertHolder(jsonResult);

            var bytes = Zerra.Serialization.Bytes.ByteSerializer.Serialize(model, holderType);
            var bytesResult = Zerra.Serialization.Bytes.ByteSerializer.Deserialize(bytes, holderType)!;
            AssertHolder(bytesResult);

            void AssertHolder(object result)
            {
                Assert.Equal("Single1", typeType.GetProperty("Name")!.GetValue(holderType.GetProperty("Single")!.GetValue(result)));
                var resultTasks = (Array)holderType.GetProperty("Tasks")!.GetValue(result)!;
                Assert.Equal(7, taskType.GetProperty("Id")!.GetValue(resultTasks.GetValue(0)));
            }
        }

        //registering the serializers looks up the detail of the element, key, and value, so each one must be generated, as native AOT can't make it at startup
        //an element that wraps a special type is its own model and is generated, the special type inside is not looked up when registered
        [Fact]
        public void CollectionsOfTypesWrappingSpecialTypes()
        {
            var output = GeneratorRunner.Run("""
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using Zerra.Reflection;

                namespace WrappedTestApp
                {
                    public class Model
                    {
                        public Type? Kind { get; set; }
                    }

                    [GenerateTypeDetail]
                    public class Holder
                    {
                        public IntPtr?[]? NullablePointers { get; set; }
                        public KeyValuePair<string, Type>[]? Pairs { get; set; }
                        public Tuple<Type, int>[]? Tuples { get; set; }
                        public Lazy<Task>[]? Lazies { get; set; }
                        public List<Model>? Models { get; set; }
                        public Dictionary<Model, Model[]>? ModelLookup { get; set; }
                        public List<int?>? NullableInts { get; set; }
                        public Type[]? Types { get; set; }
                        public Dictionary<string, Type>? TypeValues { get; set; }
                    }
                }
                """);

            var serializersAndMaps = new List<string>(output.Lines(x => x.Contains("Register.SerializersAndMap<")));
            void Registered(string type, string elementType) => Assert.Contains(serializersAndMaps, x => x.Contains($"Register.SerializersAndMap<{type},{elementType},"));
            Registered("nint?[]", "nint?");
            Registered("global::System.Collections.Generic.KeyValuePair<string, global::System.Type>[]", "global::System.Collections.Generic.KeyValuePair<string, global::System.Type>");
            Registered("global::System.Tuple<global::System.Type, int>[]", "global::System.Tuple<global::System.Type, int>");
            Registered("global::System.Lazy<global::System.Threading.Tasks.Task>[]", "global::System.Lazy<global::System.Threading.Tasks.Task>");

            AssertRegisteredTypeArgumentsGenerated(output);
            _ = output.Load();
        }

        private static void AssertRegisteredTypeArgumentsGenerated(GeneratorOutput output)
        {
            const string prefix = "Register.SerializersAndMap<";
            foreach (var line in output.Lines(x => x.Contains(prefix)))
            {
                var start = line.IndexOf(prefix) + prefix.Length;
                var end = line.LastIndexOf(">();");
                var arguments = SplitTypeArguments(line.Substring(start, end - start));
                Assert.Equal(4, arguments.Count);

                //a string is a collection of char, but its converter is a core type's and never looks the char up
                if (arguments[0] == "string")
                    continue;

                for (var i = 1; i < arguments.Count; i++)
                {
                    if (arguments[i] == "object")
                        continue;
                    Assert.True(output.Initializer.Contains($"TypeDetail<{arguments[i]}>("), $"{line.Trim()} registers {arguments[i]} without its type detail");
                }
            }

            static List<string> SplitTypeArguments(string text)
            {
                var arguments = new List<string>();
                var depth = 0;
                var argumentStart = 0;
                for (var i = 0; i < text.Length; i++)
                {
                    switch (text[i])
                    {
                        case '<': depth++; break;
                        case '>': depth--; break;
                        case ',' when depth == 0:
                            arguments.Add(text.Substring(argumentStart, i - argumentStart));
                            argumentStart = i + 1;
                            break;
                    }
                }
                arguments.Add(text.Substring(argumentStart));
                return arguments;
            }
        }

        //a custom map only looks up the detail of its source and target when registered, so a collection of a special type keeps its map
        [Fact]
        public void CustomMapOfNotGeneratedSpecialTypes()
        {
            var output = GeneratorRunner.Run("""
                using System;
                using System.Collections.Generic;
                using Zerra.Map;

                namespace SpecialMapTestApp
                {
                    public class TypesToNames : IMapDefinition<Type[], List<Type>>
                    {
                    }
                }
                """);

            output.AssertInitializerContains("global::Zerra.Reflection.Register.CustomMap<global::System.Type[],global::System.Collections.Generic.List<global::System.Type>,global::System.Type,global::System.Type,object,object,object,object>(new global::SpecialMapTestApp.TypesToNames());");
            _ = output.Load();
        }

        [Fact]
        public void NotGeneratedSpecialTypeLookup()
        {
            Assert.True(TypeLookup.IsGeneratedSpecialType(SpecialType.Dictionary));
            Assert.True(TypeLookup.IsGeneratedSpecialType(SpecialType.Object));
            Assert.False(TypeLookup.IsGeneratedSpecialType(SpecialType.Type));
            Assert.False(TypeLookup.IsGeneratedSpecialType(SpecialType.Task));
            Assert.False(TypeLookup.IsGeneratedSpecialType(SpecialType.Void));
            Assert.False(TypeLookup.IsGeneratedSpecialType(SpecialType.Pointer));
            Assert.False(TypeLookup.IsGeneratedSpecialType(SpecialType.CancellationToken));

            var userTypes = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("""
                namespace System.Models { public class Type { } }
                namespace Threading { public class CancellationToken { } }
                namespace App.System { public class Object { } }
                namespace App.System.Threading.Tasks { public class Task { } }
                namespace App { public class Outer { public class Type { } } }
                """, cancellationToken: TestContext.Current.CancellationToken);
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("SpecialTypeLookup", [userTypes], [Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
            Microsoft.CodeAnalysis.ITypeSymbol Symbol(string metadataName) => compilation.GetTypeByMetadataName(metadataName)!;

            Assert.True(TypeLookup.IsNotGeneratedSpecialType(Symbol("System.Type")));
            Assert.True(TypeLookup.IsNotGeneratedSpecialType(Symbol("System.Threading.Tasks.Task")));
            Assert.True(TypeLookup.IsNotGeneratedSpecialType(Symbol("System.Threading.Tasks.Task`1")));
            Assert.True(TypeLookup.IsNotGeneratedSpecialType(Symbol("System.Threading.CancellationToken")));
            Assert.True(TypeLookup.IsNotGeneratedSpecialType(Symbol("System.IntPtr")));
            Assert.True(TypeLookup.IsNotGeneratedSpecialType(Symbol("System.UIntPtr")));
            Assert.True(TypeLookup.IsNotGeneratedSpecialType(Symbol("System.Void")));

            Assert.False(TypeLookup.IsNotGeneratedSpecialType(Symbol("System.Object")));
            Assert.False(TypeLookup.IsNotGeneratedSpecialType(Symbol("System.Collections.Generic.Dictionary`2")));
            Assert.False(TypeLookup.IsNotGeneratedSpecialType(Symbol("System.Collections.Generic.IReadOnlyDictionary`2")));
            Assert.False(TypeLookup.IsNotGeneratedSpecialType(Symbol("System.String")));
            Assert.False(TypeLookup.IsNotGeneratedSpecialType(Symbol("System.Int32")));
            Assert.False(TypeLookup.IsNotGeneratedSpecialType(compilation.CreateArrayTypeSymbol(Symbol("System.Type"))));

            //the same names in other namespaces, including ones nested in or ending like the real namespace
            Assert.False(TypeLookup.SpecialTypeLookup(Symbol("System.Models.Type"), out _));
            Assert.False(TypeLookup.SpecialTypeLookup(Symbol("Threading.CancellationToken"), out _));
            Assert.False(TypeLookup.SpecialTypeLookup(Symbol("App.System.Object"), out _));
            Assert.False(TypeLookup.SpecialTypeLookup(Symbol("App.System.Threading.Tasks.Task"), out _));
            Assert.False(TypeLookup.SpecialTypeLookup(Symbol("App.Outer+Type"), out _));
        }

        //a reference type declared non-nullable tells its member detail it can't be null, everything else is left to the type
        [Fact]
        public void MemberNullability()
        {
            var output = GeneratorRunner.Run("""
                using Zerra.Reflection;

                namespace TestApp
                {
                    [GenerateTypeDetail]
                    public class Model
                    {
                        public string Name { get; set; } = null!;
                        public string? Nick { get; set; }
                        public byte[] Data { get; set; } = null!;
                        public int Count { get; set; }
                        public int? Maybe { get; set; }
                        public string Field = null!;
                        public string? FieldNull;
                    }

                #nullable disable
                    [GenerateTypeDetail]
                    public class Oblivious
                    {
                        public string Name { get; set; }
                    }
                #nullable enable
                }
                """);

            //attributes, isStatic, isExplicitFromInterface, then allowsNull only when it's false
            const string notNull = "[], false, false, false)";
            const string byType = "[], false, false)";
            string Member(string type, string name) => Assert.Single(output.Lines(x => x.Contains($"MemberDetail<{type}>(typeof(global::TestApp.Model)") && x.Contains($"\"{name}\",")));

            Assert.Contains(notNull, Member("string", "Name"));
            Assert.Contains(notNull, Member("byte[]", "Data"));
            Assert.Contains(notNull, Member("string", "Field"));
            Assert.Contains(byType, Member("string", "Nick"));
            Assert.Contains(byType, Member("int", "Count"));
            Assert.Contains(byType, Member("int?", "Maybe"));
            Assert.Contains(byType, Member("string", "FieldNull"));
            //code without nullable annotations allows null
            Assert.Contains(byType, Assert.Single(output.Lines(x => x.Contains("MemberDetail<string>(typeof(global::TestApp.Oblivious)"))));
        }

        [Fact]
        public void EmptyImplementationNullability()
        {
            //the empty implementation must restate nullable reference types, or it gets CS8767/CS8769 (errors under TreatWarningsAsErrors)
            var output = GeneratorRunner.Run("""
                using System.Collections.Generic;
                using Zerra.Reflection;

                namespace TestApp
                {
                    [GenerateTypeDetail]
                    public class Model
                    {
                        public IEqualityComparer<string>? Comparer { get; set; }
                        public ILookup<Child>? Lookup { get; set; }
                    }

                    public class Child
                    {
                        public int Value { get; set; }
                    }

                    public interface ILookup<T> where T : class
                    {
                        string? Label { get; set; }
                        string Name { get; }
                        T? Find(string? key, string required);
                        System.Threading.Tasks.Task<string?> FindAsync(string? key);
                    }
                }
                """);

            Assert.Contains("Empty_IEqualityComparer_string_.cs", output.Sources.Keys);
            Assert.Contains("Empty_ILookup_TestApp_Child_.cs", output.Sources.Keys);

            //internal: a public type needs XML docs (CS1591) and collides with the same generated type in other assemblies (CS0436)
            Assert.Contains("internal sealed class Empty_IEqualityComparer_string_", output.Sources["Empty_IEqualityComparer_string_.cs"]);
        }

        [Fact]
        public void LibraryTypeShapes()
        {
            //shapes found on library types a model reaches, each of which generated code that did not compile
            var output = GeneratorRunner.Run("""
                using System;
                using System.Diagnostics.CodeAnalysis;
                using Zerra.Reflection;

                namespace TestApp
                {
                    [GenerateTypeDetail]
                    public class Model
                    {
                        public Client? Client { get; set; }
                        public Func<Client>? Factory { get; set; }
                    }

                    public struct Options
                    {
                        public int Value { get; set; }
                    }

                    public class Client
                    {
                        //a parameterless constructor that isn't public can't be the creator
                        protected Client() { }
                        //an in parameter is passed the unboxed value, not by in
                        public Client(in Options options) { }
                        //a ref parameter can't be passed an object[] element
                        public Client(ref int count) { }

                        [Obsolete("Use Other", DiagnosticId = "TEST0001")]
                        public string? Old { get; set; }

                        [Experimental("TEST0002")]
                        public string? Preview { get; set; }

                        [Experimental("TEST0003")]
                        public Settings? Settings { get; set; }
                    }

                    [Experimental("TEST0003")]
                    public class Settings
                    {
                        public int Value { get; set; }
                    }
                }
                """);

            //the IDs of obsolete and experimental members and types are suppressed; CS0436 covers generated types an assembly also sees through InternalsVisibleTo
            output.AssertInitializerContains("#pragma warning disable CS0436");
            output.AssertInitializerContains("TEST0001, TEST0002, TEST0003");
            output.AssertInitializerDoesNotContain("new global::TestApp.Client()");
            output.AssertInitializerDoesNotContain("new global::System.Func<global::TestApp.Client>(");
            output.AssertInitializerDoesNotContain("in (global::TestApp.Options)");
            output.AssertInitializerDoesNotContain("ref (int)");
        }

        [Fact]
        public void MapDefinitions()
        {
            var output = GeneratorRunner.Run("""
                using System.Collections.Generic;
                using Zerra.Map;

                namespace TestApp
                {
                    public class Source
                    {
                        public int A { get; set; }
                    }

                    public class Target
                    {
                        public int B { get; set; }
                    }

                    public class SourceToTarget : MapDefinition<Source, Target>
                    {
                        public override void Define(IMapSetup<Source, Target> map)
                        {
                            map.Define(x => x.B, x => x.A);
                        }
                    }

                    public class SourcesToTargets : IMapDefinition<List<Source>, Target[]>
                    {
                    }

                    public class Other
                    {
                        public int C { get; set; }
                    }

                    //one class defining several maps registers each of them
                    public class SeveralMaps : IMapDefinition<Source, Other>, IMapDefinition<Target, Other>
                    {
                        public void Define(IMapSetup<Source, Other> map) => map.Define(x => x.C, x => x.A);
                        public void Define(IMapSetup<Target, Other> map) => map.Define(x => x.C, x => x.B);
                    }
                }
                """);

            output.AssertInitializerContains("global::Zerra.Reflection.Register.CustomMap<global::TestApp.Source,global::TestApp.Other,object,object,object,object,object,object>(new global::TestApp.SeveralMaps());");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.CustomMap<global::TestApp.Target,global::TestApp.Other,object,object,object,object,object,object>(new global::TestApp.SeveralMaps());");

            output.AssertInitializerContains("global::Zerra.Reflection.Register.CustomMap<global::TestApp.Source,global::TestApp.Target,object,object,object,object,object,object>(new global::TestApp.SourceToTarget());");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.CustomMap<global::System.Collections.Generic.List<global::TestApp.Source>,global::TestApp.Target[],global::TestApp.Source,global::TestApp.Target,object,object,object,object>(new global::TestApp.SourcesToTargets());");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.Source,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.Target,");
        }

        [Fact]
        public void PartialTypes()
        {
            //each partial declaration is its own syntax node, the generator must still see the type once
            var output = GeneratorRunner.Run("""
                using System.Threading.Tasks;
                using Zerra.CQRS;

                namespace TestApp
                {
                    public partial interface IWidgetQueryProvider : IQueryHandler
                    {
                        Task<int> A();
                    }
                    public partial interface IWidgetQueryProvider
                    {
                        Task<int> B();
                    }

                    public partial class CreateWidgetCommand : ICommand
                    {
                        public int A { get; set; }
                    }
                    public partial class CreateWidgetCommand
                    {
                        public int B { get; set; }
                    }

                    public partial interface IWidgetCommandHandler : ICommandHandler<CreateWidgetCommand> { }
                    public partial interface IWidgetCommandHandler { }
                }
                """);

            Assert.Single(output.Sources.Keys, x => x.StartsWith("Caller_"));
            Assert.Single(output.Lines(x => x.Contains("Register.Router(typeof(global::TestApp.IWidgetQueryProvider)")));
            Assert.Single(output.Lines(x => x.Contains("Register.Finder(typeof(global::TestApp.CreateWidgetCommand))")));
            Assert.Single(output.Lines(x => x.Contains("Register.CommandOrEventInfo(typeof(global::TestApp.IWidgetCommandHandler)")));
        }

        [Fact]
        public void GlobalNamespace()
        {
            var output = GeneratorRunner.Run("""
                using System.Threading.Tasks;
                using Zerra.CQRS;

                public class CreateWidgetCommand : ICommand
                {
                    public int ID { get; set; }
                }

                public interface IWidgetQueryProvider : IQueryHandler
                {
                    Task<int> GetCount();
                }
                """);

            Assert.Contains("Caller_IWidgetQueryProvider.cs", output.Sources.Keys);
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Finder(typeof(global::CreateWidgetCommand));");
        }

        [Fact]
        public void Tuples()
        {
            var output = GeneratorRunner.Run("""
                using System.Threading.Tasks;
                using Zerra.CQRS;
                using Zerra.Reflection;

                namespace TestApp
                {
                    [GenerateTypeDetail]
                    public class Model
                    {
                        public (int A, string B) Pair { get; set; }
                        public (int, int, int, int, int, int, int, int, int) Long { get; set; }
                        //a HashSet brings IEqualityComparer<(int A, string B)>, whose empty implementation is named from the tuple
                        public System.Collections.Generic.HashSet<(int A, string B)>? Pairs { get; set; }
                    }

                    public interface IWidgetQueryProvider : IQueryHandler
                    {
                        Task<(int Count, string Name)> GetPair((int, string) input);
                    }
                }
                """);

            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<(int, string),");
            output.AssertInitializerContains("new global::System.ValueTuple<int, string>(");
            Assert.Contains("Empty_IEqualityComparer__intA_stringB__.cs", output.Sources.Keys);
        }

        [Fact]
        public void KeywordIdentifiers()
        {
            var output = GeneratorRunner.Run("""
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using Zerra.CQRS;

                namespace TestApp
                {
                    public enum Keywords { @class, @int, Normal }

                    public class @event : ICommand
                    {
                        public int @object { get; set; }
                        public int @base;
                        public required int @params { get; set; }
                    }

                    public interface IHolder<T>
                    {
                        T @return { get; set; }
                        void @void(T @class);
                    }

                    public class Model : ICommand
                    {
                        public IHolder<string>? Holder { get; set; }
                    }

                    public interface IKeywordQueryProvider : IQueryHandler
                    {
                        Task<int> @string(int @class);
                        void @void();
                        Task<@class> @default<@class>(@class value) where @class : notnull;
                        int @params { get; }
                    }

                    public interface IKeywordCommandHandler : ICommandHandler<@event> { }
                }
                """);

            output.AssertInitializerContains("new global::EnumName.EnumFieldInfo(\"class\", \"class\", global::TestApp.Keywords.@class)");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Handler(typeof(global::TestApp.IKeywordQueryProvider), \"string\", true");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Finder(typeof(global::TestApp.@event));");
        }

        [Fact]
        public void AttributeArguments()
        {
            //attribute constructor arguments are written back out as code, so each kind of constant must be a valid literal,
            //and a culture with a decimal comma must not change how numbers are written
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            GeneratorOutput output;
            try
            {
                output = GeneratorRunner.Run("""
                using System;
                using Zerra.Reflection;

                namespace TestApp
                {
                    public enum Level : sbyte { Low = -1, High = 1 }

                    [AttributeUsage(AttributeTargets.All)]
                    public class InfoAttribute : Attribute
                    {
                        public InfoAttribute(string text, char c, float f, double d, long l, Level level, Type type, int[] values, object boxed) { }
                    }

                    [GenerateTypeDetail]
                    [Info("quote \" backslash \\ newline \n tab \t", '\'', 1.5f, 2.5, long.MaxValue, Level.Low, typeof(System.Collections.Generic.List<>), new[] { 1, 2 }, "boxed")]
                    public class Model
                    {
                        [Info(@"C:\path", '\\', float.NaN, double.PositiveInfinity, -1, (Level)5, typeof(int[]), new int[0], 5)]
                        public int Value { get; set; }

                        [Info(null!, 'a', 0, 0, 0, Level.High, null!, null!, null!)]
                        public int Nulls { get; set; }
                    }
                }
                """);
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = culture;
            }

            output.AssertInitializerContains("new global::TestApp.InfoAttribute(\"quote \\\" backslash \\\\ newline \\n tab \\t\", '\\'', 1.5F, 2.5D, (long)(9223372036854775807), (global::TestApp.Level)(-1), typeof(global::System.Collections.Generic.List<>), new int[] { (int)(1), (int)(2) }, \"boxed\")");
            output.AssertInitializerContains("new global::TestApp.InfoAttribute(\"C:\\\\path\", '\\\\', float.NaN, double.PositiveInfinity, (long)(-1), (global::TestApp.Level)(5), typeof(int[]), new int[] {  }, (int)(5))");
        }

        [Fact]
        public void EdgeCaseShapes()
        {
            //shapes that already generate correctly, kept so they stay that way
            _ = GeneratorRunner.Run("""
                using System;
                using System.Threading;
                using System.Threading.Tasks;
                using Zerra.CQRS;
                using Zerra.Reflection;

                namespace TestApp
                {
                    public class Outer
                    {
                        public class NestedCommand : ICommand { public int X { get; set; } }
                        public interface INestedQueryProvider : IQueryHandler { Task<int> A(NestedCommand command); }
                        public enum NestedEnum { A }
                    }

                    public interface IBaseQueryProvider : IQueryHandler { Task<int> Base(); }
                    public interface IDerivedQueryProvider : IBaseQueryProvider
                    {
                        Task<int> Defaults(int a = 5, string? b = null);
                        Task<int> Params(params int[] values);
                        Task<int> Token(int? a, CancellationToken token);
                    }

                    public class Base
                    {
                        public virtual int Virtual { get; set; }
                        public int Hidden { get; set; }
                    }

                    [GenerateTypeDetail]
                    public class Derived : Base
                    {
                        public override int Virtual { get; set; }
                        public new string? Hidden { get; set; }
                        public int this[int i] { get => i; set { } }
                        public int? NullableValue { get; set; }
                        public ReadOnlyStruct ReadOnly { get; set; }
                        public Action? Callback { get; set; }
                    }

                    public readonly struct ReadOnlyStruct { public int X { get; init; } }

                    [GenerateTypeDetail]
                    public record struct RecordStruct(int A, string B);

                    [GenerateTypeDetail]
                    internal class InternalModel { public int X { get; set; } }

                    [GenerateTypeDetail]
                    public class RequiredWithConstructor
                    {
                        public required int X { get; set; }
                        public RequiredWithConstructor(int y) { }
                    }
                }
                """);
        }

        [Fact]
        public void AggregateRoot()
        {
            var output = GeneratorRunner.Run("""
                using System;
                using System.Threading.Tasks;
                using Zerra.Repository;

                namespace TestApp
                {
                    public class WidgetCreatedAggregateEvent : IAggregateEvent
                    {
                        public string? Name { get; set; }
                    }

                    public sealed class WidgetAggregate : AggregateRoot
                    {
                        public WidgetAggregate(Guid id, IEventStoreEngine eventStore) : base(id, eventStore) { }

                        public string? Name { get; private set; }

                        public Task On(WidgetCreatedAggregateEvent @event)
                        {
                            Name = @event.Name;
                            return Task.CompletedTask;
                        }
                    }
                }
                """);

            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.WidgetAggregate,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.WidgetCreatedAggregateEvent,");
        }

        [Fact]
        public void MemberShapes()
        {
            var output = GeneratorRunner.Run("""
                using System;
                using Zerra.Reflection;
                using Zerra.Repository;

                namespace TestApp
                {
                    public interface IThing
                    {
                        void Do();
                    }

                    [GenerateTypeDetail]
                    public unsafe class Shapes
                    {
                        public int WriteOnly { set { } }
                        public static int StaticField;
                        public static string? StaticReferenceField;
                        public required int First { get; set; }
                        public required string Second { get; set; }
                        public Outer.Inner? Nested { get; set; }
                        public int* Pointer { get; set; }
                        public nint Native { get; set; }
                        public int* PointerField;
                        public delegate*<int, void> FunctionPointer;
                        public System.Span<int> Span => default;

                        public Shapes() { }
                        public Shapes(int* pointer) { }
                    }

                    //methods are generated for aggregates
                    public sealed unsafe class ShapeAggregate : AggregateRoot, IThing
                    {
                        public ShapeAggregate(Guid id, IEventStoreEngine eventStore) : base(id, eventStore) { }

                        public int* GetPointer() => null;
                        public void SetPointer(int* pointer) { }
                        public void Run(int a, string b) { }
                        public int Add(int a, int b) => a + b;
                        internal void Hidden() { }
                        public static int StaticMethod(int a) => a;
                        public T Echo<T>(T value) => value;
                        public static ShapeAggregate operator +(ShapeAggregate a, ShapeAggregate b) => a;
                        public void TryGet(out int value) { value = 1; }
                        void IThing.Do() { }
                    }

                    public class Outer
                    {
                        public class Inner
                        {
                            public int Value { get; set; }
                        }
                    }
                }
                """);

            output.AssertInitializerContains("\"Run\"");
            output.AssertInitializerContains("\"Add\"");
            output.AssertInitializerContains("\"StaticField\"");
            output.AssertInitializerContains("\"WriteOnly\"");
            output.AssertInitializerContains("global::TestApp.Outer.Inner");
            //pointers and ref structs can't be boxed, they are listed without accessors like reflection does
            output.AssertInitializerContains("new global::Zerra.Reflection.MemberDetail(typeof(global::TestApp.Shapes), typeof(nint), \"Pointer\", false, null, null, null, null, ");
            output.AssertInitializerContains("new global::Zerra.Reflection.MemberDetail(typeof(global::TestApp.Shapes), typeof(nint), \"PointerField\", true, null, null, null, null, ");
            output.AssertInitializerContains("\"Span\", false, null, null, null, null, ");
            //methods that can't be called through an object and object[] aren't generated
            output.AssertInitializerDoesNotContain("\"Hidden\"");
            output.AssertInitializerDoesNotContain("\"StaticMethod\"");
            output.AssertInitializerDoesNotContain("\"GetPointer\"");
            output.AssertInitializerDoesNotContain("\"SetPointer\"");
            output.AssertInitializerDoesNotContain("\"Echo\"");
            output.AssertInitializerDoesNotContain("\"TryGet\"");
            output.AssertInitializerDoesNotContain("\"op_Addition\"");
            output.AssertInitializerDoesNotContain("\"TestApp.IThing.Do\"");
        }

        [Fact]
        public void EmptyImplementationShapes()
        {
            var output = GeneratorRunner.Run("""
                using System.Threading.Tasks;
                using Zerra.Reflection;

                namespace TestApp
                {
                    public interface IBase
                    {
                        int BaseValue { get; }
                    }

                    public interface IStore<T> : IBase where T : class
                    {
                        T this[int index, string key] { get; set; }
                        U Convert<U, V>(T value, V other) where U : struct;
                        Task Save(T value);
                    }

                    public class Child
                    {
                        public int Value { get; set; }
                    }

                    [GenerateTypeDetail]
                    public class Model
                    {
                        public IStore<Child>? Store { get; set; }
                    }
                }
                """);

            var source = output.Sources["Empty_IStore_TestApp_Child_.cs"];
            Assert.Contains("this[", source);
            Assert.Contains("BaseValue", source);
            Assert.Contains("Convert<", source);
            Assert.Contains("System.Threading.Tasks.Task.CompletedTask", source);
        }

        [Fact]
        public void RouterAndCollectionShapes()
        {
            var output = GeneratorRunner.Run("""
                using System.Collections;
                using System.Collections.Generic;
                using System.Diagnostics.CodeAnalysis;
                using System.Threading.Tasks;
                using Zerra.CQRS;
                using Zerra.Reflection;

                namespace TestApp
                {
                    public interface IShapeQueryProvider : IQueryHandler
                    {
                        int Value { get; set; }
                        Task<T> Make<T>() where T : struct;
                    }

                    public class ReadOnlyMap : IReadOnlyDictionary<string, int>
                    {
                        public int this[string key] => 0;
                        public IEnumerable<string> Keys => [];
                        public IEnumerable<int> Values => [];
                        public int Count => 0;
                        public bool ContainsKey(string key) => false;
                        public bool TryGetValue(string key, [MaybeNullWhen(false)] out int value) { value = 0; return false; }
                        public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => new List<KeyValuePair<string, int>>().GetEnumerator();
                        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    }

                    public class Pair<TFirst, TSecond>
                    {
                        public TFirst? First { get; set; }
                        public TSecond? Second { get; set; }
                    }

                    public class Child
                    {
                        public int Value { get; set; }
                    }

                    [GenerateTypeDetail]
                    public class Model
                    {
                        public ReadOnlyMap? Map { get; set; }
                        public Pair<Child, List<Child>>? Pair { get; set; }
                    }
                }
                """);

            var caller = output.Sources["Caller_IShapeQueryProvider.cs"];
            Assert.Contains("set => throw new global::System.NotSupportedException();", caller);
            Assert.Contains("struct", caller);
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.ReadOnlyMap,global::System.Collections.Generic.KeyValuePair<string, int>,string,int>();");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.Pair<global::TestApp.Child, global::System.Collections.Generic.List<global::TestApp.Child>>,");
        }

        [Fact]
        public void AttributeSpecialNumbers()
        {
            var output = GeneratorRunner.Run("""
                using System;
                using Zerra.Reflection;

                namespace TestApp
                {
                    [AttributeUsage(AttributeTargets.All)]
                    public class LimitsAttribute : Attribute
                    {
                        public LimitsAttribute(float a, float b, double c, double d) { }
                    }

                    [GenerateTypeDetail]
                    public class Model
                    {
                        [Limits(float.PositiveInfinity, float.NegativeInfinity, double.NaN, double.NegativeInfinity)]
                        public int Value { get; set; }
                    }
                }
                """);

            output.AssertInitializerContains("new global::TestApp.LimitsAttribute(float.PositiveInfinity, float.NegativeInfinity, double.NaN, double.NegativeInfinity)");
        }

        //only public members are generated, so the types of non-public ones are never searched
        [Fact]
        public void NonPublicMemberTypesNotSearched()
        {
            var output = GeneratorRunner.Run("""
                using System.Collections.Concurrent;
                using System.Collections.Generic;
                                using Zerra.Reflection;

                namespace TestApp
                {
                    public class PublicField { public int Value { get; set; } }
                    public class PublicStaticField { public int Value { get; set; } }
                    public class PublicProperty { public int Value { get; set; } }
                    public class PublicStaticProperty { public int Value { get; set; } }
                    public class PrivateField { public int Value { get; set; } }
                    public class PrivateStaticField { public int Value { get; set; } }
                    public class ProtectedField { public int Value { get; set; } }
                    public class InternalField { public int Value { get; set; } }
                    public class PrivateProperty { public int Value { get; set; } }
                    public class ProtectedProperty { public int Value { get; set; } }
                    public class InternalProperty { public int Value { get; set; } }
                    public class PrivateGenericArgument { public int Value { get; set; } }
                    public class NestedPrivateField { public int Value { get; set; } }

                    public class Child
                    {
                        public int Value { get; set; }
                        private NestedPrivateField? nested;
                        public void Touch() => nested = null;
                    }

                    [GenerateTypeDetail]
                    public class Model
                    {
                        public PublicField? PublicField;
                        public static PublicStaticField? PublicStaticField;
                        public PublicProperty? PublicProperty { get; set; }
                        public static PublicStaticProperty? PublicStaticProperty { get; set; }
                        public Child? Child { get; set; }

                        private PrivateField? privateField;
                        private static PrivateStaticField? privateStaticField;
                        protected ProtectedField? protectedField;
                        internal InternalField? internalField;
                        private PrivateProperty? PrivateProperty { get; set; }
                        protected ProtectedProperty? ProtectedProperty { get; set; }
                        internal InternalProperty? InternalProperty { get; set; }
                        private readonly ConcurrentDictionary<string, List<PrivateGenericArgument>> cache = new();

                        public void Touch()
                        {
                            privateField = null;
                            privateStaticField = null;
                            _ = privateField;
                            _ = privateStaticField;
                            _ = PrivateProperty;
                            _ = cache;
                        }
                    }                }
                """);

            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.Model,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.PublicField,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.PublicStaticField,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.PublicProperty,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.PublicStaticProperty,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.Child,");

            output.AssertInitializerDoesNotContain("global::TestApp.PrivateField");
            output.AssertInitializerDoesNotContain("global::TestApp.PrivateStaticField");
            output.AssertInitializerDoesNotContain("global::TestApp.ProtectedField");
            output.AssertInitializerDoesNotContain("global::TestApp.InternalField");
            output.AssertInitializerDoesNotContain("global::TestApp.PrivateProperty");
            output.AssertInitializerDoesNotContain("global::TestApp.ProtectedProperty");
            output.AssertInitializerDoesNotContain("global::TestApp.InternalProperty");
            output.AssertInitializerDoesNotContain("global::TestApp.PrivateGenericArgument");
            output.AssertInitializerDoesNotContain("global::TestApp.NestedPrivateField");
            output.AssertInitializerDoesNotContain("ConcurrentDictionary");
        }

        //handler classes are added to the bus as instances, only their interfaces need generating
        [Fact]
        public void HandlerClassesNotSearched()
        {
            var output = GeneratorRunner.Run("""
                using System.Collections.Concurrent;
                using System.Threading;
                using System.Threading.Tasks;
                using Zerra.CQRS;

                namespace TestApp
                {
                    public class PingCommand : ICommand
                    {
                        public int Value { get; set; }
                    }

                    public class Widget
                    {
                        public int Value { get; set; }
                    }

                    public class HandlerPublicProperty { public int Value { get; set; } }
                    public class HandlerCache { public int Value { get; set; } }

                    public interface IPingCommandHandler : ICommandHandler<PingCommand> { }

                    public interface IWidgetQueryHandler : IQueryHandler
                    {
                        Task<Widget> GetWidget(int id);
                    }

                    public class PingHandler : BaseHandler, IPingCommandHandler
                    {
                        private readonly ConcurrentDictionary<int, HandlerCache> cache = new();
                        public HandlerPublicProperty? Settings { get; set; }
                        public Task Handle(PingCommand command, CancellationToken cancellationToken) { _ = cache; return Task.CompletedTask; }
                    }

                    public class WidgetHandler : BaseHandler, IWidgetQueryHandler
                    {
                        public HandlerPublicProperty? Settings { get; set; }
                        public Task<Widget> GetWidget(int id) => Task.FromResult(new Widget());
                    }

                    public class DirectHandler : BaseHandler, ICommandHandler<PingCommand>
                    {
                        public HandlerPublicProperty? Settings { get; set; }
                        public Task Handle(PingCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
                    }
                }
                """);

            //messages and the types reached through handler interfaces are still generated
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.PingCommand,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.Widget,");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Handler(typeof(global::TestApp.IPingCommandHandler),");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Handler(typeof(global::TestApp.IWidgetQueryHandler), \"GetWidget\"");

            //handlers are found through the bus, not by name
            output.AssertInitializerDoesNotContain("global::Zerra.Reflection.Register.Finder(typeof(global::TestApp.PingHandler));");
            output.AssertInitializerDoesNotContain("global::Zerra.Reflection.Register.Finder(typeof(global::TestApp.WidgetHandler));");
            output.AssertInitializerDoesNotContain("global::Zerra.Reflection.Register.Finder(typeof(global::TestApp.DirectHandler));");

            output.AssertInitializerDoesNotContain("global::Zerra.Reflection.Register.Finder(typeof(global::TestApp.IPingCommandHandler));");
            output.AssertInitializerDoesNotContain("global::Zerra.Reflection.Register.Finder(typeof(global::TestApp.IWidgetQueryHandler));");

            //IHandler sets up the handler, it is not callable and its types are not sent
            output.AssertInitializerDoesNotContain("\"Initialize\"");
            output.AssertInitializerDoesNotContain("global::Zerra.CQRS.BusContext");

            //and the classes get no type details, so their members are never searched
            output.AssertInitializerDoesNotContain("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.PingHandler,");
            output.AssertInitializerDoesNotContain("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.WidgetHandler,");
            output.AssertInitializerDoesNotContain("global::Zerra.Reflection.Register.SerializersAndMap<global::TestApp.DirectHandler,");
            output.AssertInitializerDoesNotContain("global::TestApp.HandlerPublicProperty");
            output.AssertInitializerDoesNotContain("global::TestApp.HandlerCache");
            output.AssertInitializerDoesNotContain("ConcurrentDictionary");
        }
    }
}
