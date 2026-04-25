[← Back to Documentation](Index.md)

# Reflection using TypeAnalyzer and TypeDetail

Zerra provides a powerful reflection system through `TypeAnalyzer` and `TypeDetail` classes that offer high-performance, cached type information for serialization, mapping, and CQRS operations.

## Overview

The reflection system provides:
- **Cached type information** - Type details are generated once and cached
- **Dynamic code generation** - Creates optimized accessors and creators at runtime
- **Source generator support** - Pre-generate type details at compile time for AOT
- **Type conversion** - Convert between core .NET types easily
- **Comprehensive metadata** - Properties, fields, constructors, methods, and type classifications

## AOT Compatibility

> **Important**: `TypeAnalyzer` and `TypeDetail` rely on dynamic code generation at runtime to create optimized accessors and creators. This **does not work in Native AOT** scenarios where dynamic code generation throws `NotSupportedException`.

To use `TypeAnalyzer` and `TypeDetail` in Native AOT applications, you must reply on source generation to pre-generate type details at compile time.

### Automatic Type Detection

The source generator automatically detects and pre-generates `TypeDetail` for:
- **CQRS entities**: `ICommand`, `IEvent`, `IQuery`
- **CQRS handlers**: `ICommandHandler<>`, `IEventHandler<>`, `IQueryHandler<>`
- **Related types**: All types referenced by properties, fields, and parameters of CQRS entities

```csharp
// Automatically detected - no attribute needed
public class CreateUserCommand : ICommand
{
    public string Username { get; set; }
    public Address Address { get; set; } // Address is also auto-detected
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
}
```

### Manual Type Registration

For other types not automatically detected, use the `GenerateTypeDetailAttribute`:

```csharp
using Zerra.Reflection;

[GenerateTypeDetail]
public class CustomModel
{
    public string Name { get; set; }
    public int Value { get; set; }
}

// Now safe to use in AOT
var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(CustomModel));
var instance = typeDetail.CreatorBoxed();
```

The source generator runs automatically during compilation and generates type details for all detected types.

### Verifying Pre-Generated Types

In AOT scenarios, attempting to use `TypeAnalyzer.GetTypeDetail()` on a type that wasn't pre-generated will throw `NotSupportedException`:

```csharp
// ❌ Throws NotSupportedException in Native AOT if not pre-generated
var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(UnknownType));

// ✅ Works in Native AOT if type is a CQRS entity or marked with [GenerateTypeDetail]
var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(CreateUserCommand));
```

For more information about AOT deployment, see [AOT Configuration](AOT.md).

## TypeAnalyzer

`TypeAnalyzer` is the entry point for retrieving detailed type information and performing type conversions.

### Getting Type Details

```csharp
using Zerra.Reflection;

// Get detailed type information (cached)
TypeDetail userType = TypeAnalyzer.GetTypeDetail(typeof(User));
TypeDetail<User> userTypeGeneric = TypeAnalyzer<User>.GetTypeDetail();

// Type details are cached - subsequent calls return the same instance
var cached = TypeAnalyzer.GetTypeDetail(typeof(User)); // same instance
```

### Type Conversion

```csharp
// Convert to specific type
int result = TypeAnalyzer.Convert<int>("123");           // 123
bool flag = TypeAnalyzer.Convert<bool>("true");          // true
DateTime date = TypeAnalyzer.Convert<DateTime>("2024-01-01"); // DateTime

// Convert with target type
object value = TypeAnalyzer.Convert("42", typeof(int));  // 42

// Convert with CoreType enum (for performance)
CoreType coreType = CoreType.Int32;
object converted = TypeAnalyzer.Convert("42", coreType); // 42

// Nullable conversions
int? nullableInt = TypeAnalyzer.Convert<int?>("123");    // 123
int? nullResult = TypeAnalyzer.Convert<int?>(null);      // null
```

### Supported Conversion Types

TypeAnalyzer supports conversion for all core types:
- **Primitives**: bool, byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal
- **Special types**: string, char, DateTime, DateTimeOffset, TimeSpan, Guid, DateOnly, TimeOnly
- **Nullable versions**: int?, bool?, DateTime?, etc.

### Registering Pre-Generated Types

For AOT scenarios, register pre-generated type details:

```csharp
// Called by source generator
TypeAnalyzer.Register(preGeneratedTypeDetail);
```

