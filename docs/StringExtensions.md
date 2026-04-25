[← Back to Documentation](Index.md)

# String Extensions

Zerra provides a set of useful extension methods for string manipulation, conversion, and formatting operations.

## Overview

String extensions in Zerra include:
- **Truncation and joining** - Smart truncation with proportional shortening
- **Type conversions** - Parse strings to common .NET types with defaults
- **Null-safe operations** - Handle null strings gracefully
- **Performance optimized** - Span-based implementations for efficiency

## Truncation

### Basic Truncate

```csharp
string text = "This is a long string";
string truncated = text.Truncate(10);
// Result: "This is a "

// Throws on null
string? nullText = null;
string result = nullText.Truncate(10); // ArgumentNullException
```

### Smart Join with Truncation

Join multiple strings with intelligent truncation when the combined length exceeds the maximum:

#### Join Two Strings

```csharp
// Join within max length (no truncation needed)
string result = StringExtensions.Join(50, "_", "Hello", "World");
// Result: "Hello_World"

// Join with truncation (proportional to longer string first)
string result = StringExtensions.Join(20, "_", "VeryLongFirstString", "Short");
// Result: "VeryLong_Short" (first string truncated more)

// Join with separator length consideration
string result = StringExtensions.Join(15, "---", "Testing", "Strings");
// Result: "Testi---String" (both truncated to fit)
```

#### Join Three Strings

```csharp
// Join three strings with intelligent truncation
string result = StringExtensions.Join(30, "_", "First", "Second", "Third");
// Result: "First_Second_Third"

// With truncation (proportional shortening)
string result = StringExtensions.Join(20, "_", "VeryLongFirst", "MediumSecond", "Short");
// Result: "VeryLo_Medi_Short" (longest strings truncated more)
```

#### Join Four Strings

```csharp
// Join four strings
string result = StringExtensions.Join(40, "_", "One", "Two", "Three", "Four");
// Result: "One_Two_Three_Four"

// With complex truncation logic
string result = StringExtensions.Join(25, "_", "Environment", "Machine", "Assembly", "Process");
// Result: "Enviro_Mach_Assem_Proce"
```

### Truncation Logic

The join methods use intelligent truncation:
1. Longer strings are truncated first
2. Truncation is applied proportionally
3. Separator length is always preserved
4. Character-level precision for optimal space usage

## Type Conversions

All conversion methods provide:
- **Default value support** - Return specified default if parsing fails
- **Nullable versions** - Return null instead of default
- **Null handling** - Null or empty strings return default/null
- **Safe parsing** - Never throw exceptions on invalid input

### Boolean Conversions

```csharp
// With default value
bool result = "true".ToBoolean();              // true
bool result = "false".ToBoolean();             // false
bool result = "1".ToBoolean();                 // true (special case)
bool result = "0".ToBoolean();                 // false (special case)
bool result = "invalid".ToBoolean();           // false (default)
bool result = "invalid".ToBoolean(true);       // true (custom default)

// Nullable version
bool? result = "true".ToBooleanNullable();     // true
bool? result = "invalid".ToBooleanNullable();  // null
bool? result = null.ToBooleanNullable();       // null
string? empty = "";
bool? result = empty.ToBooleanNullable();      // null
```

### Numeric Conversions

#### Byte

```csharp
byte result = "42".ToByte();                   // 42
byte result = "256".ToByte();                  // 0 (out of range)
byte result = "invalid".ToByte(99);            // 99 (custom default)

byte? result = "42".ToByteNullable();          // 42
byte? result = "invalid".ToByteNullable();     // null
```

#### Int16 (short)

```csharp
short result = "42".ToInt16();                 // 42
short result = "invalid".ToInt16(-1);          // -1 (custom default)

short? result = "42".ToInt16Nullable();        // 42
short? result = "invalid".ToInt16Nullable();   // null
```

#### UInt16 (ushort)

```csharp
ushort result = "42".ToUInt16();               // 42
ushort result = "invalid".ToUInt16();          // 0 (default)

ushort? result = "42".ToUInt16Nullable();      // 42
ushort? result = "invalid".ToUInt16Nullable(); // null
```

#### Int32 (int)

```csharp
int result = "42".ToInt32();                   // 42
int result = "invalid".ToInt32(-1);            // -1 (custom default)

int? result = "42".ToInt32Nullable();          // 42
int? result = "invalid".ToInt32Nullable();     // null
```

#### UInt32 (uint)

```csharp
uint result = "42".ToUInt32();                 // 42
uint result = "invalid".ToUInt32();            // 0 (default)

uint? result = "42".ToUInt32Nullable();        // 42
uint? result = "invalid".ToUInt32Nullable();   // null
```

#### Int64 (long)

```csharp
long result = "42".ToInt64();                  // 42
long result = "invalid".ToInt64(-1);           // -1 (custom default)

long? result = "42".ToInt64Nullable();         // 42
long? result = "invalid".ToInt64Nullable();    // null
```

#### UInt64 (ulong)

```csharp
ulong result = "42".ToUInt64();                // 42
ulong result = "invalid".ToUInt64();           // 0 (default)

ulong? result = "42".ToUInt64Nullable();       // 42
ulong? result = "invalid".ToUInt64Nullable();  // null
```

#### Single (float)

```csharp
float result = "3.14".ToSingle();              // 3.14f
float result = "invalid".ToSingle(0.0f);       // 0.0f (custom default)

float? result = "3.14".ToSingleNullable();     // 3.14f
float? result = "invalid".ToSingleNullable();  // null
```

