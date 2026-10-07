// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Running;
using Zerra.Benchmark.Benchmarks;

_ = BenchmarkRunner.Run<MapBenchmarks>(args: args);
_ = BenchmarkRunner.Run<SerializerBenchmarks>(args: args);
_ = BenchmarkRunner.Run<ModelSerializerBenchmarks>(args: args);
//CompressionBenchmarkData.PrintSizes();
//_ = BenchmarkRunner.Run<CompressorBenchmarks>();
//_ = BenchmarkRunner.Run<CompressionTcpBenchmarks>();

//dotnet run --project Tests\Zerra.Benchmark\Zerra.Benchmark.csproj -c Release