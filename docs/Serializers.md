[← Back to Documentation](Index.md)

# Serializers

Zerra provides built-in serialization options for efficient message transport across local and remote boundaries. Serializers implement the `ISerializer` interface and are used to convert messages to and from byte arrays for network transmission.

## Overview

Serializers are essential components in Zerra's architecture, handling:
- Message serialization for network transport (TCP, HTTP, message brokers)
- Type-safe serialization and deserialization
- Support for both binary and human-readable formats
- Stream-based serialization for large payloads

Zerra provides three serializer options:
- **[ByteSerializer](ByteSerializer.md)** - High-performance binary serialization (recommended for production)
- **[JsonSerializer](JsonSerializer.md)** - JSON serialization with Graph-based property control
- **SystemTextJsonSerializer** - Standard Microsoft System.Text.Json serializer without Zerra features

## Quick Comparison

| Feature | ZerraByteSerializer | ZerraJsonSerializer | SystemTextJsonSerializer |
|---------|---------------------|---------------------|--------------------------|
| **Performance** | ⚡⚡⚡⚡⚡ Very Fast | ⚡⚡⚡ Moderate | ⚡⚡⚡ Moderate |
| **Size** | 📦 Very Compact | 📦📦📦 Larger | 📦📦📦 Larger |
| **Readability** | ❌ Binary | ✅ Human Readable | ✅ Human Readable |
| **Interoperability** | .NET Only | ✅ Universal | ✅ Universal |
| **Nameless Mode** | ❌ | ✅ | ❌ |
| **Use Case** | Production | Development/APIs | Standard JSON APIs |
| **Details** | [Learn More →](ByteSerializer.md) | [Learn More →](JsonSerializer.md) | See below |

## ByteSerializer (ZerraByteSerializer)

The `ZerraByteSerializer` wrapper delegates to `ByteSerializer` for high-performance binary serialization with a compact format optimized for speed and minimal overhead.

See [ByteSerializer](ByteSerializer.md) for detailed documentation.

### Features

- **High Performance**: Optimized for speed with minimal allocations
- **Compact Format**: Binary encoding reduces message size
- **AOT Compatible**: Source-generated code eliminates reflection overhead
- **Type Safe**: Strong typing with generic support
- **Stream Support**: Direct serialization to/from streams for large objects

### Usage

```csharp
using Zerra.Serialization;

// Create the serializer
ISerializer serializer = new ZerraByteSerializer();

// Use in Bus configuration
var bus = Bus.New(
    service: "MyService",
    log: logger,
    busLog: busLogger,
    busScopes: busScopes
);

// Use in TCP CQRS
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, log);
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, log);

// Use in HTTP CQRS
var httpServer = new HttpCqrsServer("localhost:8080", serializer, encryptor, log);
var httpClient = new HttpCqrsClient("localhost:8080", serializer, encryptor, log);
```

### When to Use

Choose `ZerraByteSerializer` when:
- ✅ Performance is critical
- ✅ Network bandwidth is limited
- ✅ Message size matters
- ✅ Both client and server are .NET applications
- ✅ You need the smallest possible payload size

## JsonSerializer (ZerraJsonSerializer)

The `ZerraJsonSerializer` wrapper delegates to `JsonSerializer` for JSON serialization with **Graph-based property control** as its primary capability.

See [JsonSerializer](JsonSerializer.md) for detailed documentation.

### Features

- **Human Readable**: JSON format for easy debugging and inspection
- **Interoperable**: Compatible with any system that can parse JSON
- **Flexible**: Optional configuration via `JsonSerializerOptions`
- **Nameless Mode**: Compact JSON without property names (nameless JSON)
- **Standards-Based**: Uses System.Text.Json for serialization

### Usage

```csharp
using Zerra.Serialization;
using System.Text.Json;

// Create with default options
ISerializer serializer = new ZerraJsonSerializer();

// Create with custom options
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNameCaseInsensitive = true
};
ISerializer serializer = new ZerraJsonSerializer(options);

// Use in Bus configuration
var bus = Bus.New(
    service: "MyService",
    log: logger,
    busLog: busLogger,
    busScopes: busScopes
);

// Use in network components
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, log);
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, log);
```

### Configuration Options

The `ZerraJsonSerializer` accepts `JsonSerializerOptions` for customization:

```csharp
var options = new JsonSerializerOptions
{
    WriteIndented = true,              // Pretty-print for debugging
    PropertyNameCaseInsensitive = true, // Case-insensitive deserialization
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    // Additional System.Text.Json options...
};

ISerializer serializer = new ZerraJsonSerializer(options);
```

### Nameless JSON Mode

Nameless JSON mode produces compact JSON arrays without property names, useful for browser endpoints where JavaScript can decode the compact format:

```csharp
// Enable nameless JSON mode
var options = new JsonSerializerOptions
{
    Nameless = true  // Custom Zerra extension
};

ISerializer serializer = new ZerraJsonSerializer(options);

// Example output:
// Standard JSON:  {"Name":"John","Age":30,"Email":"john@example.com"}
// Nameless JSON:  ["John",30,"john@example.com"]
```

**Use Case**: Primarily for browser endpoints where:
- You need compact JSON for reduced bandwidth
- You have JavaScript code to decode the positional array format
- Property names add unnecessary overhead for client-side processing
- You're building custom web APIs with client-side decoders

**Note**: Requires client-side JavaScript to know the property order. Not recommended for general API use where self-describing JSON is preferred.

### When to Use

