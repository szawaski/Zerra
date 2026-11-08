# Documentation

New to Zerra? Read the [README](../README.md), then [Getting Started](GettingStarted.md).

## Start Here

- [Getting Started](GettingStarted.md) - Build a service and a client, then move between in-process, TCP, HTTP, and brokers
- [Store Demo](../Demo/Store/README.md) - A complete multi-service sample

## Messages

- [Queries](Queries.md) - Reads, called like methods
- [Commands](Commands.md) - Changes, handled once
- [Events](Events.md) - Notifications, to every replica or one per service
- [Delivery and Failure Handling](Reliability.md) - Acknowledgement, resilient commands, and failures

## Setup

- [Server Setup](ServerSetup.md) - Hosting handlers, and shutdown
- [Client Setup](ClientSetup.md) - Calling services, timeouts, and concurrency limits
- [Service Injection](ServiceInjection.md) - Services for handlers
- [Logging](Logging.md) - `ILogger` and `IBusLogger`
- [AOT](AOT.md) - Source generation and Native AOT
- [Serializers](Serializers.md) - Choosing one: [ByteSerializer](ByteSerializer.md) (binary) or [JsonSerializer](JsonSerializer.md)
- [Encryptors](Encryptors.md) - Message encryption and key rotation
- [Compressors](Compressors.md) - Message compression

## Brokers

- [Kafka](KafkaSetup.md)
- [RabbitMQ](RabbitMQSetup.md)
- [Azure Service Bus](AzureServiceBusSetup.md)

## Web

- [Zerra.Web](ZerraWeb.md) - The browser API gateway, and hosting services in ASP.NET Core
- [Front End Scripts](FrontEndScripts.md) - JavaScript and TypeScript clients served by the gateway
- [ApiClient](ApiClient.md) - .NET clients for the gateway
- [Security](Security.md) - Trust model, claims, and authorizers

## Repository

- [Repository](Repository.md) - LINQ data access that works the same on every store
- [Repository Queries](RepositoryQueries.md) - LINQ functions, joins, relations, aggregates, and history
- [Repository Generation](RepositoryGeneration.md) - Code First schemas and Database First models

## Production

- [Running in Production](Production.md) - Delivery, security, versioning, and observability
- [Testing](Testing.md) - Coverage and what the tests cover
- [Benchmarks](Benchmarks.md) - Serializers, mapper, bus, and encryption

## Utilities

- [Graph](Graph.md) - Choosing which members to read, write, or map
- [Mapper](Mapper.md) - Object mapping and deep copies
- [Collections](Collections.md) - Thread-safe collections
- [EnumName](EnumName.md) - Custom enum names
- [Reflection](Reflection.md) - `TypeAnalyzer` and `TypeDetail`
- [String Extensions](StringExtensions.md) - Parsing, truncation, and wildcards
- [Stream Wrappers](StreamWrappers.md) - Stream interception and transforms

## For AI Agents

- [Agents](Agents.md) - Building applications with Zerra, and changing the framework
- [Upgrading to Zerra 6](UpgradeV5ToV6.md) - Moving a solution step by step
