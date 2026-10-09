// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;

namespace Zerra.Repository.Test
{
    //code first generation run again after the models change, each version must leave the store matching its models with the rows kept
    public static class SchemaChangeTests
    {
        public static readonly string[] Tables = ["SchemaChange", "SchemaChangeParent", "SchemaStoreProperties"];

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
            }
        }

        public static void Test(ITransactStoreEngine engine, Action<string> dropTable)
        {
            foreach (var table in Tables)
                dropTable(table);

            var v1Types = new[] { typeof(Parent), typeof(V1.Model) };
            CodeFirstGeneration.Generate(engine, DataStoreGenerationType.CodeFirst, v1Types);
            RepoTest.AssertSchemaMatchesModels(engine, v1Types);

            var repo1 = Repo.New();
            repo1.AddProvider(new TransactStoreProvider<Parent>(engine));
            repo1.AddProvider(new TransactStoreProvider<V1.Model>(engine));
            var parent = new Parent() { ID = Guid.NewGuid(), Name = "parent" };
            repo1.Create(parent);
            var model = new V1.Model() { ID = Guid.NewGuid(), Number = 5, Removed = "gone", RemovedNotNull = 6, ParentID = parent.ID, OtherParentID = parent.ID, DroppedParentID = parent.ID };
            repo1.Create(model);

            var v2Types = new[] { typeof(Parent), typeof(V2.Model) };
            CodeFirstGeneration.Generate(engine, DataStoreGenerationType.CodeFirst, v2Types);
            RepoTest.AssertSchemaMatchesModels(engine, v2Types);

            var repo2 = Repo.New();
            repo2.AddProvider(new TransactStoreProvider<Parent>(engine));
            repo2.AddProvider(new TransactStoreProvider<V2.Model>(engine));
            var stored = repo2.Single<V2.Model>(x => x.ID == model.ID);
            Assert.NotNull(stored);
            Assert.Equal(5L, stored.Number);
            Assert.Equal(parent.ID, stored.ParentID);
            Assert.Equal(parent.ID, stored.DroppedParentID);
            //the non-null columns added to a table with rows get the type's default
            Assert.Equal(0, stored.Added);
            Assert.Equal(String.Empty, stored.AddedWithStoreName);

            //the rows written after the change read back with the new columns
            var model2 = new V2.Model() { ID = Guid.NewGuid(), Number = 5_000_000_000L, ParentID = null, Added = 7, AddedWithStoreName = "renamed" };
            repo2.Create(model2);
            var stored2 = repo2.Single<V2.Model>(x => x.ID == model2.ID);
            Assert.NotNull(stored2);
            Assert.Equal(5_000_000_000L, stored2.Number);
            Assert.Null(stored2.ParentID);
            Assert.Equal(7, stored2.Added);
            Assert.Equal("renamed", stored2.AddedWithStoreName);

            //back to the first version, the relations are added again and the non-null columns come back on a table that has rows
            repo2.Delete(model2);
            CodeFirstGeneration.Generate(engine, DataStoreGenerationType.CodeFirst, v1Types);
            RepoTest.AssertSchemaMatchesModels(engine, v1Types);

            TestStoreProperties(engine);
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
            CodeFirstGeneration.Generate(engine, DataStoreGenerationType.CodeFirst | DataStoreGenerationType.Preview, types, log);
            Assert.Contains(log.Infos, x => x.StartsWith("CodeFirst Plan Preview: "));
            var planned = engine.BuildStoreGenerationPlan(true, true, true, types.Select(x => Zerra.Repository.Reflection.ModelAnalyzer.GetModel(x)).ToArray()).Plan;
            Assert.NotEmpty(planned);
            Assert.Contains(log.Infos, x => x.StartsWith($"CodeFirst Plan Preview: {planned.Count} Steps"));

            CodeFirstGeneration.Generate(engine, DataStoreGenerationType.CodeFirst, types);
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
            var stored = repo.Single<StorePropertiesModel>(x => x.ID == model.ID);
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
