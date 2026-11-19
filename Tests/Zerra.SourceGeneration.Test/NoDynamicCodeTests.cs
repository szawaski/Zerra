// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using System;
using Xunit;

namespace Zerra.SourceGeneration.Test
{
    //each app runs in its own process with dynamic code turned off, so everything it uses must come from the generated code as under native AOT
    public class NoDynamicCodeTests
    {
        private static void AssertRuns(string source, string? librarySource = null)
        {
            var output = GeneratorRunner.Run(source, OutputKind.ConsoleApplication, librarySource);
            var (exitCode, text) = output.RunWithoutDynamicCode();
            Assert.True(exitCode == 0 && text.Contains("IsDynamicCodeSupported False") && text.Contains("Passed"), $"Exit code {exitCode}{Environment.NewLine}{text}");
        }

        //Type, Task, and CancellationToken never get type detail, so collections of them must not be registered at startup
        [Fact]
        public void SpecialTypes()
        {
            AssertRuns("""
                using System;
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;
                using Zerra.Reflection;
                using Zerra.Serialization.Bytes;
                using Zerra.Serialization.Json;

                namespace NoDynamicCodeApp
                {
                    //an exception reaches Type[] through TargetSite
                    [GenerateTypeDetail]
                    public class AppException : Exception
                    {
                        public AppException(string message) : base(message) { }
                        public int Code { get; set; }
                    }

                    [GenerateTypeDetail]
                    public class Holder
                    {
                        public Type[]? Types { get; set; }
                        public List<Type>? TypeList { get; set; }
                        public Dictionary<string, Type>? TypeValues { get; set; }
                        public Task[]? Tasks { get; set; }
                        public CancellationToken[]? Tokens { get; set; }
                    }

                    [GenerateTypeDetail]
                    public class Plain
                    {
                        public string? Name { get; set; }
                    }

                    //the app starts with the models above registered; serializing or mapping a model with one of these members still needs their type detail
                    public static class Program
                    {
                        public static int Main()
                        {
                            Console.WriteLine($"IsDynamicCodeSupported {System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported}");
                            try
                            {
                                var model = new Plain() { Name = "Holder1" };
                                var json = JsonSerializer.Deserialize<Plain>(JsonSerializer.Serialize(model))!;
                                if (json.Name != "Holder1")
                                    throw new Exception($"Json Name: {json.Name}");
                                var bytes = ByteSerializer.Deserialize<Plain>(ByteSerializer.Serialize(model))!;
                                if (bytes.Name != "Holder1")
                                    throw new Exception($"Bytes Name: {bytes.Name}");

                                Console.WriteLine("Passed");
                                return 0;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                return 1;
                            }
                        }
                    }
                }
                """);
        }

