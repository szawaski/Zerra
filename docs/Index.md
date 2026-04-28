# Documentation Index

Welcome to the Zerra CQRS Framework documentation. This guide provides comprehensive information about building distributed, message-driven applications using Zerra.

> 📖 **Looking for the project overview?** See the [Main Project README](../README.md) for quick start and introduction.

---

## Documentation Table of Contents

### Getting Started
- [Agents](Agents.md) - Architectural context for AI agents working with Zerra

### Configuration & Setup
- [AOT (Ahead-of-Time Compilation)](AOT.md) - Automatic source generator for precompiled reflection and Native AOT support (included with Zerra package)
- [Serializers](Serializers.md) - Configure serialization with ZerraByteSerializer and ZerraJsonSerializer
  - [ByteSerializer](ByteSerializer.md) - High-performance binary serialization
  - [JsonSerializer](JsonSerializer.md) - JSON serialization with Graph-based property control
- [Encryptors](Encryptors.md) - Secure message encryption with ZerraEncryptor
- [Logging](Logging.md) - Implement ILogger and IBusLogger for comprehensive logging
- [Service Injection](ServiceInjection.md) - Manage dependencies with BusServices
- [Zerra.Web](ZerraWeb.md) - ASP.NET integration and CQRS API Gateway
- [Client Setup](ClientSetup.md) - Configure client-side applications in Program.cs
- [Server Setup](ServerSetup.md) - Configure server-side applications in Program.cs

### Messaging Transports
- [Azure Service Bus Setup](AzureServiceBusSetup.md) - Use Azure Service Bus for distributed messaging
- [Kafka Setup](KafkaSetup.md) - Use Apache Kafka for high-throughput messaging
- [RabbitMQ Setup](RabbitMQSetup.md) - Use RabbitMQ for reliable message delivery

### Core Concepts
- [Queries](Queries.md) - Execute read operations with type-safe query interfaces
- [Commands](Commands.md) - Dispatch state-changing operations with commands
- [Events](Events.md) - Publish and handle state change notifications

### Utility Features
- [Graph](Graph.md) - Selective member inclusion/exclusion for serialization and mapping
- [Mapper](Mapper.md) - Object mapping and type conversion with AOT support
- [Collections](Collections.md) - Thread-safe collection classes (ConcurrentFactoryDictionary, ConcurrentList)
- [EnumName](EnumName.md) - Attribute-based custom string names for enum values with parse and extension method support
- [Reflection](Reflection.md) - TypeAnalyzer and TypeDetail for runtime type analysis
- [String Extensions](StringExtensions.md) - String manipulation, truncation, and type conversion helpers
- [Stream Wrappers](StreamWrappers.md) - Stream interception, transformation, and monitoring

### Additional Resources
- [Main Project README](../README.md) - Quick start, installation, and project overview

---

## Quick Reference

### Essential First Steps
1. **[Add Zerra Package](AOT.md)** - Reference Zerra NuGet package (includes automatic source generation for CQRS types)
2. **[Configure Serializer](Serializers.md)** - Choose ZerraByteSerializer (binary) or ZerraJsonSerializer (JSON)
3. **[Set Up Server](ServerSetup.md)** - Register handlers and start consumers
4. **[Set Up Client](ClientSetup.md)** - Configure remote connections

### Key Interfaces
- **`IQueryHandler`** - Handles read operations ([Queries](Queries.md))
- **`ICommandHandler`** - Handles write operations ([Commands](Commands.md))
- **`IEventHandler`** - Handles event notifications ([Events](Events.md))
- **`BaseHandler`** - Base class providing `Bus`, `Log`, and `Context` access

### Common Tasks
- **Add encryption** → See [Encryptors](Encryptors.md) for ZerraEncryptor setup
- **Add logging** → See [Logging](Logging.md) for ILogger and IBusLogger
- **Inject services** → See [Service Injection](ServiceInjection.md) for BusServices usage
- **Map between types** → See [Mapper](Mapper.md) for object mapping and conversions
- **Custom enum names** → See [EnumName](EnumName.md) for attribute-based enum string representation
- **Enable AOT** → See [AOT Support](AOT.md) for Native AOT compilation
- **Expose HTTP API** → See [Zerra.Web](ZerraWeb.md) for API Gateway setup
- **Deploy to Azure** → See [Zerra.Web](ZerraWeb.md) for IIS/Azure App Services hosting

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/szawaski/Zerra).
