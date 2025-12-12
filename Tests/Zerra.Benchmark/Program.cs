// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Running;
using Zerra.Benchmark.Benchmarks;

_ = BenchmarkRunner.Run<MapBenchmarks>();
_ = BenchmarkRunner.Run<SerializerBenchmarks>();

//dotnet run -p Tests\Zerra.Benchmark\Zerra.Benchmark.csproj -c Release