        //init setters are called through UnsafeAccessor, including one declared on a base type and one on a struct,
        //internal setters of the assembly's own types are serialized and private ones are not, the same as without the source generator
        [Fact]
        public void InitAndInternalSetters()
        {
            AssertRuns("""
                using System;
                using Zerra.Reflection;
                using Zerra.Serialization.Bytes;
                using Zerra.Serialization.Json;

                namespace NoDynamicCodeApp
                {
                    [GenerateTypeDetail]
                    public record RecordModel(bool Property1)
                    {
                        public int Property2 { get; init; }
                        public string? Property3 { get; init; }
                    }

                    [GenerateTypeDetail]
                    public class RequiredModel
                    {
                        public required int Value1 { get; set; }
                        public required int Value2 { get; init; }
                    }

                    public class BaseModel
                    {
                        public int BaseValue { get; init; }
                    }

                    [GenerateTypeDetail]
                    public class DerivedModel : BaseModel
                    {
                        public int DerivedValue { get; init; }
                    }

                    [GenerateTypeDetail]
                    public struct StructModel
                    {
                        public int Value { get; init; }
                    }

                    [GenerateTypeDetail]
                    public class SetterModel
                    {
                        public string? Public { get; set; }
                        public string? Internal { get; internal set; }
                        public string? ProtectedInternal { get; protected internal set; }
                        public string? Protected { get; protected set; }
                        public string? PrivateProtected { get; private protected set; }
                        public string? Private { get; private set; }
                        public void SetAll(string value) { Internal = value; ProtectedInternal = value; Protected = value; PrivateProtected = value; Private = value; }
                    }

                    public static class Program
                    {
                        private static void Check(string name, object? expected, object? actual)
                        {
                            if (!Equals(expected, actual))
                                throw new Exception($"{name}: expected {expected} actual {actual}");
                        }

                        public static int Main()
                        {
                            Console.WriteLine($"IsDynamicCodeSupported {System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported}");
                            try
                            {
                                var record = new RecordModel(true) { Property2 = 42, Property3 = "moo" };
                                Check("Record Json", "{\"Property1\":true,\"Property2\":42,\"Property3\":\"moo\"}", JsonSerializer.Serialize(record));
                                Check("Record Json Read", record, JsonSerializer.Deserialize<RecordModel>(JsonSerializer.Serialize(record)));
                                Check("Record Bytes", record, ByteSerializer.Deserialize<RecordModel>(ByteSerializer.Serialize(record)));

                                var required = JsonSerializer.Deserialize<RequiredModel>(JsonSerializer.Serialize(new RequiredModel() { Value1 = 1, Value2 = 2 }))!;
                                Check("Required Value2", 2, required.Value2);

                                var derived = ByteSerializer.Deserialize<DerivedModel>(ByteSerializer.Serialize(new DerivedModel() { BaseValue = 3, DerivedValue = 4 }))!;
                                Check("Derived BaseValue", 3, derived.BaseValue);
                                Check("Derived DerivedValue", 4, derived.DerivedValue);

                                Check("Struct Json", 5, JsonSerializer.Deserialize<StructModel>(JsonSerializer.Serialize(new StructModel() { Value = 5 })).Value);
                                Check("Struct Bytes", 5, ByteSerializer.Deserialize<StructModel>(ByteSerializer.Serialize(new StructModel() { Value = 5 })).Value);

                                var setters = new SetterModel() { Public = "a" };
                                setters.SetAll("b");
                                Check("Setters Json", "{\"Public\":\"a\",\"Internal\":\"b\",\"ProtectedInternal\":\"b\"}", JsonSerializer.Serialize(setters));
                                var settersRead = JsonSerializer.Deserialize<SetterModel>("{\"Public\":\"a\",\"Internal\":\"b\",\"ProtectedInternal\":\"b\",\"Protected\":\"c\",\"PrivateProtected\":\"c\",\"Private\":\"c\"}")!;
                                Check("Setters Internal", "b", settersRead.Internal);
                                Check("Setters ProtectedInternal", "b", settersRead.ProtectedInternal);
                                Check("Setters Protected", null, settersRead.Protected);
                                Check("Setters PrivateProtected", null, settersRead.PrivateProtected);
                                Check("Setters Private", null, settersRead.Private);

                                Console.WriteLine("Passed");
                                return 0;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                return 1;
                            }
                        }
                    }
                }
                """);
        }

        //another assembly's internal accessors are called through UnsafeAccessor without InternalsVisibleTo, the same members as reflection
        [Fact]
        public void OtherAssemblyInternalAccessors()
        {
            AssertRuns("""
                using System;
                using Lib;
                using Zerra.Serialization.Bytes;
                using Zerra.Serialization.Json;

                namespace NoDynamicCodeApp
                {
                    public static class Program
                    {
                        private static void Check(string name, object? expected, object? actual)
                        {
                            if (!Equals(expected, actual))
                                throw new Exception($"{name}: expected {expected} actual {actual}");
                        }

                        public static int Main()
                        {
                            Console.WriteLine($"IsDynamicCodeSupported {System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported}");
                            try
                            {
                                var model = LibModel.Create("b");
                                Check("Json", "{\"Public\":\"a\",\"Internal\":\"b\",\"ProtectedInternal\":\"b\",\"InternalGet\":\"g\",\"Init\":\"i\"}", JsonSerializer.Serialize(model));

                                var json = JsonSerializer.Deserialize<LibModel>("{\"Public\":\"a\",\"Internal\":\"b\",\"ProtectedInternal\":\"c\",\"Private\":\"d\",\"InternalGet\":\"g\",\"Init\":\"i\"}")!;
                                Check("Json Internal", "b", json.Internal);
                                Check("Json ProtectedInternal", "c", json.ProtectedInternal);
                                Check("Json Private", null, json.Private);
                                Check("Json InternalGet", "g", json.ReadInternalGet());
                                Check("Json Init", "i", json.Init);

                                var bytes = ByteSerializer.Deserialize<LibModel>(ByteSerializer.Serialize(model))!;
                                Check("Bytes Internal", "b", bytes.Internal);
                                Check("Bytes InternalGet", "g", bytes.ReadInternalGet());

                                Check("Struct", 5, JsonSerializer.Deserialize<LibStruct>(JsonSerializer.Serialize(LibStruct.Create(5))).Internal);

                                Console.WriteLine("Passed");
                                return 0;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                return 1;
                            }
                        }
                    }
                }
                """, """
                namespace Lib
                {
                    public class LibModel
                    {
                        public string? Public { get; set; }
                        public string? Internal { get; internal set; }
                        public string? ProtectedInternal { get; protected internal set; }
                        public string? Private { get; private set; }
                        public string? InternalGet { internal get; set; }
                        public string? Init { get; init; }

                        public string? ReadInternalGet() => InternalGet;
                        public static LibModel Create(string value) => new() { Public = "a", Internal = value, ProtectedInternal = value, Private = value, InternalGet = "g", Init = "i" };
                    }

                    public struct LibStruct
                    {
                        public int Internal { get; internal set; }
                        public static LibStruct Create(int value) => new() { Internal = value };
                    }
                }
                """);
        }

