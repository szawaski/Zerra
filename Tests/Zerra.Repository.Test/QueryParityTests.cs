// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    //each where is run by the engine and by LINQ over the same models, a store must find the same models LINQ does
    public static class QueryParityTests
    {
        [Entity("QueryParity")]
        public sealed class ParityModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Number { get; set; }
            public int? Maybe { get; set; }
            public int? Other { get; set; }
            public long Big { get; set; }
            [StoreProperties(false, 18, 2)]
            public decimal Money { get; set; }
            public double Ratio { get; set; }
            public bool Flag { get; set; }
            [StoreProperties(false, 50)]
            public string? Name { get; set; }
            [StoreProperties(true, 0)]
            public DateTime Date { get; set; }
            [StoreProperties(true, 0)]
            public DateTimeOffset Moment { get; set; }
            [StoreName("Label")]
            [StoreProperties(false, 20)]
            public string? Title { get; set; }
            [StoreExclude]
            public int NotStored { get; set; }
        }

        public static readonly Type[] ModelTypes = [typeof(ParityModel)];

        private static ParityModel[] Models()
        {
            var models = new List<ParityModel>();
            var names = new string?[] { "apple", "banana", "cherry", null, "avocado", "b" };
            for (var i = 0; i < 6; i++)
            {
                models.Add(new ParityModel
                {
                    ID = Guid.NewGuid(),
                    Number = i,
                    Maybe = i % 2 == 0 ? i : null,
                    Other = i % 3 == 0 ? null : i % 4,
                    Big = 10_000_000_000L * i,
                    Money = 1.25m * i,
                    Ratio = 0.5 * i,
                    Flag = i % 3 == 0,
                    Name = names[i],
                    Date = new DateTime(2020 + i, i + 1, 10 + i, i, 30, 0, DateTimeKind.Utc),
                    Moment = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(i * 4 - 10)),
                    Title = $"t{i}",
                    NotStored = 99,
                });
            }
            return models.ToArray();
        }

        public sealed class ValueBox
        {
            public int Value { get; set; }
        }

        private static Expression<Func<ParityModel, bool>> Build(Func<ParameterExpression, Expression> body)
        {
            var x = Expression.Parameter(typeof(ParityModel), "x");
            return Expression.Lambda<Func<ParityModel, bool>>(body(x), x);
        }

        public static IEnumerable<(string Name, Expression<Func<ParityModel, bool>> Where)> Wheres()
        {
            var ids = new[] { 1, 3, 5 };
            var list = new List<int> { 2, 4 };
            var date = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            //the same instant written with a negative offset
            var offset = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(-10));
            var nearOffset = new DateTimeOffset(2024, 1, 1, 11, 30, 0, TimeSpan.FromMinutes(-30));
            var money = 2.5m;
            var name = "banana";

            yield return ("equal", x => x.Number == 3);
            yield return ("not equal", x => x.Number != 3);
            yield return ("greater", x => x.Number > 3);
            yield return ("greater or equal", x => x.Number >= 3);
            yield return ("less", x => x.Number < 3);
            yield return ("less or equal", x => x.Number <= 3);
            yield return ("add", x => x.Number + 1 == 4);
            yield return ("subtract", x => x.Number - 1 == 2);
            yield return ("multiply", x => x.Number * 2 == 6);
            yield return ("divide", x => x.Number / 2 == 1);
            yield return ("modulo", x => x.Number % 2 == 1);
            yield return ("negate", x => -x.Number == -3);
            yield return ("long", x => x.Big > 20_000_000_000L);
            yield return ("decimal", x => x.Money >= money);
            yield return ("double", x => x.Ratio < 1.5);
            yield return ("null", x => x.Maybe == null);
            yield return ("not null", x => x.Maybe != null);
            yield return ("nullable equal", x => x.Maybe == 4);
            yield return ("nullable not equal", x => x.Maybe != 4);
            yield return ("nullable greater", x => x.Maybe > 1);
            yield return ("nullable not greater", x => !(x.Maybe > 1));
            yield return ("not nullable equal", x => !(x.Maybe == 4));
            yield return ("two nullable equal", x => x.Maybe == x.Other);
            yield return ("two nullable not equal", x => x.Maybe != x.Other);
            yield return ("not two nullable equal", x => !(x.Maybe == x.Other));
            yield return ("two nullable less", x => x.Maybe < x.Other);
            yield return ("not two nullable less", x => !(x.Maybe < x.Other));
            yield return ("not or with nullable", x => !(x.Maybe > 3 || x.Other == 1));
            yield return ("nullable value", x => x.Maybe.HasValue && x.Maybe.Value > 1);
            yield return ("coalesce", x => (x.Maybe ?? -1) == -1);
            yield return ("bool", x => x.Flag);
            yield return ("not bool", x => !x.Flag);
            yield return ("bool equal", x => x.Flag == true);
            yield return ("and", x => x.Number > 1 && x.Flag);
            yield return ("or", x => x.Number == 1 || x.Flag);
            yield return ("not or", x => !(x.Number > 3 || x.Flag));
            yield return ("not and", x => !(x.Number > 1 && x.Number < 4));
            yield return ("conditional", x => (x.Flag ? x.Number : 0) > 2);
            yield return ("string equal", x => x.Name == name);
            yield return ("string not equal", x => x.Name != name);
            yield return ("not string equal", x => !(x.Name == name));
            yield return ("string null", x => x.Name == null);
            yield return ("string not null", x => x.Name != null);
            yield return ("starts with", x => x.Name != null && x.Name.StartsWith("a"));
            yield return ("ends with", x => x.Name != null && x.Name.EndsWith("a"));
            yield return ("contains", x => x.Name != null && x.Name.Contains("an"));
            yield return ("array contains", x => ids.Contains(x.Number));
            yield return ("new array contains", x => new[] { 0, 2 }.Contains(x.Number));
            yield return ("list contains", x => list.Contains(x.Number));
            yield return ("not contains", x => !ids.Contains(x.Number));
            yield return ("date", x => x.Date > date);
            yield return ("date year", x => x.Date.Year == 2023);
            yield return ("date month", x => x.Date.Month > 3);
            yield return ("date day", x => x.Date.Day == 12);
            yield return ("offset", x => x.Moment > offset);
            yield return ("offset near zero", x => x.Moment >= nearOffset);
            yield return ("guid", x => x.ID != Guid.Empty);
            yield return ("store name", x => x.Title == "t2");

            yield return ("checked add", x => checked(x.Number + 1) == 4);
            yield return ("checked subtract", x => checked(x.Number - 1) == 2);
            yield return ("checked multiply", x => checked(x.Number * 2) == 6);
            yield return ("checked negate", x => checked(-x.Number) == -3);
            yield return ("checked convert", x => checked((long)x.Number) == 3L);
            yield return ("bool and", x => x.Flag & x.Number > 1);
            yield return ("bool or", x => x.Flag | x.Number > 4);
            yield return ("not bool and", x => !(x.Flag & x.Number > 1));
            yield return ("not bool or", x => !(x.Flag | x.Number > 4));
            yield return ("null on the left", x => null == x.Maybe);
            yield return ("value on the left", x => 4 == x.Maybe);

            //values the query evaluates before it's sent
            var values = new[] { 1, 3, 5 };
            var box = new ValueBox { Value = 3 };
            Func<int, int> twice = v => v * 2;
            object text = "text";
            var maybeNull = (int?)null;
            var flag = true;
            yield return ("array length", x => x.Number == values.Length);
            yield return ("array index", x => x.Number == values[1]);
            yield return ("array long index", x => x.Number == values[1L]);
            yield return ("list index", x => x.Number == list[1]);
            yield return ("invoke", x => x.Number == twice(2));
            yield return ("member init", x => x.Number == new ValueBox { Value = 4 }.Value);
            yield return ("captured member", x => x.Number == box.Value);
            yield return ("list init", x => new List<int> { 1, 3 }.Contains(x.Number));
            yield return ("array bounds", x => x.Number == new int[2].Length);
            yield return ("type is", x => x.Flag == text is string);
            yield return ("type as", x => x.Name == text as string);
            yield return ("value coalesce", x => x.Number == (maybeNull ?? 2));
            yield return ("value conditional", x => x.Number == (flag ? 1 : 2));
            yield return ("call with lambda", x => x.Number == values.First(v => v > 2));
            yield return ("call on a model member", x => x.Number == Math.Abs(x.Number - 10) - 7);

            //only built in code, C# doesn't write these
            yield return ("ones complement", Build(x => Expression.Equal(Expression.OnesComplement(Expression.Property(x, nameof(ParityModel.Number))), Expression.Constant(-4))));
            yield return ("unary plus", Build(x => Expression.Equal(Expression.UnaryPlus(Expression.Property(x, nameof(ParityModel.Number))), Expression.Constant(3))));
            yield return ("power", Build(x => Expression.Equal(Expression.Power(Expression.Property(x, nameof(ParityModel.Ratio)), Expression.Constant(2.0)), Expression.Constant(1.0))));
            yield return ("default", Build(x => Expression.Equal(Expression.Property(x, nameof(ParityModel.Number)), Expression.Default(typeof(int)))));
            yield return ("index", Build(x => Expression.Equal(Expression.Property(x, nameof(ParityModel.Number)), Expression.MakeIndex(Expression.Constant(list), typeof(List<int>).GetProperty("Item"), [Expression.Constant(0)]))));
            yield return ("type equal", Build(x => Expression.AndAlso(Expression.TypeEqual(Expression.Constant(text), typeof(string)), Expression.Equal(Expression.Property(x, nameof(ParityModel.Number)), Expression.Constant(1)))));
            yield return ("unbox", Build(x => Expression.Equal(Expression.Unbox(Expression.Constant(3, typeof(object)), typeof(int)), Expression.Property(x, nameof(ParityModel.Number)))));
        }

        public static void TestSequence(ITransactStoreEngine engine)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<ParityModel>(engine));
            var models = Models();
            foreach (var model in models)
                repo.Create(model);
            try
            {
                foreach (var (name, where) in Wheres())
                {
                    var compiled = where.Compile();
                    var expected = models.Where(compiled).Select(x => x.Number).Order().ToArray();
                    var actual = repo.Many(where).Select(x => x.Number).Order().ToArray();
                    Assert.True(expected.SequenceEqual(actual), $"{name}: expected [{String.Join(",", expected)}] found [{String.Join(",", actual)}]");
                    Assert.True(expected.Length == repo.Count(where), $"{name} count");
                    Assert.True((expected.Length > 0) == repo.Any(where), $"{name} any");
                }

                //a member with a store name reads back, an excluded member isn't stored
                var stored = repo.Single<ParityModel>(x => x.Number == 1)!;
                Assert.Equal("t1", stored.Title);
                Assert.Equal(0, stored.NotStored);

                var order = QueryOrder<ParityModel>.Create(x => x.Flag, true, x => x.Number);
                var expectedOrder = models.OrderByDescending(x => x.Flag).ThenBy(x => x.Number).Skip(1).Take(3).Select(x => x.Number).ToArray();
                Assert.Equal(expectedOrder, repo.Many<ParityModel>(x => true, order, 1, 3).Select(x => x.Number).ToArray());
                Assert.Equal(expectedOrder[0], repo.First<ParityModel>(x => x.Number > 0, order)!.Number);
            }
            finally
            {
                foreach (var model in models)
                    repo.Delete(model);
            }
        }

        public static async Task TestSequenceAsync(ITransactStoreEngine engine)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<ParityModel>(engine));
            var models = Models();
            foreach (var model in models)
                await repo.CreateAsync(model);
            try
            {
                foreach (var (name, where) in Wheres())
                {
                    var compiled = where.Compile();
                    var expected = models.Where(compiled).Select(x => x.Number).Order().ToArray();
                    var actual = (await repo.ManyAsync(where)).Select(x => x.Number).Order().ToArray();
                    Assert.True(expected.SequenceEqual(actual), $"{name}: expected [{String.Join(",", expected)}] found [{String.Join(",", actual)}]");
                }
            }
            finally
            {
                foreach (var model in models)
                    await repo.DeleteAsync(model);
            }
        }
    }

    public class QueryParityMemoryTests
    {
        [Fact]
        public async Task Memory()
        {
            QueryParityTests.TestSequence(new MemoryEngine());
            await QueryParityTests.TestSequenceAsync(new MemoryEngine());
        }
    }
}
