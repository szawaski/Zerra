[← Back to Documentation](Index.md)

# String Extensions

`Zerra.StringExtensions` adds truncation, length-limited joining, safe parsing, and wildcard matching to strings.

## Truncate

```csharp
string name = text.Truncate(10);                       // at most 10 characters
string name2 = text.Truncate(10, out bool truncated);  // and whether anything was cut
```

A string exactly `maxLength` long isn't truncated: `truncated` is `false` and the same instance comes back. A `null` string throws `ArgumentNullException`.

## Join Within a Length

`StringExtensions.Join(maxLength, separator, ...)` joins two, three, or four strings, and when the result would be too long, shortens the longest parts first, proportionally, while keeping every separator:

```csharp
StringExtensions.Join(50, "_", "Hello", "World");                                   // "Hello_World"
StringExtensions.Join(25, "_", "Environment", "Machine", "Assembly", "Process");    // "Enviro_Mach_Assem_Proce"

var topic = StringExtensions.Join(249, "_", environment, name, out var truncated);
if (truncated)
    log.Warn($"Topic shortened to {topic}; another name shortening to the same one would share it");
```

Zerra's message transports build broker names this way, and log a warning once when a broker's name limit shortens a topic, queue, exchange, consumer group, or subscription name.

## Parsing

Each `To*` method returns its default, or the `defaultValue` you pass, when the string is `null`, empty, or doesn't parse. Each `To*Nullable` method returns `null` instead. None of them throw.

```csharp
int retries = Environment.GetEnvironmentVariable("MAX_RETRIES").ToInt32(3);
TimeSpan timeout = config["Timeout"].ToTimeSpan(TimeSpan.FromSeconds(30));
int? page = request.Query["page"].ToString().ToInt32Nullable();
```

| Type | Methods |
|---|---|
| `bool` | `ToBoolean`, `ToBooleanNullable`. Also accepts `"1"` and `"0"` |
| integers | `ToByte`, `ToInt16`, `ToUInt16`, `ToInt32`, `ToUInt32`, `ToInt64`, `ToUInt64`, each with a `Nullable` version |
| floating point | `ToFloat`, `ToDouble`, `ToDecimal`, each with a `Nullable` version |
| dates and times | `ToDateTime`, `ToDateTimeOffset`, `ToTimeSpan`, `ToDateOnly`, `ToTimeOnly`, each with a `Nullable` version; `DateOnly` and `TimeOnly` not on .NET Standard 2.0 |
| `Guid` | `ToGuid`, `ToGuidNullable` |
| enums | `ToEnum<T>`, `ToEnumNullable<T>`, which understand [EnumName](EnumName.md) names |

## Wildcards

`MatchWildcard` matches a pattern where the wildcard stands for zero or more characters:

```csharp
"Zerra.CQRS.Bus".MatchWildcard("Zerra.*");              // true
"report-2024.csv".MatchWildcard("report-?.csv", '?');   // true, with a custom wildcard character
```

## See Also

- [Reflection](Reflection.md) - `TypeAnalyzer.Convert` for converting between core types
