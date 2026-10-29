[← Back to Documentation](Index.md) | [← Back to Repository](Repository.md)

# Repository Generation

Zerra Repository supports two approaches to manage your data store schema and model classes:

| Approach | Direction | Description |
|---|---|---|
| **Code First** | Model → Database | Define C# model classes and let Zerra generate/update the schema |
| **Database First (T4)** | Database → Model | Reverse-engineer an existing SQL Server database into C# model and provider classes |

---

## Code First Generation

With Code First, you define your data model classes in C# and Zerra generates the corresponding database schema (tables, columns, relationships). It can also update the schema as your models evolve.

### 1. Define Model Classes

Annotate your model classes with the `[Entity]`, `[Identity]`, `[StoreProperties]`, and `[Relation]` attributes:

```csharp
[Entity("PetType")]
public sealed class PetTypeDataModel
{
    [Identity(true)]   // auto-generated primary key
    public int Id { get; set; }

    [StoreProperties(true, 64)]   // not null, max length 64
    public string? Name { get; set; }
}

[Entity("Pet")]
public sealed class PetDataModel
{
    [Identity(true)]
    public int Id { get; set; }

    [StoreProperties(true, 128)]   // not null, max length 128
    public string? Name { get; set; }

    public int PetTypeId { get; set; }

    [Relation(nameof(PetTypeId))]   // foreign key navigation property
    public PetTypeDataModel? PetType { get; set; }
}
```

#### Attribute Reference

| Attribute | Target | Description |
|---|---|---|
| `[Entity("StoreName")]` | Class | Maps the class to a data store table. `StoreName` defaults to class name if omitted. |
| `[Identity]` | Property | Marks the primary key. Pass `true` for auto-generated (identity/serial). |
| `[StoreProperties(notNull, length)]` | Property | Controls nullability, max length, precision, scale, text encoding, or date part. |
| `[Relation("ForeignKeyPropertyName")]` | Property | Marks a navigation property linked by the named foreign key property. |
| `[StoreIndex]` or `[StoreIndex("Name")]` | Property | Indexes the column. Members with the same name form one combined index. Set `Unique`, `Order`, or `Descending`. Indexed strings and `byte[]` need a length. |

### 2. Call `CodeFirstGeneration.Generate`

Call this at application startup, passing your store's engine, the model types to manage, and an `ILogger` to receive the generation output:

```csharp
ILogger log = new ConsoleLogger(); // your ILogger implementation
var engine = new MsSqlEngine(connectionString);

CodeFirstGeneration.Generate(
    engine,
    DataStoreGenerationType.CodeFirst,
    [typeof(PetTypeDataModel), typeof(PetDataModel)],
    log);
```

#### `DataStoreGenerationType` Flags

| Flag | Description |
|---|---|
| `CodeFirst` | Apply schema changes to the data store. |
| `Preview` | Log the planned changes without applying them. |
| `NoCreate` | Skip creating new tables or columns. |
| `NoUpdate` | Skip modifying existing tables or columns. |
| `NoDelete` | Skip dropping removed tables or columns. |

Flags can be combined:

```csharp
// Preview only — logs the plan without applying any changes
CodeFirstGeneration.Generate(
    engine,
    DataStoreGenerationType.CodeFirst | DataStoreGenerationType.Preview,
    [typeof(PetTypeDataModel), typeof(PetDataModel)],
    log);

// Apply creates and updates, but never drop anything
CodeFirstGeneration.Generate(
    engine,
    DataStoreGenerationType.CodeFirst | DataStoreGenerationType.NoDelete,
    [typeof(PetTypeDataModel), typeof(PetDataModel)],
    log);
```

---

## Database First (T4 Reverse Engineering)

The `Zerra.T4` project provides T4 templates that connect to an existing SQL Server database and generate data model classes with the appropriate Zerra attributes.

> **Note:** The T4 templates currently support **SQL Server (MsSql)** only.

### Setup

1. Add a reference to `Zerra.T4.dll` in your project. A pre-built binary is available at `Front End Scripts/Binaries/Zerra.T4.dll`.
2. Add the T4 template files to your project (copy from `Framework\Zerra.T4\MsSqlFirst\`).

### Generate Model Classes

Add a `.tt` file to your project with the following content, filling in your connection string, target namespace, and desired model class suffix:

```
<#@ template language="C#" debug="false" hostspecific="true" #>
<#@ assembly name="Front End Scripts\Binaries\Zerra.T4.dll" #>
<#
    const string connectionString = "data source=.;initial catalog=MyDatabase;integrated security=True;MultipleActiveResultSets=True;";
    const string namespaceString = "MyProject.Domain.DataModels";
    const string modelSuffix = "DataModel";
    var result = Zerra.T4.MsSqlFirst.GenerateModels(connectionString, namespaceString, modelSuffix);
    #><#=result#><#
#>
```

Saving or running the template generates a `.cs` file containing one model class per database table.

### Registering the Models

Register a `TransactStoreProvider<TModel>` for each generated model in `Program.cs`:

```csharp
var engine = new MsSqlEngine(connectionString);
var repo = Repo.New();
repo.AddProvider(new TransactStoreProvider<PetTypeDataModel>(engine));
repo.AddProvider(new TransactStoreProvider<PetDataModel>(engine));

var busServices = new BusServices();
busServices.AddRepo(repo);
```

---