[← Back to Documentation](Index.md)

# ByteSerializer

`ByteSerializer` (in `Zerra.Serialization.Bytes`) is Zerra's binary serializer: the fastest and most compact option, for traffic where both sides are .NET. Member names aren't written, integers are variable-length, and type details come from the [source generator](AOT.md), so it works under Native AOT.

To use it with the bus, a server, or a client, pass `ZerraByteSerializer`, the `ISerializer` wrapper around it. See [Serializers](Serializers.md).

## Serializing

```csharp
using Zerra.Serialization.Bytes;

byte[] bytes = ByteSerializer.Serialize(command);
var result = ByteSerializer.Deserialize<CreateUserCommand>(bytes);

// by runtime type
byte[] bytes2 = ByteSerializer.Serialize(command, typeof(CreateUserCommand));
object? result2 = ByteSerializer.Deserialize(bytes2, typeof(CreateUserCommand));

// streams, without buffering the whole payload
ByteSerializer.Serialize(fileStream, command);
var fromFile = ByteSerializer.Deserialize<CreateUserCommand>(readStream);
await ByteSerializer.SerializeAsync(stream, command, cancellationToken: cancellationToken);
```

## Supported Types

- Numbers, `bool`, `char`, `string`, `Guid`, `byte[]`, enums, and their nullable forms
- `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, and `TimeOnly`
- Arrays, lists, sets, dictionaries, and their interfaces such as `IReadOnlyList<T>`, `IReadOnlySet<T>`, and `IEnumerable<T>`
- Classes, structs, and records, including nested objects

A type is created with its parameterless constructor if it has one. Otherwise the constructor whose parameter names match its member names is used, which is how positional records work. A read-only member that isn't a constructor parameter isn't deserialized.

## Options

```csharp
var options = new ByteSerializerOptions
{
    IndexType = ByteSerializerIndexType.Byte, // Byte (default, up to 254 members), UInt16 (up to 65,534), or MemberNames
    UseTypes = false,                         // write type information, needed for members typed as object or an interface
    IgnoreIndexAttribute = false              // ignore [SerializerIndex]
};

byte[] bytes = ByteSerializer.Serialize(command, options);
var serializer = new ZerraByteSerializer(options);
```

The same options must be used to serialize and deserialize.

## Versioning

By default members are identified by their declaration order, so adding, removing, or reordering members breaks compatibility with data serialized before the change. Two ways to make a type version-tolerant:

- **`[SerializerIndex]`**: give each member a stable, unique index. Once any member of a type uses it, only attributed members are serialized.
- **`ByteSerializerIndexType.MemberNames`**: identify members by name. It is the most flexible, but also the slowest and largest.

```csharp
public class CreateUserCommand : ICommand
{
    [SerializerIndex(1)] public required string Email { get; set; }
    [SerializerIndex(2)] public required string Name { get; set; }
    [SerializerIndex(3)] public string? PhoneNumber { get; set; }   // added later without breaking existing data
}
```

When services deploy independently, the sender and receiver can briefly run different versions of a contract, so give contracts that change `[SerializerIndex]` values.

An index tolerates data from an **older** version: members it doesn't contain keep their defaults. Data from a **newer** version, with an index the type doesn't have, throws, because without names or types the reader can't tell how long the unknown value is. So deploy the receiving service before the senders that add a member. With `MemberNames` or `UseTypes = true`, unknown members are skipped and either order works.

## Custom Converters

Derive from `ByteConverter<T>` (in `Zerra.Serialization.Bytes.Converters`) and register it before first use with `ByteSerializer.AddConverter(typeof(T), () => new MyConverter())`.

## Troubleshooting

- **Deserialization fails or produces wrong values:** check both sides use the same options and the same type definitions, or version the type with `[SerializerIndex]`. When encryption is used, also check both sides use the same key.
- **A type isn't supported:** give it a parameterless constructor, or one whose parameter names match its members, and use public properties. For members typed as `object` or an interface, set `UseTypes`.
- **A type fails under Native AOT:** check the project declaring it references the `Zerra` package, or mark it `[GenerateTypeDetail]`. See [AOT](AOT.md).

## See Also

- [Serializers](Serializers.md) - Choosing a serializer
- [JsonSerializer](JsonSerializer.md) - JSON serialization
