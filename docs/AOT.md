[← Back to Documentation](Index.md)

# AOT and Source Generation

The `Zerra` package includes a source generator that builds, at compile time, what Zerra would otherwise build at runtime with reflection and dynamic code: type details for serialization and mapping, query proxies, handler invocation, and message routing. That makes startup faster, and lets Zerra applications publish as Native AOT.

## Setup

Reference the `Zerra` package in every project that declares or implements CQRS types, since NuGet doesn't flow the generator through project references:

```xml
<ItemGroup>
    <PackageReference Include="Zerra" Version="*" />
</ItemGroup>
```

To publish as Native AOT, add `PublishAot` to the executable project:

```xml
<PropertyGroup>
    <PublishAot>true</PublishAot>
</PropertyGroup>
```

`PublishAot` also turns off dynamic code under `dotnet run`, so a type the generator missed fails during development instead of first in production.

Inside this repository, reference the projects instead of the package, adding the generator as an analyzer:

```xml
<ProjectReference Include="..\..\Framework\Zerra\Zerra.csproj" />
<ProjectReference Include="..\..\Framework\Zerra.SourceGeneration\Zerra.SourceGeneration.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

## What Is Generated

The generator produces type details for:

- Commands and events (`ICommand`, `IEvent`) and query and command handler interfaces (`IQueryHandler`, `ICommandHandler`)
- Every model those types use as a property, parameter, or return type, recursively
- Types marked `[GenerateTypeDetail]`, or with an attribute derived from it, such as Zerra.Repository's `[Entity]`
- `IMapDefinition` implementations, `AggregateRoot` types, and `IAggregateEvent` types

It also generates:

- A proxy class per query interface, used by `bus.Call<T>()`
- Handler invocation code per handler method
- Command and event routing metadata per handler interface
- Name mappings for enums using [EnumName](EnumName.md)

`[IgnoreGenerateTypeDetail]` excludes a type.

Everything is registered by a module initializer in `ZerraSourceGenerationInitializer.cs`, so nothing needs calling at startup. It prints one line per assembly, such as `Source Generation Startup - Store.Catalog.Service: 12 ms`. That line is expected.

To read the generated code, expand **Dependencies → Analyzers → Zerra.SourceGeneration** in Visual Studio, or set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` and look under `obj/Debug/net10.0/generated/Zerra.SourceGeneration/`.

## .NET Standard 2.0

The generator also runs for `netstandard2.0` and .NET Framework projects. There is no Native AOT there, so a type the generator missed is built at runtime as usual.

Those projects default to C# 7.3, and the generated module initializer needs C# 9, so the build fails with `ZERRA001`. Set `<LangVersion>9.0</LangVersion>` or higher.

## Troubleshooting

- **A type fails under AOT with `NotSupportedException`:** Zerra needed a type detail the generator didn't produce. Add `[GenerateTypeDetail]` to the type, or check that the project declaring it references the `Zerra` package.
- **Generated code is missing:** check the project references `Zerra` directly, then clean and rebuild.
- **Trim warnings on publish:** Zerra itself publishes without trim or AOT warnings. Warnings usually come from third-party libraries such as database or broker clients.

## See Also

- [Reflection](Reflection.md) - `TypeAnalyzer` and `TypeDetail`
- [Serializers](Serializers.md) - Serialization using the generated type details
