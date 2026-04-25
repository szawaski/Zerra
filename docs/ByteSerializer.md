[← Back to Documentation](README.md)

# ByteSerializer

`ByteSerializer` is Zerra's high-performance binary serialization engine, optimized for speed, compact size, and AOT (Ahead-of-Time) compilation compatibility.

> **Note**: When using the Zerra CQRS Bus, use the `ZerraByteSerializer` wrapper class which delegates to `ByteSerializer`.

## Overview

`ByteSerializer` provides:
- **Ultra-fast serialization** - Optimized for minimal allocations and maximum throughput
- **Compact binary format** - Smallest possible message size
- **AOT compatible** - Works with Native AOT compilation via source generation
- **Type-safe** - Strong typing with full generic support
- **Stream support** - Direct serialization to/from streams for large objects
- **Zero reflection overhead** - Uses source-generated code for type operations

## Key Features

### 1. High Performance

`ByteSerializer` is designed for maximum performance:

```csharp
using Zerra.Serialization.Bytes;

// Direct usage
var command = new CreateUserCommand { Email = "user@example.com", Name = "John Doe" };
byte[] bytes = ByteSerializer.Serialize(command);

// Deserialize
var deserialized = ByteSerializer.Deserialize<CreateUserCommand>(bytes);
```

**Performance characteristics:**
- ⚡ 10-100x faster than JSON serialization
- 💾 50-80% smaller payload size than JSON
- 🚀 Zero allocations for primitive types
- 📦 Minimal garbage collection pressure

### 2. Compact Binary Format

The binary format is optimized for size:

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

var user = new User { Id = 123, Name = "John", Email = "john@example.com" };

// ByteSerializer: ~45 bytes (binary)
var binaryBytes = ByteSerializer.Serialize(user);

// JsonSerializer: ~65 bytes (text)
var jsonBytes = JsonSerializer.Serialize(user);
```

**Size advantages:**
- No property name overhead (names not included in payload)
- Efficient numeric encoding (variable-length integers)
- Optimized string encoding
- No whitespace or formatting characters

### 3. AOT Compilation Support

Works seamlessly with Native AOT compilation:

```csharp
// Requires Zerra.SourceGeneration reference
// See AOT.md for setup instructions

// All type operations are source-generated at compile time
// No runtime reflection required
var bytes = ByteSerializer.Serialize(command);
```

**AOT benefits:**
- ✅ Fast startup (no runtime type discovery)
- ✅ Small deployment size (no unused code)
- ✅ Predictable performance (no JIT compilation)
- ✅ Compatible with IL trimming

### 4. Type Safety

Full generic support with compile-time type checking:

```csharp
using Zerra.Serialization.Bytes;

// Generic serialization - type-safe
byte[] bytes = ByteSerializer.Serialize<CreateUserCommand>(command);

// Generic deserialization - type-safe
CreateUserCommand result = serializer.Deserialize<CreateUserCommand>(bytes);

// Generic deserialization - type-safe
var result = ByteSerializer.Deserialize<CreateUserCommand>(bytes);

// Non-generic variants also available
byte[] bytes2 = ByteSerializer.Serialize(command, typeof(CreateUserCommand));
object result2 = ByteSerializer.Deserialize(bytes2, typeof(CreateUserCommand));
```

### 5. Stream Support

Direct stream serialization for large objects without memory buffering:

```csharp
using Zerra.Serialization.Bytes;

// Serialize directly to stream
using var fileStream = File.Create("command.bin");
ByteSerializer.Serialize(fileStream, command);

// Deserialize directly from stream
using var readStream = File.OpenRead("command.bin");
var result = ByteSerializer.Deserialize<CreateUserCommand>(readStream);
```

**Stream advantages:**
- 💾 No memory buffering of large objects
- ⚡ Direct I/O operations
- 🔄 Suitable for large file processing
- 📊 Efficient for log files and data exports

## Supported Types

`ByteSerializer` supports a wide range of .NET types:

### Primitive Types

```csharp
// All .NET primitives
bool, byte, sbyte, char, short, ushort, int, uint, long, ulong, float, double, decimal

// Date and time
DateTime, DateTimeOffset, TimeSpan, DateOnly, TimeOnly

// Special types
Guid, string, byte[]
```

### Collections

```csharp
// Arrays
int[], string[], User[]

// Lists
List<T>, IList<T>, IReadOnlyList<T>

// Sets
HashSet<T>, ISet<T>, IReadOnlySet<T>

// Dictionaries
Dictionary<TKey, TValue>, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>

// Enumerables
IEnumerable<T>, ICollection<T>
```

### Complex Types

```csharp
// Classes
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Address Address { get; set; }
}

// Nested objects
public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
}

// Nullable types
int?, DateTime?, User?
```

### Records

```csharp
// C# records
public record CreateUserCommand(string Email, string Name);

var command = new CreateUserCommand("user@example.com", "John");
var bytes = ByteSerializer.Serialize(command);
```

## Usage Examples

### Basic Serialization

```csharp
using Zerra.Serialization.Bytes;

