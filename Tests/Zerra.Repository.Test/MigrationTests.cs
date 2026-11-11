// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    //migrations run as CodeFirstMode.Columns, then code moving the data, then Constraints and DropColumns
    public static class MigrationTests
    {
        //tables with foreign keys come before the tables they refer to
        public static readonly string[] Tables = ["MigrationPerson", "MigrationAddress", "MigrationCustomer", "MigrationNote", "MigrationWiden", "MigrationIndexed", "MigrationIndexParent", "MigrationOwned", "MigrationOwnerA", "MigrationOwnerB", "MigrationUnrelated", "MigrationDefaults", "MigrationBinary"];

        public static void Test(ITransactStoreEngine engine, Action<string> dropTable)
        {
            foreach (var table in Tables)
                dropTable(table);

            TestRenameColumn(engine);
            TestSplitTable(engine);
            TestRequireAndNarrowColumn(engine);
            TestWidenAndAllowNulls(engine);
            TestIndexChanges(engine);
            TestMoveRelation(engine);
            TestUnrelatedType(engine);
        }

        private static void AssertColumnsPlansNothing(ITransactStoreEngine engine, Type[] modelTypes)
        {
            var plan = engine.BuildStoreGenerationPlan(false, true, false, false, modelTypes.Select(x => ModelAnalyzer.GetModel(x)).ToArray()).Plan;
            Assert.True(plan.Count == 0, $"Columns planned changes:{Environment.NewLine}{String.Join(Environment.NewLine, plan)}");
        }

        public static class RenameV1
        {
            [Entity("MigrationPerson")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                [StoreProperties(true, 100)]
                public string? FullName { get; set; }
            }
        }

        public static class RenameV2
        {
            [Entity("MigrationPerson")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                [StoreProperties(true, 100)]
                public string? DisplayName { get; set; }
            }
        }

        private static void TestRenameColumn(ITransactStoreEngine engine)
        {
            var v1Types = new[] { typeof(RenameV1.Model) };
            var v2Types = new[] { typeof(RenameV2.Model) };
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v1Types);

            var repo1 = Repo.New();
            repo1.AddProvider(new TransactStoreProvider<RenameV1.Model>(engine));
            var repo2 = Repo.New();
            repo2.AddProvider(new TransactStoreProvider<RenameV2.Model>(engine));

            var ann = new RenameV1.Model() { ID = Guid.NewGuid(), FullName = "Ann" };
            var bob = new RenameV1.Model() { ID = Guid.NewGuid(), FullName = "Bob" };
            repo1.Create(ann);
            repo1.Create(bob);

            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns, v2Types);

            //the new code writes without the old column
            var cat = new RenameV2.Model() { ID = Guid.NewGuid(), DisplayName = "Cat" };
            repo2.Create(cat);

            foreach (var old in repo1.Query<RenameV1.Model>().Where(x => x.FullName != null).ToArray())
                _ = repo2.Update(x => x.ID == old.ID, new RenameV2.Model() { DisplayName = old.FullName }, new Graph<RenameV2.Model>(x => x.DisplayName));

            CodeFirstGeneration.Generate(engine, CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v2Types);
            RepoTest.AssertSchemaMatchesModels(engine, v2Types);

            var names = repo2.Query<RenameV2.Model>().OrderBy(x => x.DisplayName).Select(x => x.DisplayName).ToArray();
            Assert.Equal(["Ann", "Bob", "Cat"], names);
        }

        public static class SplitV1
        {
            [Entity("MigrationCustomer")]
            public sealed class Customer
            {
                [Identity]
                public Guid ID { get; set; }
                [StoreProperties(true, 50)]
                public string? Name { get; set; }
                [StoreProperties(100)]
                public string? Street { get; set; }
            }
        }

        public static class SplitV2
        {
            [Entity("MigrationCustomer")]
            public sealed class Customer
            {
                [Identity]
                public Guid ID { get; set; }
                [StoreProperties(true, 50)]
                public string? Name { get; set; }
            }

            [Entity("MigrationAddress")]
            public sealed class Address
            {
                [Identity]
                public Guid ID { get; set; }
                public Guid CustomerID { get; set; }
                [Relation(nameof(CustomerID))]
                public Customer? Customer { get; set; }
                [StoreProperties(true, 100)]
                public string? Street { get; set; }
            }
        }

        private static void TestSplitTable(ITransactStoreEngine engine)
        {
            var v1Types = new[] { typeof(SplitV1.Customer) };
            var v2Types = new[] { typeof(SplitV2.Customer), typeof(SplitV2.Address) };
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v1Types);

            var repo1 = Repo.New();
            repo1.AddProvider(new TransactStoreProvider<SplitV1.Customer>(engine));
            var repo2 = Repo.New();
            repo2.AddProvider(new TransactStoreProvider<SplitV2.Customer>(engine));
            repo2.AddProvider(new TransactStoreProvider<SplitV2.Address>(engine));

            repo1.Create(new SplitV1.Customer() { ID = Guid.NewGuid(), Name = "Ann", Street = "1 Main" });
            repo1.Create(new SplitV1.Customer() { ID = Guid.NewGuid(), Name = "Bob", Street = "2 Oak" });
            repo1.Create(new SplitV1.Customer() { ID = Guid.NewGuid(), Name = "Cat", Street = null });

            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns, v2Types);

            //the foreign key waits for the constraints, so an address of no customer can be written until then
            var orphan = new SplitV2.Address() { ID = Guid.NewGuid(), CustomerID = Guid.NewGuid(), Street = "nowhere" };
            repo2.Create(orphan);
            repo2.Delete(orphan);

            foreach (var old in repo1.Query<SplitV1.Customer>().Where(x => x.Street != null).ToArray())
                repo2.Create(new SplitV2.Address() { ID = Guid.NewGuid(), CustomerID = old.ID, Street = old.Street });

            CodeFirstGeneration.Generate(engine, CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v2Types);
            RepoTest.AssertSchemaMatchesModels(engine, v2Types);

            var addresses = repo2.Query<SplitV2.Address>(new Graph<SplitV2.Address>(true, x => x.Customer)).OrderBy(x => x.Street).ToArray();
            Assert.Equal(["1 Main", "2 Oak"], addresses.Select(x => x.Street).ToArray());
            Assert.Equal(["Ann", "Bob"], addresses.Select(x => x.Customer!.Name).ToArray());
            Assert.Equal(3, repo2.Query<SplitV2.Customer>().Count());
            _ = Assert.ThrowsAny<Exception>(() => repo2.Create(new SplitV2.Address() { ID = Guid.NewGuid(), CustomerID = Guid.NewGuid(), Street = "nowhere" }));
        }

        public static class NoteV1
        {
            [Entity("MigrationNote")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                [StoreProperties(50)]
                public string? Text { get; set; }
            }
        }

        public static class NoteV2
        {
            [Entity("MigrationNote")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                //required now
                [StoreProperties(true, 50)]
                public string? Text { get; set; }
            }
        }

        public static class NoteV3
        {
            [Entity("MigrationNote")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                //shorter
                [StoreProperties(true, 10)]
                public string? Text { get; set; }
            }
        }

        private static void TestRequireAndNarrowColumn(ITransactStoreEngine engine)
        {
            var v1Types = new[] { typeof(NoteV1.Model) };
            var v2Types = new[] { typeof(NoteV2.Model) };
            var v3Types = new[] { typeof(NoteV3.Model) };
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v1Types);

            var repo1 = Repo.New();
            repo1.AddProvider(new TransactStoreProvider<NoteV1.Model>(engine));
            repo1.Create(new NoteV1.Model() { ID = Guid.NewGuid(), Text = null });
            repo1.Create(new NoteV1.Model() { ID = Guid.NewGuid(), Text = "short" });
            repo1.Create(new NoteV1.Model() { ID = Guid.NewGuid(), Text = "a note longer than ten" });

            //requiring a column can fail, so it's left to the constraints, which fail while a null is left
            AssertColumnsPlansNothing(engine, v2Types);
            _ = Assert.ThrowsAny<Exception>(() => CodeFirstGeneration.Generate(engine, CodeFirstMode.Constraints, v2Types));
            _ = repo1.Update(x => x.Text == null, new NoteV1.Model() { Text = "none" }, new Graph<NoteV1.Model>(x => x.Text));
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Constraints, v2Types);
            RepoTest.AssertSchemaMatchesModels(engine, v2Types);

            //narrowing too, which fails while a value is too long
            AssertColumnsPlansNothing(engine, v3Types);
            _ = Assert.ThrowsAny<Exception>(() => CodeFirstGeneration.Generate(engine, CodeFirstMode.Constraints, v3Types));
            _ = repo1.Update(x => x.Text == "a note longer than ten", new NoteV1.Model() { Text = "a note" }, new Graph<NoteV1.Model>(x => x.Text));
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Constraints, v3Types);
            RepoTest.AssertSchemaMatchesModels(engine, v3Types);

            var repo3 = Repo.New();
            repo3.AddProvider(new TransactStoreProvider<NoteV3.Model>(engine));
            Assert.Equal(["a note", "none", "short"], repo3.Query<NoteV3.Model>().OrderBy(x => x.Text).Select(x => x.Text).ToArray());
        }

        public static class WidenV1
        {
            [Entity("MigrationWiden")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                public int Count { get; set; }
                [StoreProperties(20)]
                public string? Name { get; set; }
                [StoreProperties(10, 2)]
                public decimal Price { get; set; }
                [StoreProperties(StoreDatePart.DateTime, 3)]
                public DateTime At { get; set; }
                public float Ratio { get; set; }
                [StoreIndex(Unique = true)]
                public short Code { get; set; }
                [StoreIndex]
                [StoreProperties(StoreTextEncoding.NonUnicode, 30)]
                public string? Tag { get; set; }
            }
        }

        public static class WidenV2
        {
            [Entity("MigrationWiden")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                //allows nulls
                public int? Count { get; set; }
                //longer and required, so it is widened first and required by the constraints
                [StoreProperties(true, 50)]
                public string? Name { get; set; }
                [StoreProperties(12, 4)]
                public decimal Price { get; set; }
                [StoreProperties(StoreDatePart.DateTime, 6)]
                public DateTime At { get; set; }
                public double Ratio { get; set; }
                //its unique index is put back after the column changes
                [StoreIndex(Unique = true)]
                public int Code { get; set; }
                [StoreIndex]
                [StoreProperties(40)]
                public string? Tag { get; set; }
            }
        }

        private static void TestWidenAndAllowNulls(ITransactStoreEngine engine)
        {
            var v1Types = new[] { typeof(WidenV1.Model) };
            var v2Types = new[] { typeof(WidenV2.Model) };
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v1Types);

            var repo1 = Repo.New();
            repo1.AddProvider(new TransactStoreProvider<WidenV1.Model>(engine));
            var repo2 = Repo.New();
            repo2.AddProvider(new TransactStoreProvider<WidenV2.Model>(engine));

            var old = new WidenV1.Model() { ID = Guid.NewGuid(), Count = 1, Name = null, Price = 1.25m, At = new DateTime(2024, 1, 2, 3, 4, 5, 123), Ratio = 0.5f, Code = 1, Tag = "old" };
            repo1.Create(old);

            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns, v2Types);

            //every value the new model can hold fits now, and the name still allows nulls
            var at = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234560);
            var wide = new WidenV2.Model() { ID = Guid.NewGuid(), Count = null, Name = null, Price = 12345678.1234m, At = at, Ratio = 0.123456789012, Code = 40000, Tag = new string('ü', 40) };
            repo2.Create(wide);
            _ = Assert.ThrowsAny<Exception>(() => repo2.Create(new WidenV2.Model() { ID = Guid.NewGuid(), Name = "taken", Code = 40000 }));

            _ = Assert.ThrowsAny<Exception>(() => CodeFirstGeneration.Generate(engine, CodeFirstMode.Constraints, v2Types));
            _ = repo2.Update(x => x.Name == null, new WidenV2.Model() { Name = "named" }, new Graph<WidenV2.Model>(x => x.Name));
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Constraints, v2Types);
            RepoTest.AssertSchemaMatchesModels(engine, v2Types);

            var stored = repo2.Query<WidenV2.Model>().Where(x => x.ID == wide.ID).Single();
            Assert.Null(stored.Count);
            Assert.Equal("named", stored.Name);
            Assert.Equal(wide.Price, stored.Price);
            Assert.Equal(at.Ticks, stored.At.Ticks);
            Assert.Equal(wide.Ratio, stored.Ratio);
            Assert.Equal(40000, stored.Code);
            Assert.Equal(wide.Tag, stored.Tag);
            var storedOld = repo2.Query<WidenV2.Model>().Where(x => x.ID == old.ID).Single();
            Assert.Equal(1, storedOld.Count);
            Assert.Equal(1.25m, storedOld.Price);
            Assert.Equal("old", storedOld.Tag);
        }

        [Entity("MigrationIndexParent")]
        public sealed class IndexParent
        {
            [Identity]
            public Guid ID { get; set; }
        }

        public static class IndexedV1
        {
            [Entity("MigrationIndexed")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                [StoreIndex]
                public int A { get; set; }
                [StoreIndex("Pair", Unique = true)]
                public int X { get; set; }
                [StoreIndex("Pair", Unique = true)]
                public int Y { get; set; }
                [StoreIndex("Grow")]
                public int G { get; set; }
                public int B { get; set; }
                public Guid? ParentID { get; set; }
                [Relation(nameof(ParentID))]
                public IndexParent? Parent { get; set; }
            }
        }

        public static class IndexedV2
        {
            [Entity("MigrationIndexed")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                //no index
                public int A { get; set; }
                //no longer unique, and one column
                [StoreIndex("Pair")]
                public int X { get; set; }
                public int Y { get; set; }
                //unique now
                [StoreIndex("Grow", Unique = true)]
                public int G { get; set; }
                //a new index
                [StoreIndex]
                public int B { get; set; }
                //no relation, so no foreign key
                public Guid? ParentID { get; set; }
            }
        }

        private static void TestIndexChanges(ITransactStoreEngine engine)
        {
            var v1Types = new[] { typeof(IndexParent), typeof(IndexedV1.Model) };
            var v2Types = new[] { typeof(IndexParent), typeof(IndexedV2.Model) };
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v1Types);

            var repo1 = Repo.New();
            repo1.AddProvider(new TransactStoreProvider<IndexedV1.Model>(engine));
            var repo2 = Repo.New();
            repo2.AddProvider(new TransactStoreProvider<IndexedV2.Model>(engine));

            repo1.Create(new IndexedV1.Model() { ID = Guid.NewGuid(), A = 1, X = 1, Y = 1, G = 1, B = 1 });
            _ = Assert.ThrowsAny<Exception>(() => repo1.Create(new IndexedV1.Model() { ID = Guid.NewGuid(), A = 2, X = 1, Y = 1, G = 2, B = 2 }));

            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns, v2Types);
            AssertColumnsPlansNothing(engine, v2Types);

            //the pair isn't unique and the foreign key is gone
            repo2.Create(new IndexedV2.Model() { ID = Guid.NewGuid(), A = 2, X = 1, Y = 1, G = 2, B = 2, ParentID = Guid.NewGuid() });

            CodeFirstGeneration.Generate(engine, CodeFirstMode.Constraints, v2Types);
            RepoTest.AssertSchemaMatchesModels(engine, v2Types);
            _ = Assert.ThrowsAny<Exception>(() => repo2.Create(new IndexedV2.Model() { ID = Guid.NewGuid(), G = 2 }));
        }

        [Entity("MigrationOwnerA")]
        public sealed class OwnerA
        {
            [Identity]
            public Guid ID { get; set; }
        }

        [Entity("MigrationOwnerB")]
        public sealed class OwnerB
        {
            [Identity]
            public Guid ID { get; set; }
        }

        public static class OwnedV1
        {
            [Entity("MigrationOwned")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                public Guid OwnerID { get; set; }
                [Relation(nameof(OwnerID))]
                public OwnerA? Owner { get; set; }
            }
        }

        public static class OwnedV2
        {
            [Entity("MigrationOwned")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                //refers to the other table now
                public Guid OwnerID { get; set; }
                [Relation(nameof(OwnerID))]
                public OwnerB? Owner { get; set; }
            }
        }

        private static void TestMoveRelation(ITransactStoreEngine engine)
        {
            var v1Types = new[] { typeof(OwnerA), typeof(OwnedV1.Model) };
            var v2Types = new[] { typeof(OwnerB), typeof(OwnedV2.Model) };
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v1Types);

            var repo1 = Repo.New();
            repo1.AddProvider(new TransactStoreProvider<OwnerA>(engine));
            repo1.AddProvider(new TransactStoreProvider<OwnedV1.Model>(engine));
            var repo2 = Repo.New();
            repo2.AddProvider(new TransactStoreProvider<OwnerB>(engine));
            repo2.AddProvider(new TransactStoreProvider<OwnedV2.Model>(engine));

            var owner = new OwnerA() { ID = Guid.NewGuid() };
            repo1.Create(owner);
            var owned = new OwnedV1.Model() { ID = Guid.NewGuid(), OwnerID = owner.ID };
            repo1.Create(owned);

            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns, v2Types);

            //the new owners take the old ones' keys before the foreign key to them is added
            foreach (var old in repo1.Query<OwnerA>().ToArray())
                repo2.Create(new OwnerB() { ID = old.ID });

            CodeFirstGeneration.Generate(engine, CodeFirstMode.Constraints, v2Types);
            RepoTest.AssertSchemaMatchesModels(engine, v2Types);

            var stored = repo2.Query<OwnedV2.Model>(new Graph<OwnedV2.Model>(true, x => x.Owner)).Where(x => x.ID == owned.ID).Single();
            Assert.Equal(owner.ID, stored.Owner?.ID);
            _ = Assert.ThrowsAny<Exception>(() => repo2.Create(new OwnedV2.Model() { ID = Guid.NewGuid(), OwnerID = Guid.NewGuid() }));
        }

        public static class UnrelatedV1
        {
            [Entity("MigrationUnrelated")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                [StoreProperties(10)]
                public string? Code { get; set; }
            }
        }

        public static class UnrelatedV2
        {
            [Entity("MigrationUnrelated")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                //text to a number needs a new column and the data moved
                public int Code { get; set; }
            }
        }

        private static void TestUnrelatedType(ITransactStoreEngine engine)
        {
            var v1Types = new[] { typeof(UnrelatedV1.Model) };
            var v2Types = new[] { typeof(UnrelatedV2.Model) };
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v1Types);

            _ = Assert.Throws<InvalidOperationException>(() => CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns, v2Types));
            _ = Assert.Throws<InvalidOperationException>(() => CodeFirstGeneration.Generate(engine, CodeFirstMode.Constraints, v2Types));
            RepoTest.AssertSchemaMatchesModels(engine, v1Types);
        }

        public static class DefaultsV1
        {
            [Entity("MigrationDefaults")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                public int Amount { get; set; }
                [StoreProperties(20)]
                public string? Note { get; set; }
                public int Removed { get; set; }
            }
        }

        public static class DefaultsV2
        {
            [Entity("MigrationDefaults")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                //widened by columns
                public long Amount { get; set; }
                //required by constraints
                [StoreProperties(true, 20)]
                public string? Note { get; set; }
            }
        }

        //objects added outside the models stay through each step, the engine's test adds them and checks for them
        public static void TestKeptObjects(ITransactStoreEngine engine, Action addObjects, Action assertObjects)
        {
            var v1Types = new[] { typeof(DefaultsV1.Model) };
            var v2Types = new[] { typeof(DefaultsV2.Model) };
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v1Types);
            addObjects();

            var repo1 = Repo.New();
            repo1.AddProvider(new TransactStoreProvider<DefaultsV1.Model>(engine));
            repo1.Create(new DefaultsV1.Model() { ID = Guid.NewGuid(), Amount = 1, Note = "n", Removed = 1 });

            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns, v2Types);
            assertObjects();
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Constraints, v2Types);
            assertObjects();
            CodeFirstGeneration.Generate(engine, CodeFirstMode.DropColumns, v2Types);
            RepoTest.AssertSchemaMatchesModels(engine, v2Types);
        }

        public static class BinaryV1
        {
            [Entity("MigrationBinary")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                [StoreProperties(10)]
                public byte[]? Data { get; set; }
            }
        }

        public static class BinaryV2
        {
            [Entity("MigrationBinary")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                [StoreProperties(20)]
                public byte[]? Data { get; set; }
            }
        }

        //a longer binary column is widened by columns, a shorter one waits for the constraints
        public static void TestBinaryLength(ITransactStoreEngine engine)
        {
            var v1Types = new[] { typeof(BinaryV1.Model) };
            var v2Types = new[] { typeof(BinaryV2.Model) };
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v1Types);

            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns, v2Types);
            RepoTest.AssertSchemaMatchesModels(engine, v2Types);

            var repo2 = Repo.New();
            repo2.AddProvider(new TransactStoreProvider<BinaryV2.Model>(engine));
            var model = new BinaryV2.Model() { ID = Guid.NewGuid(), Data = Enumerable.Range(0, 20).Select(x => (byte)x).ToArray() };
            repo2.Create(model);
            Assert.Equal(model.Data, repo2.Query<BinaryV2.Model>().Where(x => x.ID == model.ID).Select(x => x.Data).Single());
            repo2.Delete(model);

            AssertColumnsPlansNothing(engine, v1Types);
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Constraints, v1Types);
            RepoTest.AssertSchemaMatchesModels(engine, v1Types);
        }
    }
}