#### Double

```csharp
double result = "3.14".ToDouble();             // 3.14
double result = "invalid".ToDouble(0.0);       // 0.0 (custom default)

double? result = "3.14".ToDoubleNullable();    // 3.14
double? result = "invalid".ToDoubleNullable(); // null
```

#### Decimal

```csharp
decimal result = "3.14".ToDecimal();           // 3.14m
decimal result = "invalid".ToDecimal(0m);      // 0m (custom default)

decimal? result = "3.14".ToDecimalNullable();  // 3.14m
decimal? result = "invalid".ToDecimalNullable(); // null
```

### Date and Time Conversions

#### DateTime

```csharp
DateTime result = "2024-01-15".ToDateTime();
DateTime result = "invalid".ToDateTime(DateTime.MinValue);

DateTime? result = "2024-01-15".ToDateTimeNullable();
DateTime? result = "invalid".ToDateTimeNullable(); // null
```

#### DateTimeOffset

```csharp
DateTimeOffset result = "2024-01-15T10:00:00Z".ToDateTimeOffset();
DateTimeOffset result = "invalid".ToDateTimeOffset(DateTimeOffset.MinValue);

DateTimeOffset? result = "2024-01-15T10:00:00Z".ToDateTimeOffsetNullable();
DateTimeOffset? result = "invalid".ToDateTimeOffsetNullable(); // null
```

#### TimeSpan

```csharp
TimeSpan result = "01:30:00".ToTimeSpan();
TimeSpan result = "invalid".ToTimeSpan(TimeSpan.Zero);

TimeSpan? result = "01:30:00".ToTimeSpanNullable();
TimeSpan? result = "invalid".ToTimeSpanNullable(); // null
```

#### DateOnly (.NET 6+)

```csharp
DateOnly result = "2024-01-15".ToDateOnly();
DateOnly result = "invalid".ToDateOnly(DateOnly.MinValue);

DateOnly? result = "2024-01-15".ToDateOnlyNullable();
DateOnly? result = "invalid".ToDateOnlyNullable(); // null
```

#### TimeOnly (.NET 6+)

```csharp
TimeOnly result = "14:30:00".ToTimeOnly();
TimeOnly result = "invalid".ToTimeOnly(TimeOnly.MinValue);

TimeOnly? result = "14:30:00".ToTimeOnlyNullable();
TimeOnly? result = "invalid".ToTimeOnlyNullable(); // null
```

### Other Conversions

#### Char

```csharp
char result = "A".ToChar();                    // 'A'
char result = "invalid".ToChar('?');           // '?' (custom default)

char? result = "A".ToCharNullable();           // 'A'
char? result = "invalid".ToCharNullable();     // null
```

#### Guid

```csharp
Guid result = "12345678-1234-1234-1234-123456789abc".ToGuid();
Guid result = "invalid".ToGuid(Guid.Empty);

Guid? result = "12345678-1234-1234-1234-123456789abc".ToGuidNullable();
Guid? result = "invalid".ToGuidNullable();     // null
```

## Common Use Cases

### Configuration Parsing

```csharp
public class AppConfig
{
    public int MaxRetries { get; set; }
    public TimeSpan Timeout { get; set; }
    public bool EnableLogging { get; set; }
}

public AppConfig LoadFromEnvironment()
{
    return new AppConfig
    {
        MaxRetries = Environment.GetEnvironmentVariable("MAX_RETRIES").ToInt32(3),
        Timeout = Environment.GetEnvironmentVariable("TIMEOUT").ToTimeSpan(TimeSpan.FromSeconds(30)),
        EnableLogging = Environment.GetEnvironmentVariable("ENABLE_LOGGING").ToBoolean(true)
    };
}
```

### Safe Query String Parsing

```csharp
public void ProcessRequest(HttpRequest request)
{
    int? page = request.Query["page"].ToInt32Nullable();
    int pageSize = request.Query["pageSize"].ToInt32(10);
    bool includeDeleted = request.Query["includeDeleted"].ToBoolean(false);

    // Use values safely - nulls indicate missing/invalid parameters
}
```

### Building Identifiers

```csharp
public string CreateServiceId(string environment, string machine, string service)
{
    // Create ID that fits in 50 characters with intelligent truncation
    return StringExtensions.Join(50, "_", environment, machine, service);
}

// Example output:
// "production_webapp01_userservice"
// "dev_longmachinename123_verylongservicename" -> "dev_longmachinen_verylongser"
```

### Creating Topic/Queue Names

```csharp
public string CreateKafkaTopic(string environment, string eventType, string version)
{
    const int maxTopicLength = 249; // Kafka limit
    return StringExtensions.Join(maxTopicLength, ".", environment, eventType, version);
}

// Ensures topic name fits within limits even with long environment names
```

## Null Handling

```csharp
string? nullString = null;

// These throw ArgumentNullException
nullString.Truncate(10);              // throws

// These return default
nullString.ToInt32();                 // returns 0
nullString.ToBoolean(true);           // returns true

// These return null
nullString.ToInt32Nullable();         // returns null
nullString.ToBooleanNullable();       // returns null
```

## See Also

- [Serializers](Serializers.md) - Uses string conversions internally
- [Reflection](Reflection.md) - TypeAnalyzer.Convert for type-safe conversions
