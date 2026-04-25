[← Back to Documentation](README.md)

# Mapper

Zerra includes a powerful **object mapping** feature that provides fast, flexible conversion between different types. The Mapper supports automatic property matching, custom transformations, graph-based selective mapping, and deep copying.

## Overview

The Mapper handles:
- ✅ **Automatic property mapping** by name
- ✅ **Type conversion** (int ↔ double, string ↔ DateTime, etc.)
- ✅ **Collection transformations** (array ↔ List ↔ IEnumerable, etc.)
- ✅ **Dictionary mapping** with key/value conversions
- ✅ **Deep copying** of complex object graphs
- ✅ **Custom property mappings** via `IMapDefinition<TSource, TTarget>`
- ✅ **Graph-based selective mapping** to include/exclude properties
- ✅ **AOT-compatible** when using `IMapDefinition` for custom mappings

### Registration: Source Generation Handles Everything

**In most cases, you don't need to manually register anything!** Zerra's source generator automatically discovers and registers all `IMapDefinition<TSource, TTarget>` implementations at compile time.

| Scenario | What You Need To Do |
|----------|---------------------|
| **Native AOT or Regular .NET** | ✅ **Just implement `IMapDefinition`** - source generator automatically registers it |
| **Source Generation Not Available** | ⚠️ Fallback: Use `MapDiscovery.Initialize()` or `MapDefinition.Register()` |

> **💡 Quick Rule:** Just implement `IMapDefinition<TSource, TTarget>` and the source generator handles registration automatically. You only need fallback options if source generation isn't working.

## Quick Start

### Basic Mapping

```csharp
using Zerra.Map;

// Map to a new target instance
var source = new PersonDto { Name = "John", Age = 30 };
var target = source.Map<PersonModel>();

// Map between explicitly typed objects
var target2 = source.Map<PersonDto, PersonModel>();

// Map to an existing instance
var existing = new PersonModel();
source.MapTo(existing);
```

### Deep Copy

```csharp
// Create a deep copy of an object
var original = new ComplexModel { /* ... */ };
var copy = original.Copy();

// Copy using runtime type
object obj = GetComplexObject();
var objCopy = obj.CopyObject();
```

## Automatic Property Mapping

By default, the Mapper matches properties by name and automatically converts between compatible types.

### Example: Simple Types

```csharp
public class Source
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class Target
{
    public int Id { get; set; }           // Same name → auto-mapped
    public string Name { get; set; }      // Same name → auto-mapped
    public DateTime CreatedDate { get; set; } // Same name → auto-mapped
    public string ExtraField { get; set; } // No match → left as default
}

var source = new Source { Id = 1, Name = "Test", CreatedDate = DateTime.Now };
var target = source.Map<Target>();
// Result: Id=1, Name="Test", CreatedDate=<value>, ExtraField=null
```

### Type Conversion

The Mapper automatically converts between compatible types:

```csharp
public class Source
{
    public int Age { get; set; }
    public decimal Price { get; set; }
}

public class Target
{
    public double Age { get; set; }  // int → double
    public int Price { get; set; }   // decimal → int
}

var source = new Source { Age = 30, Price = 99.99m };
var target = source.Map<Target>();
// Result: Age=30.0, Price=99
```

### Collection Transformations

The Mapper handles various collection types automatically:

```csharp
public class Source
{
    public int[] ArrayToList { get; set; }
    public List<int> ListToArray { get; set; }
    public HashSet<int> SetToList { get; set; }
    public IEnumerable<int> EnumerableToArray { get; set; }
}

public class Target
{
    public List<int> ArrayToList { get; set; }        // array → List
    public int[] ListToArray { get; set; }            // List → array
    public List<int> SetToList { get; set; }          // HashSet → List
    public int[] EnumerableToArray { get; set; }      // IEnumerable → array
}

var source = new Source
{
    ArrayToList = new[] { 1, 2, 3 },
    ListToArray = new List<int> { 4, 5, 6 },
    SetToList = new HashSet<int> { 7, 8, 9 },
    EnumerableToArray = new List<int> { 10, 11, 12 }
};

var target = source.Map<Target>();
// All collections are properly converted
```

