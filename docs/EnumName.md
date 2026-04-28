[← Back to Documentation](Index.md)

# EnumName

Zerra provides `EnumName`, an attribute and static utility class for controlling the string representation of enum values, along with `EnumNameExtensions` for convenient extension method access.

## Overview

- **Custom string names** - Decorate enum fields with `[EnumName("text")]` to define their string representation
- **Flags enum support** - Correctly handles `[Flags]` enums by combining names with `|`
- **Parse and TryParse** - Convert strings back to enum values using the same name mappings
- **Extension methods** - `.EnumName()` and `.ToEnum<T>()` for fluent usage
- **Nullable support** - Null-safe overloads for nullable enum types
- **AOT compatible** - The Zerra source generator (automatically included with the Zerra package) prebuilds enum name mappings at compile time, making `EnumName` fully compatible with Native AOT and pre-compiled reflection. See [AOT](AOT.md) for details.

## Defining Names

Apply the `[EnumName]` attribute to enum fields to specify their string representation. Fields without the attribute use their exact member name as the string.

```csharp
public enum Status
{
    [EnumName("active")]
    Active,

    [EnumName("inactive")]
    Inactive,

    [EnumName("pending-review")]
    PendingReview
}
```

```csharp
public enum Status
{
    // No attribute — string name is "Active"
    Active,

    [EnumName("inactive")]
    Inactive
}
```

## Getting the Name

### Static Method

```csharp
string name = EnumName.GetName(Status.Active);
// Result: "active"

string name = EnumName.GetName(Status.PendingReview);
// Result: "pending-review"
```

### Extension Method

```csharp
string name = Status.Active.EnumName();
// Result: "active"

Status? nullable = Status.Inactive;
string? name = nullable.EnumName();
// Result: "inactive"

Status? nullValue = null;
string? name = nullValue.EnumName();
// Result: null
```

## Parsing

### Parse (throws on failure)

```csharp
Status status = EnumName.Parse<Status>("active");
// Result: Status.Active

Status status = EnumName.Parse<Status>("pending-review");
// Result: Status.PendingReview

// Throws InvalidOperationException if the string does not match any value
Status status = EnumName.Parse<Status>("unknown");
```

### TryParse (returns bool)

```csharp
if (EnumName.TryParse<Status>("active", out var status))
{
    // status == Status.Active
}
```

### Extension Method

```csharp
Status status = "active".ToEnum<Status>();
// Result: Status.Active

// Nullable — returns null if string is null or not matched
Status? status = "active".ToEnumNullable<Status>();
// Result: Status.Active

Status? status = "unknown".ToEnumNullable<Status>();
// Result: null
```

## Flags Enums

`[Flags]` enums are supported. Combined flag values are represented by joining individual names with `|`.

```csharp
[Flags]
public enum Permissions
{
    [EnumName("read")]
    Read = 1,

    [EnumName("write")]
    Write = 2,

    [EnumName("execute")]
    Execute = 4
}
```

```csharp
var perm = Permissions.Read | Permissions.Write;

string name = perm.EnumName();
// Result: "read|write"

Permissions parsed = EnumName.Parse<Permissions>("read|write");
// Result: Permissions.Read | Permissions.Write
```

## Non-Generic Overloads

`GetName` and `Parse` also have non-generic overloads that accept a `Type` and `object`:

```csharp
Type type = typeof(Status);
string name = EnumName.GetName(type, Status.Active);

object value = EnumName.Parse("active", type);
```
