[← Back to Documentation](Index.md)

# JsonSerializer

`JsonSerializer` is Zerra's JSON serialization implementation featuring **Graph-based selective property control** as its primary capability.

> **Note**: When using the Zerra CQRS Bus, use the `ZerraJsonSerializer` wrapper class which delegates to `JsonSerializer`.

## Overview

`JsonSerializer` provides:
- **🎯 Graph-based property selection** - Control exactly which properties to serialize/deserialize (main feature)
- **PATCH operation support** - Track which properties were modified during deserialization  
- **Type-safe Graph<T>** - Expression-based property selection with compile-time checking
- **Nested property control** - Select properties from child objects and collections
- **Configurable** - Extensive options via `JsonSerializerOptions`
- **Nameless mode** - Compact JSON arrays for browser endpoints
- **Custom converters** - Extensible type conversion system

## Key Features

### 1. Graph-Based Property Selection (Main Feature)

The `Graph` object is Zerra's most powerful JSON serialization feature, allowing precise control over which properties are included or excluded during serialization and deserialization.

#### Basic Property Selection

```csharp
using Zerra;
using Zerra.Serialization.Json;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }  // Sensitive!
    public DateTime CreatedAt { get; set; }
}

// Serialize only specific properties using Graph
var graph = new Graph<User>(
    x => x.Id,
    x => x.Name,
    x => x.Email
    // PasswordHash intentionally excluded
);

var user = new User 
{ 
    Id = 123, 
    Name = "John", 
    Email = "john@example.com",
    PasswordHash = "secret",
    CreatedAt = DateTime.Now
};

string json = JsonSerializer.Serialize(user, graph: graph);
// Output: {"Id":123,"Name":"John","Email":"john@example.com"}
// PasswordHash and CreatedAt are NOT included
```

#### Include All Members Except...

```csharp
// Include all properties except sensitive ones
var graph = new Graph<User>(includeAllMembers: true);
graph.RemoveMember(x => x.PasswordHash);

string json = JsonSerializer.Serialize(user, graph: graph);
// Output includes: Id, Name, Email, CreatedAt
// Output excludes: PasswordHash
```

#### Nested Object Property Selection

```csharp
public class Order
{
    public int OrderId { get; set; }
    public User Customer { get; set; }
    public List<OrderItem> Items { get; set; }
    public decimal TotalAmount { get; set; }
}

public class OrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

// Select specific nested properties
var graph = new Graph<Order>(
    x => x.OrderId,
    x => x.Customer.Name,              // Only customer name
    x => x.Customer.Email,             // Only customer email
    x => x.Items.Select(i => i.ProductName),  // Only product names
    x => x.Items.Select(i => i.Quantity),     // Only quantities
    x => x.TotalAmount
);

var json = JsonSerializer.Serialize(order, graph: graph);
// Customer object only includes Name and Email (not Id, PasswordHash, CreatedAt)
// OrderItem objects only include ProductName and Quantity (not ProductId, Price)
```

### 2. PATCH Operations with Graph Tracking

Track which properties were actually present in JSON during deserialization - essential for HTTP PATCH operations:

```csharp
// HTTP PATCH endpoint - only update fields present in request
public async Task<IActionResult> PatchUser(int userId, [FromBody] JsonElement patchData)
{
    // Deserialize and track which properties were in the JSON
    var (userPatch, graph) = JsonSerializer.DeserializePatch<User>(patchData.GetRawText());

    // Get existing user
    var existingUser = await repository.GetByIdAsync(userId);

    // Update only properties that were in the PATCH request
    if (graph.HasMember(nameof(User.Name)))
        existingUser.Name = userPatch.Name;

    if (graph.HasMember(nameof(User.Email)))
        existingUser.Email = userPatch.Email;

    // CreatedAt and Id are never updated because they weren't in the graph

    await repository.UpdateAsync(existingUser);
    return Ok();
}

// Client sends: {"Email":"newemail@example.com"}
// Only Email is updated, Name and other fields remain unchanged
```

#### Generic PATCH Handler

```csharp
public static void ApplyPatch<T>(T existingObject, T patchObject, Graph graph)
{
    var properties = typeof(T).GetProperties();

    foreach (var prop in properties)
    {
        // Only update properties that were in the JSON
        if (graph.HasMember(prop.Name))
        {
            var newValue = prop.GetValue(patchObject);
            prop.SetValue(existingObject, newValue);
        }
    }
}

// Usage
var (userPatch, graph) = JsonSerializer.DeserializePatch<User>(jsonString);
ApplyPatch(existingUser, userPatch, graph);
```

### 3. Nameless JSON Mode

Compact JSON arrays for browser endpoints with JavaScript decoders:

```csharp
using Zerra.Serialization.Json;

var options = new JsonSerializerOptions
{
    Nameless = true  // Zerra extension
};

var user = new User 
{ 
    Id = 123, 
    Name = "John", 
    Email = "john@example.com" 
};

string json = JsonSerializer.Serialize(user, options: options);

// Standard JSON:
// {"Id":123,"Name":"John","Email":"john@example.com"}

// Nameless JSON (compact array):
// [123,"John","john@example.com"]
```

