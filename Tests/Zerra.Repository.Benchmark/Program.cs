// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using BenchmarkDotNet.Running;
using Zerra.Repository.Benchmark.Benchmarks;

_ = BenchmarkRunner.Run<EFBenchmarks>();

//dotnet run --project Tests\Zerra.Repository.Benchmark\Zerra.Repository.Benchmark.csproj -c Release