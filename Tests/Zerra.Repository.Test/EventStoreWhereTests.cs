// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    //an event store reads the streams named by the identities in the where, then applies the rest of the where to the models
    public class EventStoreWhereTests
    {
        [Entity("EventStoreWhereModel")]
        public sealed class WhereModel
        {
            [Identity]
            public Guid ID { get; set; }
            public string? Name { get; set; }
            public int Number { get; set; }
            public int? Maybe { get; set; }
            public DateTime Date { get; set; }
        }

        private sealed class Holder
        {
            public Guid ID;
            public Guid[] IDs = [];
            public WhereModel? Model;
        }

        private static class Static
        {
            public static WhereModel Holder { get; } = new() { ID = second };
        }

        private static readonly Guid first = Guid.NewGuid();
        private static readonly Guid second = Guid.NewGuid();

        private static IRepo CreateRepo()
        {
            var repo = Repo.New();
            repo.AddProvider(new EventStoreAsTransactStoreProvider<WhereModel>(new MemoryEngine()));
            repo.Create(new WhereModel { ID = first, Name = "apple", Number = 1, Maybe = 1, Date = new DateTime(2024, 1, 2) });
            repo.Create(new WhereModel { ID = second, Name = "banana", Number = 2, Date = new DateTime(2024, 3, 4) });
            return repo;
        }

        public static TheoryData<string, Expression<Func<WhereModel, bool>>, Guid[]> Wheres()
        {
            var holder = new Holder { ID = first, IDs = [first, second], Model = new WhereModel { ID = second } };
            var list = new List<Guid> { first, second };
            var firstText = first.ToString();
            var both = new[] { first, second };
            var useFirst = true;
            Func<Guid> getSecond = () => second;
            return new()
            {
                { "equal", x => x.ID == first, [first] },
                { "reversed", x => first == x.ID, [first] },
                { "captured field", x => x.ID == holder.ID, [first] },
                { "captured member of a member", x => x.ID == holder.Model!.ID, [second] },
                { "parsed", x => x.ID == Guid.Parse(firstText), [first] },
                { "constructed", x => x.ID == new Guid(firstText), [first] },
                { "array contains", x => holder.IDs.Contains(x.ID), [first, second] },
                { "list contains", x => list.Contains(x.ID), [first, second] },
                { "new array contains", x => new[] { first, second }.Contains(x.ID), [first, second] },
                { "new list contains", x => new List<Guid> { first }.Contains(x.ID), [first] },
                { "static member of a member", x => x.ID == Static.Holder.ID, [second] },
                { "coalesce", x => x.ID == (holder.Model ?? Static.Holder).ID, [second] },
                { "and array index", x => x.ID == holder.IDs[1] && x.Number == 2, [second] },
                { "and type is", x => x.ID == first && x.Name is string, [first] },
                { "or", x => x.ID == first || x.ID == second, [first, second] },
                { "not not equal", x => !(x.ID != first), [first] },
                { "and number", x => x.ID == first && x.Number == 1, [first] },
                { "and number filtered out", x => x.ID == first && x.Number > 1, [] },
                { "and nullable value", x => x.ID == first && x.Maybe!.Value == 1, [first] },
                { "and date part", x => (x.ID == first || x.ID == second) && x.Date.Year == 2024 && x.Date.Month == 3, [second] },
                { "and string call", x => (x.ID == first || x.ID == second) && x.Name!.StartsWith("b"), [second] },
                { "and string length", x => (x.ID == first || x.ID == second) && x.Name!.Length == 5, [first] },
                { "and string contains", x => (x.ID == first || x.ID == second) && x.Name!.Contains("pp"), [first] },
                { "and conditional", x => (x.ID == first || x.ID == second) && (x.Number > 1 ? x.Name == "banana" : x.Name == "x"), [second] },
                { "and arithmetic", x => (x.ID == first || x.ID == second) && x.Number + 1 == 2 && x.Number - 1 == 0 && x.Number * 2 == 2 && x.Number / 1 == 1 && x.Number % 2 == 1 && -x.Number == -1, [first] },
                { "and checked arithmetic", x => (x.ID == first || x.ID == second) && checked(x.Number + 1) == 3 && checked(x.Number - 1) == 1 && checked(x.Number * 2) == 4 && checked(-x.Number) == -2 && checked((long)x.Number) == 2L, [second] },
                { "and comparisons", x => (x.ID == first || x.ID == second) && x.Number >= 2 && x.Number < 3 && x.Number <= 2, [second] },
                { "not or", x => !(x.ID != first && x.ID != second) && !(x.Number >= 2 || x.Number < 0), [first] },
                { "conditional identity", x => x.ID == (useFirst ? first : second), [first] },
                { "invoked identity", x => x.ID == getSecond(), [second] },
                { "enumerable contains", x => Enumerable.Contains(both, x.ID), [first, second] },
                { "and any with lambda", x => (x.ID == first || x.ID == second) && both.Any(y => y == x.ID && x.Number == 1), [first] },
            };
        }

        [Theory]
        [MemberData(nameof(Wheres))]
        public void Many(string name, Expression<Func<WhereModel, bool>> where, Guid[] expected)
        {
            _ = name;
            var repo = CreateRepo();
            Assert.Equal(expected.Order(), repo.Many(where).Select(x => x.ID).Order());
        }

        [Theory]
        [MemberData(nameof(Wheres))]
        public async Task ManyAsync(string name, Expression<Func<WhereModel, bool>> where, Guid[] expected)
        {
            _ = name;
            var repo = CreateRepo();
            Assert.Equal(expected.Order(), (await repo.ManyAsync(where)).Select(x => x.ID).Order());
        }

        [Entity("EventStoreCompositeModel")]
        public sealed class CompositeModel
        {
            [Identity]
            public Guid A { get; set; }
            [Identity]
            public int B { get; set; }
            public string? Name { get; set; }
        }

        [Fact]
        public async Task CompositeIdentity_ReadsEachCombination()
        {
            var repo = Repo.New();
            repo.AddProvider(new EventStoreAsTransactStoreProvider<CompositeModel>(new MemoryEngine()));
            var a1 = Guid.NewGuid();
            var a2 = Guid.NewGuid();
            foreach (var a in new[] { a1, a2 })
            {
                foreach (var b in new[] { 1, 2, 3 })
                    repo.Create(new CompositeModel { A = a, B = b, Name = $"{b}" });
            }

            Assert.Equal("2", repo.Single<CompositeModel>(x => x.A == a1 && x.B == 2)!.Name);
            var one = 1;
            Assert.Equal("3", repo.Single<CompositeModel>(x => x.A == a1 && x.B == one + 2)!.Name);
            Assert.Equal(4, repo.Many<CompositeModel>(x => (x.A == a1 || x.A == a2) && (x.B == 1 || x.B == 3)).Count);
            var bs = new[] { 1, 2, 3 };
            Assert.Equal(3, (await repo.ManyAsync<CompositeModel>(x => x.A == a2 && bs.Contains(x.B))).Count);
            _ = Assert.Throws<NotSupportedException>(() => repo.Many<CompositeModel>(x => x.A == a1));
        }

        [Fact]
        public void WithoutIdentity_Throws()
        {
            var repo = CreateRepo();
            _ = Assert.Throws<NotSupportedException>(() => repo.Many<WhereModel>(null));
        }
    }
}
