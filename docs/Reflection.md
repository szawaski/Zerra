[← Back to Documentation](Index.md)

# Reflection

`TypeAnalyzer` returns a cached `TypeDetail` for a type: its members, constructors, and methods with fast delegates to get, set, create, and call them, plus classifications such as whether it is a collection. Zerra's serializers and mapper are built on it, and you can use it the same way.

## Native AOT

A `TypeDetail` is either produced by the [source generator](AOT.md) at compile time, or built at runtime with dynamic code. Native AOT has no dynamic code, so there `GetTypeDetail` throws `NotSupportedException` for a type the generator didn't cover. Mark such a type with `[GenerateTypeDetail]`:

```csharp
using Zerra.Reflection;

[GenerateTypeDetail]
public class CustomModel
{
    public string Name { get; set; }
    public int Value { get; set; }
}
```

## Getting a TypeDetail

```csharp
using Zerra.Reflection;

TypeDetail detail = TypeAnalyzer.GetTypeDetail(typeof(User));   // or typeof(User).GetTypeDetail()
TypeDetail<User> typed = TypeAnalyzer<User>.GetTypeDetail();
```

Details are cached, so repeated calls return the same instance.

## TypeDetail

```csharp
Type type = detail.Type;
bool isNullable = detail.IsNullable;                  // Nullable<T>
CoreType? coreType = detail.CoreType;                 // built-in types, see below
CoreEnumType? enumType = detail.EnumUnderlyingType;   // for enums
SpecialType? specialType = detail.SpecialType;        // Task, Type, Dictionary, Object, Void, Pointer, CancellationToken

// creating
bool hasCreator = detail.HasCreator;
object instance = detail.CreatorBoxed!();
User user = typed.Creator!();

// collections
bool isEnumerable = detail.HasIEnumerableGeneric;     // Has*: implements it; Is*: is exactly it
Type? elementType = detail.InnerType;                 // T in IEnumerable<T>
IReadOnlyList<Type> innerTypes = detail.InnerTypes;   // every generic argument, such as K and V

// structure
IReadOnlyList<Type> baseTypes = detail.BaseTypes;
IReadOnlyList<Type> interfaces = detail.Interfaces;
IReadOnlyList<Attribute> attributes = detail.Attributes;
```

The collection flags come in pairs for each collection interface and class: `HasIList`/`IsIList`, `HasIListGeneric`/`IsIListGeneric`, `HasIReadOnlyListGeneric`, `HasListGeneric`, `HasISetGeneric`, `HasHashSetGeneric`, `HasIDictionary`, `HasIDictionaryGeneric`, `HasDictionaryGeneric`, and so on.

### Members

`Members` lists properties and fields:

```csharp
foreach (var member in detail.Members)
{
    // Name, Type, IsField, IsStatic, Attributes
    if (member.HasGetter)
        Console.WriteLine($"{member.Name} = {member.GetterBoxed!(user)}");
    if (member.HasSetter && member.Type == typeof(string))
        member.SetterBoxed!(user, "value");
}
```

`Getter` and `Setter` are the strongly typed delegates. The `Boxed` versions take and return `object`.

### Constructors and Methods

```csharp
foreach (var ctor in detail.Constructors)
{
    // ctor.Parameters: each with Name and Type
    object created = ctor.CreatorBoxed(new object?[] { "John" });
}

foreach (var method in detail.Methods)
{
    // Name, ReturnType, Parameters, GenericArguments, IsStatic, Attributes
    if (method.HasCaller)
    {
        object? result = method.CallerBoxed!(user, new object?[] { 42 });
    }
}
```

## CoreType

`CoreType` identifies the built-in types, each with a nullable counterpart: `Boolean`, `Byte`, `SByte`, `Int16`, `UInt16`, `Int32`, `UInt32`, `Int64`, `UInt64`, `Single`, `Double`, `Decimal`, `Char`, `String`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`, and `Guid`, then `BooleanNullable`, `ByteNullable`, and so on.

## Converting Values

`TypeAnalyzer.Convert` converts between the core types and their nullable forms:

```csharp
int number = TypeAnalyzer.Convert<int>("123");
int? none = TypeAnalyzer.Convert<int?>(null);
object? value = TypeAnalyzer.Convert("42", typeof(int));
object? fast = TypeAnalyzer.Convert("42", CoreType.Int32);   // skips the type lookup
```

## See Also

- [AOT](AOT.md) - Source generation
- [Mapper](Mapper.md) - Mapping built on type details
