[← Back to Documentation](Index.md)

# Benchmarks

Results from `Benchmarks/Zerra.Benchmark`, measured with BenchmarkDotNet. Run them yourself with:

```bash
dotnet run --project Benchmarks/Zerra.Benchmark/Zerra.Benchmark.csproj -c Release
```

Environment: BenchmarkDotNet 0.15.8, .NET 10.0.12 (x64 RyuJIT), Windows 11, Intel Core i9-14900HX. Measured October 6, 2026 on a development laptop, pinned to the performance cores because this CPU also has slower efficiency cores. Absolute times vary between machines; compare rows within a table.

## Serializers

One `NormalJsonModel` from `Tests/Zerra.Test` (41 properties: strings, decimals, bools, and dates, plus arrays of child models). The JSON serializers write JSON as a string or as UTF-8 bytes; `ZerraByteSerializer` writes its compact binary format.

**Time** (lower is better)

| | `ZerraJsonSerializer` | System.Text.Json | Newtonsoft.Json | `ZerraByteSerializer` |
|---|---:|---:|---:|---:|
| Serialize to string | **4.7 µs** | 5.6 µs | 11.8 µs | |
| Serialize to bytes | 4.1 µs | 5.2 µs | | **2.8 µs** |
| Deserialize from string | **9.5 µs** | 10.4 µs | 18.8 µs | |
| Deserialize from bytes | 8.4 µs | 10.2 µs | | **3.0 µs** |

**Memory allocated**

| | `ZerraJsonSerializer` | System.Text.Json | Newtonsoft.Json | `ZerraByteSerializer` |
|---|---:|---:|---:|---:|
| Serialize to string | **17.8 KB** | 18.0 KB | 55.8 KB | |
| Serialize to bytes | 9.0 KB | 9.2 KB | | **5.4 KB** |
| Deserialize from string | **13.4 KB** | 13.6 KB | 21.1 KB | |
| Deserialize from bytes | 13.4 KB | 13.6 KB | | **12.8 KB** |

- `ZerraByteSerializer`, the serializer services use between themselves, serializes in about half of System.Text.Json's time and deserializes in under a third, with less memory.
- `ZerraJsonSerializer`, for browsers and outside callers, is faster than System.Text.Json in all four comparisons, by 9–21%, and about twice as fast as Newtonsoft.Json. It adds [Graph](Graph.md) member selection, nameless JSON, and PATCH tracking.

### Other Models

`ModelSerializerBenchmarks` compares `ZerraJsonSerializer` with System.Text.Json across different shapes of data, with System.Text.Json given `JsonStringEnumConverter` so both write enums as names. Each cell is Zerra's time relative to System.Text.Json; negative is faster. Memory allocated is within 1% of System.Text.Json, or lower, for every model.

| Model | Serialize to string | Serialize to bytes | Deserialize from string | Deserialize from bytes |
|---|---:|---:|---:|---:|
| Small: one object with an int and a string (40–80 ns) | −38% | −35% | −22% | −22% |
| TypesBasic: every core type, nullable and null, enums, a child object | −7% | −11% | −19% | −17% |
| TypesList: a `List<T>` of every core type | −9% | −16% | −42% | −44% |
| Orders100: a list of 100 orders with a Guid, strings, a date, a decimal, an int, and a bool | −17% | −31% | −13% | −15% |
| SimpleArray1000: an array of 1,000 objects with an int and a string | −15% | −23% | −24% | −23% |
| Dictionary100: a `Dictionary<string, string>` of 100 entries | −7% | −11% | −2% | −8% |

## Mapper

`Map<ModelA, ModelB>` and back between two models of 37 members: arrays, lists, and other collection types, dictionaries, and nested models.

| Method | Mean | Allocated |
|---|---:|---:|
| `ModelA` to `ModelB` | 4.3 µs | 8.3 KB |
| `ModelB` to `ModelA` | 4.3 µs | 7.3 KB |

## Repository

Zerra.Repository's comparison with Entity Framework on SQL Server is in [Repository](Repository.md#compared-with-entity-framework), from `Benchmarks/Zerra.Repository.Benchmark`.

## See Also

- [Testing](Testing.md)
- [Serializers](Serializers.md)
