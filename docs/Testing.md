[← Back to Documentation](Index.md)

# Testing

Line coverage of each framework project is listed below, measured with `dotnet-coverage`. The transport and data store tests run against real Kafka, RabbitMQ, Azure Service Bus, SQL Server, PostgreSQL, MySQL, MariaDB, and KurrentDB instances, not mocks.

## Line Coverage

| Project | Coverage | Tested by |
|---|---|---|
| `Zerra` | 96% | `Zerra.Test` |
| `Zerra.Web` | 97% | `Zerra.Test` |
| `Zerra.CQRS.Kafka` | 86% | `Zerra.CQRS.Test` |
| `Zerra.CQRS.RabbitMQ` | 85% | `Zerra.CQRS.Test` |
| `Zerra.CQRS.AzureServiceBus` | 88% | `Zerra.CQRS.Test` |
| `Zerra.Repository` | 92% | `Zerra.Repository.Test` |
| `Zerra.Repository.Memory` | 92% | `Zerra.Repository.Test` |
| `Zerra.Repository.MsSql` | 90% | `Zerra.Repository.Test` |
| `Zerra.Repository.PostgreSql` | 89% | `Zerra.Repository.Test` |
| `Zerra.Repository.MySql` | 90% | `Zerra.Repository.Test` |
| `Zerra.Repository.MariaDb` | 89% | `Zerra.Repository.Test` |
| `Zerra.Repository.KurrentDB` | 96% | `Zerra.Repository.Test` |
| `Zerra.SourceGeneration` | 93% | `Zerra.SourceGeneration.Test` |
| `Zerra.T4` | not measured, the coverage tool can't load into its .NET Framework test process | `Zerra.T4.Test` |

## Running the Tests

The test projects use xunit v3. Build the solution, then run a project's test executable, filtering by class if needed:

```bash
dotnet build Zerra.slnx
Tests/Zerra.Test/bin/Debug/net10.0/Zerra.Test.exe
Tests/Zerra.CQRS.Test/bin/Debug/net10.0/Zerra.CQRS.Test.exe -class "Zerra.CQRS.Test.Kafka.KafkaMessageTests"
```

To measure coverage, run a test executable under `dotnet-coverage collect -f cobertura`.

`Demo/Infrastructure/start-infrastructure.ps1` starts the brokers and databases the transport and data store tests need, in Docker, skipping any already running. Each broker test uses its own topics and deletes them afterwards, and each data store test recreates its database.

## Benchmarks

Serializer and mapper results are in [Benchmarks](Benchmarks.md), and the repository's comparison with Entity Framework is in [Repository](Repository.md#compared-with-entity-framework).

## See Also

- [Delivery and Failure Handling](Reliability.md)
- [Running in Production](Production.md)
