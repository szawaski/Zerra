[← Back to Documentation](Index.md)

# Benchmarks

Results from `Benchmarks/Zerra.Benchmark`, measured with BenchmarkDotNet. Run them yourself with:

```bash
dotnet run --project Benchmarks/Zerra.Benchmark/Zerra.Benchmark.csproj -c Release
```

Environment: BenchmarkDotNet 0.15.8, .NET 10.0.12 (x64 RyuJIT), Windows 11, Intel Core i9-14900HX. Measured October 5, 2026 on a development laptop with containers running in the background, so absolute times vary; compare rows within a table.

## Serializers

One `NormalJsonModel` from `Tests/Zerra.Test` (41 properties: strings, decimals, bools, and dates, plus arrays of child models), serialized and deserialized as a string, as UTF-8 bytes, and with `ZerraByteSerializer`.

| Method | Mean | Allocated |
|---|---:|---:|
| Serialize, `ZerraByteSerializer` | **3.4 µs** | **5.5 KB** |
| Serialize, System.Text.Json to UTF-8 bytes | 5.8 µs | 9.2 KB |
| Serialize, System.Text.Json to string | 6.4 µs | 18.0 KB |
| Serialize, `ZerraJsonSerializer` to string | 8.8 µs | 18.0 KB |
| Serialize, `ZerraJsonSerializer` to UTF-8 bytes | 9.1 µs | 9.1 KB |
| Serialize, Newtonsoft.Json to string | 14.1 µs | 55.8 KB |
| Deserialize, `ZerraByteSerializer` | **4.2 µs** | 12.9 KB |
| Deserialize, System.Text.Json from string | 11.0 µs | 13.6 KB |
| Deserialize, System.Text.Json from UTF-8 bytes | 11.1 µs | 13.6 KB |
| Deserialize, `ZerraJsonSerializer` from string | 16.5 µs | 13.0 KB |
| Deserialize, `ZerraJsonSerializer` from UTF-8 bytes | 17.0 µs | 13.0 KB |
| Deserialize, Newtonsoft.Json from string | 20.1 µs | 21.1 KB |

- `ZerraByteSerializer`, the serializer services use between themselves, serializes in about 60% of System.Text.Json's time and deserializes in under 40%, with less memory.
- `ZerraJsonSerializer` is slower than System.Text.Json and faster than Newtonsoft.Json, with similar allocations to System.Text.Json. It's meant for browsers and outside callers, where it adds [Graph](Graph.md) member selection, nameless JSON, and PATCH tracking.

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
