# Zerra.Repository.PostgreSql

> ⚠️ **Experimental:** the Repository is still experimental and may change.

PostgreSQL provider for [Zerra.Repository](https://www.nuget.org/packages/Zerra.Repository).

## Installation

```bash
dotnet add package Zerra.Repository.PostgreSql
```

## Usage

```csharp
using Zerra.Repository;
using Zerra.Repository.PostgreSql;
using Zerra.Repository.Memory;

// MemoryEngine is a fallback when the database isn't running
ITransactStoreEngine engine = PostgreSqlConnectionTest.Test(connectionString, log)
    ? new PostgreSqlEngine(connectionString)
    : new MemoryEngine();

var repo = Repo.New();
repo.AddProvider(new MyStoreProvider<OrderDataModel>(engine));   // a TransactStoreProvider<TModel>
```

The engine doesn't connect until it's used. Create the schema at startup with Code First generation:

```csharp
CodeFirstGeneration.Generate(engine, DataStoreGenerationType.CodeFirst, [typeof(OrderDataModel)], log);
```

## Documentation

- [Repository](https://github.com/szawaski/Zerra/blob/master/docs/Repository.md) - Models, providers, queries, and relations
- [Repository Generation](https://github.com/szawaski/Zerra/blob/master/docs/RepositoryGeneration.md) - Code First schema generation
- [Zerra documentation](https://github.com/szawaski/Zerra/blob/master/docs/Index.md)

## License

MIT - See [LICENSE](https://github.com/szawaski/Zerra/blob/master/LICENSE)
