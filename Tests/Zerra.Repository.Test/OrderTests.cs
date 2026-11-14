// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Repository.Linq;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    public class OrderTests
    {
        [Entity("QueryOrderModel")]
        public sealed class OrderModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int A { get; set; }
            public string? B { get; set; }
            public long C { get; set; }
            public override string ToString() => $"{A}{B}{C}";
        }

        //every combination of two values for each member, shuffled
        private static readonly OrderModel[] models = Enumerable.Range(0, 8)
            .Select(i => new OrderModel { ID = Guid.NewGuid(), A = i % 2, B = (i / 2 % 2).ToString(), C = i / 4 })
            .OrderBy(x => x.ID)
            .ToArray();

        //a repository query starts its order over at each OrderBy, as LINQ says, where a list's stable sort keeps the earlier one for ties
        private static readonly Lazy<IRepo> repo = new(() =>
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<OrderModel>(new MemoryEngine()));
            repo.CreateMany<OrderModel>(models);
            return repo;
        });

        private static IOrderedEnumerable<OrderModel> By<T>(IEnumerable<OrderModel> source, Func<OrderModel, T> key, bool descending)
            => descending ? source.OrderByDescending(key) : source.OrderBy(key);
        private static IOrderedEnumerable<OrderModel> Then<T>(IOrderedEnumerable<OrderModel> source, Func<OrderModel, T> key, bool descending)
            => descending ? source.ThenByDescending(key) : source.ThenBy(key);

        private static IEnumerable<OrderModel> Expected(bool d1, bool? d2, bool? d3)
        {
            var result = By(models, x => x.A, d1);
            if (d2.HasValue)
                result = Then(result, x => x.B, d2.Value);
            if (d3.HasValue)
                result = Then(result, x => x.C, d3.Value);
            return result;
        }

        private static readonly Expression<Func<OrderModel, int>> a = x => x.A;
        private static readonly Expression<Func<OrderModel, string?>> b = x => x.B;
        private static readonly Expression<Func<OrderModel, long>> c = x => x.C;

        //each order as the flags of its keys a, b, c: whether descending, or null for no key
        public static TheoryData<string, bool, bool?, bool?> Orders => new()
        {
            { "a", false, null, null },
            { "a desc", true, null, null },
            { "a b", false, false, null },
            { "a b desc", false, true, null },
            { "a desc b", true, false, null },
            { "a desc b desc", true, true, null },
            { "a b c", false, false, false },
            { "a desc b c", true, false, false },
            { "a desc b desc c", true, true, false },
            { "a desc b c desc", true, false, true },
            { "a b desc c desc", false, true, true },
            { "a b desc c", false, true, false },
            { "a b c desc", false, false, true },
            { "a desc b desc c desc", true, true, true },
        };

        private static LinqOrder[] Order(bool d1, bool? d2, bool? d3)
        {
            var orders = new List<LinqOrder> { new(a, d1) };
            if (d2.HasValue)
                orders.Add(new(b, d2.Value));
            if (d3.HasValue)
                orders.Add(new(c, d3.Value));
            return orders.ToArray();
        }

        [Theory]
        [MemberData(nameof(Orders))]
        public void OrdersLikeThenBy(string name, bool d1, bool? d2, bool? d3)
        {
            var expected = String.Join(" ", Expected(d1, d2, d3));
            Assert.Equal(expected, String.Join(" ", LinqHelper.Query(models, null, Order(d1, d2, d3), null, null)));

            var query = d1 ? repo.Value.Query<OrderModel>().OrderByDescending(a) : repo.Value.Query<OrderModel>().OrderBy(a);
            if (d2.HasValue)
                query = d2.Value ? query.ThenByDescending(b) : query.ThenBy(b);
            if (d3.HasValue)
                query = d3.Value ? query.ThenByDescending(c) : query.ThenBy(c);
            Assert.Equal(expected, String.Join(" ", query.ToArray()));
            Assert.NotEmpty(name);
        }

        [Fact]
        public void Query_WhereOrderSkipTake()
        {
            Expression<Func<OrderModel, bool>> where = x => x.C == 0;
            var expected = String.Join(" ", models.Where(x => x.C == 0).OrderByDescending(x => x.A).ThenBy(x => x.B).Skip(1).Take(2));

            Assert.Equal(expected, String.Join(" ", LinqHelper.Query(models, where, Order(true, false, null), 1, 2)));
            Assert.Equal(8, LinqHelper.Query(models, null, null, null, null).Count());
            Assert.Equal("x => x.A desc, x => x.B", String.Join(", ", Order(true, false, null).Select(x => x.ToString())));

            //a where for another type is rejected
            Expression<Func<TestRelationsModel, bool>> otherWhere = x => true;
            _ = Assert.Throws<ArgumentException>(() => LinqHelper.Query(models, otherWhere, null, null, null).ToArray());
        }

        [Fact]
        public async Task Repo_OrdersSkipsAndTakes()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<OrderModel>(new MemoryEngine()));
            foreach (var model in models)
                repo.Create(model);

            var ordered = repo.Query<OrderModel>().OrderByDescending(a).ThenBy(b).ThenByDescending(c);
            var expected = String.Join(" ", Expected(true, false, true).Skip(2).Take(3));
            Assert.Equal(expected, String.Join(" ", ordered.Skip(2).Take(3).ToArray()));
            Assert.Equal(expected, String.Join(" ", await ordered.Skip(2).Take(3).ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken)));
            Assert.Equal(Expected(true, false, true).First().ToString(), ordered.First().ToString());
        }
    }
}
