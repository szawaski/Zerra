[← Back to Documentation](Index.md)

# Mapper

The Mapper converts objects between types by matching members by name, converting their types where needed, and applying custom mappings you define. It also deep copies objects, and takes a [Graph](Graph.md) to map only some members. It is AOT compatible.

## Mapping

```csharp
using Zerra.Map;

var model = dto.Map<PersonModel>();               // new instance, source type inferred
var model2 = dto.Map<PersonDto, PersonModel>();   // explicit types, slightly faster
dto.MapTo(existingModel);                         // onto an existing instance

var copy = original.Copy();                       // deep copy
object copy2 = obj.Copy(obj.GetType());           // deep copy by runtime type
```

Members are matched by name, case-sensitively, and need a public getter on the source and a public setter on the target. Unmatched target members keep their default value. Matched members are converted as needed:

- **Numbers and other convertible types:** `int` to `double`, `decimal` to `int`, and so on
- **Enums to strings:** `Status.Active` becomes `"Active"`
- **Collections:** arrays, `List<T>`, `HashSet<T>`, `IEnumerable<T>` and the other collection types convert into each other, converting their items
- **Dictionaries:** keys and values are converted
- **Nested objects:** mapped recursively

```csharp
public class Source { public int Age { get; set; } public int[] Tags { get; set; } public Dictionary<string, int> Scores { get; set; } }
public class Target { public double Age { get; set; } public List<int> Tags { get; set; } public Dictionary<string, double> Scores { get; set; } }

var target = source.Map<Target>(); // Age, Tags, and Scores are all converted
```

## Custom Mappings

When members don't line up by name, implement `IMapDefinition<TSource, TTarget>`. Members you don't define are still mapped by name.

```csharp
public class PersonDtoToModelMap : IMapDefinition<PersonDto, PersonModel>
{
    public void Define(IMapSetup<PersonDto, PersonModel> map)
    {
        // PersonDto → PersonModel
        map.Define(target => target.FullName, source => $"{source.FirstName} {source.LastName}");
        map.Define(target => target.Age, source => DateTime.Now.Year - source.BirthYear);

        // PersonModel → PersonDto
        map.DefineReverse(source => source.FirstName, target => target.FullName.Split(' ')[0]);

        // both directions
        map.DefineTwoWay(target => target.Nickname, source => source.PreferredName);
    }
}

var model = dto.Map<PersonModel>();   // uses the definition
var back = model.Map<PersonDto>();    // uses the reverse definitions
```

| Method | Direction |
|---|---|
| `Define(target member, source expression)` | source to target |
| `DefineReverse(source member, target expression)` | target to source |
| `DefineTwoWay(target member, source member)` | both, as a direct member-to-member mapping |

### Registration

The source generator finds every `IMapDefinition` implementation at compile time and registers it, so there is nothing to call. The implementation must be public and not generic.

If source generation isn't available, register them at startup instead:

```csharp
MapDiscovery.Initialize();                           // finds them by reflection; not for Native AOT
MapDefinition.Register(new PersonDtoToModelMap());   // one at a time; works everywhere
```

## Mapping Only Some Members

Pass a [Graph](Graph.md) to map or copy only the members it includes. The rest keep their default values, or their current values with `MapTo`:

```csharp
var summary = person.Map<Person>(new Graph<Person>(x => x.Name, x => x.Address.City));

updates.MapTo(existing, new Graph<Person>(x => x.Email)); // apply only Email
```

## Custom Converters

To change how one type converts to another everywhere, derive from `MapConverter<TSource, TTarget>` and register it:

```csharp
using Zerra.Map.Converters;

public sealed class DateToStringConverter : MapConverter<DateTime, string>
{
    public override string? Map(DateTime source, string? target, Graph? graph) => source.ToString("yyyy-MM-dd");
}

Mapper.AddConverter(typeof(DateTime), typeof(string), () => new DateToStringConverter());
```

## API Reference

```csharp
TTarget Map<TTarget>(this object source, Graph? graph = null)
TTarget Map<TTarget>(this object source, Type sourceType, Graph? graph = null)
TTarget Map<TSource, TTarget>(this TSource source, Graph? graph = null)
object Map(this object source, Type sourceType, Type targetType, Graph? graph = null)

void MapTo<TSource, TTarget>(this TSource source, TTarget target, Graph? graph = null)
void MapTo(this object source, Type sourceType, object target, Type targetType, Graph? graph = null)

TTarget Copy<TTarget>(this TTarget source, Graph? graph = null)
TTarget Copy<TTarget>(this object source, Graph? graph = null)
object Copy(this object source, Type sourceType, Graph? graph = null)

void Mapper.AddConverter(Type sourceType, Type targetType, Func<MapConverter> converter)
void MapDefinition.Register<TSource, TTarget>(IMapDefinition<TSource, TTarget> mapDefinition)
void MapDiscovery.Initialize()
```

## Troubleshooting

- **A member isn't mapped:** check the names match exactly, including case, that the types can convert, and that the source has a public getter and the target a public setter.
- **A custom definition is ignored:** check its type arguments match the source and target types exactly, and that it is public and not generic. Check the build output for source generator warnings. `MapDefinition.Register` confirms whether registration is the problem.

## See Also

- [Graph](Graph.md) - Selecting members
- [AOT](AOT.md) - Source generation
