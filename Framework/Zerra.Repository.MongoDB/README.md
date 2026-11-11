# Zerra.Repository.MongoDB

MongoDB provider for [Zerra.Repository](https://www.nuget.org/packages/Zerra.Repository).

## Installation

```bash
dotnet add package Zerra.Repository.MongoDB
```

## Usage

```csharp
using Zerra.Repository;
using Zerra.Repository.MongoDB;
using Zerra.Repository.Memory;

// the connection string names the database
var connectionString = "mongodb://localhost:27017/store";

// MemoryEngine is a fallback when the server isn't running
ITransactStoreEngine engine = MongoDBConnectionTest.Test(connectionString, log)
    ? new MongoDBEngine(connectionString)
    : new MemoryEngine();

var repo = Repo.New();
repo.AddProvider(new TransactStoreProvider<OrderDataModel>(engine));
```

Create the collections and indexes at startup with Code First generation:

```csharp
CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints, [typeof(OrderDataModel)], log);
```

Each model is a collection and each row a document. Queries run as aggregation pipelines. Transactions need a replica set or a sharded cluster.

## Documentation

- [Repository](https://github.com/szawaski/Zerra/blob/master/docs/Repository.md#mongodb) - Models, providers, queries, and how MongoDB stores them
- [Repository Generation](https://github.com/szawaski/Zerra/blob/master/docs/RepositoryGeneration.md) - Code First schema generation
- [Zerra documentation](https://github.com/szawaski/Zerra/blob/master/docs/Index.md)

## License

MIT - See [LICENSE](https://github.com/szawaski/Zerra/blob/master/LICENSE)
