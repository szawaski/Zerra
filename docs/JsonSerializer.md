[← Back to Documentation](Index.md)

# JsonSerializer

`JsonSerializer` (in `Zerra.Serialization.Json`) is Zerra's JSON serializer. Beyond ordinary JSON, it can write or read only the members a [Graph](Graph.md) selects, report which members a JSON document contained (for PATCH), and write compact nameless JSON.

To use it with the bus, a server, or a client, pass `ZerraJsonSerializer`, the `ISerializer` wrapper around it. See [Serializers](Serializers.md).

## Serializing

```csharp
using Zerra.Serialization.Json;

string json = JsonSerializer.Serialize(command);
var result = JsonSerializer.Deserialize<CreateUserCommand>(json);
```

There are overloads for `ReadOnlySpan<char>`, `ReadOnlySpan<byte>`, and `Stream`, async `Stream` versions, and non-generic versions taking a `Type`.

## Selecting Members with a Graph

Pass a [Graph](Graph.md) to write only some members, for example to keep sensitive ones out of a response:

```csharp
var graph = new Graph<User>(includeAllMembers: true);
graph.RemoveMember(x => x.PasswordHash);

string json = JsonSerializer.Serialize(user, graph: graph);

// nested members and collection items
var orderGraph = new Graph<Order>(
    x => x.OrderId,
    x => x.Customer.Name,
    x => x.Items.Select(i => i.ProductName)
);
```

`Deserialize` also takes a graph, and then reads only the members it includes.

## PATCH: Which Members Were Sent

`DeserializePatch` returns the object and a `Graph` of the members the JSON actually contained. Apply only those to the existing object with the [Mapper](Mapper.md):

```csharp
var (patch, sent) = JsonSerializer.DeserializePatch<User>(requestBody);

// client sent {"Email":"new@example.com"}: only Email is copied
patch.MapTo(existingUser, sent);

bool emailSent = sent.HasMember(nameof(User.Email));
```

## Options

```csharp
var options = new JsonSerializerOptions
{
    Nameless = false,                    // write arrays of values instead of named properties
    DoNotWriteNullProperties = true,     // omit properties whose value is null
    DoNotWriteDefaultProperties = false, // omit properties whose value is the type's default
    EnumAsNumber = false,                // write enums as numbers instead of names
    ErrorOnTypeMismatch = false,         // throw when a JSON value doesn't match the target type
    IgnoreCase = true                    // match property names case-insensitively (slower)
};

string json = JsonSerializer.Serialize(obj, options: options);
var serializer = new ZerraJsonSerializer(options); // the same options for the bus
```

Output is always compact; there is no indented option.

### Mismatched Values

Each type has an expected kind of JSON value: a number for numbers, `true` or `false` for `bool`, and a string for strings, `char`, dates, times, and GUIDs. Enums accept a name or a number.

- **Invalid content always throws.** A value of the expected kind that isn't valid for the type, such as `1.5` for an `int`, `300` for a `byte`, or `"tomorrow"` for a `DateTime`, throws whatever the options.
- **Another kind that converts is read.** For example, `"5"` reads into an `int` and `"true"` into a `bool`. Numbers and `true`/`false` read into a `string` as text when `ErrorOnTypeMismatch` is off.
- **Any other kind is a mismatch.** With `ErrorOnTypeMismatch = false` (the default) it becomes the type's default, `null` for nullable and reference types. With `ErrorOnTypeMismatch = true` it throws. `null` is a mismatch only for non-nullable value types.

Anything after the value other than whitespace throws in both modes.

Per member, use `[JsonPropertyName("name")]`, and `[JsonIgnore]` with a `JsonIgnoreCondition` of `Always`, `WhenReading`, `WhenWriting`, `WhenWritingDefault`, or `WhenWritingNull`, both from `Zerra.Serialization.Json`.

## Nameless JSON

With `Nameless = true`, objects are written as arrays of values in member order, without names:

```text
standard:  {"Id":123,"Name":"John","Email":"john@example.com"}
nameless:  [123,"John","john@example.com"]
```

Payloads are smaller, but the reader has to know the member order, and nameless JSON can't be combined with a graph. The [Front End Scripts](FrontEndScripts.md) request nameless responses from the API gateway and decode them with the generated model types.

## Supported Types

- Numbers, `bool`, `string`, `char`, `Guid`, and enums (as names, or numbers with `EnumAsNumber`)
- `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, and `TimeOnly`, as ISO 8601 strings
- Arrays, lists, sets, dictionaries, and their interfaces such as `IReadOnlyList<T>` and `IEnumerable<T>`
- Classes, structs, and records, including nested objects

Numbers and dates don't depend on the machine's culture.

Dictionaries are JSON objects when the keys are strings, numbers, `bool`, `char`, `Guid`, dates, or enums, like System.Text.Json. Other keys, such as objects, are written as an array of `{"Key":…,"Value":…}` pairs.

## Custom Converters

Derive from `JsonConverter<T>` and register it before first use. Converters are resumable: the reader and writer work on buffered segments, so each method returns `true` when it completed and `false` when it needs more data (for writes, set `state.SizeNeeded`). A value that fits in one token, like the one below, can set `StackRequired => false` and complete in one call.

```csharp
using System.Globalization;
using System.Text;
using Zerra.Serialization.Json;
using Zerra.Serialization.Json.Converters;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

public sealed class DateOnlyStringConverter : JsonConverter<DateTime>
{
    protected override bool StackRequired => false;

    protected override bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out DateTime value)
    {
        if (token != JsonToken.String)
        {
            value = default;
            return true;
        }

        var text = reader.UseBytes ? Encoding.UTF8.GetString(reader.ValueBytes) : reader.ValueChars.ToString();
        value = DateTime.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return true;
    }

    protected override bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in DateTime value)
        => writer.TryWriteQuoted(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), out state.SizeNeeded);
}

JsonSerializer.AddConverter(typeof(DateTime), () => new DateOnlyStringConverter());
```

The built-in converters under `Zerra.Serialization.Json.Converters` show objects, collections, and other multi-token values.

## Troubleshooting

- **Properties aren't set on deserialize:** check the names match, or set `IgnoreCase = true`, and that the sender and receiver use the same types and options.
- **A graph has no effect:** check it is passed as `graph:` and that nested members use `x => x.Parent.Child` or `x => x.Items.Select(i => i.Child)`.
- **PATCH reports no members:** use `DeserializePatch`, not `Deserialize`.

## See Also

- [Serializers](Serializers.md) - Choosing a serializer for the bus
- [Graph](Graph.md) - Selecting members
- [ByteSerializer](ByteSerializer.md) - Binary serialization
