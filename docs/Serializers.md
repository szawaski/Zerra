[← Back to Documentation](Index.md)

# Serializers

Servers, clients, producers, and consumers turn messages into bytes with an `ISerializer`. Both ends of a connection must use the same serializer with the same options.

| Serializer | Format | Use for |
|---|---|---|
| `ZerraByteSerializer` | compact binary | service-to-service traffic, where both sides are .NET. The fastest and smallest. See [ByteSerializer](ByteSerializer.md) |
| `ZerraJsonSerializer` | JSON | browsers and other outside callers through the API gateway, and messages you want to read. Supports [graphs](Graph.md) and nameless JSON. See [JsonSerializer](JsonSerializer.md) |
| `SystemTextJsonSerializer` | JSON via System.Text.Json | when you need System.Text.Json's behavior, options, or converters |

All three are in `Zerra.Serialization`.

```csharp
using Zerra.Serialization;

ISerializer serializer = new ZerraByteSerializer();                                        // optional ByteSerializerOptions
ISerializer json = new ZerraJsonSerializer(new Zerra.Serialization.Json.JsonSerializerOptions { IgnoreCase = true });
ISerializer stj = new SystemTextJsonSerializer(new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, null, log);
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, null, log);
```

A serializer is passed to each server, client, producer, and consumer, not to `Bus.New`. A typical system uses `ZerraByteSerializer` between services and `ZerraJsonSerializer` at the API gateway.

`ZerraJsonSerializer` takes `Zerra.Serialization.Json.JsonSerializerOptions`, not System.Text.Json's type of the same name. With `Nameless = true`, its content type becomes `application/jsonnameless`.

## ISerializer

```csharp
public interface ISerializer
{
    ContentType ContentType { get; }

    byte[] SerializeBytes(object? obj);
    byte[] SerializeBytes(object? obj, Type type);
    byte[] SerializeBytes<T>(T? obj);
    object? Deserialize(ReadOnlySpan<byte> bytes, Type type);
    T? Deserialize<T>(ReadOnlySpan<byte> bytes);

    void Serialize(Stream stream, object? obj);
    void Serialize(Stream stream, object? obj, Type type);
    void Serialize<T>(Stream stream, T? obj);
    object? Deserialize(Stream stream, Type type);
    T? Deserialize<T>(Stream stream);

    Task SerializeAsync(Stream stream, object? obj, CancellationToken cancellationToken);
    Task SerializeAsync(Stream stream, object? obj, Type type, CancellationToken cancellationToken);
    Task SerializeAsync<T>(Stream stream, T? obj, CancellationToken cancellationToken);
    Task<object?> DeserializeAsync(Stream stream, Type type, CancellationToken cancellationToken);
    Task<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken);
}
```

## See Also

- [ByteSerializer](ByteSerializer.md) and [JsonSerializer](JsonSerializer.md) - Details of each
- [Encryptors](Encryptors.md) - Encrypting what the serializer produces
- [Compressors](Compressors.md) - Compressing what the serializer produces