**Trade-offs:**
- ✅ Smaller payload size (no property names)
- ✅ Reduced bandwidth for browser APIs  
- ❌ Requires client-side knowledge of property order
- ❌ Not self-describing
- ❌ Deserialization issues if used with Graph (deserializer can't determine property mappings without names)

> **Note**: See [Zerra.Web](ZerraWeb.md) for JavaScript decoder scripts that can decode nameless JSON arrays in browser applications.

### 4. Custom Converters

Extend `JsonSerializer` with custom type conversion:

```csharp
using Zerra.Serialization.Json;
using Zerra.Serialization.Json.Converters;

public class CustomDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref JsonReader reader)
    {
        // Custom deserialization logic
        var str = reader.ReadString();
        return DateTime.ParseExact(str, "yyyy-MM-dd", null);
    }

    public override void Write(ref JsonWriter writer, DateTime value)
    {
        // Custom serialization logic
        writer.Write(value.ToString("yyyy-MM-dd"));
    }
}

// Register converter before first use
JsonSerializer.AddConverter(typeof(DateTime), () => new CustomDateTimeConverter());
```

## Configuration Options

### JsonSerializerOptions

`JsonSerializer` accepts optional configuration:

```csharp
using Zerra.Serialization.Json;

var options = new JsonSerializerOptions
{
    WriteIndented = true,              // Pretty-print JSON  
    Nameless = true,                   // Compact array mode
    PropertyNameCaseInsensitive = true // Case-insensitive deserialization
};

string json = JsonSerializer.Serialize(obj, options: options);
```

### For CQRS Bus Usage

When using with the Zerra CQRS Bus, wrap with `ZerraJsonSerializer`:

```csharp
using Zerra.Serialization;
using Zerra.Serialization.Json;

var options = new JsonSerializerOptions
{
    WriteIndented = false  // Compact for production
};

var serializer = new ZerraJsonSerializer(options);
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, logger);
```

## Usage Examples

### Basic Serialization

```csharp
using Zerra.Serialization.Json;

// Serialize to JSON string
var command = new CreateUserCommand 
{ 
    Email = "user@example.com", 
    Name = "John Doe" 
};

string json = JsonSerializer.Serialize(command);
Log?.Info($"JSON: {json}");

// Deserialize from JSON string
var result = JsonSerializer.Deserialize<CreateUserCommand>(json);
Log?.Info($"Deserialized: {result.Email}");
```

### Serialization with Graph (Selective Properties)

```csharp
using Zerra;
using Zerra.Serialization.Json;

// API returns only safe properties
var user = await repository.GetByIdAsync(userId);

var graph = new Graph<User>(
    x => x.Id,
    x => x.Name,
    x => x.Email,
    x => x.CreatedAt
    // PasswordHash, SecurityStamp, etc. excluded
);

string json = JsonSerializer.Serialize(user, graph: graph);

// Only specified properties are in JSON
// {"Id":123,"Name":"John","Email":"john@example.com","CreatedAt":"2024-01-01T00:00:00Z"}
```

### Using with CQRS Bus

```csharp
using Zerra.Serialization;
using Zerra.Serialization.Json;

// Wrap JsonSerializer for Bus usage
var options = new JsonSerializerOptions { WriteIndented = false };
var serializer = new ZerraJsonSerializer(options);
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, logger);
```

### HTTP API with PATCH Support

```csharp
using Zerra;
using Zerra.Serialization;
using Microsoft.AspNetCore.Mvc;

// ASP.NET API Controller with Graph-based PATCH
public class UsersController : ControllerBase
{
    private readonly IUserRepository _repository;
    private readonly JsonSerializer _jsonSerializer;

    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchUser(int id, [FromBody] JsonElement patchData)
    {
        // Deserialize and track which properties were in the request
        var jsonString = patchData.GetRawText();
        var (userPatch, graph) = JsonSerializer.DeserializePatch<User>(jsonString);

        // Load existing user
        var existingUser = await _repository.GetByIdAsync(id);
        if (existingUser == null)
            return NotFound();

        // Apply only properties that were in the PATCH request
        if (graph.HasMember(nameof(User.Name)))
            existingUser.Name = userPatch.Name;

        if (graph.HasMember(nameof(User.Email)))
            existingUser.Email = userPatch.Email;

        if (graph.HasMember(nameof(User.PhoneNumber)))
            existingUser.PhoneNumber = userPatch.PhoneNumber;

        // Properties not in graph are never touched
        // Id, PasswordHash, CreatedAt, etc. remain unchanged

        await _repository.UpdateAsync(existingUser);

        // Return updated user with safe properties only
        var responseGraph = new Graph<User>(
            x => x.Id,
            x => x.Name,
            x => x.Email,
            x => x.PhoneNumber,
            x => x.UpdatedAt
        );

        var responseJson = JsonSerializer.Serialize(existingUser, graph: responseGraph);
        return Ok(responseJson);
    }
}

// Client side - JavaScript PATCH request
// fetch('http://localhost:8080/api/users/123', {
//   method: 'PATCH',
//   body: JSON.stringify({ Email: "newemail@example.com" }),
//   headers: { 'Content-Type': 'application/json' }
// });
// Only Email is updated, other properties remain unchanged
```

## Supported Types

### Primitives

```csharp
// Numbers
int, long, float, double, decimal

// Text
string, char

// Boolean
bool

// Date/Time (ISO 8601 format)
DateTime, DateTimeOffset, TimeSpan, DateOnly, TimeOnly

// Identifiers
Guid (string format: "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx")
```

### Collections

```csharp
// Arrays
int[], string[], User[]

// Lists
List<T>, IList<T>, IReadOnlyList<T>

// Sets
HashSet<T>, ISet<T>

// Dictionaries
Dictionary<string, T>, IDictionary<string, T>

// Enumerables
IEnumerable<T>, ICollection<T>
```

### Complex Objects

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public Address Address { get; set; }
    public List<string> Roles { get; set; }
}

