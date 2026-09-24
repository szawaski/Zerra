[← Back to Documentation](Index.md)

# AOT (Ahead-of-Time Compilation) Support

Zerra includes a powerful **source generator** that creates precompiled reflection information at build time, enabling:
- ✅ **Native AOT compilation** support
- 🚀 **Zero runtime reflection overhead**
- ⚡ **Faster startup and execution times**
- 📦 **Smaller deployment sizes** (with AOT publishing)

## Overview

Traditional reflection in .NET uses runtime type inspection, which:
- Has performance overhead for type discovery and member access
- Is not compatible with Native AOT compilation
- Can cause issues with trimming and tree shaking

Zerra's source generator solves these problems by **generating all reflection metadata at compile time**, replacing runtime reflection with fast, direct code paths.

## How It Works

The `Zerra.SourceGeneration` source generator analyzes your CQRS types (commands, queries, events, handlers) during compilation and generates:

1. **Type Metadata** - Complete type information for all CQRS models
2. **Bus Router Code** - Direct routing logic for commands, queries, and events
3. **Handler Proxies** - Precompiled handler invocation code
4. **Serialization Code** - Fast serialization without runtime inspection
5. **Constructor and Member Access** - Direct property/field access without reflection

## Setup

### 1. Add Zerra Package

The source generator is **automatically included** when you reference the Zerra NuGet package:

```xml
<ItemGroup>
    <PackageReference Include="Zerra" Version="*" />
</ItemGroup>
```

**That's it!** No additional configuration needed. The source generator will automatically discover and generate code for all CQRS types in your project.

### 2. Enable AOT Publishing (Optional)

If you want to publish as a Native AOT application, add this to your project file:

```xml
<PropertyGroup>
    <PublishAot>true</PublishAot>
</PropertyGroup>
```

### Example: Pets.Service.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <PublishAot>true</PublishAot>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\Pets.Domain\Pets.Domain.csproj" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Zerra" Version="*" />
    </ItemGroup>

</Project>
```

## What Gets Generated

The source generator automatically discovers and generates code for:

### Types Discovered
- All **command types** implementing `ICommand` or `ICommand<TResult>`
- All **event types** implementing `IEvent`
- All **query handler interfaces** derived from `IQueryHandler`
- All **command handler interfaces** derived from `ICommandHandler`
- All **event handler interfaces** derived from `IEventHandler`
- All **model types** used as parameters or return types in handlers

### Generated Code

The generator adds an initializer file, `ZerraSourceGenerationInitializer.cs`, plus one generated class per bus router and empty implementation. The initializer calls `Zerra.Reflection.Register` methods like the following (shapes simplified):

#### 1. Type Registration

```csharp
// Generated type metadata (simplified example)
global::Zerra.Reflection.Register.Type(new global::Zerra.Reflection.TypeDetail<MyCommand>(
    /* member, constructor, and method metadata */
));
```

#### 2. Handler Registration

```csharp
// Generated handler method registration (simplified example)
global::Zerra.Reflection.Register.Handler(
    typeof(IMyQueryHandler),
    "GetPetById",
    /* isTask */ true,
    /* taskInnerType */ typeof(Pet),
    /* parameterTypes */ [typeof(Guid)],
    /* method */ static (object instance, object?[]? args) => ((IMyQueryHandler)instance).GetPetById((Guid)args![0]!),
    /* taskResult */ static (object task) => ((Task<Pet>)task).Result
);
```

#### 3. Bus Router Registration

```csharp
// Generated command/event routing metadata (simplified example)
global::Zerra.Reflection.Register.CommandOrEventInfo(
    typeof(IPetsCommandHandler),
    "IPetsCommandHandler",
    [typeof(CreatePetCommand)],   // command types
    []                            // event types
);

