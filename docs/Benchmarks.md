[← Back to Documentation](Index.md)

# Benchmarks

Results from `Benchmarks/Zerra.Benchmark`, measured with BenchmarkDotNet. Run them yourself with:

```bash
dotnet run --project Benchmarks/Zerra.Benchmark/Zerra.Benchmark.csproj -c Release
```

Environment: BenchmarkDotNet 0.15.8, .NET 10.0.12, Windows 11, Intel Core i9-14900HX laptop, pinned to the performance cores. Measured October 9, 2026. Times vary between machines, so compare rows within a table.

## Serializers

One `NormalJsonModel` from `Tests/Zerra.Test` (41 properties: strings, decimals, bools, and dates, plus arrays of child models). The JSON serializers write JSON as a string or as UTF-8 bytes; `ZerraByteSerializer` writes its compact binary format.

**Time** (lower is better)

| | `ZerraJsonSerializer` | System.Text.Json | Newtonsoft.Json | `ZerraByteSerializer` |
|---|---:|---:|---:|---:|
| Serialize to string | **5.0 µs** | 6.0 µs | 12.8 µs | |
| Serialize to bytes | 4.3 µs | 5.4 µs | | **3.0 µs** |
| Deserialize from string | **9.8 µs** | 11.1 µs | 20.5 µs | |
| Deserialize from bytes | 9.4 µs | 10.4 µs | | **3.3 µs** |

**Memory allocated**

| | `ZerraJsonSerializer` | System.Text.Json | Newtonsoft.Json | `ZerraByteSerializer` |
|---|---:|---:|---:|---:|
| Serialize to string | **17.8 KB** | 18.0 KB | 55.8 KB | |
| Serialize to bytes | 9.0 KB | 9.2 KB | | **5.4 KB** |
| Deserialize from string | **13.4 KB** | 13.6 KB | 21.1 KB | |
| Deserialize from bytes | 13.4 KB | 13.6 KB | | **12.8 KB** |

- `ZerraByteSerializer`, the serializer services use between themselves, serializes in about half of System.Text.Json's time and deserializes in about a third, with less memory.
- `ZerraJsonSerializer`, for browsers and outside callers, is faster than System.Text.Json in all four comparisons, by 10–20%, and about twice as fast as Newtonsoft.Json. It adds [Graph](Graph.md) member selection, nameless JSON, and PATCH tracking.

### Other Models

`ModelSerializerBenchmarks` compares `ZerraJsonSerializer` with System.Text.Json on other kinds of data, both writing enums as names. Each cell is Zerra's time relative to System.Text.Json; negative is faster. Memory is the same or lower, within 1%.

| Model | Serialize to string | Serialize to bytes | Deserialize from string | Deserialize from bytes |
|---|---:|---:|---:|---:|
| Small: one object with an int and a string (40–80 ns) | −37% | −31% | −21% | −20% |
| TypesBasic: every core type, nullable and null, enums, a child object | −8% | −10% | −17% | −18% |
| TypesList: a `List<T>` of every core type | −13% | −15% | −42% | −43% |
| Orders100: a list of 100 orders with a Guid, strings, a date, a decimal, an int, and a bool | −24% | −28% | −18% | −16% |
| SimpleArray1000: an array of 1,000 objects with an int and a string | −17% | −25% | −22% | −22% |
| Dictionary100: a `Dictionary<string, string>` of 100 entries | −1% | −15% | −2% | −5% |

## Mapper

`Map<ModelA, ModelB>` and back between two models of 37 members: arrays, lists, and other collection types, dictionaries, and nested models.

| Method | Mean | Allocated |
|---|---:|---:|
| `ModelA` to `ModelB` | 4.5 µs | 8.3 KB |
| `ModelB` to `ModelA` | 4.7 µs | 7.5 KB |

## Repository

Zerra.Repository's comparison with Entity Framework on SQL Server is in [Repository](Repository.md#compared-with-entity-framework), from `Benchmarks/Zerra.Repository.Benchmark`.

## See Also

- [Testing](Testing.md)
- [Serializers](Serializers.md)
