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
        private static void AssertRuns(string source)
        {
            var output = GeneratorRunner.Run(source, OutputKind.ConsoleApplication);
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
