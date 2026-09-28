[← Back to Documentation](Index.md)

# Graph

A `Graph` selects which members of an object, and of its nested objects, an operation includes. It is used by:

- **[Repository](Repository.md)**: which relations to load, and which columns an update writes
- **[Mapper](Mapper.md)**: which members to map or copy
- **[JsonSerializer](JsonSerializer.md)**: which members to write

## Creating a Graph

`Graph<T>` names members with expressions. `Graph` names them with strings, for when the type isn't known at compile time.

```csharp
using Zerra;

// only these members
var graph = new Graph<User>(x => x.Id, x => x.Name, x => x.Email);

// every member except Password
var graph = new Graph<User>(includeAllMembers: true);
graph.RemoveMember(x => x.Password);

// string-based
var graph = new Graph("Id", "Name", "Email");

// nothing
var graph = new Graph<User>();
bool isEmpty = graph.IsEmpty; // true
```

The first constructor argument can also be `includeAllMembers`, followed by the members: `new Graph<Order>(true, x => x.Customer)` includes every member of `Order` plus its `Customer` relation.

## Nested Members

Reach into a nested object with a member chain, and into a collection's items with `Select`. Each expression names one member:

```csharp
var graph = new Graph<Order>(
    x => x.Id,
    x => x.Customer.Name,              // Customer, with only its Name
    x => x.Customer.Address.City,      // and its Address, with only City
    x => x.Items.Select(i => i.ProductId),
    x => x.Items.Select(i => i.Quantity)
);
```

Naming an object member without going into it, such as `x => x.Customer`, includes that member whole.

For a string-based graph, add a child graph for the nested member: `graph.AddChildGraph("Address", new Graph("City"))`.

## Changing a Graph

```csharp
var graph = new Graph<User>();
graph.AddMember(x => x.Id);
graph.AddMembers(x => x.Email, x => x.CreatedAt);
graph.RemoveMember(x => x.CreatedAt);
graph.RemoveMembers(x => x.Email, x => x.Id);

bool hasId = graph.HasMember(x => x.Id);                   // included, explicitly or by includeAllMembers
bool added = graph.HasMemberExplicitly(x => x.Password);   // explicitly added and not removed
IEnumerable<string> explicitMembers = graph.ExplicitMembers;
```

`HasAddedMembers` and `HasRemovedMembers` report whether any members were added or removed.

## Per-Instance Graphs

A graph can use a different graph for one specific object, for example to show an administrator more than other users:

```csharp
var graph = new Graph<User>(includeAllMembers: true);
graph.RemoveMember(x => x.Password);

graph.AddInstanceGraph(adminUser, new Graph<User>(includeAllMembers: true));
// adminUser uses the second graph, every other User the first
```

`RemoveInstanceGraph` takes it off again.

## Signatures

`Signature` is a string describing the graph's members, and two graphs with the same members have the same signature, so it works as a cache key. `Graph.ParseSignature` rebuilds a graph from one. Graphs also compare equal with `==` when their members are the same.

## Examples

```csharp
// Repository: load an order with its customer and lines
var orders = await Repo.ManyAsync<OrderDataModel>(new Graph<OrderDataModel>(true, x => x.Customer, x => x.Lines));

// Repository: update only the Status column
await Repo.UpdateAsync(order, new Graph<OrderDataModel>(x => x.Status));

// Mapper: copy only the public fields to a DTO
var dto = user.Map<UserDto>(new Graph<User>(x => x.Id, x => x.Name));

// Mapper: apply only Email and Name onto an existing object
updates.MapTo(existing, new Graph<User>(x => x.Email, x => x.Name));

// JSON: write only some members
string json = JsonSerializer.Serialize(user, graph: new Graph<User>(x => x.Id, x => x.Name));
```

## See Also

- [Repository](Repository.md) - Relations and partial updates
- [Mapper](Mapper.md) - Mapping with graphs
- [JsonSerializer](JsonSerializer.md) - JSON serialization with graphs
