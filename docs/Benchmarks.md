[← Back to Documentation](Index.md)

# Benchmarks

Results from `Benchmarks/Zerra.Benchmark`, measured with BenchmarkDotNet. Run them yourself with:

```bash
dotnet run --project Benchmarks/Zerra.Benchmark/Zerra.Benchmark.csproj -c Release
```

Environment: BenchmarkDotNet 0.15.8, .NET 10.0.12 (x64 RyuJIT), Windows 11, Intel Core i9-14900HX. Measured October 5, 2026 on a development laptop with containers running in the background, so absolute times vary; compare rows within a table.

## Serializers

One `NormalJsonModel` from `Tests/Zerra.Test` (41 properties: strings, decimals, bools, and dates, plus arrays of child models). The JSON serializers write JSON as a string or as UTF-8 bytes; `ZerraByteSerializer` writes its compact binary format.

**Time** (lower is better)

| | `ZerraJsonSerializer` | System.Text.Json | Newtonsoft.Json | `ZerraByteSerializer` |
|---|---:|---:|---:|---:|
| Serialize to string | **5.7 µs** | 6.6 µs | 12.5 µs | |
| Serialize to bytes | 5.4 µs | 5.3 µs | | **3.2 µs** |
| Deserialize from string | **9.7 µs** | 10.7 µs | 19.9 µs | |
| Deserialize from bytes | 9.9 µs | 10.4 µs | | **3.8 µs** |

**Memory allocated**

| | `ZerraJsonSerializer` | System.Text.Json | Newtonsoft.Json | `ZerraByteSerializer` |
|---|---:|---:|---:|---:|
| Serialize to string | 18.0 KB | 18.0 KB | 55.8 KB | |
| Serialize to bytes | 9.1 KB | 9.2 KB | | **5.5 KB** |
| Deserialize from string | **13.5 KB** | 13.6 KB | 21.1 KB | |
| Deserialize from bytes | 13.5 KB | 13.6 KB | | **12.9 KB** |

- `ZerraByteSerializer`, the serializer services use between themselves, serializes in about 60% of System.Text.Json's time and deserializes in under 40%, with less memory.
- `ZerraJsonSerializer`, for browsers and outside callers, is as fast as System.Text.Json: ahead in three of the four comparisons and within 3% in the fourth, differences about the size of the variation between runs. It's about twice as fast as Newtonsoft.Json. It adds [Graph](Graph.md) member selection, nameless JSON, and PATCH tracking.

## Mapper

`Map<ModelA, ModelB>` and back between two models of 37 members: arrays, lists, and other collection types, dictionaries, and nested models.

| Method | Mean | Allocated |
|---|---:|---:|
| `ModelA` to `ModelB` | 5.7 µs | 9.1 KB |
| `ModelB` to `ModelA` | 6.0 µs | 8.3 KB |

## Repository

Zerra.Repository's comparison with Entity Framework on SQL Server is in [Repository](Repository.md#compared-with-entity-framework), from `Benchmarks/Zerra.Repository.Benchmark`.

## See Also

- [Testing](Testing.md)
- [Serializers](Serializers.md)