        //generic types can't use an UnsafeAccessor, their init and other assembly internal accessors are delegates made on first use,
        //a static internal setter and one inherited from another assembly's base class go through an UnsafeAccessor
        [Fact]
        public void FallbackAndStaticAccessors()
        {
            AssertRuns("""
                using System;
                using Lib;
                using Zerra.Reflection;
                using Zerra.Serialization.Bytes;
                using Zerra.Serialization.Json;

                namespace NoDynamicCodeApp
                {
                    public class Wrapper<T>
                    {
                        public T? Value { get; init; }
                    }

                    public struct StructWrapper<T>
                    {
                        public T Value { get; init; }
                    }

                    public class AppDerived : LibBase
                    {
                        public int Own { get; set; }
                    }

                    public static class Program
                    {
                        private static void Check(string name, object? expected, object? actual)
                        {
                            if (!Equals(expected, actual))
                                throw new Exception($"{name}: expected {expected} actual {actual}");
                        }

                        public static int Main()
                        {
                            Console.WriteLine($"IsDynamicCodeSupported {System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported}");
                            try
                            {
                                Check("Wrapper Json", "{\"Value\":2.5}", JsonSerializer.Serialize(new Wrapper<double>() { Value = 2.5 }));
                                Check("Wrapper Read", 2.5, JsonSerializer.Deserialize<Wrapper<double>>("{\"Value\":2.5}")!.Value);
                                Check("StructWrapper", 3, ByteSerializer.Deserialize<StructWrapper<int>>(ByteSerializer.Serialize(new StructWrapper<int>() { Value = 3 })).Value);

                                Check("LibGeneric", "a", JsonSerializer.Deserialize<LibGeneric<string>>(JsonSerializer.Serialize(LibGeneric<string>.Create("a")))!.Internal);
                                Check("LibGenericStruct", 4, JsonSerializer.Deserialize<LibGenericStruct<int>>(JsonSerializer.Serialize(LibGenericStruct<int>.Create(4))).Internal);

                                var derived = new AppDerived() { Own = 1 };
                                derived.SetBase(5);
                                var derivedRead = JsonSerializer.Deserialize<AppDerived>(JsonSerializer.Serialize(derived))!;
                                Check("Derived Own", 1, derivedRead.Own);
                                Check("Derived BaseInternal", 5, derivedRead.BaseInternal);

                                var count = TypeAnalyzer.GetTypeDetail(typeof(LibStatic)).GetMember(nameof(LibStatic.Count));
                                count.SetterBoxed!(new object(), 7);
                                Check("Static Set", 7, LibStatic.Count);
                                Check("Static Get", 7, count.GetterBoxed!(new object()));
                                _ = JsonSerializer.Serialize(new LibStatic());

                                Console.WriteLine("Passed");
                                return 0;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                return 1;
                            }
                        }
                    }
                }
                """, """
                namespace Lib
                {
                    public class LibGeneric<T>
                    {
                        public T? Internal { get; internal set; }
                        public static LibGeneric<T> Create(T value) => new() { Internal = value };
                    }

                    public struct LibGenericStruct<T>
                    {
                        public T Internal { get; internal set; }
                        public static LibGenericStruct<T> Create(T value) => new() { Internal = value };
                    }

                    public class LibBase
                    {
                        public int BaseInternal { get; internal set; }
                        public void SetBase(int value) => BaseInternal = value;
                    }

                    public class LibStatic
                    {
                        public static int Count { get; internal set; }
                        public int Value { get; set; }
                    }
                }
                """);
        }

