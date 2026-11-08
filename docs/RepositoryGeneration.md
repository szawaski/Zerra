[← Back to Documentation](Index.md) | [← Back to Repository](Repository.md)

# Repository Generation

Zerra Repository supports two approaches to manage your data store schema and model classes:

| Approach | Direction | Description |
|---|---|---|
| **Code First** | Model → Database | Define C# model classes and let Zerra generate/update the schema |
| **Database First** | Database → Model | Write C# model classes for the tables of an existing database |

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

    [StoreProperties(64)]   // max length 64, not null because string isn't nullable
    public string Name { get; set; } = null!;
}

[Entity("Pet")]
public sealed class PetDataModel
{
    [Identity(true)]
    public int Id { get; set; }

    [StoreProperties(128)]
    public string Name { get; set; } = null!;

    public string? Nickname { get; set; }   // can be null

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
| `[StoreProperties(length)]` | Property | Controls max length, precision, scale, text encoding, date part, or nullability. A column is NOT NULL when its member can't be null: `string` and `int` can't, `string?` and `int?` can, and in code without nullable annotations every reference type can. Pass `notNull`, or set `NotNull = true` or `false`, to decide it yourself. `StoreTextEncoding.NonUnicode` stores a string without Unicode (such as `varchar` instead of `nvarchar`); Unicode is the default. `StoreDatePart.Date` stores only the date of a `DateTime`. |
| `[StoreName("Name")]` | Property | Names the column, when it differs from the property. Letters, digits, and underscores only. |
| `[StoreExclude]` | Property | Leaves the property out of the store: no column, never read or saved. |
| `[Relation("ForeignKeyPropertyName")]` | Property | Marks a navigation property linked by the named foreign key property, or by several in the order of the related identities when the related model has more than one. |
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

## Database First Generation

`DatabaseFirstGeneration.Generate` reads the tables of an existing SQL Server, PostgreSQL, MySQL, or MariaDB database and writes a model class for each. Run it once from a small console app or a test, and again whenever the database changes:

```csharp
var engine = new MsSqlEngine(connectionString);
var models = DatabaseFirstGeneration.Generate(engine, "MyProject.Domain.DataModels", "DataModel");
foreach (var model in models)
    File.WriteAllText(Path.Combine(@"..\MyProject.Domain\DataModels", model.FileName), model.Source);
```

Each model is a `partial` class in its own file, with the attributes code-first would need to build the same table:

```csharp
[Entity("Order")]
public partial class OrderDataModel
{
    [Identity(true)]
    public int ID { get; set; }

    public int CustomerID { get; set; }

    [StoreProperties(32)]
    public string Number { get; set; } = null!;

    public string? Notes { get; set; }

    [StoreProperties(18, 2)]
    public decimal Total { get; set; }

    [Relation(nameof(CustomerID))]
    public CustomerDataModel? Customer { get; set; }

    [Relation(nameof(OrderLineDataModel.OrderID))]
    public List<OrderLineDataModel>? OrderLines { get; set; }
}
```

- Running `CodeFirstGeneration` with the generated models finds nothing to change.
- A NOT NULL column is a member that can't be null (`string`, `decimal`), and a nullable one is nullable (`string?`, `decimal?`).
- Foreign keys become relations in both directions: the related object on the side holding the key, and a collection on the other side.
- Primary keys, identities, composite keys, and indexes named `IX_{Table}_...` come across as well.
- Add your own members in another file of the same `partial` class, so regenerating doesn't overwrite them.
- Where a column doesn't fit a model exactly, a comment above the member says why. For example, a SQL Server `money` column becomes a `decimal(19, 4)` member, and code-first would change the column to `decimal`.
- PostgreSQL keeps names in lowercase unless they were created quoted, so its tables and columns come back that way.
- MongoDB, the in-memory engine, and the event stores have no schema to read, and throw `NotSupportedException`.

### Registering the Models

Register a `TransactStoreProvider<TModel>` for each generated model in `Program.cs`:

```csharp
var engine = new MsSqlEngine(connectionString);
var repo = Repo.New();
repo.AddProvider(new TransactStoreProvider<OrderDataModel>(engine));
repo.AddProvider(new TransactStoreProvider<CustomerDataModel>(engine));

var busServices = new BusServices();
busServices.AddRepo(repo);
```

---