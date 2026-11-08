# Zerra.Repository.PostgreSql

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
repo.AddProvider(new TransactStoreProvider<OrderDataModel>(engine));
```

The engine doesn't connect until it's used. Create the schema at startup with Code First generation:

```csharp
CodeFirstGeneration.Generate(engine, DataStoreGenerationType.CodeFirst, [typeof(OrderDataModel)], log);
```

For an existing database, write the models from its tables instead:

```csharp
foreach (var model in DatabaseFirstGeneration.Generate(engine, "MyApp.DataModels"))
    File.WriteAllText(Path.Combine(folder, model.FileName), model.Source);
```

## Documentation

- [Repository](https://github.com/szawaski/Zerra/blob/master/docs/Repository.md) - Models, providers, queries, and relations
- [Repository Generation](https://github.com/szawaski/Zerra/blob/master/docs/RepositoryGeneration.md) - Code First schema generation and Database First models
- [Zerra documentation](https://github.com/szawaski/Zerra/blob/master/docs/Index.md)

## License

MIT - See [LICENSE](https://github.com/szawaski/Zerra/blob/master/LICENSE)
