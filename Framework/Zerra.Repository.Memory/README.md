# Zerra.Repository.Memory

In-memory provider for [Zerra.Repository](https://www.nuget.org/packages/Zerra.Repository), for tests, prototyping, and as a fallback when a database isn't running.

## Installation

```bash
dotnet add package Zerra.Repository.Memory
```

## Usage

```csharp
using Zerra.Repository;
using Zerra.Repository.Memory;

// each engine is its own store, so a test can start from an empty one
var engine = new MemoryEngine();

var repo = Repo.New();
repo.AddProvider(new TransactStoreProvider<OrderDataModel>(engine));
```

`MemoryEngine` is also an `IEventStoreEngine`, so it can stand in for KurrentDB under an `AggregateRoot`.

## Documentation

- [Repository](https://github.com/szawaski/Zerra/blob/master/docs/Repository.md) - Models, providers, queries, and relations
- [Zerra documentation](https://github.com/szawaski/Zerra/blob/master/docs/Index.md)

## License

MIT - See [LICENSE](https://github.com/szawaski/Zerra/blob/master/LICENSE)