## TypeDetail

`TypeDetail` contains comprehensive metadata about a type.

### Accessing Type Information

```csharp
var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(User));

// Basic information
Type type = typeDetail.Type;                    // The actual Type
bool isNullable = typeDetail.IsNullable;        // Is Nullable<T>?
CoreType? coreType = typeDetail.CoreType;       // Core type classification
SpecialType? specialType = typeDetail.SpecialType; // Special type classification

// Collection information
bool hasIEnumerable = typeDetail.HasIEnumerable;
bool hasIEnumerableGeneric = typeDetail.HasIEnumerableGeneric;
bool hasICollection = typeDetail.HasICollection;
bool hasIList = typeDetail.HasIList;
bool hasIDictionary = typeDetail.HasIDictionary;
Type? innerType = typeDetail.InnerType;         // T in IEnumerable<T>
Type[]? innerTypes = typeDetail.InnerTypes;     // K,V in IDictionary<K,V>

// Type characteristics
bool hasCreator = typeDetail.HasCreator;        // Can be instantiated
Func<object>? creator = typeDetail.CreatorBoxed; // Factory for creating instances
```

### Working with Members

```csharp
var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(User));

// Get all members (properties and fields)
IReadOnlyList<MemberDetail> members = typeDetail.Members;

foreach (var member in members)
{
    string name = member.Name;
    Type memberType = member.Type;
    bool canRead = member.CanRead;
    bool canWrite = member.CanWrite;

    // Get value from instance
    var user = new User { Name = "John" };
    object? value = member.GetterBoxed(user);

    // Set value on instance
    member.SetterBoxed(user, "Jane");

    // Check for attributes
    var attributes = member.GetCustomAttributes();
}
```

### Working with Constructors

```csharp
var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(User));

// Get all constructors
IReadOnlyList<ConstructorDetail> constructors = typeDetail.Constructors;

foreach (var ctor in constructors)
{
    // Parameter information
    IReadOnlyList<ParameterDetail> parameters = ctor.Parameters;

    foreach (var param in parameters)
    {
        string paramName = param.Name;
        Type paramType = param.Type;
        bool hasDefaultValue = param.HasDefaultValue;
        object? defaultValue = param.DefaultValue;
    }

    // Create instance
    object instance = ctor.CreatorBoxed(new object[] { arg1, arg2 });
}
```

### Working with Methods

```csharp
var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(User));

// Get all methods
IReadOnlyList<MethodDetail> methods = typeDetail.Methods;

foreach (var method in methods)
{
    string methodName = method.Name;
    Type returnType = method.ReturnType;
    bool isStatic = method.IsStatic;

    // Parameters
    IReadOnlyList<ParameterDetail> parameters = method.Parameters;

    // Invoke method
    var user = new User();
    object? result = method.CallerBoxed(user, new object[] { arg1, arg2 });
}
```

## Generic TypeDetail<T>

For type-safe scenarios, use the generic version:

```csharp
using Zerra.Reflection;

var typeDetail = TypeAnalyzer<User>.GetTypeDetail();

// Typed creator
Func<User>? creator = typeDetail.Creator; // Func<User> instead of Func<object>
User newUser = creator();

// Members with typed getters/setters
foreach (var member in typeDetail.Members)
{
    if (member.Getter != null)
    {
        // Typed getter returns object but is optimized
        var value = member.Getter(user);
    }
}
```

## Type Classifications

### CoreType Enum

Identifies built-in .NET types:

```csharp
public enum CoreType
{
    Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64,
    Single, Double, Decimal, Char, String, DateTime, DateTimeOffset,
    TimeSpan, DateOnly, TimeOnly, Guid,
    BooleanNullable, ByteNullable, SByteNullable, // ... nullable versions
}

var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(int));
if (typeDetail.CoreType == CoreType.Int32)
{
    // Handle as integer
}
```

### SpecialType Enum

Identifies framework-specific types:

```csharp
public enum SpecialType
{
    ICommand, ICommandWithResult, IEvent, IQuery, IQueryResult,
    // ... other Zerra framework types
}

var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(CreateUserCommand));
if (typeDetail.SpecialType == SpecialType.ICommand)
{
    // Handle as CQRS command
}
```

### CoreEnumType

For enum types, identifies the underlying type:

