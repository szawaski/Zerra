// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Attributes;
using Zerra.Serialization.Bytes;
using Zerra.Serialization.Json;
using Zerra.Test.Helpers.Models;

namespace Zerra.Benchmark.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 5)]
    public class SerializerBenchmarks
    {
        private NormalJsonModel normalJsonModel;
        private string normalJsonModelJson;
        private byte[] normalJsonModelBytes;

        [GlobalSetup]
        public void Setup()
        {
            normalJsonModel = NormalJsonModel.Create();
            normalJsonModelJson = JsonSerializer.Serialize(normalJsonModel);
            normalJsonModelBytes = ByteSerializer.Serialize(normalJsonModel);
        }

        [Benchmark]
        public string Serialize_Json()
        {
            return JsonSerializer.Serialize(normalJsonModel);
        }

        [Benchmark]
        public string Serialize_SystemTextJson()
        {
            return System.Text.Json.JsonSerializer.Serialize(normalJsonModel);
        }

        [Benchmark]
        public byte[] Serialize_Bytes()
        {
            return ByteSerializer.Serialize(normalJsonModel);
        }

        [Benchmark]
        public NormalJsonModel Deserialize_Json()
        {
            return JsonSerializer.Deserialize<NormalJsonModel>(normalJsonModelJson);
        }

        [Benchmark]
        public NormalJsonModel Deserialize_SystemTextJson()
        {
            return System.Text.Json.JsonSerializer.Deserialize<NormalJsonModel>(normalJsonModelJson);
        }

        [Benchmark]
        public NormalJsonModel Deserialize_Bytes()
        {
            return ByteSerializer.Deserialize<NormalJsonModel>(normalJsonModelBytes);
        }

    }
}