### Dictionary Mapping

Dictionaries are mapped with both key and value conversions:

```csharp
public class Source
{
    public Dictionary<string, int> Scores { get; set; }
}

public class Target
{
    public Dictionary<string, double> Scores { get; set; }  // Values converted int → double
}

var source = new Source
{
    Scores = new Dictionary<string, int>
    {
        { "Math", 95 },
        { "Science", 88 }
    }
};

var target = source.Map<Target>();
// Result: Scores = { "Math": 95.0, "Science": 88.0 }
```

## Custom Mappings with IMapDefinition

For complex scenarios, implement `IMapDefinition<TSource, TTarget>` to define custom property mappings.

> **📋 How It Works:**
> - **Just implement `IMapDefinition`** - source generator automatically discovers and registers it
> - No manual registration code needed in most cases
> - Fallback options available if source generation doesn't work
> 
> See [Registration Details](#registering-map-definitions) below.

> **⚠️ Custom Mapping Requirement:** The source generator can only auto-register custom mappings if they're defined in an `IMapDefinition<TSource, TTarget>` implementation. Without this interface, custom mapping logic cannot be discovered at compile time.

### Example: Custom Map Definition

```csharp
using Zerra.Map;

public class PersonDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int BirthYear { get; set; }
}

public class PersonModel
{
    public string FullName { get; set; }
    public int Age { get; set; }
}

// Define custom mapping between PersonDto and PersonModel
public class PersonDtoToModelMap : IMapDefinition<PersonDto, PersonModel>
{
    public void Define(IMapSetup<PersonDto, PersonModel> map)
    {
        // Map FullName from FirstName + LastName
        map.Define(
            target => target.FullName,
            source => $"{source.FirstName} {source.LastName}"
        );

        // Map Age from BirthYear
        map.Define(
            target => target.Age,
            source => DateTime.Now.Year - source.BirthYear
        );

        // Define reverse mapping: FullName → FirstName (LastName will be empty)
        map.DefineReverse(
            source => source.FirstName,
            target => target.FullName?.Split(' ').FirstOrDefault() ?? ""
        );
    }
}
```

### Registering Map Definitions

**The source generator automatically handles registration in nearly all scenarios.** You only need fallback options if source generation isn't available.

#### Default: Automatic Registration (Recommended)

Zerra's **source generator automatically discovers and registers** all `IMapDefinition<TSource, TTarget>` implementations at compile time.

```csharp
using Zerra.Map;

// Just implement IMapDefinition - source generator handles everything!
public class PersonDtoToModelMap : IMapDefinition<PersonDto, PersonModel>
{
    public void Define(IMapSetup<PersonDto, PersonModel> map)
    {
        map.Define(target => target.FullName, 
                   source => $"{source.FirstName} {source.LastName}");
    }
}

// In your code - it just works!
var model = dto.Map<PersonModel>(); // Map definition is already registered
```

**✅ No registration code required!** The source generator handles everything automatically for both AOT and non-AOT scenarios.

> **💡 How It Works:** The source generator scans your code at compile time, finds all `IMapDefinition<TSource, TTarget>` implementations, and generates registration code automatically.

#### Fallback Options (If Source Generation Unavailable)

If for some reason source generation isn't working in your environment:

**Option 1: Auto-Discovery via Reflection**

```csharp
using Zerra.Map;

// Program.cs or Startup.cs
MapDiscovery.Initialize(); // Uses reflection to find and register all IMapDefinition implementations
```

**⚠️ Note:** This uses reflection and won't work with Native AOT. Only use as a fallback.

**Option 2: Manual Registration**

```csharp
using Zerra.Map;

// Register each map definition explicitly
MapDefinition.Register(new PersonDtoToModelMap());
MapDefinition.Register(new OrderDtoToModelMap());
```

**When you might need fallback options:**
- Source generator is disabled or not working
- You're using an older build environment
- You need to dynamically register mappings at runtime

### Using Registered Definitions

Once registered, mappings work automatically:

```csharp
var dto = new PersonDto
{
    FirstName = "John",
    LastName = "Doe",
    BirthYear = 1990
};

// Forward mapping (PersonDto → PersonModel)
var model = dto.Map<PersonModel>();
// Result: FullName = "John Doe", Age = 34 (calculated)

// Reverse mapping (PersonModel → PersonDto)
var dtoBack = model.Map<PersonDto>();
// Result: FirstName = "John", LastName = null, BirthYear = 0 (not mapped)
```

## IMapSetup Methods

The `IMapSetup<TSource, TTarget>` interface provides three methods for defining custom mappings:

### 1. Define (One-Way Forward)

Maps a target property from a source expression:

```csharp
map.Define(
    target => target.TargetProperty,
    source => source.SourceProperty
);
```

**Use when:**
- Transforming source data to target format
- Combining multiple source properties
- Performing calculations or conversions

### 2. DefineReverse (One-Way Backward)

Maps a source property from a target expression (reverse direction):

```csharp
map.DefineReverse(
    source => source.SourceProperty,
    target => target.TargetProperty
);
```

**Use when:**
- Reverse mapping requires different logic than forward mapping
- Only reverse direction needs custom logic

### 3. DefineTwoWay (Bidirectional)

Creates a symmetric two-way mapping between properties:

```csharp
map.DefineTwoWay(
    target => target.TargetProperty,
    source => source.SourceProperty
);
```

**Use when:**
- Properties map directly in both directions
- Same conversion logic applies forward and backward

### Example: All Three Methods

```csharp
public class DefineTwoWayExample : IMapDefinition<ModelA, ModelB>
{
    public void Define(IMapSetup<ModelA, ModelB> map)
    {
        // Forward only: Parse string to int, append "1"
        map.Define(
            b => b.PropB,
            a => Int32.Parse(a.PropA.ToString() + "1")
        );

        // Reverse only: Remove trailing "1" and parse
        map.DefineReverse(
            a => a.PropA,
            b => Int32.Parse(b.PropB.ToString().TrimEnd('1'))
        );

        // Bidirectional: PropD ↔ PropC (direct mapping both ways)
        map.DefineTwoWay(
            b => b.PropD,
            a => a.PropC
        );
    }
}
```

## Graph-Based Selective Mapping

Use `Graph` to selectively include or exclude properties during mapping:

```csharp
using Zerra;

public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
    public Address Address { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
}

var source = new Person
{
    Name = "Alice",
    Age = 25,
    Email = "alice@example.com",
    Address = new Address { Street = "123 Main St", City = "Springfield" }
};

// Map only Name and Email (exclude Age and Address)
var graph = new Graph(typeof(Person))
    .AddProperty(nameof(Person.Name))
    .AddProperty(nameof(Person.Email));

var target = source.Map<Person>(graph);
// Result: Name="Alice", Age=0, Email="alice@example.com", Address=null

// Map with nested properties
var graph2 = new Graph(typeof(Person))
    .AddProperty(nameof(Person.Name))
    .AddProperty(nameof(Person.Address), new Graph(typeof(Address))
        .AddProperty(nameof(Address.City))
    );

var target2 = source.Map<Person>(graph2);
// Result: Name="Alice", Age=0, Email=null, Address.City="Springfield", Address.Street=null
```

## Custom Type Converters

For specialized conversions, register custom converters:

```csharp
using Zerra.Map;
using Zerra.Map.Converters;

// Define a custom converter
public class CustomDateConverter : MapConverter
{
    public override object? Convert(object? source)
    {
        if (source is DateTime dt)
            return dt.ToString("yyyy-MM-dd");
        return null;
    }
}

// Register the converter
Mapper.AddConverter(typeof(DateTime), typeof(string), () => new CustomDateConverter());

// Now DateTime → string uses the custom converter
var source = new { Date = DateTime.Now };
var target = source.Map<dynamic>();
```

## Source Generation and Registration

Zerra's Mapper uses a **source generator** to automatically discover and register all `IMapDefinition<TSource, TTarget>` implementations at compile time. This works for both Native AOT and regular .NET projects.

### How It Works

1. **You implement `IMapDefinition<TSource, TTarget>`** with your custom mapping logic
2. **Source generator scans your code** during compilation
3. **Registration code is generated automatically** - no manual setup needed
4. **Mappings are available immediately** when your application runs

### Default Behavior (Recommended)

```csharp
using Zerra.Map;

// Just implement IMapDefinition
public class PersonDtoToModelMap : IMapDefinition<PersonDto, PersonModel>
{
    public void Define(IMapSetup<PersonDto, PersonModel> map)
    {
        map.Define(target => target.FullName, 
                   source => $"{source.FirstName} {source.LastName}");
    }
}

// No registration code needed - it just works!
var model = dto.Map<PersonModel>();
```

**✅ Works everywhere:**
- Native AOT projects
- Regular .NET runtime projects
- .NET Framework 4.8 projects
- .NET Standard 2.0 libraries

> **💡 Best Practice:** Just implement `IMapDefinition` and let the source generator handle registration. No manual registration code is needed in 99% of cases.

> **Note:** Zerra automatically includes source generation support. No additional project references are needed.

### Fallback Options (Rarely Needed)

If source generation isn't working in your environment, you can fall back to runtime registration:

**Fallback 1: Reflection-Based Discovery**

```csharp
// Uses reflection to find and register IMapDefinition implementations
MapDiscovery.Initialize();
```

⚠️ **Limitations:** Won't work with Native AOT (uses reflection)

**Fallback 2: Manual Registration**

```csharp
// Explicitly register each map definition
MapDefinition.Register(new PersonDtoToModelMap());
MapDefinition.Register(new OrderDtoToModelMap());
```

✅ **Works everywhere**, including Native AOT

**When you might need fallback options:**
- Source generator is disabled or not functioning
- Older build environment without source generator support
- Dynamic runtime registration scenarios

### Automatic Property Mapping (Always Works)

Automatic property mapping by name **does not require `IMapDefinition`** and works in all scenarios because:
- No custom logic is needed
- Type conversions use built-in converters
- The Mapper can infer mappings directly from type structure

**Example (works everywhere, including AOT):**

```csharp
public class Source
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Target
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// This works everywhere without IMapDefinition
var target = source.Map<Source, Target>();
```

### When IMapDefinition is Required

Implement `IMapDefinition` when you need custom mapping logic:
- ❌ Properties don't match by name
- ❌ Custom transformation logic is needed
- ❌ Combining/splitting properties
- ❌ Calculated properties (e.g., Age from BirthDate)

## API Reference

### Map Extension Methods

```csharp
// Map to new instance (inferred source type)
TTarget Map<TTarget>(this object source, Graph? graph = null)

// Map to new instance (explicit source and target types)
TTarget Map<TSource, TTarget>(this TSource source, Graph? graph = null)

// Map to existing instance
void MapTo<TSource, TTarget>(this TSource source, TTarget target, Graph? graph = null)
    where TSource : notnull
    where TTarget : notnull
```

### Copy Extension Methods

```csharp
// Deep copy (generic)
TTarget Copy<TTarget>(this TTarget source, Graph? graph = null)

// Deep copy (object)
object CopyObject(this object source, Graph? graph = null)
```

### Mapper Static Methods

```csharp
// Register custom type converter
void Mapper.AddConverter(Type sourceType, Type targetType, Func<MapConverter> converter)
```

### MapDiscovery (Fallback Only)

```csharp
// Reflection-based discovery - only use if source generation isn't working
// NOT needed in most cases - source generator handles registration automatically
void MapDiscovery.Initialize()
```

## Performance Tips

1. **Reuse map definitions**: The Mapper caches generated mapping logic per type pair
2. **Avoid unnecessary Graph complexity**: Simple mappings are faster than graph-filtered mappings
3. **Use strongly typed `Map<TSource, TTarget>()`**: Slightly faster than runtime type inference
4. **Register converters once**: Custom converters are cached globally

## Common Scenarios

### DTO to Domain Model

```csharp
public class CreateOrderDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class Order
{
    public Guid Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateOrderDtoToOrderMap : IMapDefinition<CreateOrderDto, Order>
{
    public void Define(IMapSetup<CreateOrderDto, Order> map)
    {
        map.Define(o => o.Id, dto => Guid.NewGuid());
        map.Define(o => o.CreatedAt, dto => DateTime.UtcNow);
        // ProductId and Quantity are auto-mapped by name
    }
}

// Usage
MapDefinition.Register(new CreateOrderDtoToOrderMap());
var dto = new CreateOrderDto { ProductId = 42, Quantity = 10 };
var order = dto.Map<Order>();
```

### API Response Flattening

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Address Address { get; set; }
}

