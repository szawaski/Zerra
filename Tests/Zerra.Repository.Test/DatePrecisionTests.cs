// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;

namespace Zerra.Repository.Test
{
    //a date with a precision keeps microseconds through writes and queries, and one without stays the store's default
    public static class DatePrecisionTests
    {
        [Entity("DatePrecision")]
        public sealed class Model
        {
            [Identity]
            public Guid ID { get; set; }
            [StoreProperties(StoreDatePart.DateTime, 6)]
            public DateTime Precise { get; set; }
            public DateTime Plain { get; set; }
        }

        public static void Test(ITransactStoreEngine engine)
        {
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns | CodeFirstMode.Constraints | CodeFirstMode.DropColumns, [typeof(Model)]);

            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<Model>(engine));

            var precise = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234560);
            var plain = new DateTime(2024, 1, 2, 3, 4, 5, 120, DateTimeKind.Utc);
            var model = new Model() { ID = Guid.NewGuid(), Precise = precise, Plain = plain };
            repo.Create(model);

            var stored = repo.Query<Model>().Where(x => x.ID == model.ID).Single();
            Assert.Equal(precise.Ticks, stored.Precise.Ticks);

            //compared exactly, to the microsecond
            Assert.Equal(model.ID, repo.Query<Model>().Where(x => x.Precise == precise).Select(x => x.ID).Single());
            Assert.False(repo.Query<Model>().Where(x => x.Precise == precise.AddTicks(10)).Any());
            Assert.True(repo.Query<Model>().Where(x => x.Precise > precise.AddTicks(-10) && x.Precise < precise.AddTicks(10)).Any());

            //a column without a precision compares with a value finer than it holds, and with the value read from it
            var now = DateTime.UtcNow;
            Assert.True(repo.Query<Model>().Where(x => x.ID == model.ID && x.Plain < now).Any());
            Assert.True(repo.Query<Model>().Where(x => x.ID == model.ID && x.Plain == stored.Plain).Any());

            //set by a condition
            var changed = precise.AddTicks(10);
            _ = repo.Update<Model>(x => x.ID == model.ID, x => new Model() { Precise = changed });
            Assert.Equal(changed.Ticks, repo.Query<Model>().Where(x => x.ID == model.ID).Select(x => x.Precise).Single().Ticks);

            repo.Delete(model);
        }
    }
}
