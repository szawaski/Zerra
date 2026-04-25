[← Back to Documentation](Index.md)

# Graph

Graph is a powerful member selection and mapping tool that allows you to specify which properties and nested properties of an object should be included or excluded in various operations such as serialization, mapping, and API responses.

## Overview

The `Graph` and `Graph<T>` classes provide:
- **Selective member inclusion/exclusion** - Control which properties are processed
- **Nested member mapping** - Define graphs for child objects
- **Instance-specific graphs** - Different graphs for different object instances
- **Type-safe expressions** - Use lambda expressions with `Graph<T>` for compile-time safety
- **Signature comparison** - Unique signatures for comparing graph configurations

## Basic Usage

### Generic Type-Safe Graph

```csharp
using Zerra;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public DateTime CreatedAt { get; set; }
    public Address Address { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
}

// Include specific members only
var graph = new Graph<User>(
    x => x.Id,
    x => x.Name,
    x => x.Email
);

// Include all members except specified ones
var graph = new Graph<User>(includeAllMembers: true);
graph.RemoveMember(x => x.Password);
```

### Non-Generic String-Based Graph

```csharp
// For dynamic scenarios where type isn't known at compile time
var graph = new Graph(
    "Id",
    "Name", 
    "Email"
);

// Include all members
var graph = new Graph(includeAllMembers: true);
graph.RemoveMember("Password");
```

## Creating Graphs

### Include Specific Members

```csharp
// Only include Id and Name
var graph = new Graph<User>(
    x => x.Id,
    x => x.Name
);
```

### Include All Members

```csharp
// Include all members
var graph = new Graph<User>(includeAllMembers: true);

// Include all except Password
var graph = new Graph<User>(includeAllMembers: true);
graph.RemoveMember(x => x.Password);
```

### Empty Graph

```csharp
// No members included
var graph = new Graph<User>();

bool isEmpty = graph.IsEmpty; // true
```

## Nested Object Graphs

### Child Graphs

```csharp
// Include user properties and specific address properties
var graph = new Graph<User>(
    x => x.Id,
    x => x.Name,
    x => x.Address.Graph(
        a => a.City,
        a => a.State
    )
);

// User.Id, User.Name, User.Address.City, User.Address.State will be included
```

### Multiple Levels

```csharp
public class Order
{
    public int Id { get; set; }
    public User Customer { get; set; }
    public List<OrderItem> Items { get; set; }
}

var graph = new Graph<Order>(
    x => x.Id,
    x => x.Customer.Select(
        u => u.Id,
        u => u.Name,
        u => u.Address.Select(
            a => a.City,
            a => a.State
        )
    ),
    x => x.Items.Select(
        i => i.ProductId,
        i => i.Quantity
    )
);
```

## Adding and Removing Members

### Add Members

```csharp
var graph = new Graph<User>();

// Add individual members
graph.AddMember(x => x.Id);
graph.AddMember(x => x.Name);

// Add multiple members
graph.AddMembers(
    x => x.Email,
    x => x.CreatedAt
);
```

### Remove Members

```csharp
var graph = new Graph<User>(includeAllMembers: true);

// Remove individual member
graph.RemoveMember(x => x.Password);

// Remove multiple members
graph.RemoveMembers(
    x => x.CreatedAt,
    x => x.UpdatedAt
);
```

### Check Members

```csharp
bool hasId = graph.HasMember(x => x.Id);
bool hasPassword = graph.HasRemovedMember(x => x.Password);

bool hasAnyAdded = graph.HasAddedMembers;
bool hasAnyRemoved = graph.HasRemovedMembers;

// Get explicitly added members
IEnumerable<string> explicitMembers = graph.ExplicitMembers;
```

## Instance-Specific Graphs

Different graph configurations for specific object instances:

```csharp
var adminUser = new User { Id = 1, Role = "Admin" };
var regularUser = new User { Id = 2, Role = "User" };

// Default graph excludes password
var defaultGraph = new Graph<User>(includeAllMembers: true);
defaultGraph.RemoveMember(x => x.Password);

// Admin graph includes everything
var adminGraph = new Graph<User>(includeAllMembers: true);

// Apply instance-specific graph
defaultGraph.AddInstanceGraph(adminUser, adminGraph);

// Now adminUser will use adminGraph, others use defaultGraph
```

## Graph Signatures

Each graph has a unique signature for comparison:

```csharp
var graph1 = new Graph<User>(x => x.Id, x => x.Name);
var graph2 = new Graph<User>(x => x.Id, x => x.Name);
var graph3 = new Graph<User>(x => x.Id, x => x.Email);

string sig1 = graph1.Signature;
string sig2 = graph2.Signature;
string sig3 = graph3.Signature;

bool same = sig1 == sig2;      // true - same members
bool different = sig1 == sig3; // false - different members
```

## Common Use Cases

### API Response Filtering

```csharp
public class UserController
{
    public IActionResult GetUser(int id)
    {
        var user = userRepository.GetById(id);

        // Public API - exclude sensitive data
        var graph = new Graph<User>(includeAllMembers: true);
        graph.RemoveMembers(
            x => x.Password,
            x => x.SecurityQuestion,
            x => x.InternalNotes
        );

        return Ok(Mapper.Map(user, graph));
    }
}
```

### Serialization Control

```csharp
// Only serialize specific fields
var graph = new Graph<User>(
    x => x.Id,
    x => x.Name,
    x => x.Email
);

var json = JsonSerializer.Serialize(user, graph);
```

### Partial Updates

```csharp
public void UpdateUser(int id, User updates, Graph<User> updateGraph)
{
    var existing = userRepository.GetById(id);

    // Only update properties included in the graph
    Mapper.Map(updates, existing, updateGraph);

    userRepository.Save(existing);
}

// Usage: only update email and name
var updateGraph = new Graph<User>(
    x => x.Email,
    x => x.Name
);
UpdateUser(1, userUpdates, updateGraph);
```

### Collection Projection

```csharp
// Define what to include for list items
var listGraph = new Graph<User>(
    x => x.Id,
    x => x.Name,
    x => x.Email,
    x => x.CreatedAt
);

var users = userRepository.GetAll();
var projectedList = users.Select(u => Mapper.Map(u, listGraph));
```

## Integration with Zerra Features

### With Mapper

```csharp
var graph = new Graph<User>(
    x => x.Id,
    x => x.Name
);

// Map only specified members
var dto = Mapper.Map<UserDto>(user, graph);
```

### With Serialization

```csharp
var graph = new Graph<User>(includeAllMembers: true);
graph.RemoveMember(x => x.Password);

// Serialize with graph control
var bytes = ZerraByteSerializer.Serialize(user, graph);
var json = ZerraJsonSerializer.Serialize(user, graph);
```

## Best Practices

1. **Use Generic Graphs** - Prefer `Graph<T>` over `Graph` for compile-time safety
2. **Define Reusable Graphs** - Create static graph definitions for common scenarios
3. **Document Graphs** - Comment why certain members are included/excluded
4. **Consider Security** - Always exclude sensitive data in public APIs
5. **Test Instance Graphs** - Verify instance-specific graphs work as expected

## See Also

- [Mapper](Mapper.md) - Object mapping with graph support
- [Serializers](Serializers.md) - Serialization with graph control
- [Zerra.Web](ZerraWeb.md) - API response filtering with graphs
