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
            [Relation(nameof(ParityLineModel.ParityID))]
            public ParityLineModel[]? Lines { get; set; }
        }

        [Entity("QueryParityLine")]
        public sealed class ParityLineModel
        {
            [Identity]
            public Guid ID { get; set; }
            public Guid ParityID { get; set; }
            public int Quantity { get; set; }
            [StoreProperties(false, 18, 2)]
            public decimal Price { get; set; }
            [Relation(nameof(ParityID))]
            public ParityModel? Parent { get; set; }
        }

        public static readonly Type[] ModelTypes = [typeof(ParityModel), typeof(ParityLineModel)];

        public sealed class ParitySelectResult
        {
            public Guid ID { get; set; }
            public int Number { get; set; }
            public string? Name { get; set; }
            public int Doubled { get; set; }
            public int LineCount { get; set; }
            public decimal LineTotal { get; set; }
            public bool Flagged { get; set; }
            public bool IsBig { get; set; }
        }
        public sealed class ParityGroupResult
        {
            public bool Flag { get; set; }
            public int? Other { get; set; }
            public int Rows { get; set; }
            public int SumNumber { get; set; }
            public double AverageNumber { get; set; }
            public decimal MaxMoney { get; set; }
            public string? MinName { get; set; }
            public int LineQuantity { get; set; }
        }
        public sealed class ParityLineGroupResult
        {
            public Guid ParentID { get; set; }
            public string? ParentName { get; set; }
            public int Lines { get; set; }
            public decimal Revenue { get; set; }
        }

        //two lines for the models numbered 1 to 4, none for 0 and 5
        private static ParityLineModel[] Lines(ParityModel[] models)
        {
            var lines = new List<ParityLineModel>();
            foreach (var model in models.Where(x => x.Number >= 1 && x.Number <= 4))
            {
                lines.Add(new ParityLineModel { ID = Guid.NewGuid(), ParityID = model.ID, Quantity = model.Number, Price = 2.5m * model.Number });
                lines.Add(new ParityLineModel { ID = Guid.NewGuid(), ParityID = model.ID, Quantity = model.Number + 1, Price = 1.25m });
            }
            return lines.ToArray();
        }

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
            repo.AddProvider(new TransactStoreProvider<ParityLineModel>(engine));
            var models = Models();
            foreach (var model in models)
                repo.Create(model);
            var lines = Lines(models);
            repo.Create<ParityLineModel>(lines);
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

                AssertCalculation(repo, models);
                AssertCalculationRelations(repo, models, lines);
                AssertProjections(repo, models, lines);

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
                repo.Delete<ParityLineModel>(lines);
                foreach (var model in models)
                    repo.Delete(model);
            }
        }

        //the key and the value reach through relations: a join for a single related model and a subquery for a collection
        private static void AssertCalculationRelations(IRepo repo, ParityModel[] models, ParityLineModel[] lines)
        {
            var parents = models.ToDictionary(x => x.ID);
            int LineQuantities(ParityModel model) => lines.Where(l => l.ParityID == model.ID).Sum(l => l.Quantity);

            Assert.Equal(lines.Sum(l => l.Price * l.Quantity), repo.Sum<ParityModel>(x => x.Lines!.Sum(l => l.Price * l.Quantity)));
            Assert.Equal(2, repo.Max<ParityModel, int>(x => x.Lines!.Count()));
            AssertGroups("sum of lines by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Sum(LineQuantities))), repo.Sum<ParityModel, bool>(x => x.Flag, x => x.Lines!.Sum(l => l.Quantity)));

            Assert.Equal(lines.Sum(l => parents[l.ParityID].Number * l.Quantity), repo.Sum<ParityLineModel>(x => x.Parent!.Number * x.Quantity));
            Assert.Equal(lines.Count(l => parents[l.ParityID].Flag), repo.Count<ParityLineModel>(x => x.Parent!.Flag));
            AssertGroups("count of lines by parent name", lines.GroupBy(l => parents[l.ParityID].Name).Select(g => (g.Key, (long)g.Count())), repo.Count<ParityLineModel, string?>(x => x.Parent!.Name));
            AssertGroups("average price by parent flag", lines.GroupBy(l => parents[l.ParityID].Flag).Select(g => (g.Key, g.Average(l => l.Price))), repo.Average<ParityLineModel, bool>(x => x.Parent!.Flag, x => x.Price), (a, b) => Math.Abs(a - b) < 0.000001m);
            AssertGroups("max quantity by parent number", lines.Where(l => parents[l.ParityID].Flag).GroupBy(l => parents[l.ParityID].Number).Select(g => (g.Key, g.Max(l => l.Quantity))), repo.Max<ParityLineModel, int, int>(x => x.Parent!.Number, x => x.Quantity, x => x.Parent!.Flag));
        }

        //every calculation gives what LINQ gives over the same models, over all of them and per group
        private static void AssertCalculation(IRepo repo, ParityModel[] models)
        {
            Expression<Func<ParityModel, bool>> some = x => x.Number > 0 && x.Number < 5;
            Expression<Func<ParityModel, bool>> none = x => x.Number > 100;
            var someModels = models.Where(some.Compile()).ToArray();

            Assert.Equal(models.Sum(x => x.Number), repo.Sum<ParityModel>(x => x.Number));
            Assert.Equal(models.Sum(x => x.Big), repo.Sum<ParityModel>(x => x.Big));
            Assert.Equal(models.Sum(x => x.Money), repo.Sum<ParityModel>(x => x.Money));
            Assert.Equal(models.Sum(x => x.Ratio), repo.Sum<ParityModel>(x => x.Ratio), 9);
            Assert.Equal(models.Sum(x => x.Maybe), repo.Sum<ParityModel>(x => x.Maybe));
            Assert.Equal(someModels.Sum(x => x.Number * 2 + 1), repo.Sum<ParityModel>(x => x.Number * 2 + 1, some));
            Assert.Equal(0, repo.Sum<ParityModel>(x => x.Number, none));
            Assert.Equal(0, repo.Sum<ParityModel>(x => x.Maybe, none));

            Assert.Equal(models.Average(x => x.Number), repo.Average<ParityModel>(x => x.Number), 9);
            Assert.Equal(models.Average(x => x.Big), repo.Average<ParityModel>(x => x.Big), 9);
            Assert.Equal(models.Average(x => x.Money), repo.Average<ParityModel>(x => x.Money), 6);
            Assert.Equal(models.Average(x => x.Ratio), repo.Average<ParityModel>(x => x.Ratio), 9);
            Assert.Equal(models.Average(x => x.Maybe)!.Value, repo.Average<ParityModel>(x => x.Maybe)!.Value, 9);
            Assert.Null(repo.Average<ParityModel>(x => x.Maybe, none));
            _ = Assert.Throws<InvalidOperationException>(() => repo.Average<ParityModel>(x => x.Number, none));

            Assert.Equal(models.Min(x => x.Number), repo.Min<ParityModel, int>(x => x.Number));
            Assert.Equal(models.Max(x => x.Money), repo.Max<ParityModel, decimal>(x => x.Money));
            Assert.Equal(models.Min(x => x.Maybe), repo.Min<ParityModel, int?>(x => x.Maybe));
            Assert.Equal(models.Max(x => x.Name), repo.Max<ParityModel, string?>(x => x.Name));
            Assert.Equal(models.Min(x => x.Date), repo.Min<ParityModel, DateTime>(x => x.Date));
            Assert.Equal(someModels.Max(x => x.Date), repo.Max<ParityModel, DateTime>(x => x.Date, some));
            Assert.Null(repo.Max<ParityModel, string?>(x => x.Name, none));
            _ = Assert.Throws<InvalidOperationException>(() => repo.Min<ParityModel, int>(x => x.Number, none));

            AssertGroups("count by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, (long)g.Count())), repo.Count<ParityModel, bool>(x => x.Flag));
            AssertGroups("count by nullable", models.GroupBy(x => x.Other).Select(g => (g.Key, (long)g.Count())), repo.Count<ParityModel, int?>(x => x.Other));
            AssertGroups("count by string", models.GroupBy(x => x.Name).Select(g => (g.Key, (long)g.Count())), repo.Count<ParityModel, string?>(x => x.Name));
            AssertGroups("count by year", models.GroupBy(x => x.Date.Year).Select(g => (g.Key, (long)g.Count())), repo.Count<ParityModel, int>(x => x.Date.Year));
            AssertGroups("count by two members", models.GroupBy(x => new { x.Flag, x.Other }).Select(g => (g.Key, (long)g.Count())), repo.Count((ParityModel x) => new { x.Flag, x.Other }));
            AssertGroups("count some", someModels.GroupBy(x => x.Flag).Select(g => (g.Key, (long)g.Count())), repo.Count<ParityModel, bool>(x => x.Flag, some));
            Assert.Empty(repo.Count<ParityModel, bool>(x => x.Flag, none));

            AssertGroups("sum by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Sum(x => x.Number))), repo.Sum<ParityModel, bool>(x => x.Flag, x => x.Number));
            AssertGroups("sum money by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Sum(x => x.Money))), repo.Sum<ParityModel, bool>(x => x.Flag, x => x.Money));
            AssertGroups("sum nullable by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Sum(x => x.Maybe))), repo.Sum<ParityModel, bool>(x => x.Flag, x => x.Maybe));
            AssertGroups("average by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Average(x => x.Number))), repo.Average<ParityModel, bool>(x => x.Flag, x => x.Number), (a, b) => Math.Abs(a - b) < 1e-9);
            AssertGroups("average money by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Average(x => x.Money))), repo.Average<ParityModel, bool>(x => x.Flag, x => x.Money), (a, b) => Math.Abs(a - b) < 0.000001m);
            AssertGroups("min name by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Min(x => x.Name))), repo.Min<ParityModel, bool, string?>(x => x.Flag, x => x.Name));
            AssertGroups("max date by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Max(x => x.Date))), repo.Max<ParityModel, bool, DateTime>(x => x.Flag, x => x.Date));
            AssertGroups("max nullable by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Max(x => x.Maybe))), repo.Max<ParityModel, bool, int?>(x => x.Flag, x => x.Maybe));
        }

        //a projection of each model and of each group gives what LINQ gives, with the where, order, and paging where they apply
        private static void AssertProjections(IRepo repo, ParityModel[] models, ParityLineModel[] lines)
        {
            var parents = models.ToDictionary(x => x.ID);
            ParityLineModel[] LinesOf(ParityModel model) => lines.Where(l => l.ParityID == model.ID).ToArray();

            var selected = repo.Select((ParityModel x) => new ParitySelectResult { ID = x.ID, Number = x.Number, Name = x.Name, Doubled = x.Number * 2, LineCount = x.Lines!.Count(), LineTotal = x.Lines!.Sum(l => l.Price * l.Quantity), Flagged = x.Flag, IsBig = x.Number > 2 },
                x => x.Number > 0, QueryOrder<ParityModel>.Create(x => x.Number, true), 1, 3);
            var expectedSelected = models.Where(x => x.Number > 0).OrderByDescending(x => x.Number).Skip(1).Take(3)
                .Select(x => (x.ID, x.Number, x.Name, x.Number * 2, LinesOf(x).Length, LinesOf(x).Sum(l => l.Price * l.Quantity), x.Flag, x.Number > 2)).ToArray();
            Assert.Equal(expectedSelected, selected.Select(r => (r.ID, r.Number, r.Name, r.Doubled, r.LineCount, r.LineTotal, r.Flagged, r.IsBig)).ToArray());

            var lineParents = repo.Select((ParityLineModel x) => new ParityLineGroupResult { ParentID = x.ParityID, ParentName = x.Parent!.Name });
            Assert.Equal(lines.Select(l => (l.ParityID, parents[l.ParityID].Name)).OrderBy(x => x.Item1).ThenBy(x => x.Item2).ToArray(), lineParents.Select(r => (r.ParentID, r.ParentName)).OrderBy(x => x.Item1).ThenBy(x => x.Item2).ToArray());

            //a Guid key and a Guid grouped calculation, which some stores keep as bytes
            var byParentID = repo.Group((ParityLineModel x) => x.ParityID, g => new ParityLineGroupResult { ParentID = g.Key, Lines = g.Count() });
            Assert.Equal(lines.GroupBy(l => l.ParityID).Select(g => (g.Key, g.Count())).OrderBy(x => x.Key).ToArray(), byParentID.Select(r => (r.ParentID, r.Lines)).OrderBy(x => x.Item1).ToArray());
            AssertGroups("sum quantity by parent id", lines.GroupBy(l => l.ParityID).Select(g => (g.Key, g.Sum(l => l.Quantity))), repo.Sum<ParityLineModel, Guid>(x => x.ParityID, x => x.Quantity));

            var byFlag = repo.Group((ParityModel x) => x.Flag, g => new ParityGroupResult
            {
                Flag = g.Key,
                Rows = g.Count(),
                SumNumber = g.Sum(x => x.Number),
                AverageNumber = g.Average(x => x.Number),
                MaxMoney = g.Max(x => x.Money),
                MinName = g.Min(x => x.Name),
                LineQuantity = g.Sum(x => x.Lines!.Sum(l => l.Quantity)),
            });
            Assert.Equal(2, byFlag.Count);
            foreach (var group in models.GroupBy(x => x.Flag))
            {
                var actual = byFlag.Single(r => r.Flag == group.Key);
                Assert.Equal(group.Count(), actual.Rows);
                Assert.Equal(group.Sum(x => x.Number), actual.SumNumber);
                Assert.Equal(group.Average(x => x.Number), actual.AverageNumber, 9);
                Assert.Equal(group.Max(x => x.Money), actual.MaxMoney);
                Assert.Equal(group.Min(x => x.Name), actual.MinName);
                Assert.Equal(group.Sum(x => LinesOf(x).Sum(l => l.Quantity)), actual.LineQuantity);
            }

            var byTwo = repo.Group((ParityModel x) => new { x.Flag, x.Other }, g => new ParityGroupResult { Flag = g.Key.Flag, Other = g.Key.Other, Rows = g.Count() });
            var expectedTwo = models.GroupBy(x => new { x.Flag, x.Other }).Select(g => (g.Key.Flag, g.Key.Other, g.Count())).OrderBy(x => x.Flag).ThenBy(x => x.Other).ToArray();
            Assert.Equal(expectedTwo, byTwo.Select(r => (r.Flag, r.Other, r.Rows)).OrderBy(x => x.Flag).ThenBy(x => x.Other).ToArray());

            //the having, the order, and the paging apply to the results
            var topOthers = repo.Group((ParityModel x) => x.Other, g => new ParityGroupResult { Other = g.Key, Rows = g.Count(), SumNumber = g.Sum(x => x.Number) },
                having: r => r.Rows >= 1 && r.SumNumber > 2, order: QueryOrder<ParityGroupResult>.Create(r => r.SumNumber, true), take: 2);
            var expectedTop = models.GroupBy(x => x.Other).Select(g => (g.Key, g.Count(), g.Sum(x => x.Number))).Where(x => x.Item2 >= 1 && x.Item3 > 2).OrderByDescending(x => x.Item3).Take(2).ToArray();
            Assert.Equal(expectedTop, topOthers.Select(r => (r.Other, r.Rows, r.SumNumber)).ToArray());

            //a key through a relation
            var byParent = repo.Group((ParityLineModel x) => x.Parent!.Name, g => new ParityLineGroupResult { ParentName = g.Key, Lines = g.Count(), Revenue = g.Sum(l => l.Price * l.Quantity) });
            var expectedByParent = lines.GroupBy(l => parents[l.ParityID].Name).Select(g => (g.Key, g.Count(), g.Sum(l => l.Price * l.Quantity))).OrderBy(x => x.Key).ToArray();
            Assert.Equal(expectedByParent, byParent.Select(r => (r.ParentName, r.Lines, r.Revenue)).OrderBy(x => x.Item1).ToArray());

            Assert.Empty(repo.Group((ParityModel x) => x.Flag, g => new ParityGroupResult { Flag = g.Key, Rows = g.Count() }, x => x.Number > 100));
            Assert.Empty(repo.Select((ParityModel x) => new ParitySelectResult { Number = x.Number }, x => x.Number > 100));
        }

        private static void AssertGroups<TKey, TValue>(string name, IEnumerable<(TKey Key, TValue Value)> expected, IReadOnlyCollection<(TKey Key, TValue Value)> actual, Func<TValue, TValue, bool>? equal = null)
        {
            var expectedArray = expected.OrderBy(x => x.Key?.ToString() ?? "", StringComparer.Ordinal).ToArray();
            var actualArray = actual.OrderBy(x => x.Key?.ToString() ?? "", StringComparer.Ordinal).ToArray();
            Assert.True(expectedArray.Length == actualArray.Length, $"{name}: expected {expectedArray.Length} groups found {actualArray.Length}");
            for (var i = 0; i < expectedArray.Length; i++)
            {
                Assert.True(Object.Equals(expectedArray[i].Key, actualArray[i].Key), $"{name}: expected key {expectedArray[i].Key} found {actualArray[i].Key}");
                var same = equal is null ? Object.Equals(expectedArray[i].Value, actualArray[i].Value) : equal(expectedArray[i].Value, actualArray[i].Value);
                Assert.True(same, $"{name}: {expectedArray[i].Key} expected {expectedArray[i].Value} found {actualArray[i].Value}");
            }
        }

        public static async Task TestSequenceAsync(ITransactStoreEngine engine)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<ParityModel>(engine));
            repo.AddProvider(new TransactStoreProvider<ParityLineModel>(engine));
            var models = Models();
            foreach (var model in models)
                await repo.CreateAsync(model);
            var lines = Lines(models);
            await repo.CreateAsync<ParityLineModel>(lines);
            try
            {
                foreach (var (name, where) in Wheres())
                {
                    var compiled = where.Compile();
                    var expected = models.Where(compiled).Select(x => x.Number).Order().ToArray();
                    var actual = (await repo.ManyAsync(where)).Select(x => x.Number).Order().ToArray();
                    Assert.True(expected.SequenceEqual(actual), $"{name}: expected [{String.Join(",", expected)}] found [{String.Join(",", actual)}]");
                }

                Assert.Equal(models.Sum(x => x.Money), await repo.SumAsync<ParityModel>(x => x.Money));
                Assert.Equal(models.Average(x => x.Number), await repo.AverageAsync<ParityModel>(x => x.Number), 9);
                Assert.Equal(models.Max(x => x.Name), await repo.MaxAsync<ParityModel, string?>(x => x.Name));
                AssertGroups("async count by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, (long)g.Count())), await repo.CountAsync<ParityModel, bool>(x => x.Flag));
                AssertGroups("async min by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Min(x => x.Number))), await repo.MinAsync<ParityModel, bool, int>(x => x.Flag, x => x.Number));
                Assert.Equal(lines.Sum(l => l.Price * l.Quantity), await repo.SumAsync<ParityModel>(x => x.Lines!.Sum(l => l.Price * l.Quantity)));
                var selected = await repo.SelectAsync((ParityModel x) => new ParitySelectResult { Number = x.Number, Doubled = x.Number * 2 }, x => x.Number < 2);
                Assert.Equal(models.Where(x => x.Number < 2).Select(x => x.Number * 2).Order(), selected.Select(r => r.Doubled).Order());
                var grouped = await repo.GroupAsync((ParityModel x) => x.Flag, g => new ParityGroupResult { Flag = g.Key, Rows = g.Count() });
                Assert.Equal(models.Count(x => x.Flag), grouped.Single(r => r.Flag).Rows);
            }
            finally
            {
                await repo.DeleteAsync<ParityLineModel>(lines);
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