// Generated query proxy class (Caller_IPetsQueryHandler) used by bus.Call<IPetsQueryHandler>()
global::Zerra.Reflection.Register.Router(typeof(IPetsQueryHandler), static (global::Zerra.CQRS.IBusInternal bus, string source) => new global::Pets.Domain.SourceGeneration.Caller_IPetsQueryHandler(bus, source));
```

#### 4. Serialization Code

Fast serialization paths for all discovered types without runtime reflection.

## Benefits

### Performance

**Before (Runtime Reflection):**
```csharp
// Slow: Runtime type inspection
var properties = typeof(MyCommand).GetProperties();
foreach (var prop in properties)
{
    var value = prop.GetValue(instance); // Reflection call
}
```

**After (Source Generated):**
```csharp
// Fast: Direct property access
var value = instance.MyProperty; // Direct access
```

**Performance Improvements:**
- 🚀 **Faster type operations** - direct delegates instead of reflection invocation
- ⚡ **Faster startup** - no runtime assembly/type scanning
- 💾 **Less reflection work at runtime** - metadata is built at compile time

### AOT Compatibility

Native AOT compilation requires all code to be statically analyzable at compile time. The source generator ensures:
- ✅ No `Type.GetType()` or `Assembly.GetTypes()` at runtime
- ✅ No `MethodInfo.Invoke()` or `PropertyInfo.GetValue()` at runtime
- ✅ All types and members discovered at compile time
- ✅ Compatible with IL trimming and tree shaking

### Example: Publishing AOT

```bash
dotnet publish -c Release -r win-x64 /p:PublishAot=true
```

This produces:
- 📦 **Small executable** (~10-50MB for typical apps)
- ⚡ **Fast startup** (milliseconds instead of seconds)
- 🚀 **No JIT overhead** - native machine code

## Multi-Project Setup

For solutions with multiple CQRS projects, simply reference the Zerra package in each project containing CQRS types:

```
Solution/
├── Pets.Domain/               ← Add Zerra package reference
│   ├── Commands/
│   ├── Events/
│   └── Queries/
├── Pets.Service/              ← Add Zerra package reference
│   └── Handlers/
└── Pets.Client/               ← Add Zerra package reference (if creating commands/queries locally)
```

The source generator automatically runs in each project and generates metadata for the types it contains.

## Viewing Generated Code

To inspect what the source generator creates:

1. **Visual Studio**: Expand **Dependencies → Analyzers → Zerra.SourceGeneration** in Solution Explorer
2. **Command Line**: Set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` in the project, build, then look under `obj/Debug/net10.0/generated/Zerra.SourceGeneration/`
3. **Build Output**: Enable detailed build output to see generation messages

The initializer file is named:
```
ZerraSourceGenerationInitializer.cs
```

## Troubleshooting

### Build Errors After Adding Source Generator

**Problem**: Build fails with type resolution errors

**Solution**: Clean and rebuild the solution
```bash
dotnet clean
dotnet build
```

### Source Generator Not Running

**Problem**: Expected generated code is missing

**Checklist**:
- ✅ Zerra package is referenced in the project
- ✅ Project contains CQRS types (commands, queries, events, or handlers)
- ✅ Project targets `net10.0`, or `netstandard2.0`/.NET Framework (the generator runs for both; native AOT needs `net10.0`)
- ✅ Clean and rebuild the project

### AOT Publishing Warnings

**Problem**: AOT publish shows trim warnings

**Solution**: Ensure all CQRS projects reference the Zerra package. The generated code is trim-safe.

## Best Practices

### ✅ Do

- **Reference Zerra package in all CQRS projects** - Ensures complete type coverage
- **Reference Zerra in Domain projects** - Where commands, queries, and events are defined
- **Reference Zerra in Service projects** - Where handlers are implemented
- **Clean rebuild after adding** - Ensures fresh generation

### ❌ Don't

- **Don't use runtime reflection** - Let source generator handle type operations
- **Don't mix reflection and generated code** - Use one approach consistently

## Integration with Other Features

The source generator works seamlessly with all Zerra features:

- ✅ **Serializers** - [ZerraByteSerializer](Serializers.md) and [ZerraJsonSerializer](Serializers.md) use generated metadata
- ✅ **Encryptors** - [ZerraEncryptor](Encryptors.md) works with generated serialization
- ✅ **Logging** - [IBusLogger](Logging.md) receives full type information from generated code
- ✅ **Service Injection** - [BusServices](ServiceInjection.md) uses generated type metadata
- ✅ **Bus Routing** - [Commands](Commands.md), [Queries](Queries.md), and [Events](Events.md) use generated routing logic
- ✅ **EnumName** - [EnumName](EnumName.md) enum name mappings are prebuilt by the source generator for AOT-safe string conversion and parsing

## Example: Full Setup

The generator runs only in projects that reference the Zerra package directly: NuGet does not flow analyzers through project references by default. So reference `Zerra` in each project that defines or handles CQRS types.

Here's a complete example showing source generator setup in a real project:

### Domain Project (Pets.Domain.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Zerra" Version="*" />
    </ItemGroup>
</Project>
```

### Service Project (Pets.Service.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <PublishAot>true</PublishAot>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\Pets.Domain\Pets.Domain.csproj" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Zerra" Version="*" />
    </ItemGroup>
</Project>
```

### Client Project (Pets.Client.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <PublishAot>true</PublishAot>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\Pets.Domain\Pets.Domain.csproj" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Zerra" Version="*" />
    </ItemGroup>
</Project>
```

## Summary

Zerra's source generator is essential for:
- 🎯 **High-performance CQRS** applications
- 🚀 **Native AOT compilation** support
- ⚡ **Fast startup and execution**
- 📦 **Small deployment sizes**

Simply reference the Zerra package in your CQRS projects, and the source generator will automatically handle all reflection operations at compile time.

---

**Related Documentation:**
- [Queries](Queries.md) - Query pattern with generated routing
- [Commands](Commands.md) - Command pattern with generated handlers
- [Events](Events.md) - Event pattern with generated dispatching
- [Serializers](Serializers.md) - Serialization with generated metadata
- [Server Setup](ServerSetup.md) - Configure servers with AOT support
- [Client Setup](ClientSetup.md) - Configure clients with AOT support
