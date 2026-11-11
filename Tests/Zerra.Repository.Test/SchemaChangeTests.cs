// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;

namespace Zerra.Repository.Test
{
    //code first generation run again after the models change, each version must leave the store matching its models with the rows kept
    public static class SchemaChangeTests
    {
        //no name for zero, so a new column takes the first, and the text needs escaping in the column's default
        public enum SchemaKind
        {
            [EnumName("it's first")]
            First = 1,
            Second = 2
        }

        public static readonly string[] Tables = ["SchemaIndex", "SchemaChange", "SchemaChangeParent", "SchemaStoreProperties", "SchemaSteps"];

        [Entity("SchemaChangeParent")]
        public sealed class Parent
        {
            [Identity]
            public Guid ID { get; set; }
            public string? Name { get; set; }
        }

        public static class V1
        {
            [Entity("SchemaChange")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                public int Number { get; set; }
                [StoreProperties(false, 20)]
                public string? Removed { get; set; }
                public int RemovedNotNull { get; set; }
                public Guid ParentID { get; set; }
                [Relation(nameof(ParentID))]
                public Parent? Parent { get; set; }
                public Guid OtherParentID { get; set; }
                [Relation(nameof(OtherParentID))]
                public Parent? OtherParent { get; set; }
                public Guid? DroppedParentID { get; set; }
                [Relation(nameof(DroppedParentID))]
                public Parent? DroppedParent { get; set; }
            }
        }

        public static class V2
        {
            [Entity("SchemaChange")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                //a different type
                public long Number { get; set; }
                //nullable now, the column has a foreign key
                public Guid? ParentID { get; set; }
                [Relation(nameof(ParentID))]
                public Parent? Parent { get; set; }
                //the relation is gone, the column stays
                public Guid? DroppedParentID { get; set; }
                //new columns on a table that has rows
                public int Added { get; set; }
                [StoreName("AddedRenamed")]
                [StoreProperties(true, 20)]
                public string? AddedWithStoreName { get; set; }
                //an index needs a length, which an enum has from its names
                [StoreIndex]
                public SchemaKind AddedKind { get; set; }
                public SchemaKind? AddedMaybeKind { get; set; }
            }
        }

        public static void Test(ITransactStoreEngine engine, Action<string> dropTable)
        {
            foreach (var table in Tables)
                dropTable(table);

            var v1Types = new[] { typeof(Parent), typeof(V1.Model) };
            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v1Types);
            RepoTest.AssertSchemaMatchesModels(engine, v1Types);

            var repo1 = Repo.New();
            repo1.AddProvider(new TransactStoreProvider<Parent>(engine));
            repo1.AddProvider(new TransactStoreProvider<V1.Model>(engine));
            var parent = new Parent() { ID = Guid.NewGuid(), Name = "parent" };
            repo1.Create(parent);
            var model = new V1.Model() { ID = Guid.NewGuid(), Number = 5, Removed = "gone", RemovedNotNull = 6, ParentID = parent.ID, OtherParentID = parent.ID, DroppedParentID = parent.ID };
            repo1.Create(model);

            var v2Types = new[] { typeof(Parent), typeof(V2.Model) };
            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v2Types);
            RepoTest.AssertSchemaMatchesModels(engine, v2Types);

            var repo2 = Repo.New();
            repo2.AddProvider(new TransactStoreProvider<Parent>(engine));
            repo2.AddProvider(new TransactStoreProvider<V2.Model>(engine));
            var stored = repo2.Query<V2.Model>().SingleOrDefault(x => x.ID == model.ID);
            Assert.NotNull(stored);
            Assert.Equal(5L, stored.Number);
            Assert.Equal(parent.ID, stored.ParentID);
            Assert.Equal(parent.ID, stored.DroppedParentID);
            //the non-null columns added to a table with rows get the type's default
            Assert.Equal(0, stored.Added);
            Assert.Equal(String.Empty, stored.AddedWithStoreName);
            Assert.Equal(SchemaKind.First, stored.AddedKind);
            Assert.Null(stored.AddedMaybeKind);

