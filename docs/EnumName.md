[← Back to Documentation](Index.md)

# EnumName

`[EnumName("text")]` gives an enum value a custom string name, and `EnumName` converts between values and names in both directions. `EnumName` is in the global namespace, so it needs no `using`. The source generator prebuilds the name mappings, so it works under Native AOT.

```csharp
public enum Status
{
    [EnumName("active")] Active,
    [EnumName("pending-review")] PendingReview,
    Archived                                    // no attribute: the name is "Archived"
}
```

## Value to Name

```csharp
string name = EnumName.GetName(Status.PendingReview);   // "pending-review"
string name2 = Status.Active.EnumName();                // "active"

Status? missing = null;
string? name3 = missing.EnumName();                     // null
```

## Name to Value

```csharp
Status status = EnumName.Parse<Status>("pending-review");   // throws InvalidOperationException if no value matches

if (EnumName.TryParse<Status>("active", out var parsed)) { }

Status fromString = "active".ToEnum<Status>();
Status? orNull = "unknown".ToEnumNullable<Status>();         // null when missing or unmatched
```

## Flags

Combined `[Flags]` values join their names with `|`:

```csharp
[Flags]
public enum Permissions
{
    [EnumName("read")] Read = 1,
    [EnumName("write")] Write = 2,
    [EnumName("execute")] Execute = 4
}

string names = (Permissions.Read | Permissions.Write).EnumName();   // "read|write"
Permissions both = EnumName.Parse<Permissions>("read|write");
```

## By Runtime Type

```csharp
string name = EnumName.GetName(typeof(Status), Status.Active);
object value = EnumName.Parse("active", typeof(Status));
```
