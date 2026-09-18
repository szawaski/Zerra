// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Attributes;
using Zerra.Map;
using Zerra.Test.Map;

namespace Zerra.Benchmark.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 5)]
    public class MapBenchmarks
    {
        ModelA modelA;
        ModelB modelB;

        [GlobalSetup]
        public void Setup()
        {
            modelA = ModelA.GetModelA();
            modelB = modelA.Map<ModelA, ModelB>();
        }

        [Benchmark]
        public ModelB ModelA_to_ModelB()
        {
            return modelA.Map<ModelA, ModelB>();
        }

        [Benchmark]
        public ModelA ModelB_to_ModelA()
        {
            return modelB.Map<ModelB, ModelA>();
        }
    }
}