            //the rows written after the change read back with the new columns
            var model2 = new V2.Model() { ID = Guid.NewGuid(), AddedKind = SchemaKind.Second, AddedMaybeKind = SchemaKind.First, Number = 5_000_000_000L, ParentID = null, Added = 7, AddedWithStoreName = "renamed" };
            repo2.Create(model2);
            var stored2 = repo2.Query<V2.Model>().SingleOrDefault(x => x.ID == model2.ID);
            Assert.NotNull(stored2);
            Assert.Equal(5_000_000_000L, stored2.Number);
            Assert.Null(stored2.ParentID);
            Assert.Equal(7, stored2.Added);
            Assert.Equal("renamed", stored2.AddedWithStoreName);
            Assert.Equal(SchemaKind.Second, stored2.AddedKind);
            Assert.Equal(SchemaKind.First, stored2.AddedMaybeKind);

            //back to the first version, the dropped columns and relations are added again, on an empty table since a new foreign key column holds no parent
            repo2.Delete(model2);
            repo2.Delete(stored);
            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v1Types);
            RepoTest.AssertSchemaMatchesModels(engine, v1Types);

            TestStoreProperties(engine);
            TestIndexes(engine);
            TestSteps(engine);
        }

        public static class StepsV1
        {
            [Entity("SchemaSteps")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                [StoreProperties(20)]
                public string? Name { get; set; }
                public int Count { get; set; }
                public int Removed { get; set; }
            }
        }

        public static class StepsV2
        {
            [Entity("SchemaSteps")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                //wider
                [StoreProperties(50)]
                public string? Name { get; set; }
                public long Count { get; set; }
                //new, unique, so the rows need their values before the index
                [StoreIndex(Unique = true)]
                public int Added { get; set; }
            }
        }

        //columns, then moving the data, then constraints, with the code of both versions writing the table between
        private static void TestSteps(ITransactStoreEngine engine)
        {
            var v1Types = new[] { typeof(StepsV1.Model) };
            var v2Types = new[] { typeof(StepsV2.Model) };
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v1Types);
            RepoTest.AssertSchemaMatchesModels(engine, v1Types);

            var repo1 = Repo.New();
            repo1.AddProvider(new TransactStoreProvider<StepsV1.Model>(engine));
            var repo2 = Repo.New();
            repo2.AddProvider(new TransactStoreProvider<StepsV2.Model>(engine));

            var oldIDs = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            repo1.Create(new StepsV1.Model() { ID = oldIDs[0], Name = "a", Count = 1, Removed = 1 });
            repo1.Create(new StepsV1.Model() { ID = oldIDs[1], Name = "b", Count = 2, Removed = 2 });

            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns, v2Types);

            //the old code still writes the removed column, the new code writes the wider columns without it
            repo1.Create(new StepsV1.Model() { ID = oldIDs[2], Name = "c", Count = 3, Removed = 3 });
            var newModel = new StepsV2.Model() { ID = Guid.NewGuid(), Name = new string('x', 40), Count = 5_000_000_000L, Added = 100 };
            repo2.Create(newModel);

            //the old rows have no value for the new column yet
            _ = Assert.ThrowsAny<Exception>(() => CodeFirstGeneration.Generate(engine, CodeFirstMode.Constraints, v2Types));

            for (var i = 0; i < oldIDs.Length; i++)
            {
                var id = oldIDs[i];
                _ = repo2.Update(x => x.ID == id, new StepsV2.Model() { Added = i + 1 }, new Graph<StepsV2.Model>(x => x.Added));
            }

            CodeFirstGeneration.Generate(engine, CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v2Types);
            RepoTest.AssertSchemaMatchesModels(engine, v2Types);

            var stored = repo2.Query<StepsV2.Model>().OrderBy(x => x.Added).ToArray();
            Assert.Equal([1, 2, 3, 100], stored.Select(x => x.Added).ToArray());
            Assert.Equal(new string('x', 40), stored[3].Name);
            Assert.Equal(5_000_000_000L, stored[3].Count);
            _ = Assert.ThrowsAny<Exception>(() => repo2.Create(new StepsV2.Model() { ID = Guid.NewGuid(), Added = 100 }));
        }

        public static class IndexV1
        {
            [Entity("SchemaIndex")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                [StoreIndex]
                public int Number { get; set; }
                [StoreIndex(Unique = true)]
                [StoreProperties(50)]
                public string? Code { get; set; }
                [StoreIndex("CustomerDate", Order = 1, Descending = true)]
                public DateTime Date { get; set; }
                [StoreIndex("CustomerDate")]
                public Guid CustomerID { get; set; }
                [StoreIndex]
                public long BecomesUnique { get; set; }
                [StoreIndex]
                public int Removed { get; set; }
                [StoreIndex]
                public int TypeChanged { get; set; }
                public Guid? ParentID { get; set; }
                [Relation(nameof(ParentID))]
                public Parent? Parent { get; set; }
                [StoreIndex]
                public int DroppedIndexed { get; set; }
                [StoreIndex("Pair")]
                public int PairKept { get; set; }
                [StoreIndex("Pair")]
                public int PairDropped { get; set; }
            }
        }

        public static class IndexV2
        {
            [Entity("SchemaIndex")]
            public sealed class Model
            {
                [Identity]
                public Guid ID { get; set; }
                [StoreIndex]
                public int Number { get; set; }
                [StoreIndex(Unique = true)]
                [StoreProperties(50)]
                public string? Code { get; set; }
                //now ascending and the column order is swapped
                [StoreIndex("CustomerDate")]
                public DateTime Date { get; set; }
                [StoreIndex("CustomerDate", Order = 1)]
                public Guid CustomerID { get; set; }
                [StoreIndex(Unique = true)]
                public long BecomesUnique { get; set; }
                //the index is gone, the column stays
                public int Removed { get; set; }
                //a different type, the index stays
                [StoreIndex]
                public long TypeChanged { get; set; }
                [StoreIndex]
                public int Added { get; set; }
                //an index on the foreign key column
                [StoreIndex]
                public Guid? ParentID { get; set; }
                [Relation(nameof(ParentID))]
                public Parent? Parent { get; set; }
                //the columns DroppedIndexed and PairDropped are dropped with their indexes, the column left from the pair has none
                public int PairKept { get; set; }
            }
        }

        private static void TestIndexes(ITransactStoreEngine engine)
        {
            var v1Types = new[] { typeof(Parent), typeof(IndexV1.Model) };
            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v1Types);
            RepoTest.AssertSchemaMatchesModels(engine, v1Types);

            var repo1 = Repo.New();
            repo1.AddProvider(new TransactStoreProvider<IndexV1.Model>(engine));
            repo1.Create(new IndexV1.Model() { ID = Guid.NewGuid(), Number = 1, Code = "a", Date = new DateTime(2024, 1, 1), CustomerID = Guid.NewGuid(), BecomesUnique = 1, Removed = 1, TypeChanged = 1 });
            repo1.Create(new IndexV1.Model() { ID = Guid.NewGuid(), Number = 1, Code = null, Date = new DateTime(2024, 1, 1), CustomerID = Guid.NewGuid(), BecomesUnique = 2, Removed = 1, TypeChanged = 1 });
            //a unique index allows many nulls
            repo1.Create(new IndexV1.Model() { ID = Guid.NewGuid(), Number = 1, Code = null, Date = new DateTime(2024, 1, 1), CustomerID = Guid.NewGuid(), BecomesUnique = 3, Removed = 1, TypeChanged = 1 });
            _ = Assert.ThrowsAny<Exception>(() => repo1.Create(new IndexV1.Model() { ID = Guid.NewGuid(), Number = 1, Code = "a", Date = new DateTime(2024, 1, 1), CustomerID = Guid.NewGuid(), BecomesUnique = 4, Removed = 1, TypeChanged = 1 }));

            var v2Types = new[] { typeof(Parent), typeof(IndexV2.Model) };
            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v2Types);
            RepoTest.AssertSchemaMatchesModels(engine, v2Types);

            var repo2 = Repo.New();
            repo2.AddProvider(new TransactStoreProvider<IndexV2.Model>(engine));
            Assert.Equal(3L, repo2.Query<IndexV2.Model>().Count());
            _ = Assert.ThrowsAny<Exception>(() => repo2.Create(new IndexV2.Model() { ID = Guid.NewGuid(), Code = "b", BecomesUnique = 1 }));

            //back to the first version, the indexes it had come back
            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, v1Types);
            RepoTest.AssertSchemaMatchesModels(engine, v1Types);
        }

        [Entity("SchemaStoreProperties")]
        public sealed class StorePropertiesModel
        {
            [Identity]
            public Guid ID { get; set; }
            [StoreProperties(StoreTextEncoding.NonUnicode, 30)]
            public string? NonUnicodeLength { get; set; }
            [StoreProperties(StoreTextEncoding.NonUnicode)]
            public string? NonUnicode { get; set; }
            [StoreProperties(40)]
            public string? Length { get; set; }
            [StoreProperties(18, 4)]
            public decimal Money { get; set; }
            [StoreProperties(StoreDatePart.Date)]
            public DateTime DateOnlyPart { get; set; }
            [StoreProperties(StoreDatePart.DateTime, 3)]
            public DateTime Milliseconds { get; set; }
        }

        private sealed class InfoLog : Zerra.Logging.ILogger
        {
            public readonly List<string> Infos = new();
            public void Trace(string message) { }
            public void Debug(string message) { }
            public void Info(string message) { lock (Infos) Infos.Add(message); }
            public void Warn(string message) { }
            public void Error(string? message = null, Exception? ex = null) { }
            public void Error(Exception? ex = null) { }
            public void Critical(string? message = null, Exception? ex = null) { }
            public void Critical(Exception? ex = null) { }
        }

        private static void TestStoreProperties(ITransactStoreEngine engine)
        {
            var types = new[] { typeof(StorePropertiesModel) };

            //a preview logs the plan and changes nothing
            var log = new InfoLog();
            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns | CodeFirstMode.Preview, types, log);
            Assert.Contains(log.Infos, x => x.StartsWith("CodeFirst Plan Preview: "));
            var planned = engine.BuildStoreGenerationPlan(true, true, true, true, types.Select(x => Zerra.Repository.Reflection.ModelAnalyzer.GetModel(x)).ToArray()).Plan;
            Assert.NotEmpty(planned);
            Assert.Contains(log.Infos, x => x.StartsWith($"CodeFirst Plan Preview: {planned.Count} Steps"));

            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore | CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, types);
            RepoTest.AssertSchemaMatchesModels(engine, types);

            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<StorePropertiesModel>(engine));
            var model = new StorePropertiesModel()
            {
                ID = Guid.NewGuid(),
                NonUnicodeLength = "short text",
                NonUnicode = new string('a', 500),
                Length = "forty",
                Money = 12345.6789m,
                DateOnlyPart = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc),
                Milliseconds = new DateTime(2024, 5, 6, 7, 8, 9, 123, DateTimeKind.Utc),
            };
            repo.Create(model);
            var stored = repo.Query<StorePropertiesModel>().SingleOrDefault(x => x.ID == model.ID);
            Assert.NotNull(stored);
            Assert.Equal(model.NonUnicodeLength, stored.NonUnicodeLength);
            Assert.Equal(model.NonUnicode, stored.NonUnicode);
            Assert.Equal(model.Length, stored.Length);
            Assert.Equal(model.Money, stored.Money);
            Assert.Equal(new DateTime(2024, 5, 6), stored.DateOnlyPart.Date);
            Assert.Equal(TimeSpan.Zero, stored.DateOnlyPart.TimeOfDay);
            Assert.Equal(model.Milliseconds.Ticks, stored.Milliseconds.Ticks);
            repo.Delete(stored);
        }
    }
}