        //types only used with the mapper, found from the Map, MapTo, and Copy calls
        [Fact]
        public void MappedTypes()
        {
            AssertRuns("""
                using System;
                using Zerra.Map;

                namespace NoDynamicCodeApp
                {
                    public class Source
                    {
                        public int A { get; set; }
                        public string? B { get; set; }
                    }

                    public class Target
                    {
                        public int A { get; set; }
                        public string? B { get; set; }
                    }

                    public class Into
                    {
                        public int A { get; set; }
                    }

                    public class Copied
                    {
                        public int C { get; set; }
                    }

                    public static class Program
                    {
                        public static int Main()
                        {
                            Console.WriteLine($"IsDynamicCodeSupported {System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported}");
                            try
                            {
                                var target = new Source() { A = 1, B = "x" }.Map<Target>();
                                if (target.A != 1 || target.B != "x")
                                    throw new Exception("Map<TTarget>");
                                var target2 = new Source() { A = 2 }.Map<Source, Target>();
                                if (target2.A != 2)
                                    throw new Exception("Map<TSource, TTarget>");
                                var into = new Into();
                                new Source() { A = 3 }.MapTo(into);
                                if (into.A != 3)
                                    throw new Exception("MapTo");
                                var copied = new Copied() { C = 4 }.Copy();
                                if (copied.C != 4)
                                    throw new Exception("Copy");

                                Console.WriteLine("Passed");
                                return 0;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                return 1;
                            }
                        }
                    }
                }
                """);
        }

        //types only used with a serializer, found from the Serialize and Deserialize calls
        [Fact]
        public void SerializedTypes()
        {
            AssertRuns("""
                using System;
                using System.Collections.Generic;
                using Zerra.CQRS.Network;
                using Zerra.Serialization.Bytes;
                using Zerra.Serialization.Json;

                namespace NoDynamicCodeApp
                {
                    public class Inner { public int Value { get; set; } }
                    public class A { public int Value { get; set; } public Inner? Inner { get; set; } }
                    public class C { public int Value { get; set; } }
                    public class D { public int Value { get; set; } }
                    public class E { public int Value { get; set; } }
                    public class F { public int Value { get; set; } public string? Name { get; set; } }
                    public class G { public int Value { get; set; } }
                    public class H { public int Value { get; set; } }
                    public class I { public int Value { get; set; } }

                    public static class Program
                    {
                        public static int Main()
                        {
                            Console.WriteLine($"IsDynamicCodeSupported {System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported}");
                            try
                            {
                                var a = ByteSerializer.Deserialize<A>(ByteSerializer.Serialize(new A() { Value = 1, Inner = new Inner() { Value = 2 } }))!;
                                if (a.Value != 1 || a.Inner?.Value != 2)
                                    throw new Exception("A");

                                var c = (C)ByteSerializer.Deserialize(ByteSerializer.Serialize(new C() { Value = 3 }, typeof(C)), typeof(C))!;
                                if (c.Value != 3)
                                    throw new Exception("C");

                                //D is only reachable through typeof, read from a C with the same members
                                var dBytes = ByteSerializer.Serialize(new C() { Value = 4 }, typeof(C));
                                var d = (D?)ByteSerializer.Deserialize(dBytes, typeof(D));
                                if (d?.Value != 4)
                                    throw new Exception("D");

                                var e = JsonSerializer.Deserialize<E>(JsonSerializer.SerializeToUtf8Bytes(new E() { Value = 5 }))!;
                                if (e.Value != 5)
                                    throw new Exception("E");

                                var (f, graph) = JsonSerializer.DeserializePatch<F>("{\"Value\":6}");
                                if (f?.Value != 6)
                                    throw new Exception("F");

                                INetworkSerializer network = new NetworkByteSerializer();
                                var g = network.Deserialize<G>(network.SerializeBytes(new G() { Value = 7 }))!;
                                if (g.Value != 7)
                                    throw new Exception("G");

                                var json = new NetworkJsonSerializer();
                                var h = json.Deserialize<H>(json.SerializeBytes(new H() { Value = 8 }))!;
                                if (h.Value != 8)
                                    throw new Exception("H");

                                var list = JsonSerializer.Deserialize<List<I>>(JsonSerializer.Serialize(new List<I>() { new I() { Value = 9 } }))!;
                                if (list[0].Value != 9)
                                    throw new Exception("I");

                                Console.WriteLine("Passed");
                                return 0;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                return 1;
                            }
                        }
                    }
                }
                """);
        }