// Serializes to:
// {
//   "Id": 123,
//   "Name": "John",
//   "Email": "john@example.com",
//   "Address": { "Street": "...", "City": "..." },
//   "Roles": ["Admin", "User"]
// }
```

### Records

```csharp
public record CreateUserCommand(string Email, string Name);

// Serializes to:
// {"Email":"user@example.com","Name":"John"}
```

## Best Practices

### 1. Use Graph for Security

```csharp
using Zerra;
using Zerra.Serialization.Json;

// Always exclude sensitive properties from API responses
var safeGraph = new Graph<User>(
    x => x.Id,
    x => x.Name,
    x => x.Email
    // Never include: PasswordHash, SecurityStamp, etc.
);

string json = JsonSerializer.Serialize(user, graph: safeGraph);
```

### 2. Use DeserializePatch for HTTP PATCH

```csharp
// Track which properties were actually in the request
var (patch, graph) = JsonSerializer.DeserializePatch<User>(jsonString);

// Only update properties that were sent
if (graph.HasMember(nameof(User.Email)))
    existingUser.Email = patch.Email;

// Other properties remain unchanged
```

### 3. Use IncludeAllMembers When Appropriate

```csharp
// Easier than listing many properties
var graph = new Graph<User>(includeAllMembers: true);
graph.RemoveMember(x => x.PasswordHash);
graph.RemoveMember(x => x.SecurityStamp);

string json = JsonSerializer.Serialize(user, graph: graph);
```

### 4. Match Serializers Client-Server

```csharp
// ✅ Both must use same serializer
// Server
var serverSerializer = new ZerraJsonSerializer();

// Client  
var clientSerializer = new ZerraJsonSerializer();
```

### 5. Configure for Environment

```csharp
// Production - compact
var prodOptions = new JsonSerializerOptions
{
    WriteIndented = false
};

// Development - readable
var devOptions = new JsonSerializerOptions
{
    WriteIndented = true
};
```

## When to Use JsonSerializer

## When to Use JsonSerializer

Choose `JsonSerializer` when you need:

- **🎯 Graph-based property selection** - Main feature for selective serialization
- **PATCH operations** - Track which properties were modified  
- **Security** - Exclude sensitive properties from responses
- **Nested property control** - Select specific child object properties
- **Type-safe property selection** - Use Graph<T> with expressions
- **JSON format** - Human-readable, debuggable, interoperable

**Key advantages:**
- Precise control over serialized properties
- Support for HTTP PATCH semantics
- Security by default (explicit property inclusion)
- Works with any JSON-compatible system

See [Serializers Overview](Serializers.md) for comparison with other serialization options.

## Troubleshooting

## Troubleshooting

### Deserialization Fails

**Problem**: JSON deserialization throws exception

**Solutions**:
- Enable case-insensitive deserialization: `PropertyNameCaseInsensitive = true`
- Verify JSON property names match .NET property names
- Ensure both client and server use `ZerraJsonSerializer`
- Check that type definitions match between sender and receiver

### Graph Not Working

**Problem**: Properties still appear/missing despite Graph configuration

**Solutions**:
- Verify you're passing the `graph:` parameter to Serialize
- Check property names in Graph match actual property names (case-sensitive)
- For nested properties, use correct syntax: `x => x.Parent.Child`
- Avoid using Graph with Nameless mode (deserializer can't map properties without names)

### PATCH Not Tracking Properties

**Problem**: `graph.HasMember` returns false for sent properties

**Solutions**:
- Use `DeserializePatch` method, not regular `Deserialize`
- Check property names with `nameof()` operator for correctness
- Verify JSON property names match .NET property names exactly

### Nameless Mode Issues

**Problem**: Client can't decode nameless JSON arrays

**Solutions**:
- Ensure client JavaScript knows the exact property order
- Document property order in API specification
- Test with sample payloads before deploying
- Don't use Graph with Nameless mode (property order must be fixed and known)

## See Also

- [Serializers Overview](Serializers.md) - Compare serialization options
- [Server Setup](ServerSetup.md) - Configure server-side serialization
- [Client Setup](ClientSetup.md) - Configure client-side serialization