public class Address
{
    public string City { get; set; }
    public string Country { get; set; }
}

public class UserFlatDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
}

public class UserToFlatDtoMap : IMapDefinition<User, UserFlatDto>
{
    public void Define(IMapSetup<User, UserFlatDto> map)
    {
        map.Define(dto => dto.City, user => user.Address?.City ?? "");
        map.Define(dto => dto.Country, user => user.Address?.Country ?? "");
        // Id and Name are auto-mapped
    }
}
```

### Enum to String Conversion

```csharp
public enum Status { Active, Inactive, Pending }

public class Entity
{
    public Status Status { get; set; }
}

public class EntityDto
{
    public string Status { get; set; }
}

// Automatic enum → string conversion
var entity = new Entity { Status = Status.Active };
var dto = entity.Map<EntityDto>();
// Result: dto.Status = "Active"
```

## Troubleshooting

### Issue: Properties Not Mapping

**Problem:** Properties with the same name aren't being mapped.

**Solution:**
- Ensure property names match exactly (case-sensitive)
- Check that property types are compatible or convertible
- Verify both properties have public getters and setters

### Issue: Source Generation Not Working

**Problem:** Map definitions aren't being registered automatically.

**Solution:**
- Verify your `IMapDefinition` implementation is public and not generic
- Ensure the source generator is enabled (should be automatic with Zerra)
- Check build output for any source generator warnings
- As a temporary fallback, use `MapDiscovery.Initialize()` or manual registration

### Issue: Custom Mapping Not Applied

**Problem:** Custom map definition is ignored.

**Solution:**
- **Most likely:** Source generator should auto-register - check if it's enabled
- **Fallback:** If source generation isn't working:
  - Use `MapDiscovery.Initialize()` to auto-discover (non-AOT only), **or**
  - Use `MapDefinition.Register(new YourMapDefinition())` for manual registration (works everywhere)
- Verify `IMapDefinition<TSource, TTarget>` generic types match your source/target types exactly
- Check that `Define()` method is being called (use debugger)

### Issue: MapDiscovery.Initialize() Not Finding Definitions

**Problem:** `MapDiscovery.Initialize()` doesn't register your map definitions.

**Solution:**
- **First:** Check if source generation is working - you shouldn't need `MapDiscovery.Initialize()` if it is
- If you must use MapDiscovery:
  - Verify you're **not** using Native AOT (MapDiscovery uses reflection, won't work with AOT)
  - Ensure your `IMapDefinition` implementations are in loaded assemblies
  - Check that classes implementing `IMapDefinition` are public and not generic
- **Best solution:** Let source generator handle it automatically

## See Also

- [AOT Support](AOT.md) - Source generator and Native AOT compilation (Mapper uses source generation automatically)
- [Serializers](Serializers.md) - Serialization options for message transport
- [Graph](../README.md) - Graph-based property filtering (referenced in main README)

---

**Next Steps:**
- Learn about [Source Generation](AOT.md) used by Mapper for automatic registration
- Explore [Serializers](Serializers.md) for message serialization options
- Review [Zerra.Web](ZerraWeb.md) for API integration with mapped DTOs
