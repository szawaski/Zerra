// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using Xunit;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    public class DatabaseFirstTests
    {
        //reads the tables code-first just made, compiles the models written for them, and code-first then has nothing to change
        internal static void TestSequence(ITransactStoreEngine engine, Type[] modelTypes)
        {
            //the indexes of every kind come back too
            modelTypes = modelTypes.Append(typeof(StoreIndexTests.IndexModel)).ToArray();
            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, [typeof(StoreIndexTests.IndexModel)]);

            var models = DatabaseFirstGeneration.Generate(engine, "DatabaseFirstTest", "DataModel");

            var tableNames = modelTypes.Select(x => ModelAnalyzer.GetModel(x).DataSourceEntityName).ToArray();
            foreach (var tableName in tableNames)
                Assert.Contains(models, x => String.Equals(x.TableName, tableName, StringComparison.OrdinalIgnoreCase));

            var assembly = Compile(models);
            var types = assembly.GetTypes().Where(x => x.GetCustomAttribute<EntityAttribute>() is not null).ToArray();
            Assert.Equal(models.Count, types.Length);

            var modelDetails = types.Select(x => ModelAnalyzer.GetModel(x)).ToArray();
            var plan = engine.BuildStoreGenerationPlan(true, true, true, true, modelDetails);
            Assert.True(plan.Plan.Count == 0, $"Schema changes planned for the generated models:{Environment.NewLine}{String.Join(Environment.NewLine, plan.Plan)}");

            //the relations of the test models come back in both directions
            var relations = ModelAnalyzer.GetModel(types.Single(x => String.Equals(x.GetCustomAttribute<EntityAttribute>()!.StoreName, ModelAnalyzer.GetModel(typeof(CompositeRelationTests.LineModel)).DataSourceEntityName, StringComparison.OrdinalIgnoreCase)));
            var orderRelation = Assert.Single(relations.RelatedNonEnumerableMembers);
            Assert.Equal(2, orderRelation.ForeignIdentities!.Length);
            var orders = ModelAnalyzer.GetModel(orderRelation.ActualType);
            var linesRelation = Assert.Single(orders.RelatedEnumerableMembers);
            Assert.Equal(relations.Type, linesRelation.ActualType);

            var indexes = ModelAnalyzer.GetModel(types.Single(x => String.Equals(x.GetCustomAttribute<EntityAttribute>()!.StoreName, "IndexModel", StringComparison.OrdinalIgnoreCase)));
            var expected = ModelAnalyzer.GetModel(typeof(StoreIndexTests.IndexModel));
            Assert.Equal(expected.Indexes.Count, indexes.Indexes.Count);
            foreach (var index in expected.Indexes)
            {
                var found = indexes.Indexes.Single(x => String.Equals(x.StoreName, index.StoreName, StringComparison.OrdinalIgnoreCase));
                Assert.Equal(index.IsUnique, found.IsUnique);
                Assert.Equal(index.Members.Select(x => x.StoreName.ToLower()), found.Members.Select(x => x.StoreName.ToLower()));
                Assert.Equal(index.Descending, found.Descending);
            }
        }

        private static Assembly Compile(IReadOnlyList<DatabaseFirstModel> models)
        {
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Append(typeof(EntityAttribute).Assembly.Location)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(x => MetadataReference.CreateFromFile(x))
                .ToArray();

            var compilation = CSharpCompilation.Create(
                "DatabaseFirstTest" + Guid.NewGuid().ToString("N"),
                models.Select(x => CSharpSyntaxTree.ParseText(x.Source, path: x.FileName)),
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

            using var stream = new MemoryStream();
            var result = compilation.Emit(stream);
            var problems = result.Diagnostics.Where(x => x.Severity >= DiagnosticSeverity.Warning).ToArray();
            Assert.True(result.Success && problems.Length == 0, String.Join(Environment.NewLine, problems.Select(x => x.ToString())) + Environment.NewLine + String.Join(Environment.NewLine + Environment.NewLine, models.Select(x => x.Source)));
            return Assembly.Load(stream.ToArray());
        }

        [Fact]
        public void Names()
        {
            var tables = new[]
            {
                new DataStoreSchemaTable("Customer", [Column("CustomerID", typeof(int), true, true, true), Column("Name", typeof(string), true, length: 64), Column("2nd Phone", typeof(string), false, length: 20)], [], []),
                new DataStoreSchemaTable("Order", [Column("ID", typeof(Guid), true, true), Column("CustomerID", typeof(int), true), Column("BackupCustomerID", typeof(int), false), Column("Order", typeof(int), true)],
                    [new DataStoreSchemaForeignKey("FK_Order_Customer", ["CustomerID"], "Customer", ["CustomerID"]), new DataStoreSchemaForeignKey("FK_Order_Customer2", ["BackupCustomerID"], "Customer", ["CustomerID"])], []),
            };

            var models = DatabaseFirstGeneration.Generate(tables, "MyApp.DataModels");
            var customer = models.Single(x => x.TableName == "Customer");
            var order = models.Single(x => x.TableName == "Order");

            Assert.Equal("CustomerDataModel", customer.TypeName);
            Assert.Equal("CustomerDataModel.cs", customer.FileName);
            Assert.Contains("namespace MyApp.DataModels;", customer.Source);
            Assert.Contains("[Entity(\"Customer\")]\r\npublic partial class CustomerDataModel", customer.Source.Replace(Environment.NewLine, "\r\n"));
            Assert.Contains("    [Identity(true)]\r\n    public int CustomerID { get; set; }", customer.Source.Replace(Environment.NewLine, "\r\n"));
            Assert.Contains("    [StoreProperties(64)]\r\n    public string Name { get; set; } = null!;", customer.Source.Replace(Environment.NewLine, "\r\n"));
            //a name C# can't use keeps the column name in StoreName, a name StoreName can't hold isn't stored
            Assert.Contains("    [StoreExclude]\r\n    [StoreProperties(20)]\r\n    public string? ndPhone { get; set; }", customer.Source.Replace(Environment.NewLine, "\r\n"));

            Assert.Contains("\r\n    public int Order { get; set; }", order.Source.Replace(Environment.NewLine, "\r\n"));
            //without a suffix the member named like its class is renamed
            var orderWithoutSuffix = DatabaseFirstGeneration.Generate(tables, "MyApp.DataModels", "").Single(x => x.TableName == "Order");
            Assert.Equal("Order", orderWithoutSuffix.TypeName);
            Assert.Contains("    [StoreName(\"Order\")]\r\n    public int OrderValue { get; set; }", orderWithoutSuffix.Source.Replace(Environment.NewLine, "\r\n"));
            _ = Compile(DatabaseFirstGeneration.Generate(tables, "MyApp.DataModels", ""));
            Assert.Contains("    [Relation(nameof(CustomerID))]\r\n    public CustomerDataModel? Customer { get; set; }", order.Source.Replace(Environment.NewLine, "\r\n"));
            Assert.Contains("    [Relation(nameof(BackupCustomerID))]\r\n    public CustomerDataModel? BackupCustomer { get; set; }", order.Source.Replace(Environment.NewLine, "\r\n"));
            //two keys to the same table name the collections by their columns
            Assert.Contains("    [Relation(nameof(OrderDataModel.CustomerID))]\r\n    public List<OrderDataModel>? OrdersByCustomerID { get; set; }", customer.Source.Replace(Environment.NewLine, "\r\n"));
            Assert.Contains("    [Relation(nameof(OrderDataModel.BackupCustomerID))]\r\n    public List<OrderDataModel>? OrdersByBackupCustomerID { get; set; }", customer.Source.Replace(Environment.NewLine, "\r\n"));
            Assert.Contains("using System.Collections.Generic;", customer.Source);
            Assert.DoesNotContain("using System.Collections.Generic;", order.Source);

            _ = Compile(models);
        }

        [Fact]
        public void StoreProperties()
        {
            var tables = new[]
            {
                new DataStoreSchemaTable("Thing", [
                    Column("ID", typeof(long), true, true),
                    Column("Count", typeof(int), true),
                    Column("CountNull", typeof(int), false),
                    Column("Price", typeof(decimal), true, precision: 18, scale: 2),
                    Column("PriceNull", typeof(decimal), false, precision: 18, scale: 2),
                    Column("Code", typeof(string), true, length: 10, encoding: StoreTextEncoding.NonUnicode),
                    Column("Notes", typeof(string), false),
                    Column("Day", typeof(DateTime), true, datePart: StoreDatePart.Date),
                    Column("Data", typeof(byte[]), true),
                    Column("DataNull", typeof(byte[]), false),
                    Column("DataLength", typeof(byte[]), true, length: 16),
                    Column("Money", typeof(decimal), false, precision: 19, scale: 4, note: "money in the data store, code-first writes decimal(19, 4)"),
                    Column("Shape", typeof(object), false, note: "geometry in the data store, which models can't hold"),
                    ], [], []),
            };

            var source = DatabaseFirstGeneration.Generate(tables, "MyApp").Single().Source.Replace(Environment.NewLine, "\r\n");

            Assert.Contains("    [Identity]\r\n    public long ID { get; set; }", source);
            Assert.Contains("\r\n    public int Count { get; set; }", source);
            Assert.Contains("\r\n    public int? CountNull { get; set; }", source);
            Assert.DoesNotContain("[StoreProperties(false)]\r\n    public int? CountNull", source);
            Assert.Contains("    [StoreProperties(18, 2)]\r\n    public decimal Price { get; set; }", source);
            Assert.Contains("    [StoreProperties(18, 2)]\r\n    public decimal? PriceNull { get; set; }", source);
            Assert.Contains("    [StoreProperties(StoreTextEncoding.NonUnicode, 10)]\r\n    public string Code { get; set; } = null!;", source);
            Assert.Contains("\r\n    public string? Notes { get; set; }", source);
            Assert.Contains("    [StoreProperties(StoreDatePart.Date)]\r\n    public DateTime Day { get; set; }", source);
            //NOT NULL is the member's nullability, the attribute never says it
            Assert.DoesNotContain("NotNull", source);
            Assert.Contains("\r\n    public byte[] Data { get; set; } = null!;", source);
            Assert.Contains("\r\n    public byte[]? DataNull { get; set; }", source);
            Assert.Contains("    [StoreProperties(16)]\r\n    public byte[] DataLength { get; set; } = null!;", source);
            Assert.Contains("    //money in the data store, code-first writes decimal(19, 4)\r\n    [StoreProperties(19, 4)]\r\n    public decimal? Money { get; set; }", source);
            Assert.Contains("    //geometry in the data store, which models can't hold\r\n    [StoreExclude]\r\n    public object? Shape { get; set; }", source);

            var model = ModelAnalyzer.GetModel(Compile(DatabaseFirstGeneration.Generate(tables, "MyApp")).GetTypes().Single());
            Assert.True(model.GetMember("Price").IsDataSourceNotNull);
            Assert.False(model.GetMember("PriceNull").IsDataSourceNotNull);
            Assert.True(model.GetMember("Code").IsDataSourceNotNull);
            Assert.Equal(StoreTextEncoding.NonUnicode, model.GetMember("Code").TextEncoding);
            Assert.True(model.GetMember("Day").IsDataSourceNotNull);
            Assert.Equal(StoreDatePart.Date, model.GetMember("Day").DatePart);
            Assert.True(model.GetMember("Data").IsDataSourceNotNull);
            Assert.False(model.GetMember("DataNull").IsDataSourceNotNull);
            Assert.True(model.GetMember("DataLength").IsDataSourceNotNull);
        }

        [Fact]
        public void Indexes()
        {
            var tables = new[]
            {
                new DataStoreSchemaTable("Item", [Column("ID", typeof(int), true, true), Column("Sku", typeof(string), true, length: 32), Column("Category", typeof(int), true), Column("Rank", typeof(int), true), Column("Notes", typeof(string), false)], [],
                [
                    new DataStoreSchemaIndex("IX_Item_Sku", true, ["Sku"], [false]),
                    new DataStoreSchemaIndex("IX_Item_ByCategory", false, ["Category", "Rank"], [false, true]),
                    new DataStoreSchemaIndex("SomeOtherName", false, ["Rank"], [false]),
                    new DataStoreSchemaIndex("IX_Item_Notes", false, ["Notes"], [false]),
                ]),
            };

            var source = DatabaseFirstGeneration.Generate(tables, "MyApp").Single().Source.Replace(Environment.NewLine, "\r\n");

            Assert.Contains("    [StoreIndex(Unique = true)]\r\n    public string Sku { get; set; } = null!;", source);
            Assert.Contains("    [StoreIndex(\"ByCategory\")]\r\n    public int Category { get; set; }", source);
            Assert.Contains("    [StoreIndex(\"ByCategory\", Order = 1, Descending = true)]\r\n    public int Rank { get; set; }", source);
            Assert.Contains("//Index SomeOtherName isn't named IX_Item_...", source);
            Assert.Contains("//Index IX_Item_Notes has a column without a length", source);

            var model = ModelAnalyzer.GetModel(Compile(DatabaseFirstGeneration.Generate(tables, "MyApp")).GetTypes().Single());
            Assert.Equal(["IX_Item_ByCategory", "IX_Item_Sku"], model.Indexes.Select(x => x.StoreName).OrderBy(x => x).ToArray());
            var byCategory = model.Indexes.Single(x => x.StoreName == "IX_Item_ByCategory");
            Assert.Equal(["Category", "Rank"], byCategory.Members.Select(x => x.Name).ToArray());
            Assert.Equal([false, true], byCategory.Descending.ToArray());
        }

        [Fact]
        public void ForeignKeysThatCantBeRelations()
        {
            var tables = new[]
            {
                new DataStoreSchemaTable("Parent", [Column("ID", typeof(int), true, true), Column("Code", typeof(int), true)], [], []),
                new DataStoreSchemaTable("Child", [Column("ID", typeof(int), true, true), Column("ParentCode", typeof(int), true), Column("OtherID", typeof(int), true)],
                    [new DataStoreSchemaForeignKey("FK_Child_Parent", ["ParentCode"], "Parent", ["Code"]), new DataStoreSchemaForeignKey("FK_Child_Other", ["OtherID"], "Other", ["ID"])], []),
            };

            var models = DatabaseFirstGeneration.Generate(tables, "MyApp");
            var child = models.Single(x => x.TableName == "Child").Source;
            Assert.Contains("//Foreign key FK_Child_Parent doesn't refer to the primary key of Parent", child);
            Assert.Contains("//Foreign key FK_Child_Other refers to Other, which wasn't read", child);
            Assert.DoesNotContain("[Relation", child);
            Assert.DoesNotContain("[Relation", models.Single(x => x.TableName == "Parent").Source);
        }

        [Fact]
        public void CollectionNames()
        {
            var tables = new[]
            {
                new DataStoreSchemaTable("Owner", [Column("ID", typeof(int), true, true)], [], []),
                new DataStoreSchemaTable("Items", [Column("ID", typeof(int), true, true), Column("OwnerKey", typeof(int), true)], [new DataStoreSchemaForeignKey("FK_Items_Owner", ["OwnerKey"], "Owner", ["ID"])], []),
                new DataStoreSchemaTable("Category", [Column("ID", typeof(int), true, true), Column("OwnerID", typeof(int), true)], [new DataStoreSchemaForeignKey("FK_Category_Owner", ["OwnerID"], "Owner", ["ID"])], []),
                new DataStoreSchemaTable("Address", [Column("ID", typeof(int), true, true), Column("OwnerID", typeof(int), true)], [new DataStoreSchemaForeignKey("FK_Address_Owner", ["OwnerID"], "Owner", ["ID"])], []),
            };

            var models = DatabaseFirstGeneration.Generate(tables, "MyApp");
            var owner = models.Single(x => x.TableName == "Owner").Source;
            Assert.Contains("public List<ItemsDataModel>? Items { get; set; }", owner);
            Assert.Contains("public List<CategoryDataModel>? Categories { get; set; }", owner);
            Assert.Contains("public List<AddressDataModel>? Addresses { get; set; }", owner);
            //OwnerKey holds the key of Owner
            Assert.Contains("[Relation(nameof(OwnerKey))]" + Environment.NewLine + "    public OwnerDataModel? Owner { get; set; }", models.Single(x => x.TableName == "Items").Source);
            _ = Compile(models);
        }

        private static DataStoreSchemaColumn Column(string name, Type type, bool notNull, bool isIdentity = false, bool isAutoGenerated = false, int? length = null, int? precision = null, int? scale = null, StoreTextEncoding encoding = StoreTextEncoding.Unicode, StoreDatePart datePart = StoreDatePart.DateTime, string? note = null)
            => new(name, type, notNull, isIdentity, isAutoGenerated, encoding, datePart, length ?? precision, scale, type.Name, note);
    }
}
