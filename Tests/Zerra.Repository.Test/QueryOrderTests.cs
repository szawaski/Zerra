// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    public class QueryOrderTests
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

        public static TheoryData<string, QueryOrder<OrderModel>, bool, bool?, bool?> Orders => new()
        {
            { "a", QueryOrder<OrderModel>.Create(a), false, null, null },
            { "a desc", QueryOrder<OrderModel>.Create(a, true), true, null, null },
            { "a b", QueryOrder<OrderModel>.Create(a, b), false, false, null },
            { "a b desc", QueryOrder<OrderModel>.Create(a, b, true), false, true, null },
            { "a desc b", QueryOrder<OrderModel>.Create(a, true, b), true, false, null },
            { "a desc b desc", QueryOrder<OrderModel>.Create(a, true, b, true), true, true, null },
            { "a b c", QueryOrder<OrderModel>.Create(a, b, c), false, false, false },
            { "a desc b c", QueryOrder<OrderModel>.Create(a, true, b, c), true, false, false },
            { "a desc b desc c", QueryOrder<OrderModel>.Create(a, true, b, true, c), true, true, false },
            { "a desc b c desc", QueryOrder<OrderModel>.Create(a, true, b, c, true), true, false, true },
            { "a b desc c desc", QueryOrder<OrderModel>.Create(a, b, true, c, true), false, true, true },
            { "a b desc c", QueryOrder<OrderModel>.Create(a, b, true, c), false, true, false },
            { "a b c desc", QueryOrder<OrderModel>.Create(a, b, c, true), false, false, true },
            { "a desc b desc c desc", QueryOrder<OrderModel>.Create(a, true, b, true, c, true), true, true, true },
        };

        [Theory]
        [MemberData(nameof(Orders))]
        public void OrdersLikeThenBy(string name, QueryOrder<OrderModel> order, bool d1, bool? d2, bool? d3)
        {
            var expected = String.Join(" ", Expected(d1, d2, d3));
            Assert.Equal(expected, String.Join(" ", models.OrderBy(order)));
            Assert.Equal(expected, String.Join(" ", models.AsQueryable().OrderBy(order)));
            Assert.Equal(d3.HasValue ? 3 : d2.HasValue ? 2 : 1, order.OrderExpressions.Length);
            Assert.Equal(d1, order.OrderExpressions[0].Descending);
            Assert.NotEmpty(name);
        }

        [Fact]
        public void Query_WhereOrderSkipTake()
        {
            Expression<Func<OrderModel, bool>> where = x => x.C == 0;
            var order = QueryOrder<OrderModel>.Create(a, true, b);
            var expected = String.Join(" ", models.Where(x => x.C == 0).OrderByDescending(x => x.A).ThenBy(x => x.B).Skip(1).Take(2));

            Assert.Equal(expected, String.Join(" ", models.Query(where, order, 1, 2)));
            Assert.Equal(expected, String.Join(" ", models.AsQueryable().Query(where, order, 1, 2)));
            var query = new Query(QueryOperation.Many, typeof(OrderModel), where, order, 1, 2, null);
            Assert.Equal(expected, String.Join(" ", models.Query(query)));
            Assert.Equal(expected, String.Join(" ", models.AsQueryable().Query(query)));

            Assert.Equal(8, models.Query(null, null, null, null).Count());
            Assert.Equal(8, models.AsQueryable().Query(null, null, null, null).Count());

            //a where for another type is rejected
            Expression<Func<TestRelationsModel, bool>> otherWhere = x => true;
            _ = Assert.Throws<ArgumentException>(() => models.Query(otherWhere, null, null, null).ToArray());
            _ = Assert.Throws<ArgumentException>(() => models.AsQueryable().Query(otherWhere, null, null, null).ToArray());
            _ = Assert.Throws<ArgumentException>(() => models.Query(new Query(QueryOperation.Many, typeof(OrderModel), otherWhere, null, null, null, null)).ToArray());
            _ = Assert.Throws<ArgumentException>(() => models.AsQueryable().Query(new Query(QueryOperation.Many, typeof(OrderModel), otherWhere, null, null, null, null)).ToArray());

            //an order for another type is rejected
            var otherOrder = QueryOrder<TestRelationsModel>.Create(x => x.RelationAKey);
            _ = Assert.Throws<ArgumentException>(() => models.OrderBy(otherOrder).ToArray());
            _ = Assert.Throws<ArgumentException>(() => models.AsQueryable().OrderBy(otherOrder).ToArray());
        }

        [Fact]
        public async Task Repo_OrdersSkipsAndTakes()
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<OrderModel>(new MemoryEngine()));
            foreach (var model in models)
                repo.Create(model);

            var order = QueryOrder<OrderModel>.Create(a, true, b, c, true);
            var expected = String.Join(" ", Expected(true, false, true).Skip(2).Take(3));
            Assert.Equal(expected, String.Join(" ", repo.Many<OrderModel>(x => true, order, 2, 3)));
            Assert.Equal(expected, String.Join(" ", await repo.ManyAsync<OrderModel>(x => true, order, 2, 3)));
            Assert.Equal(Expected(true, false, true).First().ToString(), repo.First<OrderModel>(x => true, order)!.ToString());
        }
    }
}
