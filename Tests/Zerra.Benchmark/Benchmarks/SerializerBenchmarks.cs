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
        private byte[] normalJsonModelJsonBytes;
        private byte[] normalJsonModelBytes;

        [GlobalSetup]
        public void Setup()
        {
            normalJsonModel = NormalJsonModel.Create();
            normalJsonModelJson = JsonSerializer.Serialize(normalJsonModel);
            normalJsonModelJsonBytes = JsonSerializer.SerializeBytes(normalJsonModel);
            normalJsonModelBytes = ByteSerializer.Serialize(normalJsonModel);
        }

        //[Benchmark]
        public string Serialize_Json_Zerra()
        {
            return JsonSerializer.Serialize(normalJsonModel);
        }

        //[Benchmark]
        public byte[] Serialize_Json_Zerra_ByteArray()
        {
            return JsonSerializer.SerializeBytes(normalJsonModel);
        }

        //[Benchmark]
        public string Serialize_Json_SystemTextJson()
        {
            return System.Text.Json.JsonSerializer.Serialize(normalJsonModel);
        }

        //[Benchmark]
        public byte[] Serialize_Json_SystemTextJson_ByteArray()
        {
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(normalJsonModel);
        }

        //[Benchmark]
        public string Serialize_Json_Newtonsoft()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(normalJsonModel);
        }

        //[Benchmark]
        public byte[] Serialize_Bytes_Zerra()
        {
            return ByteSerializer.Serialize(normalJsonModel);
        }

        [Benchmark]
        public NormalJsonModel Deserialize_Json_Zerra()
        {
            return JsonSerializer.Deserialize<NormalJsonModel>(normalJsonModelJson);
        }

        [Benchmark]
        public NormalJsonModel Deserialize_Json_Zerra_ByteArray()
        {
            return JsonSerializer.Deserialize<NormalJsonModel>(normalJsonModelJsonBytes);
        }

        [Benchmark]
        public NormalJsonModel Deserialize_Json_SystemTextJson()
        {
            return System.Text.Json.JsonSerializer.Deserialize<NormalJsonModel>(normalJsonModelJson);
        }

        [Benchmark]
        public NormalJsonModel Deserialize_Json_SystemTextJson_ByteArray()
        {
            return System.Text.Json.JsonSerializer.Deserialize<NormalJsonModel>(normalJsonModelJsonBytes);
        }

        [Benchmark]
        public NormalJsonModel Deserialize_Json_Newtonsoft()
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<NormalJsonModel>(normalJsonModelJson);
        }

        //[Benchmark]
        public NormalJsonModel Deserialize_Bytes_Zerra()
        {
            return ByteSerializer.Deserialize<NormalJsonModel>(normalJsonModelBytes);
        }
    }
}