```csharp
public enum UserRole
{
    Admin = 1,
    User = 2
}

var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(UserRole));
CoreEnumType? underlyingType = typeDetail.EnumUnderlyingType; // CoreEnumType.Int32
```

## Performance Optimization

### Caching

Type details are automatically cached:

```csharp
// First call generates and caches
var detail1 = TypeAnalyzer.GetTypeDetail(typeof(User));

// Subsequent calls use cached instance (very fast)
var detail2 = TypeAnalyzer.GetTypeDetail(typeof(User)); // Same instance
```

### AOT Pre-Generation

For Native AOT scenarios, use source generators to pre-generate type details:

```csharp
// Add to your project file
<PackageReference Include="Zerra.SourceGeneration" />

// Add attribute to types
[Zerra.SourceGeneration.GenerateTypeDetail]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// Type details are generated at compile time
// No runtime code generation needed
```

## Common Use Cases

### Dynamic Property Access

```csharp
public object? GetPropertyValue(object obj, string propertyName)
{
    var typeDetail = TypeAnalyzer.GetTypeDetail(obj.GetType());
    var member = typeDetail.Members.FirstOrDefault(m => m.Name == propertyName);
    return member?.GetterBoxed(obj);
}

public void SetPropertyValue(object obj, string propertyName, object? value)
{
    var typeDetail = TypeAnalyzer.GetTypeDetail(obj.GetType());
    var member = typeDetail.Members.FirstOrDefault(m => m.Name == propertyName);
    member?.SetterBoxed(obj, value);
}
```

### Object Creation

```csharp
public T CreateInstance<T>() where T : class
{
    var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
    if (typeDetail.Creator != null)
    {
        return typeDetail.Creator();
    }
    throw new InvalidOperationException($"Cannot create instance of {typeof(T).Name}");
}
```

### Type Checking

```csharp
public bool IsCollection(Type type)
{
    var typeDetail = TypeAnalyzer.GetTypeDetail(type);
    return typeDetail.HasIEnumerableGeneric;
}

public Type? GetCollectionElementType(Type collectionType)
{
    var typeDetail = TypeAnalyzer.GetTypeDetail(collectionType);
    return typeDetail.InnerType;
}
```

### Serialization Support

```csharp
public void SerializeObject(object obj, Stream stream)
{
    var typeDetail = TypeAnalyzer.GetTypeDetail(obj.GetType());

    foreach (var member in typeDetail.Members)
    {
        if (member.CanRead)
        {
            var value = member.GetterBoxed(obj);
            // Write value to stream
            WriteValue(stream, member.Name, value, member.Type);
        }
    }
}
```

### Dynamic Mapping

```csharp
public TTarget MapProperties<TSource, TTarget>(TSource source)
    where TTarget : new()
{
    var sourceDetail = TypeAnalyzer<TSource>.GetTypeDetail();
    var targetDetail = TypeAnalyzer<TTarget>.GetTypeDetail();
    var target = new TTarget();

    foreach (var sourceMember in sourceDetail.Members)
    {
        if (!sourceMember.CanRead) continue;

        var targetMember = targetDetail.Members
            .FirstOrDefault(m => m.Name == sourceMember.Name && m.CanWrite);

        if (targetMember != null)
        {
            var value = sourceMember.GetterBoxed(source);
            targetMember.SetterBoxed(target, value);
        }
    }

    return target;
}
```

## Best Practices

1. **Cache TypeDetail instances** - They're already cached internally, but store references if used frequently
2. **Use generic versions** - `TypeAnalyzer<T>` provides better type safety than non-generic
3. **Enable AOT generation** - Use source generators for Native AOT compatibility
4. **Handle missing members gracefully** - Check for null before accessing members
5. **Consider alternatives** - For known types at compile time, use direct property access

## Limitations

- **Dynamic code generation** - Requires runtime compilation (unless using source generators)
- **AOT compatibility** - Must use source generators for Native AOT scenarios
- **Trimming** - May require trimming hints for some scenarios
- **Private members** - Respects accessibility rules

## See Also

- [AOT](AOT.md) - Source generation for type details
- [Mapper](Mapper.md) - Uses TypeAnalyzer for object mapping
- [Serializers](Serializers.md) - Uses TypeAnalyzer for serialization
- [Collections](Collections.md) - ConcurrentFactoryDictionary used internally for caching
