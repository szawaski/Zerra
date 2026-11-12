// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Memory;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    //tables in a schema other than the default, one with the same name as a default schema table
    public static class SchemaTests
    {
        //lowercase, PostgreSQL folds unquoted names and MySQL databases can be lowercased
        public const string Schema = "zerraschema";

        [Entity("SchemaTwin")]
        public sealed class DefaultTwinModel
        {
            [Identity]
            public int ID { get; set; }
            [StoreProperties(50)]
            public string? Name { get; set; }
        }

        [Entity(Schema, "SchemaTwin")]
        public sealed class SchemaTwinModel
        {
            [Identity]
            public int ID { get; set; }
            [StoreProperties(50)]
            public string? Name { get; set; }
            [StoreIndex]
            public int Number { get; set; }
        }

        [Entity(Schema, "SchemaParent")]
        public sealed class SchemaParentModel
        {
            [Identity]
            public int ID { get; set; }
            [StoreProperties(50)]
            public string? Name { get; set; }
            [Relation(nameof(SchemaChildModel.ParentID))]
            public List<SchemaChildModel>? Children { get; set; }
        }

        [Entity("SchemaChild")]
        public sealed class SchemaChildModel
        {
            [Identity]
            public int ID { get; set; }
            public int ParentID { get; set; }
            [Relation(nameof(ParentID))]
            public SchemaParentModel? Parent { get; set; }
            public int? TwinID { get; set; }
            [Relation(nameof(TwinID))]
            public SchemaTwinModel? Twin { get; set; }
        }

        public static readonly Type[] ModelTypes = [typeof(DefaultTwinModel), typeof(SchemaTwinModel), typeof(SchemaParentModel), typeof(SchemaChildModel)];

        private static IRepo NewRepo(ITransactStoreEngine engine)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<DefaultTwinModel>(engine));
            repo.AddProvider(new TransactStoreProvider<SchemaTwinModel>(engine));
            repo.AddProvider(new TransactStoreProvider<SchemaParentModel>(engine));
            repo.AddProvider(new TransactStoreProvider<SchemaChildModel>(engine));
            return repo;
        }

        public static void TestSequence(ITransactStoreEngine engine)
        {
            var repo = NewRepo(engine);

            var defaultTwin = new DefaultTwinModel { ID = 1, Name = "default" };
            var schemaTwin = new SchemaTwinModel { ID = 1, Name = "schema", Number = 5 };
            var parent = new SchemaParentModel { ID = 1, Name = "parent" };
            var child = new SchemaChildModel { ID = 1, ParentID = 1, TwinID = 1 };
            repo.Create(defaultTwin);
            repo.Create(schemaTwin);
            repo.Create(parent);
            repo.Create(child);
            try
            {
                //the same table name in two schemas holds different rows
                Assert.Equal("default", repo.Query<DefaultTwinModel>().Single().Name);
                Assert.Equal("schema", repo.Query<SchemaTwinModel>().Single().Name);
                Assert.Equal(1, repo.Query<SchemaTwinModel>().Count(x => x.Number == 5));

                //relations both ways between the schemas
                var readChild = repo.Query<SchemaChildModel>(new Graph<SchemaChildModel>(x => x.ID, x => x.ParentID, x => x.TwinID, x => x.Parent, x => x.Twin)).Single();
                Assert.Equal("parent", readChild.Parent?.Name);
                Assert.Equal("schema", readChild.Twin?.Name);
                var readParent = repo.Query<SchemaParentModel>(new Graph<SchemaParentModel>(x => x.ID, x => x.Name, x => x.Children)).Single();
                Assert.Equal(1, Assert.Single(readParent.Children!).ID);

                //joins of the relations in a where
                Assert.Equal(1, repo.Query<SchemaChildModel>().Count(x => x.Parent!.Name == "parent" && x.Twin!.Name == "schema"));
                Assert.Equal(0, repo.Query<SchemaChildModel>().Count(x => x.Twin!.Name == "default"));
                Assert.True(repo.Query<SchemaParentModel>().Any(x => x.Children!.Any(c => c.TwinID == 1)));

                //a join of the two tables of the same name
                var joined = repo.Query<DefaultTwinModel>().Join(repo.Query<SchemaTwinModel>(), x => x.ID, x => x.ID, (d, s) => new { Default = d.Name, Schema = s.Name }).Single();
                Assert.Equal("default", joined.Default);
                Assert.Equal("schema", joined.Schema);

                //writes only change their own schema's table
                schemaTwin.Name = "schema2";
                repo.Update(schemaTwin);
                Assert.Equal("default", repo.Query<DefaultTwinModel>().Single().Name);
                Assert.Equal("schema2", repo.Query<SchemaTwinModel>().Single().Name);

                Assert.Equal(1, repo.Update<SchemaTwinModel>(x => x.ID == 1, x => new SchemaTwinModel { Number = x.Number + 1 }));
                Assert.Equal(6, repo.Query<SchemaTwinModel>().Single().Number);
                Assert.Equal(1, repo.Update<SchemaChildModel>(x => x.Parent!.Name == "parent", new SchemaChildModel { TwinID = null }, new Graph<SchemaChildModel>(x => x.TwinID)));
                Assert.Null(repo.Query<SchemaChildModel>().Single().TwinID);

                repo.Create(new SchemaTwinModel { ID = 2, Name = "other" });
                Assert.Equal(1, repo.Delete<SchemaTwinModel>(x => x.Name == "other"));
                Assert.Equal(1, repo.Query<SchemaTwinModel>().Count());
                Assert.Equal(1, repo.Query<DefaultTwinModel>().Count());
            }
            finally
            {
                repo.Delete(child);
                repo.Delete(parent);
                _ = repo.Delete<SchemaTwinModel>(x => x.ID > 0);
                repo.Delete(defaultTwin);
            }
        }

        public static async Task TestSequenceAsync(ITransactStoreEngine engine)
        {
            var repo = NewRepo(engine);

            var defaultTwin = new DefaultTwinModel { ID = 3, Name = "default" };
            var schemaTwin = new SchemaTwinModel { ID = 3, Name = "schema" };
            await repo.CreateAsync(defaultTwin);
            await repo.CreateAsync(schemaTwin);
            try
            {
                Assert.Equal("default", (await repo.Query<DefaultTwinModel>().Where(x => x.ID == 3).SingleAsync()).Name);
                Assert.Equal("schema", (await repo.Query<SchemaTwinModel>().Where(x => x.ID == 3).SingleAsync()).Name);
                Assert.Equal(1, await repo.DeleteAsync<SchemaTwinModel>(x => x.ID == 3));
                Assert.Equal(1, await repo.Query<DefaultTwinModel>().CountAsync(x => x.ID == 3));
            }
            finally
            {
                await repo.DeleteAsync(defaultTwin);
            }
        }
    }

    public class SchemaMemoryTests
    {
        [Fact]
        public void EntityAttributeNames()
        {
            Assert.Equal("Name", new EntityAttribute("Name").FullName);
            Assert.Null(new EntityAttribute().FullName);
            var withSchema = new EntityAttribute("sales", "Name");
            Assert.Equal("sales", withSchema.Schema);
            Assert.Equal("Name", withSchema.StoreName);
            Assert.Equal("sales.Name", withSchema.FullName);
            Assert.Null(new EntityAttribute(null, "Name").Schema);
            _ = Assert.Throws<ArgumentException>(() => new EntityAttribute("sa.les", "Name"));

            var model = ModelAnalyzer.GetModel(typeof(SchemaTests.SchemaTwinModel));
            Assert.Equal(SchemaTests.Schema, model.DataSourceSchemaName);
            Assert.Equal("SchemaTwin", model.DataSourceEntityName);
            Assert.Equal(SchemaTests.Schema + ".SchemaTwin", model.DataSourceEntityFullName);
            var defaultModel = ModelAnalyzer.GetModel(typeof(SchemaTests.DefaultTwinModel));
            Assert.Null(defaultModel.DataSourceSchemaName);
            Assert.Equal("SchemaTwin", defaultModel.DataSourceEntityFullName);
        }

        [Fact]
        public void Memory() => SchemaTests.TestSequence(new MemoryEngine());

        [Fact]
        public Task MemoryAsync() => SchemaTests.TestSequenceAsync(new MemoryEngine());
    }
}