Choose `ZerraJsonSerializer` when:
- ✅ Debugging and need to inspect messages
- ✅ Interoperability with non-.NET systems
- ✅ Human readability is important
- ✅ Working with web APIs or browsers
- ✅ Need Nameless JSON mode for compact browser payloads
- ✅ Standards compliance is required

## SystemTextJsonSerializer

The `SystemTextJsonSerializer` provides standard Microsoft System.Text.Json serialization without Zerra-specific extensions.

### Features

- **Pure System.Text.Json**: Uses Microsoft's built-in JSON serializer
- **No Extensions**: Standard JSON without Nameless mode or other Zerra customizations
- **Fully Compatible**: Works with all System.Text.Json features and converters
- **Standards-Based**: Pure JSON serialization following .NET conventions

### Usage

```csharp
using Zerra.Serialization;
using System.Text.Json;

// Create with default options
ISerializer serializer = new SystemTextJsonSerializer();

// Create with custom System.Text.Json options
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

ISerializer serializer = new SystemTextJsonSerializer(options);

// Use in network components
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, log);
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, log);
```

### When to Use

Choose `SystemTextJsonSerializer` when:
- ✅ You need pure System.Text.Json behavior
- ✅ Compatibility with existing System.Text.Json configurations
- ✅ You don't need Zerra-specific extensions (Nameless mode)
- ✅ Working with standard .NET JSON APIs
- ✅ Migrating from other System.Text.Json-based systems

### ZerraJsonSerializer vs SystemTextJsonSerializer

| Feature | ZerraJsonSerializer | SystemTextJsonSerializer |
|---------|---------------------|--------------------------|
| **Base Library** | System.Text.Json | System.Text.Json |
| **Nameless Mode** | ✅ Supported | ❌ Not supported |
| **Zerra Extensions** | ✅ Yes | ❌ No |
| **Standard JSON** | ✅ Yes | ✅ Yes |
| **Use Case** | Compact browser endpoints | Standard JSON APIs |

**Recommendation**: Use `ZerraJsonSerializer` for Zerra-specific features like Nameless mode. Use `SystemTextJsonSerializer` for pure System.Text.Json compatibility.

## ISerializer Interface

Both serializers implement the `ISerializer` interface with consistent methods:

```csharp
public interface ISerializer
{
    ContentType ContentType { get; }

    // Byte array serialization
    byte[] SerializeBytes(object? obj);
    byte[] SerializeBytes(object? obj, Type type);
    byte[] SerializeBytes<T>(T? obj);

    // Byte array deserialization
    object? Deserialize(ReadOnlySpan<byte> bytes, Type type);
    T? Deserialize<T>(ReadOnlySpan<byte> bytes);

    // Stream serialization
    void Serialize(Stream stream, object? obj);
    void Serialize(Stream stream, object? obj, Type type);
    void Serialize<T>(Stream stream, T? obj);

    // Stream deserialization
    object? Deserialize(Stream stream, Type type);
    T? Deserialize<T>(Stream stream);

    // Async stream serialization
    Task SerializeAsync(Stream stream, object? obj, CancellationToken cancellationToken);
    Task SerializeAsync(Stream stream, object? obj, Type type, CancellationToken cancellationToken);
    Task SerializeAsync<T>(Stream stream, T? obj, CancellationToken cancellationToken);

    // Async stream deserialization
    Task<object?> DeserializeAsync(Stream stream, Type type, CancellationToken cancellationToken);
    Task<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken);
}
```

## Serializer Compatibility

⚠️ **Important**: The serializer used on the client must match the serializer used on the server. Mismatched serializers will cause deserialization errors.

```csharp
// ❌ Will NOT work - mismatched serializers
// Server
var server = new TcpCqrsServer("localhost:9001", new ZerraByteSerializer(), encryptor, log);

// Client
var client = new TcpCqrsClient("localhost:9001", new ZerraJsonSerializer(), encryptor, log);

// ✅ Correct - matching serializers
// Server
var server = new TcpCqrsServer("localhost:9001", new ZerraByteSerializer(), encryptor, log);

// Client
var client = new TcpCqrsClient("localhost:9001", new ZerraByteSerializer(), encryptor, log);
```

## Performance Comparison

| Serializer | Speed | Size | Readability | Interoperability |
|------------|-------|------|-------------|------------------|
| ZerraByteSerializer | ⚡⚡⚡⚡⚡ | 📦 Very Small | ❌ Binary | .NET Only |
| ZerraJsonSerializer | ⚡⚡⚡ | 📦📦📦 Larger | ✅ Human Readable | ✅ Universal |

## Best Practices

1. **Use ZerraByteSerializer for production**: Optimal performance and minimal bandwidth
2. **Use ZerraJsonSerializer for development**: Easy debugging and message inspection
3. **Match serializers**: Always use the same serializer on client and server
4. **Consider payload size**: Choose binary for large volumes, JSON for occasional messages
5. **Test serialization**: Ensure your models serialize correctly before deployment

## Common Scenarios

### Development and Debugging

```csharp
// Use JSON serializer for easy debugging
#if DEBUG
    ISerializer serializer = new ZerraJsonSerializer(new JsonSerializerOptions { WriteIndented = true });
#else
    ISerializer serializer = new ZerraByteSerializer();
#endif
```

### High-Performance Production

```csharp
// Always use binary serializer in production
ISerializer serializer = new ZerraByteSerializer();
```

### Interop with External Systems

```csharp
// Use JSON for compatibility with non-.NET systems
ISerializer serializer = new ZerraJsonSerializer();
```

## See Also

- [Encryptors](Encryptors.md) - Secure message encryption
- [Server Setup](ServerSetup.md) - Configure server-side serialization
- [Client Setup](ClientSetup.md) - Configure client-side serialization
