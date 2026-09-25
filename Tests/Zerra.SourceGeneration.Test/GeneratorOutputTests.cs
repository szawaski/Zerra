// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

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
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Router(typeof(global::TestApp.IWidgetQueryProvider), static (global::Zerra.CQRS.IBusInternal bus, string source) => new global::TestApp.SourceGeneration.Caller_IWidgetQueryProvider(bus, source));");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Handler(typeof(global::TestApp.IWidgetQueryProvider), \"GetWidget\", true, typeof(global::TestApp.Widget)");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Handler(typeof(global::TestApp.IWidgetQueryProvider), \"Ping\", true, null");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Handler(typeof(global::TestApp.IWidgetQueryProvider), \"Count\", false, null");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Handler(typeof(global::TestApp.IWidgetQueryProvider), \"Touch\", false, null");
            //generic methods can only go through the caller, there is no closed method to register
            output.AssertInitializerDoesNotContain("\"Generic\"");
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Finder(typeof(global::TestApp.IWidgetQueryProvider));");
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
            output.AssertInitializerContains("global::Zerra.Reflection.Register.Finder(typeof(global::TestApp.IWidgetCommandHandler));");
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
    }
}
