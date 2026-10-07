// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Attributes;
using Zerra.Serialization.Json;
using Zerra.Test.Helpers.Models;
using Zerra.Test.Helpers.TypesModels;

namespace Zerra.Benchmark.Benchmarks
{
    [MemoryDiagnoser]
    public class ModelSerializerBenchmarks
    {
        public class Order
        {
            public Guid Id { get; set; }
            public string Customer { get; set; }
            public DateTime Placed { get; set; }
            public decimal Total { get; set; }
            public int Quantity { get; set; }
            public bool Shipped { get; set; }
            public string Notes { get; set; }
        }

        //enums as names, the same as ZerraJsonSerializer
        private static readonly System.Text.Json.JsonSerializerOptions systemTextJsonOptions = new() { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };

        [Params("Small", "TypesBasic", "TypesList", "Orders100", "SimpleArray1000", "Dictionary100")]
        public string Model;

        private object model;
        private Type type;
        private string json;
        private byte[] jsonBytes;
        private string systemTextJson;
        private byte[] systemTextJsonBytes;

        [GlobalSetup]
        public void Setup()
        {
            (model, type) = Model switch
            {
                "Small" => ((object)new SimpleModel() { Value1 = 42, Value2 = "Hello" }, typeof(SimpleModel)),
                "TypesBasic" => (TypesBasicModel.Create(), typeof(TypesBasicModel)),
                "TypesList" => (TypesListTModel.Create(), typeof(TypesListTModel)),
                "Orders100" => (Enumerable.Range(0, 100).Select(i => new Order() { Id = Guid.NewGuid(), Customer = "Customer Name " + i, Placed = DateTime.UtcNow.AddMinutes(-i), Total = 19.99m * i, Quantity = i, Shipped = i % 2 == 0, Notes = "Leave at the front door, ring the bell twice " + i }).ToList(), typeof(List<Order>)),
                "SimpleArray1000" => (Enumerable.Range(0, 1000).Select(i => new SimpleModel() { Value1 = i, Value2 = "Value " + i }).ToArray(), typeof(SimpleModel[])),
                "Dictionary100" => (Enumerable.Range(0, 100).ToDictionary(i => "Key" + i, i => "Value number " + i), typeof(Dictionary<string, string>)),
                _ => throw new NotSupportedException(),
            };
            json = JsonSerializer.Serialize(model, type);
            jsonBytes = JsonSerializer.SerializeBytes(model, type);
            systemTextJson = System.Text.Json.JsonSerializer.Serialize(model, type, systemTextJsonOptions);
            systemTextJsonBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(model, type, systemTextJsonOptions);
        }

        [Benchmark]
        public string Serialize_Json_Zerra() => JsonSerializer.Serialize(model, type);

        [Benchmark]
        public string Serialize_Json_SystemTextJson() => System.Text.Json.JsonSerializer.Serialize(model, type, systemTextJsonOptions);

        [Benchmark]
        public byte[] Serialize_Json_Zerra_ByteArray() => JsonSerializer.SerializeBytes(model, type);

        [Benchmark]
        public byte[] Serialize_Json_SystemTextJson_ByteArray() => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(model, type, systemTextJsonOptions);

        [Benchmark]
        public object Deserialize_Json_Zerra() => JsonSerializer.Deserialize(json, type);

        [Benchmark]
        public object Deserialize_Json_SystemTextJson() => System.Text.Json.JsonSerializer.Deserialize(systemTextJson, type, systemTextJsonOptions);

        [Benchmark]
        public object Deserialize_Json_Zerra_ByteArray() => JsonSerializer.Deserialize(jsonBytes, type);

        [Benchmark]
        public object Deserialize_Json_SystemTextJson_ByteArray() => System.Text.Json.JsonSerializer.Deserialize(systemTextJsonBytes, type, systemTextJsonOptions);
    }
}