// Serialize command
var command = new CreateUserCommand 
{ 
    Email = "user@example.com", 
    Name = "John Doe" 
};

byte[] bytes = ByteSerializer.Serialize(command);
Log?.Info($"Serialized {bytes.Length} bytes");

// Deserialize command
var result = ByteSerializer.Deserialize<CreateUserCommand>(bytes);
Log?.Info($"Deserialized: {result.Email}");
```

### Using with CQRS Bus

```csharp
using Zerra.Serialization;

// Wrap ByteSerializer for Bus usage
var serializer = new ZerraByteSerializer();
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, logger);
bus.AddCommandConsumer<IUserCommandHandler>(server);

// Client side
var clientSerializer = new ZerraByteSerializer();
var client = new TcpCqrsClient("localhost:9001", clientSerializer, encryptor, logger);
bus.AddCommandProducer<IUserCommandHandler>(client);
```

### File Serialization

### File Serialization

```csharp
using Zerra.Serialization.Bytes;

// Write to file
using (var stream = File.Create("users.bin"))
{
    foreach (var user in users)
    {
        ByteSerializer.Serialize(stream, user);
    }
}

// Read from file
var loadedUsers = new List<User>();
using (var stream = File.OpenRead("users.bin"))
{
    while (stream.Position < stream.Length)
    {
        var user = ByteSerializer.Deserialize<User>(stream);
        loadedUsers.Add(user);
    }
}
```

## Best Practices

## Best Practices

### 1. Use for High-Performance Scenarios

```csharp
using Zerra.Serialization;

// Ideal for high-performance production systems
var serializer = new ZerraByteSerializer();
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, logger);
```

### 2. Match Serializers on Client and Server

```csharp
// ✅ Both client and server must use same serializer
// Server
var serverSerializer = new ZerraByteSerializer();

// Client
var clientSerializer = new ZerraByteSerializer();
```

### 3. Use Source Generation for AOT

```csharp
// Add to .csproj for AOT support:
// <ProjectReference Include="Zerra.SourceGeneration.csproj" 
//                   OutputItemType="Analyzer" 
//                   ReferenceOutputAssembly="false" />

// All serialization uses generated code - no reflection
var bytes = ByteSerializer.Serialize(command);
```

### 4. Leverage Stream Support for Large Objects

```csharp
using Zerra.Serialization.Bytes;

// ✅ Stream large objects to avoid memory buffering
using var stream = File.OpenRead("large-data.bin");
var result = ByteSerializer.Deserialize<LargeDataSet>(stream);
```

### 5. Design Serializable Types

```csharp
// ✅ Good - simple properties
public class UserCommand : ICommand
{
    public required string Email { get; set; }
    public required string Name { get; set; }
}

// ❌ Avoid - complex constructors, private setters without init
public class ComplexCommand : ICommand
{
    private string _email;
    public string Email => _email;

    public ComplexCommand(string email)
    {
        _email = email;
    }
}
```

## Limitations

1. **Binary Format Only**: Not human-readable; use [JsonSerializer](JsonSerializer.md) for debugging
2. **.NET Specific**: Both client and server must be .NET applications
3. **Type Compatibility**: Sender and receiver must have matching type definitions
4. **No Versioning Support**: Adding/removing properties breaks compatibility (design for forward compatibility)

## When to Use ByteSerializer

Choose `ByteSerializer` (via `ZerraByteSerializer` wrapper for Bus) when:

- ✅ **Performance is critical** - High-throughput systems, real-time applications
- ✅ **Bandwidth matters** - Cloud deployments with data egress costs
- ✅ **Both sides are .NET** - Microservices, distributed systems
- ✅ **Message volume is high** - Processing thousands/millions of messages
- ✅ **AOT compilation** - Native AOT deployment scenarios
- ✅ **Production workloads** - Stable, high-performance requirements

See [Serializers Overview](Serializers.md) for comparison with other serialization options.

## Troubleshooting

### Deserialization Fails

**Problem**: `Deserialize` throws exception

**Solutions**:
- Ensure both client and server use same binary serializer
- Verify type definitions match exactly between sender and receiver
- Check that [Zerra.SourceGeneration](AOT.md) is referenced in both projects
- Ensure encryption keys match if using encryption

### Poor Performance

**Problem**: Serialization is slower than expected

**Solutions**:
- Verify [source generation](AOT.md) is configured correctly
- Check for excessive object allocations in your models
- Use stream-based serialization for large objects
- Profile to identify bottlenecks in your domain objects

### Type Not Supported

**Problem**: Custom type fails to serialize

**Solutions**:
- Ensure type has a parameterless constructor
- Use public properties with getters and setters
- Reference [Zerra.SourceGeneration](AOT.md) to generate type metadata
- Simplify complex type hierarchies

## See Also

- [JsonSerializer](JsonSerializer.md) - JSON serialization with Graph-based property control
- [Serializers Overview](Serializers.md) - Compare serialization options
- [AOT Support](AOT.md) - Configure source generation for Native AOT
- [Server Setup](ServerSetup.md) - Configure server-side serialization
- [Client Setup](ClientSetup.md) - Configure client-side serialization