        [Fact]
        public void Collections()
        {
            AssertRuns("""
                using System;
                using System.Collections.Concurrent;
                using System.Collections.Generic;
                using System.Linq;
                using Zerra.Map;
                using Zerra.Reflection;
                using Zerra.Serialization.Bytes;
                using Zerra.Serialization.Json;

                namespace NoDynamicCodeApp
                {
                    public class Item
                    {
                        public int A { get; set; }
                    }

                    [GenerateTypeDetail]
                    public class Holder
                    {
                        public Dictionary<string, int>? Values { get; set; }
                        public Dictionary<int, Item>? Items { get; set; }
                        public Dictionary<Item, string>? ItemKeys { get; set; }
                        public IDictionary<string, string>? InterfaceValues { get; set; }
                        public ConcurrentDictionary<string, int>? ConcurrentValues { get; set; }
                        public Dictionary<string, Dictionary<string, int>>? Nested { get; set; }
                        public KeyValuePair<string, int>[]? Pairs { get; set; }
                        public List<Item>? List { get; set; }
                        public Item[]? Array { get; set; }
                    }

                    //the mapper has no converter that can fill an IReadOnlyDictionary, with or without dynamic code, so this one is only serialized
                    [GenerateTypeDetail]
                    public class ReadOnlyHolder
                    {
                        public IReadOnlyDictionary<string, long>? ReadOnlyValues { get; set; }
                    }

                    public static class Program
                    {
                        public static int Main()
                        {
                            Console.WriteLine($"IsDynamicCodeSupported {System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported}");
                            try
                            {
                                var concurrent = new ConcurrentDictionary<string, int>();
                                concurrent["c"] = 3;
                                var model = new Holder()
                                {
                                    Values = new Dictionary<string, int>() { { "a", 1 } },
                                    Items = new Dictionary<int, Item>() { { 2, new Item() { A = 2 } } },
                                    ItemKeys = new Dictionary<Item, string>() { { new Item() { A = 9 }, "nine" } },
                                    InterfaceValues = new Dictionary<string, string>() { { "b", "bee" } },
                                    ConcurrentValues = concurrent,
                                    Nested = new Dictionary<string, Dictionary<string, int>>() { { "outer", new Dictionary<string, int>() { { "inner", 4 } } } },
                                    Pairs = [new KeyValuePair<string, int>("p", 6)],
                                    List = [new Item() { A = 7 }],
                                    Array = [new Item() { A = 8 }]
                                };

                                Check("Json", JsonSerializer.Deserialize<Holder>(JsonSerializer.Serialize(model))!);
                                Check("Bytes", ByteSerializer.Deserialize<Holder>(ByteSerializer.Serialize(model))!);
                                Check("Map", model.Map<Holder, Holder>());

                                var readOnlyModel = new ReadOnlyHolder() { ReadOnlyValues = new Dictionary<string, long>() { { "l", 5L } } };
                                Equal("Json", "ReadOnlyValues", 5L, JsonSerializer.Deserialize<ReadOnlyHolder>(JsonSerializer.Serialize(readOnlyModel))!.ReadOnlyValues!["l"]);
                                Equal("Bytes", "ReadOnlyValues", 5L, ByteSerializer.Deserialize<ReadOnlyHolder>(ByteSerializer.Serialize(readOnlyModel))!.ReadOnlyValues!["l"]);

                                Console.WriteLine("Passed");
                                return 0;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                return 1;
                            }
                        }

                        private static void Check(string name, Holder result)
                        {
                            Equal(name, "Values", 1, result.Values!["a"]);
                            Equal(name, "Items", 2, result.Items![2].A);
                            Equal(name, "ItemKeys", 9, result.ItemKeys!.Single().Key.A);
                            Equal(name, "ItemKeys", "nine", result.ItemKeys!.Single().Value);
                            Equal(name, "InterfaceValues", "bee", result.InterfaceValues!["b"]);
                            Equal(name, "ConcurrentValues", 3, result.ConcurrentValues!["c"]);
                            Equal(name, "Nested", 4, result.Nested!["outer"]["inner"]);
                            Equal(name, "Pairs", "p", result.Pairs![0].Key);
                            Equal(name, "Pairs", 6, result.Pairs![0].Value);
                            Equal(name, "List", 7, result.List![0].A);
                            Equal(name, "Array", 8, result.Array![0].A);
                        }

                        private static void Equal<T>(string name, string member, T expected, T actual)
                        {
                            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                                throw new Exception($"{name} {member}: expected {expected}, got {actual}");
                        }
                    }
                }
                """);
        }
    }
}
