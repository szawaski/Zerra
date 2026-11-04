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
        public enum ParityKind
        {
            [EnumName("tiny")]
            Small,
            Medium,
            Large
        }

        [Flags]
        public enum ParityAccess
        {
            None = 0,
            Read = 1,
            [EnumName("w")]
            Write = 2,
            Execute = 4,
            ReadWrite = Read | Write
        }

        //the same table as ParityModel with its enums read as the text in the column
        [Entity("QueryParity")]
        public sealed class ParityTextModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Number { get; set; }
            public string? Kind { get; set; }
            public string? MaybeKind { get; set; }
            public string? Access { get; set; }
        }

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
            public Guid? SelfID { get; set; }
            public TimeSpan Span { get; set; }
            public TimeOnly Clock { get; set; }
            public DateOnly Day { get; set; }
            public ParityKind Kind { get; set; }
            public ParityKind? MaybeKind { get; set; }
            public ParityAccess Access { get; set; }
            [Relation(nameof(SelfID))]
            public ParityModel? Self { get; set; }
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
            [Relation(nameof(ParityNoteModel.LineID))]
            public ParityNoteModel[]? Notes { get; set; }
        }

        [Entity("QueryParityNote")]
        public sealed class ParityNoteModel
        {
            [Identity]
            public Guid ID { get; set; }
            public Guid LineID { get; set; }
            public int Rank { get; set; }
            [StoreProperties(false, 20)]
            public string? Text { get; set; }
            public Guid? ReviewerID { get; set; }
            [Relation(nameof(LineID))]
            public ParityLineModel? Line { get; set; }
            [Relation(nameof(ReviewerID))]
            public ParityModel? Reviewer { get; set; }
            public Guid? SecondReviewerID { get; set; }
            [Relation(nameof(SecondReviewerID))]
            public ParityModel? SecondReviewer { get; set; }
        }

        public static readonly Type[] ModelTypes = [typeof(ParityModel), typeof(ParityLineModel), typeof(ParityNoteModel)];

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
            public int Flagged { get; set; }
            public int DistinctOthers { get; set; }
        }
        public sealed class ParityNestedResult
        {
            public int Number { get; set; }
            public List<ParityLineResult>? Lines { get; set; }
            public ParityLineResult[]? Cheap { get; set; }
        }
        public sealed class ParityLineResult
        {
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public string? ParentName { get; set; }
            public List<ParityNoteResult>? Notes { get; set; }
        }
        public sealed class ParityNoteResult
        {
            public int Rank { get; set; }
            public string? Text { get; set; }
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

        //a note per line, a second for an even quantity, and one ranked zero for a quantity of three
        private static ParityNoteModel[] Notes(ParityLineModel[] lines)
        {
            var notes = new List<ParityNoteModel>();
            foreach (var line in lines)
            {
                notes.Add(new ParityNoteModel { ID = Guid.NewGuid(), LineID = line.ID, Rank = 1, Text = $"a{line.Quantity}" });
                if (line.Quantity % 2 == 0)
                    notes.Add(new ParityNoteModel { ID = Guid.NewGuid(), LineID = line.ID, Rank = 2, Text = $"b{line.Quantity}" });
                if (line.Quantity == 3)
                    notes.Add(new ParityNoteModel { ID = Guid.NewGuid(), LineID = line.ID, Rank = 0, Text = "z" });
            }
            return notes.ToArray();
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
                    Span = new TimeSpan(i + 1, 15, 0),
                    Clock = new TimeOnly(10 + i, 45, 30),
                    Day = new DateOnly(2021 + i, i + 1, 5 + i),
                    Kind = (ParityKind)(i % 3),
                    MaybeKind = i % 2 == 0 ? (ParityKind)(i / 2 % 3) : null,
                    Access = (ParityAccess)(i < 5 ? i : 7),
                    NotStored = 99,
                });
            }
            return models.ToArray();
        }

        public sealed class ValueBox
        {
            public int Value { get; set; }
        }

        public sealed class SetRow
        {
            public int Number { get; set; }
            public string? Text { get; set; }
        }

        //built through its constructor, with or without members set after it
        public sealed class BuiltRow
        {
            public BuiltRow(int number) => Number = number;
            public BuiltRow(int number, string? text)
            {
                Number = number;
                Text = text;
            }
            public int Number { get; }
            public string? Text { get; set; }
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

        public sealed class HeldTable(Action release) : IDisposable
        {
            public void Dispose() => release();
        }

        //a cancelled query stops at once, and one waiting on the store stops waiting, here for a table another connection holds
        public static async Task TestCancellationAsync(ITransactStoreEngine engine, Func<IDisposable> holdTable)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<ParityModel>(engine));
            repo.AddProvider(new TransactStoreProvider<ParityLineModel>(engine));
            repo.AddProvider(new TransactStoreProvider<ParityNoteModel>(engine));
            var models = Models();
            foreach (var model in models)
                await repo.CreateAsync(model);
            var lines = Lines(models);
            await repo.CreateAsync<ParityLineModel>(lines);
            try
            {
                using (var cancelled = new CancellationTokenSource())
                {
                    cancelled.Cancel();
                    var token = cancelled.Token;
                    _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.Query<ParityModel>().ToArrayAsync(token));
                    _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.Query<ParityModel>().FirstOrDefaultAsync(token));
                    _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.Query<ParityModel>().CountAsync(token));
                    _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.Query<ParityModel>().AnyAsync(token));
                    _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.Query<ParityModel>().SumAsync(x => x.Number, token));
                    _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repo.Query<ParityModel>().AllAsync(x => x.Number >= 0, token));
                }

                async Task StopsWaiting(Func<CancellationToken, Task> read)
                {
                    using var source = new CancellationTokenSource();
                    using (holdTable())
                    {
                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        source.CancelAfter(200);
                        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read(source.Token));
                        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(20), $"took {watch.Elapsed}");
                    }
                }
                await StopsWaiting(token => repo.Query<ParityModel>().ToArrayAsync(token));
                await StopsWaiting(token => repo.Query<ParityModel>().CountAsync(token));
                await StopsWaiting(token => repo.Query<ParityModel>(new Graph<ParityModel>(true, x => x.Lines)).ToArrayAsync(token));

                //a change waiting on the store stops too, and nothing is written
                var added = Models()[0];
                added.ID = Guid.NewGuid();
                added.Number = 1000;
                var original = repo.Query<ParityModel>().Single(x => x.ID == models[0].ID).Name;
                await StopsWaiting(token => repo.CreateAsync(added, token));
                await StopsWaiting(token => repo.UpdateAsync(new ParityModel { ID = models[0].ID, Name = "changed" }, new Graph<ParityModel>(x => x.Name), token));
                await StopsWaiting(token => repo.UpdateAsync<ParityModel>(x => x.ID == models[0].ID, x => new ParityModel { Number = x.Number + 1 }, token));
                await StopsWaiting(token => repo.DeleteAsync<ParityModel>(x => x.ID == models[0].ID, token));
                await StopsWaiting(token => repo.DeleteAsync(models[0], token));
                Assert.False(repo.Query<ParityModel>().Any(x => x.Number == 1000));
                var unchanged = repo.Query<ParityModel>().Single(x => x.ID == models[0].ID);
                Assert.Equal(original, unchanged.Name);
                Assert.Equal(models[0].Number, unchanged.Number);

                //the connection isn't left broken, and a token nobody cancels changes nothing
                using var unused = new CancellationTokenSource();
                Assert.Equal(models.Length, (await repo.Query<ParityModel>().ToArrayAsync(unused.Token)).Length);
            }
            finally
            {
                await repo.DeleteAsync<ParityLineModel>(lines);
                foreach (var model in models)
                    await repo.DeleteAsync(model);
            }
        }

        //each value is what C# gives for the same expression over the same models
        private static void AssertFunctions(IRepo repo, ParityModel[] models, ParityLineModel[] lines, bool inStore)
        {
            var ordered = models.OrderBy(x => x.Number).ToArray();
            var q = repo.Query<ParityModel>().OrderBy(x => x.Number);
            void Same<T>(IEnumerable<T> expected, IEnumerable<T> actual, string name) => Assert.True(expected.SequenceEqual(actual), $"{name}: expected [{String.Join("|", expected)}] found [{String.Join("|", actual)}]");

            //to text
            Same(ordered.Select(x => x.Number.ToString()), q.Select(x => x.Number.ToString()).ToArray(), "int");
            Same(ordered.Select(x => x.Big.ToString()), q.Select(x => x.Big.ToString()).ToArray(), "long");
            Same(ordered.Select(x => x.Money.ToString()), q.Select(x => x.Money.ToString()).ToArray(), "decimal");
            Same(ordered.Select(x => x.Flag.ToString()), q.Select(x => x.Flag.ToString()).ToArray(), "bool");
            Same(ordered.Select(x => x.ID.ToString()), q.Select(x => x.ID.ToString()).ToArray(), "guid");
            Same(ordered.Select(x => ((ParityKind)(x.Number % 3)).ToString()), q.Select(x => ((ParityKind)(x.Number % 3)).ToString()).ToArray(), "enum");
            Same(ordered.Select(x => x.Maybe.ToString()), q.Select(x => x.Maybe.ToString()).ToArray(), "nullable");
            Same(ordered.Select(x => x.Date.ToString("yyyy-MM-dd")), q.Select(x => x.Date.ToString("yyyy-MM-dd")).ToArray(), "date");
            Same(ordered.Select(x => x.Date.ToString("yyyy/MM/dd HH:mm:ss")), q.Select(x => x.Date.ToString("yyyy/MM/dd HH:mm:ss")).ToArray(), "date time");
            Same(ordered.Select(x => x.Day.ToString("dd.MM.yyyy")), q.Select(x => x.Day.ToString("dd.MM.yyyy")).ToArray(), "date only");
            Same(ordered.Select(x => x.Money.ToString("F1")), q.Select(x => x.Money.ToString("F1")).ToArray(), "fixed");
            Same(ordered.Select(x => x.Number.ToString("D4")), q.Select(x => x.Number.ToString("D4")).ToArray(), "digits");
            Same(ordered.Where(x => x.Number.ToString() == "2").Select(x => x.Number), q.Where(x => x.Number.ToString() == "2").Select(x => x.Number).ToArray(), "text in a condition");
            Same(ordered.Select(x => (x.Name ?? "").PadLeft(8, '*')), q.Select(x => (x.Name ?? "").PadLeft(8, '*')).ToArray(), "pad left");
            Same(ordered.Select(x => (x.Name ?? "").PadRight(8, '-')), q.Select(x => (x.Name ?? "").PadRight(8, '-')).ToArray(), "pad right");
            Same(ordered.Select(x => (x.Name ?? "").PadLeft(2, '*')), q.Select(x => (x.Name ?? "").PadLeft(2, '*')).ToArray(), "pad shorter leaves it");

            //from text and between numbers
            Same(ordered.Select(x => Int32.Parse(x.Number.ToString()) + 1), q.Select(x => Int32.Parse(x.Number.ToString()) + 1).ToArray(), "int parse");
            Same(ordered.Select(x => Int64.Parse(x.Big.ToString())), q.Select(x => Int64.Parse(x.Big.ToString())).ToArray(), "long parse");
            Same(ordered.Select(x => Decimal.Parse(x.Money.ToString())), q.Select(x => Decimal.Parse(x.Money.ToString())).ToArray(), "decimal parse");
            Same(ordered.Select(x => Double.Parse(x.Number.ToString())), q.Select(x => Double.Parse(x.Number.ToString())).ToArray(), "double parse");
            Same(ordered.Select(x => Guid.Parse(x.ID.ToString())), q.Select(x => Guid.Parse(x.ID.ToString())).ToArray(), "guid parse");
            Same(ordered.Select(x => Convert.ToString(x.Number)), q.Select(x => Convert.ToString(x.Number)).ToArray(), "convert to text");
            Same(ordered.Select(x => Convert.ToString(x.Flag)), q.Select(x => Convert.ToString(x.Flag)).ToArray(), "convert a bool to text");
            Same(ordered.Select(x => Convert.ToString(x.ID)), q.Select(x => Convert.ToString(x.ID)).ToArray(), "convert a Guid to text");
            Same(ordered.Select(x => Convert.ToDouble(x.Number)), q.Select(x => Convert.ToDouble(x.Number)).ToArray(), "convert to double");
            Same(ordered.Select(x => Convert.ToInt64(x.Number)), q.Select(x => Convert.ToInt64(x.Number)).ToArray(), "convert to long");
            Same(ordered.Select(x => Convert.ToInt32(x.Ratio + 0.25)), q.Select(x => Convert.ToInt32(x.Ratio + 0.25)).ToArray(), "convert rounds");
            Same(ordered.Select(x => Convert.ToBoolean(x.Number)), q.Select(x => Convert.ToBoolean(x.Number)).ToArray(), "convert to bool");

            //joined
            Same(ordered.Select(x => String.Join("-", new[] { x.Name, x.Title })), q.Select(x => String.Join("-", new[] { x.Name, x.Title })).ToArray(), "join values");
            Same(ordered.Select(x => String.Join(x.Title, new[] { x.Name, "z" })), q.Select(x => String.Join(x.Title, new[] { x.Name, "z" })).ToArray(), "join values with a separator of the row");
            string[] Quantities(ParityModel m) => lines.Where(l => l.ParityID == m.ID).OrderBy(l => l.Quantity).Select(l => l.Quantity.ToString()).ToArray();
            Same(ordered.Select(x => String.Join(",", Quantities(x))), q.Select(x => String.Join(",", x.Lines!.OrderBy(l => l.Quantity).Select(l => l.Quantity.ToString()))).ToArray(), "join rows");
            Same(ordered.Select(x => String.Join(x.Title, Quantities(x))), q.Select(x => String.Join(x.Title, x.Lines!.OrderBy(l => l.Quantity).Select(l => l.Quantity.ToString()))).ToArray(), "join rows with a separator of the row");
            Same(ordered.Select(x => String.Join("/", Quantities(x).Where(y => Int32.Parse(y) > 2))), q.Select(x => String.Join("/", x.Lines!.Where(l => l.Quantity > 2).OrderBy(l => l.Quantity).Select(l => l.Quantity.ToString()))).ToArray(), "join filtered rows");
            var joinedGroups = repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Names = String.Join(",", g.Select(x => x.Title)) }).ToArray();
            Assert.Equal(models.GroupBy(x => x.Flag).Count(), joinedGroups.Length);
            foreach (var group in joinedGroups)
                Same(models.Where(x => x.Flag == group.Key).Select(x => x.Title!).Order(), group.Names.Split(',').Order(), "join a group");

            //a joined text longer than the limit MySQL has by default
            var longJoined = repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Names = String.Join(",", g.Select(x => x.Title!.PadRight(800, '.'))) }).ToArray();
            foreach (var group in longJoined)
                Assert.Equal(models.Count(x => x.Flag == group.Key) * 801 - 1, group.Names.Length);
            if (inStore)
            {
                var longRows = repo.Update<ParityModel>(x => String.Join(",", x.Lines!.Select(l => l.Quantity.ToString().PadRight(800, '.'))).Length > 2000, x => new ParityModel { Other = x.Other });
                Assert.Equal(models.Count(x => lines.Count(l => l.ParityID == x.ID) * 801 - 1 > 2000), longRows);
            }

            //a midpoint rounds to the even number
            Same(ordered.Select(x => Convert.ToInt32(x.Number + 0.5)), q.Select(x => Convert.ToInt32(x.Number + 0.5)).ToArray(), "midpoint double");
            Same(ordered.Select(x => Convert.ToInt32(-x.Number - 0.5)), q.Select(x => Convert.ToInt32(-x.Number - 0.5)).ToArray(), "negative midpoint double");
            Same(ordered.Select(x => Convert.ToInt32(x.Number + 0.5m)), q.Select(x => Convert.ToInt32(x.Number + 0.5m)).ToArray(), "midpoint decimal");
            Same(ordered.Select(x => Convert.ToInt32(-x.Number - 0.5m)), q.Select(x => Convert.ToInt32(-x.Number - 0.5m)).ToArray(), "negative midpoint decimal");
            Same(ordered.Select(x => Convert.ToInt64(x.Number + 0.5)), q.Select(x => Convert.ToInt64(x.Number + 0.5)).ToArray(), "midpoint long");
            Same(ordered.Select(x => Convert.ToInt32(x.Number + 0.75)), q.Select(x => Convert.ToInt32(x.Number + 0.75)).ToArray(), "above a midpoint");
            Same(ordered.Select(x => Convert.ToInt32(x.Number + 0.25)), q.Select(x => Convert.ToInt32(x.Number + 0.25)).ToArray(), "below a midpoint");

            //Math.Round to even unless another MidpointRounding is given, as C# rounds
            Same(ordered.Select(x => Math.Round(x.Number + 0.5)), q.Select(x => Math.Round(x.Number + 0.5)).ToArray(), "round double");
            Same(ordered.Select(x => Math.Round(x.Number + 0.5m)), q.Select(x => Math.Round(x.Number + 0.5m)).ToArray(), "round decimal");
            Same(ordered.Select(x => Math.Round(x.Number * 0.125m, 2)), q.Select(x => Math.Round(x.Number * 0.125m, 2)).ToArray(), "round digits");
            foreach (var mode in Enum.GetValues<MidpointRounding>())
            {
                Same(ordered.Select(x => Math.Round(x.Number + 0.5, mode)), q.Select(x => Math.Round(x.Number + 0.5, mode)).ToArray(), $"round double {mode}");
                Same(ordered.Select(x => Math.Round(-x.Number - 0.5, mode)), q.Select(x => Math.Round(-x.Number - 0.5, mode)).ToArray(), $"round negative double {mode}");
                Same(ordered.Select(x => Math.Round(x.Number + 0.5m, mode)), q.Select(x => Math.Round(x.Number + 0.5m, mode)).ToArray(), $"round decimal {mode}");
                Same(ordered.Select(x => Math.Round(-x.Number - 0.5m, mode)), q.Select(x => Math.Round(-x.Number - 0.5m, mode)).ToArray(), $"round negative decimal {mode}");
                Same(ordered.Select(x => Math.Round(x.Number + 0.7m, mode)), q.Select(x => Math.Round(x.Number + 0.7m, mode)).ToArray(), $"round above {mode}");
                Same(ordered.Select(x => Math.Round(-x.Number - 0.3m, mode)), q.Select(x => Math.Round(-x.Number - 0.3m, mode)).ToArray(), $"round negative below {mode}");
                Same(ordered.Select(x => Math.Round(x.Number * 0.125m, 2, mode)), q.Select(x => Math.Round(x.Number * 0.125m, 2, mode)).ToArray(), $"round digits {mode}");
                Same(ordered.Select(x => Math.Round(-x.Number * 0.125m, 2, mode)), q.Select(x => Math.Round(-x.Number * 0.125m, 2, mode)).ToArray(), $"round negative digits {mode}");
            }
            var digits = 2;
            Same(ordered.Select(x => Math.Round(x.Money, digits)), q.Select(x => Math.Round(x.Money, digits)).ToArray(), "round digits from a variable");
            //digits from the model
            foreach (var mode in Enum.GetValues<MidpointRounding>())
            {
                Same(ordered.Select(x => Math.Round(x.Number * 0.125m + 0.0625m, x.Number % 4, mode)), q.Select(x => Math.Round(x.Number * 0.125m + 0.0625m, x.Number % 4, mode)).ToArray(), $"round model digits {mode}");
                Same(ordered.Select(x => Math.Round(-x.Number * 0.125m - 0.0625m, x.Number % 4, mode)), q.Select(x => Math.Round(-x.Number * 0.125m - 0.0625m, x.Number % 4, mode)).ToArray(), $"round negative model digits {mode}");
            }
            Same(ordered.Select(x => Math.Round(x.Number * 0.125 + 0.0625, x.Number % 4)), q.Select(x => Math.Round(x.Number * 0.125 + 0.0625, x.Number % 4)).ToArray(), "round model digits double");

            //dates and times
            Same(ordered.Select(x => x.Date.AddDays(3)), q.Select(x => x.Date.AddDays(3)).ToArray(), "add days");
            Same(ordered.Select(x => x.Date.AddDays(1.5)), q.Select(x => x.Date.AddDays(1.5)).ToArray(), "add part of a day");
            Same(ordered.Select(x => x.Date.AddDays(-2)), q.Select(x => x.Date.AddDays(-2)).ToArray(), "add negative days");
            Same(ordered.Select(x => x.Date.AddHours(x.Number)), q.Select(x => x.Date.AddHours(x.Number)).ToArray(), "add hours of the row");
            Same(ordered.Select(x => x.Date.AddMinutes(90)), q.Select(x => x.Date.AddMinutes(90)).ToArray(), "add minutes");
            Same(ordered.Select(x => x.Date.AddSeconds(45)), q.Select(x => x.Date.AddSeconds(45)).ToArray(), "add seconds");
            Same(ordered.Select(x => x.Date.AddMonths(2)), q.Select(x => x.Date.AddMonths(2)).ToArray(), "add months");
            Same(ordered.Select(x => x.Date.AddYears(1)), q.Select(x => x.Date.AddYears(1)).ToArray(), "add years");
            Same(ordered.Select(x => x.Date + TimeSpan.FromHours(2)), q.Select(x => x.Date + TimeSpan.FromHours(2)).ToArray(), "plus a time span");
            Same(ordered.Select(x => x.Date - TimeSpan.FromHours(2)), q.Select(x => x.Date - TimeSpan.FromHours(2)).ToArray(), "minus a time span");
            Same(ordered.Select(x => x.Date.Add(x.Span)), q.Select(x => x.Date.Add(x.Span)).ToArray(), "add a stored time span");
            Same(ordered.Select(x => x.Date.AddDays(1).Year), q.Select(x => x.Date.AddDays(1).Year).ToArray(), "a part of what was added");
            Same(ordered.Select(x => x.Day.AddDays(3)), q.Select(x => x.Day.AddDays(3)).ToArray(), "date only days");
            Same(ordered.Select(x => x.Day.AddMonths(2)), q.Select(x => x.Day.AddMonths(2)).ToArray(), "date only months");
            Same(ordered.Select(x => x.Day.AddYears(1)), q.Select(x => x.Day.AddYears(1)).ToArray(), "date only years");
            Same(ordered.Select(x => x.Clock.AddHours(2)), q.Select(x => x.Clock.AddHours(2)).ToArray(), "time hours");
            Same(ordered.Select(x => x.Clock.AddMinutes(-30)), q.Select(x => x.Clock.AddMinutes(-30)).ToArray(), "time minutes");
            Same(ordered.Select(x => x.Clock.AddHours(20)), q.Select(x => x.Clock.AddHours(20)).ToArray(), "time wraps past midnight");
            Same(ordered.Select(x => x.Clock.Add(x.Span)), q.Select(x => x.Clock.Add(x.Span)).ToArray(), "time plus a stored time span");
            var early = new TimeOnly(1, 0);
            Same(ordered.Select(x => (x.Date.AddDays(2) - x.Date).TotalHours), q.Select(x => (x.Date.AddDays(2) - x.Date).TotalHours).ToArray(), "total of a difference");
            Same(ordered.Select(x => (x.Date.AddHours(50) - x.Date).Days), q.Select(x => (x.Date.AddHours(50) - x.Date).Days).ToArray(), "days of a difference");
            Same(ordered.Select(x => (x.Date.AddHours(50) - x.Date).Hours), q.Select(x => (x.Date.AddHours(50) - x.Date).Hours).ToArray(), "hours of a difference");
            Same(ordered.Select(x => (x.Clock - early).TotalMinutes), q.Select(x => (x.Clock - early).TotalMinutes).ToArray(), "total between times");
            Same(ordered.Select(x => (x.Span + x.Span).TotalMinutes), q.Select(x => (x.Span + x.Span).TotalMinutes).ToArray(), "total of a sum of time spans");
            Same(ordered.Select(x => (x.Span - TimeSpan.FromMinutes(5)).TotalSeconds), q.Select(x => (x.Span - TimeSpan.FromMinutes(5)).TotalSeconds).ToArray(), "total of a difference of time spans");
            Same(ordered.Select(x => x.Span.TotalHours), q.Select(x => x.Span.TotalHours).ToArray(), "total of a stored time span");
            Same(ordered.Select(x => x.Date.Subtract(x.Span)), q.Select(x => x.Date.Subtract(x.Span)).ToArray(), "subtract a stored time span");
            Same(ordered.Select(x => x.Date.AddDays(2).Subtract(x.Date).TotalHours), q.Select(x => x.Date.AddDays(2).Subtract(x.Date).TotalHours).ToArray(), "total of a subtracted date");
            Same(ordered.Select(x => x.Span.Add(x.Span).TotalMinutes), q.Select(x => x.Span.Add(x.Span).TotalMinutes).ToArray(), "total of an added time span");
            Same(ordered.Select(x => (x.Date.AddSeconds(x.Number * 3725 + 1) - x.Date).Minutes), q.Select(x => (x.Date.AddSeconds(x.Number * 3725 + 1) - x.Date).Minutes).ToArray(), "minutes of a difference");
            Same(ordered.Select(x => (x.Date.AddSeconds(x.Number * 3725 + 1) - x.Date).Seconds), q.Select(x => (x.Date.AddSeconds(x.Number * 3725 + 1) - x.Date).Seconds).ToArray(), "seconds of a difference");
            Same(ordered.Select(x => (x.Date.AddSeconds(x.Number * 3725 + 1) - x.Date).Milliseconds), q.Select(x => (x.Date.AddSeconds(x.Number * 3725 + 1) - x.Date).Milliseconds).ToArray(), "milliseconds of a difference");
            Same(ordered.Select(x => (x.Date.AddSeconds(x.Number * 3725 + 1) - x.Date).Ticks), q.Select(x => (x.Date.AddSeconds(x.Number * 3725 + 1) - x.Date).Ticks).ToArray(), "ticks of a difference");
            Same(ordered.Select(x => (x.Date.AddSeconds(x.Number * 3725 + 1) - x.Date).TotalMilliseconds), q.Select(x => (x.Date.AddSeconds(x.Number * 3725 + 1) - x.Date).TotalMilliseconds).ToArray(), "total milliseconds of a difference");
            var hour = TimeSpan.FromHours(1);
            Same(ordered.Where(x => x.Date.AddMinutes(x.Number * 30) - x.Date > hour).Select(x => x.Number), q.Where(x => x.Date.AddMinutes(x.Number * 30) - x.Date > hour).Select(x => x.Number).ToArray(), "compare a difference");
            Same(ordered.Where(x => x.Date.AddDays(1) > new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Select(x => x.Number), q.Where(x => x.Date.AddDays(1) > new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Select(x => x.Number).ToArray(), "compare an added date");

            if (inStore)
            {
                _ = Assert.Throws<NotSupportedException>(() => q.Select(x => x.Date.ToString()).ToArray());
                _ = Assert.Throws<NotSupportedException>(() => q.Select(x => x.Date.ToString("d")).ToArray());
                _ = Assert.Throws<NotSupportedException>(() => q.Select(x => x.Number.ToString("G")).ToArray());
                _ = Assert.Throws<NotSupportedException>(() => q.Select(x => x.Date - x.Date).ToArray());
                _ = Assert.Throws<NotSupportedException>(() => q.Select(x => x.Span + x.Span).ToArray());
            }
        }

        //another query looked in beyond a Where, and the parts of a tuple key
        private static void AssertEnums(IRepo repo, ParityModel[] models, bool inStore)
        {
            var ordered = models.OrderBy(x => x.Number).ToArray();
            var q = repo.Query<ParityModel>().OrderBy(x => x.Number);
            void Same<T>(IEnumerable<T> expected, IEnumerable<T> actual, string name) => Assert.True(expected.SequenceEqual(actual), $"{name}: expected [{String.Join("|", expected)}] found [{String.Join("|", actual)}]");
            void Numbers(Func<ParityModel, bool> expected, IQueryable<ParityModel> actual, string name) => Same(models.Where(expected).Select(x => x.Number).Order(), actual.Select(x => x.Number).ToArray().Order(), name);

            //stored as the text from EnumName
            if (inStore)
            {
                var text = repo.Query<ParityTextModel>().OrderBy(x => x.Number).ToArray();
                Same(ordered.Select(x => EnumName.GetName(x.Kind)), text.Select(x => x.Kind!), "stored text");
                Assert.Contains("tiny", text.Select(x => x.Kind));
                Same(ordered.Select(x => x.MaybeKind is null ? null : EnumName.GetName(x.MaybeKind.Value)), text.Select(x => x.MaybeKind), "stored nullable text");
                Same(ordered.Select(x => EnumName.GetName(x.Access)), text.Select(x => x.Access!), "stored flags text");
            }

            //read back
            Same(ordered.Select(x => x.Kind), q.ToArray().Select(x => x.Kind), "model");
            Same(ordered.Select(x => x.MaybeKind), q.ToArray().Select(x => x.MaybeKind), "nullable model");
            Same(ordered.Select(x => x.Access), q.ToArray().Select(x => x.Access), "flags model");
            Same(ordered.Select(x => x.Kind), q.Select(x => x.Kind).ToArray(), "selected");
            Same(ordered.Select(x => x.MaybeKind), q.Select(x => x.MaybeKind).ToArray(), "selected nullable");
            Same(ordered.Select(x => x.Kind), q.Select(x => new { x.Number, x.Kind }).ToArray().Select(x => x.Kind), "in an object");
            Same(ordered.Select(x => new ValueBox { Value = (int)x.Kind }.Value), q.Select(x => new ValueBox { Value = (int)x.Kind }).ToArray().Select(x => x.Value), "number in an object");
            Same(ordered.Select(x => (int)x.Kind), q.Select(x => (int)x.Kind).ToArray(), "cast");
            Same(ordered.Select(x => (long)x.Kind + 1), q.Select(x => (long)x.Kind + 1).ToArray(), "cast in a sum");
            Same(ordered.Select(x => x.Flag ? x.Kind : ParityKind.Large), q.Select(x => x.Flag ? x.Kind : ParityKind.Large).ToArray(), "conditional");
            Same(ordered.Select(x => x.MaybeKind ?? ParityKind.Medium), q.Select(x => x.MaybeKind ?? ParityKind.Medium).ToArray(), "coalesce");
            Same(ordered.Select(x => (ParityKind)(x.Number % 3)), q.Select(x => (ParityKind)(x.Number % 3)).ToArray(), "computed");
            Same(ordered.Select(x => x.Kind.ToString()), q.Select(x => x.Kind.ToString()).ToArray(), "to text");
            Same(ordered.Select(x => x.MaybeKind.ToString()), q.Select(x => x.MaybeKind.ToString()).ToArray(), "nullable to text");

            //conditions
            var kind = ParityKind.Medium;
            ParityKind? maybe = ParityKind.Small;
            ParityKind? none = null;
            var number = 1;
            Numbers(x => x.Kind == ParityKind.Small, q.Where(x => x.Kind == ParityKind.Small), "equal");
            Numbers(x => ParityKind.Small == x.Kind, q.Where(x => ParityKind.Small == x.Kind), "equal flipped");
            Numbers(x => x.Kind != ParityKind.Small, q.Where(x => x.Kind != ParityKind.Small), "not equal");
            Numbers(x => x.Kind == kind, q.Where(x => x.Kind == kind), "equal variable");
            Numbers(x => (int)x.Kind == number, q.Where(x => (int)x.Kind == number), "equal number");
            Numbers(x => (long)x.Kind == Int64.MaxValue, q.Where(x => (long)x.Kind == Int64.MaxValue), "equal beyond the enum");
            Numbers(x => x.Kind == (ParityKind)99, q.Where(x => x.Kind == (ParityKind)99), "equal unnamed");
            Numbers(x => x.Kind != (ParityKind)99, q.Where(x => x.Kind != (ParityKind)99), "not equal unnamed");
            Numbers(x => x.MaybeKind == ParityKind.Small, q.Where(x => x.MaybeKind == ParityKind.Small), "nullable equal");
            Numbers(x => x.MaybeKind != ParityKind.Small, q.Where(x => x.MaybeKind != ParityKind.Small), "nullable not equal");
            Numbers(x => x.MaybeKind == maybe, q.Where(x => x.MaybeKind == maybe), "nullable equal variable");
            Numbers(x => x.MaybeKind == none, q.Where(x => x.MaybeKind == none), "nullable equal null variable");
            Numbers(x => x.MaybeKind == null, q.Where(x => x.MaybeKind == null), "is null");
            Numbers(x => x.MaybeKind != null, q.Where(x => x.MaybeKind != null), "is not null");
            Numbers(x => x.MaybeKind != null && x.MaybeKind.Value == ParityKind.Small, q.Where(x => x.MaybeKind != null && x.MaybeKind.Value == ParityKind.Small), "nullable value");
            Numbers(x => x.Kind > ParityKind.Small, q.Where(x => x.Kind > ParityKind.Small), "greater");
            Numbers(x => x.Kind >= kind, q.Where(x => x.Kind >= kind), "greater or equal");
            Numbers(x => x.Kind < ParityKind.Large, q.Where(x => x.Kind < ParityKind.Large), "less");
            Numbers(x => x.MaybeKind > ParityKind.Small, q.Where(x => x.MaybeKind > ParityKind.Small), "nullable greater");
            Numbers(x => !(x.Kind == ParityKind.Small), q.Where(x => !(x.Kind == ParityKind.Small)), "not");
            Numbers(x => x.Kind == x.MaybeKind, q.Where(x => x.Kind == x.MaybeKind), "column to column");
            Numbers(x => x.Kind > x.MaybeKind, q.Where(x => x.Kind > x.MaybeKind), "column above column");
            Numbers(x => x.Kind == (ParityKind)(x.Number % 2), q.Where(x => x.Kind == (ParityKind)(x.Number % 2)), "computed");
            Numbers(x => (x.Flag ? x.Kind : ParityKind.Large) == ParityKind.Large, q.Where(x => (x.Flag ? x.Kind : ParityKind.Large) == ParityKind.Large), "conditional");
            Numbers(x => (x.MaybeKind ?? ParityKind.Large) == ParityKind.Large, q.Where(x => (x.MaybeKind ?? ParityKind.Large) == ParityKind.Large), "coalesce");
            Numbers(x => x.Date.DayOfWeek == DayOfWeek.Monday, q.Where(x => x.Date.DayOfWeek == DayOfWeek.Monday), "a date's enum");
            var kinds = new[] { ParityKind.Small, ParityKind.Large };
            var kindList = new List<ParityKind> { ParityKind.Medium };
            var maybes = new ParityKind?[] { ParityKind.Medium };
            var numbers = new[] { 0, 2 };
            Numbers(x => kinds.Contains(x.Kind), q.Where(x => kinds.Contains(x.Kind)), "in an array");
            Numbers(x => !kinds.Contains(x.Kind), q.Where(x => !kinds.Contains(x.Kind)), "not in an array");
            Numbers(x => kindList.Contains(x.Kind), q.Where(x => kindList.Contains(x.Kind)), "in a list");
            Numbers(x => maybes.Contains(x.MaybeKind), q.Where(x => maybes.Contains(x.MaybeKind)), "nullable in an array");
            Numbers(x => numbers.Contains((int)x.Kind), q.Where(x => numbers.Contains((int)x.Kind)), "number in an array");
            Numbers(x => x.Access == (ParityAccess.Read | ParityAccess.Write), q.Where(x => x.Access == (ParityAccess.Read | ParityAccess.Write)), "flags equal");

            //ordered by the enum's numbers, the text would sort tiny last
            Same(models.OrderBy(x => x.Kind).ThenBy(x => x.Number).Select(x => x.Number), repo.Query<ParityModel>().OrderBy(x => x.Kind).ThenBy(x => x.Number).Select(x => x.Number).ToArray(), "order");
            Same(models.OrderByDescending(x => x.Kind).ThenBy(x => x.Number).Select(x => x.Number), repo.Query<ParityModel>().OrderByDescending(x => x.Kind).ThenBy(x => x.Number).Select(x => x.Number).ToArray(), "order descending");
            Same(models.OrderByDescending(x => x.MaybeKind ?? ParityKind.Medium).ThenBy(x => x.Number).Select(x => x.Number), repo.Query<ParityModel>().OrderByDescending(x => x.MaybeKind ?? ParityKind.Medium).ThenBy(x => x.Number).Select(x => x.Number).ToArray(), "order coalesce");
            Same(models.OrderBy(x => (int)x.Kind).ThenBy(x => x.Number).Select(x => x.Number), repo.Query<ParityModel>().OrderBy(x => (int)x.Kind).ThenBy(x => x.Number).Select(x => x.Number).ToArray(), "order number");

            //aggregates
            Assert.Equal(models.Sum(x => (int)x.Kind), repo.Query<ParityModel>().Sum(x => (int)x.Kind));
            Assert.Equal(models.Max(x => x.Kind), repo.Query<ParityModel>().Max(x => x.Kind));
            Assert.Equal(models.Min(x => x.Kind), repo.Query<ParityModel>().Min(x => x.Kind));
            Assert.Equal(models.Max(x => x.MaybeKind), repo.Query<ParityModel>().Max(x => x.MaybeKind));
            Assert.Equal(models.Select(x => x.Kind).Distinct().Count(), repo.Query<ParityModel>().Select(x => x.Kind).Distinct().Count());

            //groups
            var grouped = repo.Query<ParityModel>().GroupBy(x => x.Kind).Select(g => new { g.Key, Rows = g.Count(), Largest = g.Max(x => x.Kind) }).ToArray();
            Assert.Equal(models.GroupBy(x => x.Kind).Count(), grouped.Length);
            foreach (var group in grouped)
            {
                Assert.Equal(models.Count(x => x.Kind == group.Key), group.Rows);
                Assert.Equal(group.Key, group.Largest);
            }
            var groupWhere = repo.Query<ParityModel>().GroupBy(x => x.Kind).Select(g => new { g.Key, Rows = g.Count() }).Where(x => x.Key == ParityKind.Small).ToArray();
            Assert.Equal(ParityKind.Small, Assert.Single(groupWhere).Key);
            var groupAbove = repo.Query<ParityModel>().GroupBy(x => x.Kind).Select(g => new { g.Key, Rows = g.Count() }).Where(x => x.Key > ParityKind.Small).ToArray();
            Assert.Equal(2, groupAbove.Length);
            Same(models.Select(x => x.Kind).Distinct().Order(), repo.Query<ParityModel>().GroupBy(x => x.Kind).Select(g => new { g.Key, Rows = g.Count() }).OrderBy(x => x.Key).ToArray().Select(x => x.Key), "group order");
            var computedGroups = repo.Query<ParityModel>().GroupBy(x => (ParityKind)(x.Number % 3)).Select(g => new { g.Key, Rows = g.Count() }).Where(x => x.Key == ParityKind.Medium).ToArray();
            Assert.Equal(ParityKind.Medium, Assert.Single(computedGroups).Key);

            //a query looked in
            Numbers(x => x.Kind == models.First(y => y.Number == 1).Kind, q.Where(x => x.Kind == repo.Query<ParityModel>().First(y => y.Number == 1).Kind), "a query's member");
            Numbers(x => models.Where(y => y.Number < 2).Select(y => y.Kind).Contains(x.Kind), q.Where(x => repo.Query<ParityModel>().Where(y => y.Number < 2).Select(y => y.Kind).Contains(x.Kind)), "in a query");

            //written
            var first = ordered[0];
            var setKind = first.Kind;
            var setMaybe = first.MaybeKind;
            Assert.Equal(1, repo.Update<ParityModel>(x => x.Number == 0, x => new ParityModel { Kind = ParityKind.Large }));
            Assert.Equal(ParityKind.Large, repo.Query<ParityModel>().First(x => x.Number == 0).Kind);
            Assert.Equal(1, repo.Update<ParityModel>(x => x.Number == 0, x => new ParityModel { Kind = x.Flag ? ParityKind.Medium : x.Kind }));
            Assert.Equal(ParityKind.Medium, repo.Query<ParityModel>().First(x => x.Number == 0).Kind);
            Assert.Equal(1, repo.Update<ParityModel>(x => x.Number == 5, x => new ParityModel { MaybeKind = x.Kind }));
            Assert.Equal(models[5].Kind, repo.Query<ParityModel>().First(x => x.Number == 5).MaybeKind);
            Assert.Equal(1, repo.Update<ParityModel>(x => x.Number == 5, x => new ParityModel { MaybeKind = null }));
            Assert.Null(repo.Query<ParityModel>().First(x => x.Number == 5).MaybeKind);
            Assert.Equal(1, repo.Update<ParityModel>(x => x.Number == 4, x => new ParityModel { Kind = (ParityKind)(x.Number % 3) }));
            Assert.Equal(ParityKind.Medium, repo.Query<ParityModel>().First(x => x.Number == 4).Kind);
            first.Kind = ParityKind.Small;
            first.MaybeKind = ParityKind.Large;
            repo.Update(first, new Graph<ParityModel>(x => x.Kind, x => x.MaybeKind));
            var updated = repo.Query<ParityModel>().First(x => x.Number == 0);
            Assert.Equal(ParityKind.Small, updated.Kind);
            Assert.Equal(ParityKind.Large, updated.MaybeKind);
            first.Kind = setKind;
            first.MaybeKind = setMaybe;
            repo.Update(first, new Graph<ParityModel>(x => x.Kind, x => x.MaybeKind));

            //the number of flags is read from their text, a name of several flags among the names overlapping the others
            Assert.Contains(models, x => x.Access == (ParityAccess.ReadWrite | ParityAccess.Execute));
            Same(ordered.Select(x => (int)x.Access), q.Select(x => (int)x.Access).ToArray(), "flags cast");
            Same(models.OrderBy(x => x.Access).ThenBy(x => x.Number).Select(x => x.Number), repo.Query<ParityModel>().OrderBy(x => x.Access).ThenBy(x => x.Number).Select(x => x.Number).ToArray(), "flags order");
            Same(models.OrderByDescending(x => x.Access).ThenBy(x => x.Number).Select(x => x.Number), repo.Query<ParityModel>().OrderByDescending(x => x.Access).ThenBy(x => x.Number).Select(x => x.Number).ToArray(), "flags order descending");
            Numbers(x => x.Access.HasFlag(ParityAccess.Write), q.Where(x => x.Access.HasFlag(ParityAccess.Write)), "has flag");
            Numbers(x => x.Access.HasFlag(ParityAccess.Read | ParityAccess.Execute), q.Where(x => x.Access.HasFlag(ParityAccess.Read | ParityAccess.Execute)), "has flags");
            Numbers(x => x.Access.HasFlag(ParityAccess.ReadWrite), q.Where(x => x.Access.HasFlag(ParityAccess.ReadWrite)), "has a name of flags");
            Numbers(x => !x.Access.HasFlag(ParityAccess.Read), q.Where(x => !x.Access.HasFlag(ParityAccess.Read)), "has no flag");
            Numbers(x => x.Access.HasFlag(ParityAccess.None), q.Where(x => x.Access.HasFlag(ParityAccess.None)), "has no flags");
            Numbers(x => (x.Access & ParityAccess.Execute) != 0, q.Where(x => (x.Access & ParityAccess.Execute) != 0), "flags and");
            Numbers(x => x.Access > ParityAccess.ReadWrite, q.Where(x => x.Access > ParityAccess.ReadWrite), "flags greater");
            Same(ordered.Select(x => x.Access.HasFlag(ParityAccess.Execute)), q.Select(x => x.Access.HasFlag(ParityAccess.Execute)).ToArray(), "has flag value");
            Assert.Equal(models.Max(x => x.Access), q.Max(x => x.Access));
            Assert.Equal(models.Min(x => x.Access), q.Min(x => x.Access));

            //flags are written as EnumName writes them
            Same(ordered.Select(x => EnumName.GetName(x.Access)), q.Select(x => x.Access.ToString()).ToArray(), "flags text");
            var allText = EnumName.GetName(ParityAccess.ReadWrite | ParityAccess.Execute);
            Assert.Contains("|", allText);
            Numbers(x => EnumName.GetName(x.Access) == allText, q.Where(x => x.Access.ToString() == allText), "flags text condition");
            _ = Assert.Single(q.Where(x => x.Access.ToString() == allText).ToArray());
        }

        private static void AssertSets(IRepo repo, ParityModel[] models, ParityLineModel[] lines, bool inStore)
        {
            void Same<T>(IEnumerable<T> expected, IEnumerable<T> actual, string name)
            {
                var e = expected.Order().ToArray();
                var a = actual.Order().ToArray();
                Assert.True(e.SequenceEqual(a), $"{name}: expected [{String.Join("|", e)}] found [{String.Join("|", a)}]");
            }
            var q = repo.Query<ParityModel>();
            var low = models.Where(x => x.Number < 4).Select(x => x.Number % 3);
            var high = models.Where(x => x.Number > 1).Select(x => x.Number % 3);
            var lowQuery = q.Where(x => x.Number < 4).Select(x => x.Number % 3);
            var highQuery = q.Where(x => x.Number > 1).Select(x => x.Number % 3);

            //values combined as LINQ combines them, the ones other than Concat each once
            Same(low.Union(high), lowQuery.Union(highQuery).ToArray(), "union");
            Same(low.Concat(high), lowQuery.Concat(highQuery).ToArray(), "concat");
            Same(low.Intersect(high), lowQuery.Intersect(highQuery).ToArray(), "intersect");
            Same(low.Except(high), lowQuery.Except(highQuery).ToArray(), "except");

            //queries of different models
            var quantities = repo.Query<ParityLineModel>().Select(l => l.Quantity);
            Same(models.Select(x => x.Number).Union(lines.Select(l => l.Quantity)), q.Select(x => x.Number).Union(quantities).ToArray(), "union of models");
            Same(models.Select(x => x.Number).Except(lines.Select(l => l.Quantity)), q.Select(x => x.Number).Except(quantities).ToArray(), "except of models");

            //objects compare by their values, as rows do
            var rows = q.Where(x => x.Number < 3).Select(x => new { x.Number, Text = x.Name });
            var otherRows = q.Where(x => x.Number > 1).Select(x => new { x.Number, Text = x.Name });
            Same(models.Where(x => x.Number < 3).Select(x => x.Number).Union(models.Where(x => x.Number > 1).Select(x => x.Number)), rows.Union(otherRows).ToArray().Select(x => x.Number), "union of objects");
            Same(models.Where(x => x.Number is > 1 and < 3).Select(x => x.Number), rows.Intersect(otherRows).ToArray().Select(x => x.Number), "intersect of objects");
            var classRows = q.Where(x => x.Number < 3).Select(x => new SetRow { Number = x.Number, Text = x.Name });
            var otherClassRows = q.Where(x => x.Number > 1).Select(x => new SetRow { Number = x.Number, Text = x.Name });
            Same(models.Where(x => x.Number is > 1 and < 3).Select(x => x.Number), classRows.Intersect(otherClassRows).ToArray().Select(x => x.Number), "intersect of classes");
            Same(models.Select(x => x.Number), q.Where(x => x.Number < 3).Union(q.Where(x => x.Number >= 2)).ToArray().Select(x => x.Number), "union of models");

            //a page of a query is its own rows
            Same(models.OrderBy(x => x.Number).Take(2).Select(x => x.Number).Concat(models.OrderByDescending(x => x.Number).Take(2).Select(x => x.Number)), q.OrderBy(x => x.Number).Take(2).Select(x => x.Number).Concat(q.OrderByDescending(x => x.Number).Take(2).Select(x => x.Number)).ToArray(), "pages");

            //combined in the order written, where SQL's INTERSECT binds tighter than UNION
            Same(low.Union(high).Intersect(new[] { 0, 1 }), lowQuery.Union(highQuery).Intersect(q.Where(x => x.Number < 2).Select(x => x.Number)).ToArray(), "union then intersect");
            Same(low.Except(high.Union(new[] { 2 })), lowQuery.Except(highQuery.Union(q.Where(x => x.Number == 2).Select(x => x.Number))).ToArray(), "except of a union");

            //ended by a count or whether there are any
            Assert.Equal(low.Concat(high).Count(), lowQuery.Concat(highQuery).Count());
            Assert.Equal(low.Union(high).LongCount(), lowQuery.Union(highQuery).LongCount());
            Assert.True(lowQuery.Intersect(highQuery).Any());
            Assert.False(lowQuery.Except(lowQuery).Any());

            //the combined elements filtered, ordered, and paged
            var all = low.Concat(high);
            Assert.Equal(low.Union(high).Order(), lowQuery.Union(highQuery).OrderBy(x => x).ToArray());
            Assert.Equal(all.OrderByDescending(x => x).Take(3), lowQuery.Concat(highQuery).OrderByDescending(x => x).Take(3).ToArray());
            Assert.Equal(all.Order().Skip(2).Take(2), lowQuery.Concat(highQuery).OrderBy(x => x).Skip(2).Take(2).ToArray());
            Assert.Equal(all.Count(x => x > 0), lowQuery.Concat(highQuery).Where(x => x > 0).Count());
            Assert.Equal(all.Count(x => x > 0), lowQuery.Concat(highQuery).Count(x => x > 0));
            Assert.Equal(all.Order().Skip(1).First(), lowQuery.Concat(highQuery).OrderBy(x => x).Skip(1).First());
            Assert.Equal(all.Max(), lowQuery.Concat(highQuery).OrderByDescending(x => x).FirstOrDefault());
            Assert.Equal(2, lowQuery.Union(highQuery).Single(x => x == 2));
            Assert.True(lowQuery.Union(highQuery).Any(x => x == 2));
            Assert.False(lowQuery.Union(highQuery).Any(x => x > 5));
            Assert.Empty(lowQuery.Union(highQuery).Take(0).ToArray());
            Assert.Equal(3, lowQuery.Concat(highQuery).OrderBy(x => x).Take(3).Count());
            Same(models.Where(x => x.Number < 3).Select(x => x.Number).Union(models.Where(x => x.Number > 1).Select(x => x.Number)).Where(x => x > 1), rows.Union(otherRows).Where(r => r.Number > 1).ToArray().Select(x => x.Number), "objects filtered");
            Assert.Equal(models.Select(x => x.Number).OrderByDescending(x => x).Take(2), classRows.Union(otherClassRows).OrderByDescending(r => r.Number).Take(2).ToArray().Select(x => x.Number));
            Assert.Equal(models.OrderBy(x => x.Number).Skip(1).Take(2).Select(x => x.Number), q.Where(x => x.Number < 3).Union(q.Where(x => x.Number >= 2)).OrderBy(x => x.Number).Skip(1).Take(2).ToArray().Select(x => x.Number));
            Same(models.Select(x => x.Number).Where(x => x is > 0 and < 4), q.Where(x => x.Number < 3).Union(q.Where(x => x.Number >= 2)).Where(x => x.Number > 0 && x.Number < 4).ToArray().Select(x => x.Number), "models filtered");
            //an order with a tie broken by another, and a single element or none
            Assert.Equal(models.Where(x => x.Number < 3).Select(x => new { x.Number, Text = x.Name }).Union(models.Where(x => x.Number > 1).Select(x => new { x.Number, Text = x.Name })).OrderBy(r => r.Text == null).ThenByDescending(r => r.Number).Select(r => r.Number),
                rows.Union(otherRows).OrderBy(r => r.Text == null).ThenByDescending(r => r.Number).ToArray().Select(r => r.Number));
            Assert.Equal(2, lowQuery.Union(highQuery).SingleOrDefault(x => x == 2));
            Assert.Equal(0, lowQuery.Union(highQuery).SingleOrDefault(x => x > 5));
            //classes built through a constructor, in a query and after a set
            Same(models.Select(x => $"{x.Number}:{x.Name}"), q.Select(x => new BuiltRow(x.Number, x.Name)).ToArray().Select(r => $"{r.Number}:{r.Text}"), "constructed");
            Same(models.Select(x => $"{x.Number}:{x.Name}"), q.Select(x => new BuiltRow(x.Number) { Text = x.Name }).ToArray().Select(r => $"{r.Number}:{r.Text}"), "constructed and set");
            Same(low.Union(high).Select(x => x + 1), lowQuery.Union(highQuery).Select(x => new BuiltRow(x + 1)).ToArray().Select(r => r.Number), "constructed after a set");
            Same(low.Union(high).Select(x => $"{x}:{x * 2}"), lowQuery.Union(highQuery).Select(x => new BuiltRow(x) { Text = (x * 2).ToString() }).ToArray().Select(r => $"{r.Number}:{r.Text}"), "constructed and set after a set");
            //an object within an element is read a member at a time
            var nested = q.Where(x => x.Number < 3).Select(x => new { x.Number, Inner = new { Text = x.Name, Twice = x.Number * 2 } });
            var otherNested = q.Where(x => x.Number > 1).Select(x => new { x.Number, Inner = new { Text = x.Name, Twice = x.Number * 2 } });
            Same(models.Where(x => x.Number * 2 > 2).Select(x => x.Number), nested.Union(otherNested).Where(r => r.Inner.Twice > 2).ToArray().Select(r => r.Number), "nested objects filtered");
            Assert.Equal(models.OrderByDescending(x => x.Number).Select(x => x.Name), nested.Union(otherNested).OrderByDescending(r => r.Inner.Twice).ToArray().Select(r => r.Inner.Text));

            //an enum element compares and orders by its numbers, as in C#, where the column holds its name
            var kinds = q.Select(x => x.Kind);
            Assert.Equal(models.Select(x => x.Kind).Union(models.Select(x => x.Kind)).Count(x => x == ParityKind.Medium), kinds.Union(kinds).Count(x => x == ParityKind.Medium));
            Assert.Equal(models.Select(x => x.Kind).Distinct().Order(), kinds.Union(kinds).OrderBy(x => x).ToArray());

            //the combined elements built from, after their condition, order, and page
            Same(low.Union(high).Select(x => x + 1), lowQuery.Union(highQuery).Select(x => x + 1).ToArray(), "select of values");
            Assert.Equal(all.OrderByDescending(x => x).Take(3).Select(x => x * 2), lowQuery.Concat(highQuery).OrderByDescending(x => x).Take(3).Select(x => x * 2).ToArray());
            Same(low.Union(high).Where(x => x > 0).Select(x => $"{x > 1}:{x}"), lowQuery.Union(highQuery).Where(x => x > 0).Select(x => new { Value = x, Big = x > 1 }).ToArray().Select(x => $"{x.Big}:{x.Value}"), "select of objects from values");
            Same(models.Where(x => x.Number < 3).Select(x => x.Name).Union(models.Where(x => x.Number > 1).Select(x => x.Name)), rows.Union(otherRows).Select(r => r.Text).ToArray(), "select from objects");
            Same(models.Where(x => x.Number is > 1 and < 3).Select(x => x.Number + 10), classRows.Intersect(otherClassRows).Select(r => new SetRow { Number = r.Number + 10, Text = r.Text }).ToArray().Select(x => x.Number), "select of classes");
            Same(models.Select(x => x.Number * 2), q.Where(x => x.Number < 3).Union(q.Where(x => x.Number >= 2)).Select(x => x.Number * 2).ToArray(), "select from models");
            Same(models.Select(x => x.Kind), q.Where(x => x.Number < 3).Union(q.Where(x => x.Number >= 2)).Select(x => x.Kind).ToArray(), "select of an enum from models");
            Assert.Equal(models.OrderBy(x => x.Number).Select(x => x.Name).First(), q.Where(x => x.Number < 3).Union(q.Where(x => x.Number >= 2)).OrderBy(x => x.Number).Select(x => x.Name).First());
            Assert.Equal(low.Union(high).Count(), lowQuery.Union(highQuery).Select(x => x + 1).Count());

            //what a Select builds filtered and ordered, after or in place of the order before it
            Same(low.Union(high).Select(x => x + 1).Where(x => x > 1), lowQuery.Union(highQuery).Select(x => x + 1).Where(x => x > 1).ToArray(), "select filtered");
            Assert.Equal(low.Union(high).Select(x => x * -1).Order(), lowQuery.Union(highQuery).Select(x => x * -1).OrderBy(x => x).ToArray());
            Assert.Equal(all.Where(x => x > 0).Select(x => x + 1).OrderByDescending(x => x).Skip(1).Take(2), lowQuery.Concat(highQuery).Where(x => x > 0).Select(x => x + 1).OrderByDescending(x => x).Skip(1).Take(2).ToArray());
            Assert.Equal(all.Count(x => x + 1 > 1), lowQuery.Concat(highQuery).Select(x => x + 1).Count(x => x > 1));
            Assert.Equal(all.Order().Select(x => x * 2).Where(x => x > 0).First(), lowQuery.Concat(highQuery).OrderBy(x => x).Select(x => x * 2).Where(x => x > 0).First());
            Assert.True(lowQuery.Union(highQuery).Select(x => x + 1).Any(x => x == 3));
            var allModels = models.Select(x => new { x.Number, Text = x.Name, x.Kind });
            Assert.Equal(allModels.OrderBy(x => x.Number % 2).ThenByDescending(x => x.Number).Select(x => x.Number),
                q.Where(x => x.Number < 3).Union(q.Where(x => x.Number >= 2)).Select(x => new { x.Number, Text = x.Name, x.Kind }).OrderBy(x => x.Number % 2).ThenByDescending(x => x.Number).ToArray().Select(x => x.Number));
            Same(allModels.Where(x => x.Kind == ParityKind.Medium).Select(x => x.Number), q.Where(x => x.Number < 3).Union(q.Where(x => x.Number >= 2)).Select(x => new { x.Number, Text = x.Name, x.Kind }).Where(x => x.Kind == ParityKind.Medium).ToArray().Select(x => x.Number), "selected enum filtered");
            Assert.Equal(models.Select(x => x.Kind).Distinct().Order(), kinds.Union(kinds).Select(k => k).OrderBy(k => k).ToArray());
            Same(models.Where(x => x.Number is > 1 and < 3).Select(x => x.Number + 10).Where(x => x > 11), classRows.Intersect(otherClassRows).Select(r => new SetRow { Number = r.Number + 10, Text = r.Text }).Where(r => r.Number > 11).ToArray().Select(x => x.Number), "selected classes filtered");
            _ = Assert.Throws<NotSupportedException>(() => lowQuery.Union(new[] { 1 }.AsQueryable()).ToArray());

            //rows combine by position, so the members of a class are built in the same order
            if (inStore)
            {
                var reordered = q.Where(x => x.Number > 1).Select(x => new SetRow { Text = x.Name, Number = x.Number });
                _ = Assert.Throws<NotSupportedException>(() => classRows.Union(reordered).ToArray());
            }
        }

        private static void AssertJoins(IRepo repo, ParityModel[] models, ParityLineModel[] lines, ParityNoteModel[] notes, bool inStore)
        {
            void Same<T>(IEnumerable<T> expected, IEnumerable<T> actual, string name)
            {
                var e = expected.Select(x => x?.ToString() ?? "null").Order(StringComparer.Ordinal).ToArray();
                var a = actual.Select(x => x?.ToString() ?? "null").Order(StringComparer.Ordinal).ToArray();
                Assert.True(e.SequenceEqual(a), $"{name}: expected [{String.Join("|", e)}] found [{String.Join("|", a)}]");
            }
            var q = repo.Query<ParityModel>();
            var lq = repo.Query<ParityLineModel>();
            var nq = repo.Query<ParityNoteModel>();

            //pairs by keys, of values and of whole models
            Same(models.Join(lines, x => x.ID, l => l.ParityID, (x, l) => $"{x.Number}:{l.Quantity}"), q.Join(lq, x => x.ID, l => l.ParityID, (x, l) => new { x.Number, l.Quantity }).ToArray().Select(r => $"{r.Number}:{r.Quantity}"), "join");
            Same(models.Join(lines, x => x.ID, l => l.ParityID, (x, l) => $"{x.Name}:{l.Price:0.##}"), q.Join(lq, x => x.ID, l => l.ParityID, (x, l) => new { x, l }).ToArray().Select(r => $"{r.x.Name}:{r.l.Price:0.##}"), "join of models");
            Same(models.Join(lines, x => x.Number, l => l.Quantity, (x, l) => $"{x.Number}:{l.Price:0.##}"), q.Join(lq, x => x.Number, l => l.Quantity, (x, l) => new { x.Number, l.Price }).ToArray().Select(r => $"{r.Number}:{r.Price:0.##}"), "join of unrelated values");
            Same(models.Join(lines, x => x.ID, l => l.ParityID, (x, l) => x.Number * 10 + l.Quantity), q.Join(lq, x => x.ID, l => l.ParityID, (x, l) => x.Number * 10 + l.Quantity).ToArray(), "join of a value");
            Same(models.Join(lines, x => x.ID, l => l.ParityID, (x, l) => x.Kind), q.Join(lq, x => x.ID, l => l.ParityID, (x, l) => x.Kind).ToArray(), "join of an enum");

            //a model with itself, keys of several parts, and keys that can be null, which match none on their own and each other as parts
            Same(models.Join(models, a => a.Number, b => b.Number + 1, (a, b) => $"{a.Number}:{b.Number}"), q.Join(q, a => a.Number, b => b.Number + 1, (a, b) => new { A = a.Number, B = b.Number }).ToArray().Select(r => $"{r.A}:{r.B}"), "self join");
            Same(models.Join(models, a => new { a.Flag, a.Kind }, b => new { b.Flag, b.Kind }, (a, b) => $"{a.Number}:{b.Number}"), q.Join(q, a => new { a.Flag, a.Kind }, b => new { b.Flag, b.Kind }, (a, b) => new { A = a.Number, B = b.Number }).ToArray().Select(r => $"{r.A}:{r.B}"), "composite key");
            Same(models.Join(models, a => a.Maybe, b => b.Other, (a, b) => $"{a.Number}:{b.Number}"), q.Join(q, a => a.Maybe, b => b.Other, (a, b) => new { A = a.Number, B = b.Number }).ToArray().Select(r => $"{r.A}:{r.B}"), "nullable key");
            Same(models.Join(models, a => a.Name, b => b.Name, (a, b) => $"{a.Number}:{b.Number}"), q.Join(q, a => a.Name, b => b.Name, (a, b) => new { A = a.Number, B = b.Number }).ToArray().Select(r => $"{r.A}:{r.B}"), "string key");
            Same(models.Join(models, a => new { a.Maybe, a.Flag }, b => new { Maybe = b.Other, b.Flag }, (a, b) => $"{a.Number}:{b.Number}"), q.Join(q, a => new { a.Maybe, a.Flag }, b => new { Maybe = b.Other, b.Flag }, (a, b) => new { A = a.Number, B = b.Number }).ToArray().Select(r => $"{r.A}:{r.B}"), "composite nullable key");
            Same(models.Join(models, a => a.Kind, b => b.Kind, (a, b) => $"{a.Number}:{b.Number}"), q.Join(q, a => a.Kind, b => b.Kind, (a, b) => new { A = a.Number, B = b.Number }).ToArray().Select(r => $"{r.A}:{r.B}"), "enum key");

            //query syntax, with a condition on both after the join and a third query
            Same(from x in models join l in lines on x.ID equals l.ParityID where l.Quantity > x.Number select $"{x.Name}:{l.Price:0.##}",
                (from x in q join l in lq on x.ID equals l.ParityID where l.Quantity > x.Number select new { x.Name, l.Price }).ToArray().Select(r => $"{r.Name}:{r.Price:0.##}"), "query syntax");
            Same(from x in models join l in lines on x.ID equals l.ParityID join n in notes on l.ID equals n.LineID select $"{x.Number}:{l.Quantity}:{n.Text}",
                (from x in q join l in lq on x.ID equals l.ParityID join n in nq on l.ID equals n.LineID select new { x.Number, l.Quantity, n.Text }).ToArray().Select(r => $"{r.Number}:{r.Quantity}:{r.Text}"), "three queries");
            Same(from x in models join l in lines on x.ID equals l.ParityID orderby x.Number, l.Quantity descending select l.Quantity,
                (from x in q join l in lq on x.ID equals l.ParityID orderby x.Number, l.Quantity descending select l.Quantity).ToArray(), "ordered");
            Assert.Equal((from x in models join l in lines on x.ID equals l.ParityID orderby x.Number descending, l.Quantity select l.Quantity).Skip(1).Take(3),
                (from x in q join l in lq on x.ID equals l.ParityID orderby x.Number descending, l.Quantity select l.Quantity).Skip(1).Take(3).ToArray());

            //filtered, paged, and projected queries paired
            Same(models.Where(x => x.Number > 1).Join(lines.Where(l => l.Quantity < 4), x => x.ID, l => l.ParityID, (x, l) => $"{x.Number}:{l.Quantity}"),
                q.Where(x => x.Number > 1).Join(lq.Where(l => l.Quantity < 4), x => x.ID, l => l.ParityID, (x, l) => new { x.Number, l.Quantity }).ToArray().Select(r => $"{r.Number}:{r.Quantity}"), "filtered");
            Same(models.OrderBy(x => x.Number).Take(3).Join(lines, x => x.ID, l => l.ParityID, (x, l) => $"{x.Number}:{l.Quantity}"),
                q.OrderBy(x => x.Number).Take(3).Join(lq, x => x.ID, l => l.ParityID, (x, l) => new { x.Number, l.Quantity }).ToArray().Select(r => $"{r.Number}:{r.Quantity}"), "paged");
            Same(models.Select(x => new { x.ID, Twice = x.Number * 2 }).Join(lines.Select(l => new { l.ParityID, l.Quantity }), x => x.ID, l => l.ParityID, (x, l) => $"{x.Twice}:{l.Quantity}"),
                q.Select(x => new { x.ID, Twice = x.Number * 2 }).Join(lq.Select(l => new { l.ParityID, l.Quantity }), x => x.ID, l => l.ParityID, (x, l) => new { x.Twice, l.Quantity }).ToArray().Select(r => $"{r.Twice}:{r.Quantity}"), "projected");
            Same(models.Select(x => x.Number).Join(lines.Select(l => l.Quantity), n => n, n => n, (a, b) => a + b), q.Select(x => x.Number).Join(lq.Select(l => l.Quantity), n => n, n => n, (a, b) => a + b).ToArray(), "values");

            //ended, filtered, ordered, and built from after the join
            var joined = models.Join(lines, x => x.ID, l => l.ParityID, (x, l) => new { x, l });
            var joinedQuery = q.Join(lq, x => x.ID, l => l.ParityID, (x, l) => new { x, l });
            Assert.Equal(joined.Count(), joinedQuery.Count());
            Assert.Equal(joined.Count(r => r.l.Quantity > 2), joinedQuery.Count(r => r.l.Quantity > 2));
            Assert.True(joinedQuery.Any(r => r.x.Name == "banana"));
            Assert.False(joinedQuery.Any(r => r.x.Number == 0));
            Assert.Equal(joined.OrderBy(r => r.l.Price).ThenBy(r => r.l.Quantity).First().l.Quantity, joinedQuery.OrderBy(r => r.l.Price).ThenBy(r => r.l.Quantity).First().l.Quantity);
            Same(joined.Where(r => r.x.Kind == ParityKind.Medium).Select(r => r.l.Quantity), joinedQuery.Where(r => r.x.Kind == ParityKind.Medium).Select(r => r.l.Quantity).ToArray(), "enum after");
            Same(joined.Where(r => r.x.Name != null && r.x.Name.StartsWith("a")).Select(r => r.l.Quantity * 2), joinedQuery.Where(r => r.x.Name != null && r.x.Name.StartsWith("a")).Select(r => r.l.Quantity * 2).ToArray(), "function after");

            //different elements, aggregates, and the other endings after the join
            Same(joined.Select(r => r.x.Number).Distinct(), joinedQuery.Select(r => r.x.Number).Distinct().ToArray(), "distinct");
            Assert.Equal(joined.Select(r => r.x.Number).Distinct().Count(), joinedQuery.Select(r => r.x.Number).Distinct().Count());
            Assert.Equal(joined.Select(r => r.x.Number).Distinct().OrderByDescending(n => n).Take(2), joinedQuery.Select(r => r.x.Number).Distinct().OrderByDescending(n => n).Take(2).ToArray());
            Assert.Equal(joined.Select(r => new { r.x.Flag }).Distinct().Count(), joinedQuery.Select(r => new { r.x.Flag }).Distinct().Count());
            Assert.Equal(joined.Sum(r => r.l.Price * r.l.Quantity), joinedQuery.Sum(r => r.l.Price * r.l.Quantity));
            Assert.Equal(joined.Select(r => r.l.Quantity).Sum(), joinedQuery.Select(r => r.l.Quantity).Sum());
            Assert.Equal(joined.Min(r => r.x.Number + r.l.Quantity), joinedQuery.Min(r => r.x.Number + r.l.Quantity));
            Assert.Equal(joined.Max(r => r.x.Kind), joinedQuery.Max(r => r.x.Kind));
            Assert.Equal(joined.Average(r => r.l.Quantity), joinedQuery.Average(r => r.l.Quantity), 9);
            Assert.Equal(joined.Where(r => r.x.Number > 2).Sum(r => r.l.Quantity), joinedQuery.Where(r => r.x.Number > 2).Sum(r => r.l.Quantity));
            Assert.Equal(0, joinedQuery.Where(r => r.x.Number > 100).Sum(r => r.l.Quantity));
            Assert.Equal(joined.OrderBy(r => r.l.Quantity).Take(3).Sum(r => r.l.Quantity), joinedQuery.OrderBy(r => r.l.Quantity).Take(3).Sum(r => r.l.Quantity));
            Assert.True(joinedQuery.All(r => r.l.Quantity > 0));
            Assert.False(joinedQuery.All(r => r.l.Quantity > 1));
            Assert.Equal(joined.OrderBy(r => r.l.Quantity).ThenBy(r => r.x.Number).Last().l.Quantity, joinedQuery.OrderBy(r => r.l.Quantity).ThenBy(r => r.x.Number).Last().l.Quantity);
            Assert.Equal(joined.OrderBy(r => r.x.Number).ThenBy(r => r.l.Quantity).ElementAt(3).l.Quantity, joinedQuery.OrderBy(r => r.x.Number).ThenBy(r => r.l.Quantity).ElementAt(3).l.Quantity);
            Assert.Null(joinedQuery.OrderBy(r => r.x.Number).ElementAtOrDefault(100));
            Assert.Equal(joined.Select(r => r.l.Quantity).OrderBy(n => n).Last(), joinedQuery.Select(r => r.l.Quantity).OrderBy(n => n).Last());

            //every model, with its lines or none
            Same(from x in models join l in lines on x.ID equals l.ParityID into g from l in g.DefaultIfEmpty() select $"{x.Number}:{l?.Quantity}",
                (from x in q join l in lq on x.ID equals l.ParityID into g from l in g.DefaultIfEmpty() select new { x.Number, l }).ToArray().Select(r => $"{r.Number}:{r.l?.Quantity}"), "left join");
            Same(from x in models join l in lines on x.ID equals l.ParityID into g from l in g.DefaultIfEmpty() select $"{x.Number}:{(l == null ? -1 : l.Quantity)}",
                (from x in q join l in lq on x.ID equals l.ParityID into g from l in g.DefaultIfEmpty() select new { x.Number, Quantity = l == null ? -1 : l.Quantity }).ToArray().Select(r => $"{r.Number}:{r.Quantity}"), "left join null check");
            Same(from x in models join l in lines on x.ID equals l.ParityID into g from l in g.DefaultIfEmpty() where l == null select x.Number,
                (from x in q join l in lq on x.ID equals l.ParityID into g from l in g.DefaultIfEmpty() where l == null select x.Number).ToArray(), "left join without a match");
            Same(from x in models join l in lines on x.ID equals l.ParityID into g from l in g select $"{x.Number}:{l.Quantity}",
                (from x in q join l in lq on x.ID equals l.ParityID into g from l in g select new { x.Number, l.Quantity }).ToArray().Select(r => $"{r.Number}:{r.Quantity}"), "group join flattened");
            Same(models.LeftJoin(lines, x => x.ID, l => l.ParityID, (x, l) => $"{x.Number}:{l?.Quantity}"), q.LeftJoin(lq, x => x.ID, l => l.ParityID, (x, l) => new { x.Number, l }).ToArray().Select(r => $"{r.Number}:{r.l?.Quantity}"), "LeftJoin");
            Same(models.LeftJoin(lines.Select(l => new { l.ParityID, l.Quantity }), x => x.ID, l => l.ParityID, (x, l) => $"{x.Number}:{l?.Quantity}"), q.LeftJoin(lq.Select(l => new { l.ParityID, l.Quantity }), x => x.ID, l => l.ParityID, (x, l) => new { x.Number, l }).ToArray().Select(r => $"{r.Number}:{r.l?.Quantity}"), "LeftJoin of objects");
            Same(models.Where(x => x.Number < 3).RightJoin(lines, x => x.ID, l => l.ParityID, (x, l) => $"{x?.Number}:{l.Quantity}"), q.Where(x => x.Number < 3).RightJoin(lq, x => x.ID, l => l.ParityID, (x, l) => new { x, l.Quantity }).ToArray().Select(r => $"{r.x?.Number}:{r.Quantity}"), "RightJoin");
            Assert.Equal(models.LeftJoin(lines, x => x.ID, l => l.ParityID, (x, l) => l).Count(l => l == null), q.LeftJoin(lq, x => x.ID, l => l.ParityID, (x, l) => new { x.Number, l }).Count(r => r.l == null));

            //a member of the row that isn't there reads as null, in memory as in SQL
            Same(from x in models join l in lines on x.ID equals l.ParityID into g from l in g.DefaultIfEmpty() select $"{x.Number}:{l?.Quantity}:{l?.Price:0.##}",
                (from x in q join l in lq on x.ID equals l.ParityID into g from l in g.DefaultIfEmpty() select new { x.Number, Quantity = (int?)l.Quantity, Price = (decimal?)l.Price }).ToArray().Select(r => $"{r.Number}:{r.Quantity}:{r.Price:0.##}"), "missing member");
            Same(models.LeftJoin(lines, x => x.ID, l => l.ParityID, (x, l) => new { x.Number, l }).Where(r => r.l != null && r.l.Quantity < 3).Select(r => r.Number),
                q.LeftJoin(lq, x => x.ID, l => l.ParityID, (x, l) => new { x.Number, l }).Where(r => r.l.Quantity < 3).ToArray().Select(r => r.Number), "missing member condition");
            Same(models.LeftJoin(lines, x => x.ID, l => l.ParityID, (x, l) => new { x.Number, l }).Where(r => r.l == null || r.l.Quantity != 3).Select(r => r.Number),
                q.LeftJoin(lq, x => x.ID, l => l.ParityID, (x, l) => new { x.Number, l }).Where(r => r.l.Quantity != 3).ToArray().Select(r => r.Number), "missing member not equal");

            //a filter of the group is a condition of the left join, which keeps a model it leaves no lines of
            Same(from x in models join l in lines on x.ID equals l.ParityID into g from l in g.Where(l => l.Quantity > 2).DefaultIfEmpty() select $"{x.Number}:{l?.Quantity}",
                (from x in q join l in lq on x.ID equals l.ParityID into g from l in g.Where(l => l.Quantity > 2).DefaultIfEmpty() select new { x.Number, Quantity = (int?)l.Quantity }).ToArray().Select(r => $"{r.Number}:{r.Quantity}"), "filtered left join");
            Same(from x in models join l in lines on x.ID equals l.ParityID into g from l in g.Where(l => l.Quantity > x.Number) select $"{x.Number}:{l.Quantity}",
                (from x in q join l in lq on x.ID equals l.ParityID into g from l in g.Where(l => l.Quantity > x.Number) select new { x.Number, l.Quantity }).ToArray().Select(r => $"{r.Number}:{r.Quantity}"), "filtered by the outer");

            //SelectMany of another query: every pair, or those its conditions keep
            Same(from x in models.Where(x => x.Number < 3) from y in models.Where(y => y.Number < 2) select $"{x.Number}:{y.Number}",
                (from x in q.Where(x => x.Number < 3) from y in q.Where(y => y.Number < 2) select new { A = x.Number, B = y.Number }).ToArray().Select(r => $"{r.A}:{r.B}"), "cross join");
            Same(from x in models from l in lines where l.ParityID == x.ID && l.Quantity > 2 select $"{x.Number}:{l.Quantity}",
                (from x in q from l in lq where l.ParityID == x.ID && l.Quantity > 2 select new { x.Number, l.Quantity }).ToArray().Select(r => $"{r.Number}:{r.Quantity}"), "cross join filtered");
            Same(from x in models from l in lines.Where(l => l.ParityID == x.ID) select $"{x.Number}:{l.Quantity}",
                (from x in q from l in lq.Where(l => l.ParityID == x.ID) select new { x.Number, l.Quantity }).ToArray().Select(r => $"{r.Number}:{r.Quantity}"), "correlated");
            Same(from x in models from l in lines.Where(l => l.ParityID == x.ID).Where(l => l.Quantity < 4).DefaultIfEmpty() select $"{x.Number}:{l?.Quantity}",
                (from x in q from l in lq.Where(l => l.ParityID == x.ID).Where(l => l.Quantity < 4).DefaultIfEmpty() select new { x.Number, Quantity = (int?)l.Quantity }).ToArray().Select(r => $"{r.Number}:{r.Quantity}"), "correlated left");
            Same(models.SelectMany(x => lines.Where(l => l.ParityID == x.ID && l.Quantity > x.Number)).Select(l => l.Quantity),
                q.SelectMany(x => lq.Where(l => l.ParityID == x.ID && l.Quantity > x.Number)).ToArray().Select(l => l.Quantity), "correlated without a result");
            Assert.Equal(models.Length * lines.Length, (from x in q from l in lq select x.Number + l.Quantity).Count());

            //a group read by aggregates of it
            Same(models.GroupJoin(lines, x => x.ID, l => l.ParityID, (x, g) => $"{x.Number}:{g.Count()}:{g.Sum(l => l.Quantity)}:{g.Any(l => l.Quantity > 3)}:{g.All(l => l.Quantity > 1)}:{g.Max(l => (int?)l.Quantity)}:{g.Where(l => l.Quantity > 2).Count()}"),
                q.GroupJoin(lq, x => x.ID, l => l.ParityID, (x, g) => new { x.Number, Count = g.Count(), Sum = g.Sum(l => l.Quantity), Big = g.Any(l => l.Quantity > 3), Small = g.All(l => l.Quantity > 1), Max = g.Max(l => (int?)l.Quantity), Over = g.Where(l => l.Quantity > 2).Count() }).ToArray().Select(r => $"{r.Number}:{r.Count}:{r.Sum}:{r.Big}:{r.Small}:{r.Max}:{r.Over}"), "group join");
            Same(models.GroupJoin(lines, x => x.ID, l => l.ParityID, (x, g) => $"{x.Number}:{(g.Any() ? g.Average(l => l.Quantity) : 0)}"),
                q.GroupJoin(lq, x => x.ID, l => l.ParityID, (x, g) => new { x.Number, Average = g.Any() ? g.Average(l => l.Quantity) : 0 }).ToArray().Select(r => $"{r.Number}:{r.Average}"), "group join average");
            Same(models.GroupJoin(lines, x => x.ID, l => l.ParityID, (x, g) => new { x.Number, Count = g.Count() }).Where(r => r.Count > 0).Select(r => r.Number),
                q.GroupJoin(lq, x => x.ID, l => l.ParityID, (x, g) => new { x.Number, Count = g.Count() }).Where(r => r.Count > 0).Select(r => r.Number).ToArray(), "group join filtered");

            //what LINQ can't pair in a store
            _ = Assert.Throws<NotSupportedException>(() => q.Join(lq, x => x.ID, l => l.ParityID, (x, l) => new { x, l }, EqualityComparer<Guid>.Default).ToArray());
            //a group is read whole in memory, a store reads it by aggregates
            if (inStore)
                _ = Assert.Throws<NotSupportedException>(() => q.GroupJoin(lq, x => x.ID, l => l.ParityID, (x, g) => new { x, g }).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => q.Join(new[] { lines[0] }.AsQueryable(), x => x.ID, l => l.ParityID, (x, l) => x.Number).ToArray());
        }

        private static void AssertOperators(IRepo repo, ParityModel[] models, ParityLineModel[] lines)
        {
            void Same<T>(IEnumerable<T> expected, IEnumerable<T> actual, string name)
            {
                var e = expected.Select(x => x?.ToString() ?? "null").Order(StringComparer.Ordinal).ToArray();
                var a = actual.Select(x => x?.ToString() ?? "null").Order(StringComparer.Ordinal).ToArray();
                Assert.True(e.SequenceEqual(a), $"{name}: expected [{String.Join("|", e)}] found [{String.Join("|", a)}]");
            }
            var q = repo.Query<ParityModel>();
            var lq = repo.Query<ParityLineModel>();
            var none = q.Where(x => x.Number > 100);

            //elements typed again, and values that aren't null
            Assert.Equal(models.Select(x => x.Number).Order(), q.Select(x => x.Number).Cast<int>().ToArray().Order());
            Assert.Equal(models.Length, q.Cast<object>().ToArray().Length);
            Assert.Equal(models.Select(x => (int?)x.Number).Order(), q.Select(x => x.Number).Cast<int?>().ToArray().Order());
            Assert.Equal(models.Where(x => x.Maybe != null).Select(x => x.Maybe!.Value).Order(), q.Select(x => x.Maybe).OfType<int>().ToArray().Order());
            Assert.Equal(models.Select(x => x.Name).OfType<string>().Order(), q.Select(x => x.Name).OfType<string>().ToArray().Order());
            Assert.Equal(models.Length, q.OfType<ParityModel>().Count());
            _ = Assert.Throws<NotSupportedException>(() => q.Select(x => x.Number).Cast<long>().ToArray());

            //values ordered as they are
            Assert.Equal(models.Select(x => x.Number).Order(), q.Select(x => x.Number).Order().ToArray());
            Assert.Equal(models.Select(x => x.Money).OrderDescending(), q.Select(x => x.Money).OrderDescending().ToArray());
            Assert.Equal(models.Select(x => x.Kind).Order(), q.Select(x => x.Kind).Order().ToArray());
            Assert.Equal(models.Select(x => x.Number).Order().Skip(1).Take(2), q.Select(x => x.Number).Order().Skip(1).Take(2).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => q.Order().ToArray());

            //the value given for no element
            Assert.Equal(-1, none.Select(x => x.Number).FirstOrDefault(-1));
            Assert.Equal(-1, q.Select(x => x.Number).FirstOrDefault(n => n > 100, -1));
            Assert.Equal(3, q.Select(x => x.Number).FirstOrDefault(n => n == 3, -1));
            Assert.Equal(-1, q.Select(x => x.Number).SingleOrDefault(n => n > 100, -1));
            Assert.Equal(-1, q.Select(x => x.Number).OrderBy(n => n).LastOrDefault(n => n > 100, -1));
            Assert.Equal(5, q.Select(x => x.Number).OrderBy(n => n).LastOrDefault(-1));

            //the element with the smallest or largest key
            Assert.Equal(models.MinBy(x => x.Money)!.Number, q.MinBy(x => x.Money)!.Number);
            Assert.Equal(models.MaxBy(x => x.Date)!.Number, q.MaxBy(x => x.Date)!.Number);
            Assert.Equal(models.Where(x => x.Flag).MaxBy(x => x.Number)!.Number, q.Where(x => x.Flag).MaxBy(x => x.Number)!.Number);
            Assert.Null(none.MinBy(x => x.Number));
            Assert.Equal(models.Select(x => new { x.Number, x.Ratio }).MaxBy(r => r.Ratio)!.Number, q.Select(x => new { x.Number, x.Ratio }).MaxBy(r => r.Ratio)!.Number);
            Assert.Equal(lines.MaxBy(l => l.Price * l.Quantity)!.Quantity, lq.MaxBy(l => l.Price * l.Quantity)!.Quantity);
            Assert.Equal(models.Select(x => x.Number).Max(), q.Select(x => x.Number).MaxBy(n => n));
            _ = Assert.Throws<InvalidOperationException>(() => none.Select(x => x.Number).MinBy(n => n));

            //groups of what GroupBy selects, and results of the key and the group
            Same(models.GroupBy(x => x.Flag, x => x.Number).Select(g => $"{g.Key}:{g.Sum()}:{g.Count()}:{g.Max()}:{g.Count(n => n > 1)}"),
                q.GroupBy(x => x.Flag, x => x.Number).Select(g => new { g.Key, Sum = g.Sum(), Count = g.Count(), Max = g.Max(), Big = g.Count(n => n > 1) }).ToArray().Select(r => $"{r.Key}:{r.Sum}:{r.Count}:{r.Max}:{r.Big}"), "element selector");
            Same(models.GroupBy(x => x.Flag, (k, g) => $"{k}:{g.Count()}:{g.Sum(x => x.Number)}"),
                q.GroupBy(x => x.Flag, (k, g) => new { Key = k, Count = g.Count(), Sum = g.Sum(x => x.Number) }).ToArray().Select(r => $"{r.Key}:{r.Count}:{r.Sum}"), "result selector");
            Same(models.GroupBy(x => x.Kind, x => x.Money, (k, g) => $"{k}:{g.Sum():0.##}"),
                q.GroupBy(x => x.Kind, x => x.Money, (k, g) => new { k, Total = g.Sum() }).ToArray().Select(r => $"{r.k}:{r.Total:0.##}"), "both selectors");
            Same(from x in models group x.Number by x.Flag into g select $"{g.Key}:{g.Sum()}",
                (from x in q group x.Number by x.Flag into g select new { g.Key, Total = g.Sum() }).ToArray().Select(r => $"{r.Key}:{r.Total}"), "query syntax");
            Same(models.GroupBy(x => new { x.Flag, x.Kind }, (k, g) => $"{k.Flag}:{k.Kind}:{g.Count()}").Where(r => !r.EndsWith(":1")),
                q.GroupBy(x => new { x.Flag, x.Kind }, (k, g) => new { k.Flag, k.Kind, Count = g.Count() }).Where(r => r.Count > 1).ToArray().Select(r => $"{r.Flag}:{r.Kind}:{r.Count}"), "composite result selector");

            //how many each key has
            Same(models.CountBy(x => x.Flag).Select(p => $"{p.Key}:{p.Value}"), q.CountBy(x => x.Flag).ToArray().Select(p => $"{p.Key}:{p.Value}"), "count by");
            Same(models.CountBy(x => x.Kind).Select(p => $"{p.Key}:{p.Value}"), q.CountBy(x => x.Kind).ToArray().Select(p => $"{p.Key}:{p.Value}"), "count by enum");
            Same(models.CountBy(x => x.Number % 3).Where(p => p.Value > 1).Select(p => p.Key), q.CountBy(x => x.Number % 3).Where(p => p.Value > 1).ToArray().Select(p => p.Key), "count by filtered");
            Assert.Equal(models.CountBy(x => x.Number % 4).OrderByDescending(p => p.Value).ThenBy(p => p.Key).Select(p => p.Key), q.CountBy(x => x.Number % 4).OrderByDescending(p => p.Value).ThenBy(p => p.Key).ToArray().Select(p => p.Key));

            //no element is one of the default
            Assert.Equal(0, none.Select(x => x.Number).DefaultIfEmpty().Max());
            Assert.Equal(7, none.Select(x => x.Number).DefaultIfEmpty(7).Max());
            Assert.Equal(models.Max(x => x.Number), q.Select(x => x.Number).DefaultIfEmpty().Max());
            Assert.Equal(0m, none.Select(x => x.Money).DefaultIfEmpty().Sum());
            Assert.Equal([0], none.Select(x => x.Number).DefaultIfEmpty().ToArray());
            Assert.Equal([9], none.Select(x => x.Number).DefaultIfEmpty(9).ToArray());
            Assert.Equal(1, none.DefaultIfEmpty().Count());
            Assert.True(none.DefaultIfEmpty().Any());
            Assert.Null(none.DefaultIfEmpty().First());
            Assert.Equal(models.Length, q.DefaultIfEmpty().Count());
            _ = Assert.Throws<NotSupportedException>(() => none.Select(x => x.Number).DefaultIfEmpty().Where(n => n > 0).ToArray());

            //chunks of the elements in order
            Assert.Equal(String.Join("|", models.OrderBy(x => x.Number).Select(x => x.Number).Chunk(4).Select(c => String.Join(",", c))), String.Join("|", q.OrderBy(x => x.Number).Select(x => x.Number).Chunk(4).ToArray().Select(c => String.Join(",", c))));
            Assert.Equal([5, 1], q.OrderBy(x => x.Number).Chunk(5).ToArray().Select(c => c.Length));
            _ = Assert.Throws<NotSupportedException>(() => q.Chunk(2).Count());

            //what no store runs
            _ = Assert.Throws<NotSupportedException>(() => q.Select(x => x.Number).Aggregate((a, b) => a + b));

            //after a join or a set
            var joinedValues = q.Join(lq, x => x.ID, l => l.ParityID, (x, l) => l.Quantity);
            Assert.Equal(lines.Select(l => l.Quantity).Order(), joinedValues.Order().ToArray());
            Assert.Equal(lines.Select(l => l.Quantity).OrderDescending().Take(3), joinedValues.OrderDescending().Take(3).ToArray());
            Assert.Equal(lines.Max(l => l.Quantity), joinedValues.MaxBy(n => n));
            Assert.Equal(lines.Min(l => l.Price), q.Join(lq, x => x.ID, l => l.ParityID, (x, l) => new { x.Number, l.Price }).MinBy(r => r.Price)!.Price);
            Assert.Equal(-1, joinedValues.Where(n => n > 100).FirstOrDefault(-1));
            Assert.Equal(0, joinedValues.Where(n => n > 100).DefaultIfEmpty().Max());
            Assert.Equal(lines.Length, joinedValues.Cast<int>().Count());
            Assert.Equal(lines.Length, q.LeftJoin(lq, x => x.ID, l => l.ParityID, (x, l) => l == null ? (int?)null : l.Quantity).OfType<int>().Count());
            var low = q.Where(x => x.Number < 4).Select(x => x.Number % 3);
            var high = q.Where(x => x.Number > 1).Select(x => x.Number % 3);
            Assert.Equal(models.Where(x => x.Number < 4).Select(x => x.Number % 3).Union(models.Where(x => x.Number > 1).Select(x => x.Number % 3)).Order(), low.Union(high).Order().ToArray());
            Assert.Equal(models.Select(x => x.Kind).Distinct().Order(), q.Select(x => x.Kind).Union(q.Select(x => x.Kind)).Order().ToArray());
        }

        private static void AssertPositions(IRepo repo, ParityModel[] models, ParityLineModel[] lines)
        {
            var q = repo.Query<ParityModel>();
            var lq = repo.Query<ParityLineModel>();
            var ordered = models.OrderBy(x => x.Number).ToArray();
            var oq = q.OrderBy(x => x.Number);

            //the place of each element in the order
            Assert.Equal(ordered.Index().Select(t => $"{t.Index}:{t.Item.Number}"), oq.Index().ToArray().Select(t => $"{t.Index}:{t.Item.Number}"));
            Assert.Equal(ordered.Select((x, i) => x.Number * 10 + i), oq.Select((x, i) => x.Number * 10 + i).ToArray());
            Assert.Equal(ordered.Where((x, i) => i % 2 == 0).Select(x => x.Number), oq.Where((x, i) => i % 2 == 0).ToArray().Select(x => x.Number));
            Assert.Equal(ordered.Select(x => x.Name).Index().Where(t => t.Index > 2).Select(t => t.Item), oq.Select(x => x.Name).Index().Where(t => t.Index > 2).Select(t => t.Item).ToArray());
            Assert.Equal(ordered.Where((x, i) => i > 1).Count(), oq.Where((x, i) => i > 1).Count());
            Assert.Equal(ordered.Index().Where(t => t.Index % 2 == 1).Sum(t => t.Item.Number), oq.Index().Where(t => t.Index % 2 == 1).Sum(t => t.Item.Number));
            Assert.Equal(ordered.Select(x => x.Number).Index().Skip(2).First().Index, oq.Select(x => x.Number).Index().Skip(2).First().Index);

            //the elements before the first that fails, or from it on
            Assert.Equal(ordered.TakeWhile(x => x.Number < 3).Select(x => x.Number), oq.TakeWhile(x => x.Number < 3).ToArray().Select(x => x.Number));
            Assert.Equal(ordered.SkipWhile(x => x.Name != null).Select(x => x.Number), oq.SkipWhile(x => x.Name != null).ToArray().Select(x => x.Number));
            Assert.Equal(ordered.TakeWhile((x, i) => i < 2 || x.Flag).Select(x => x.Number), oq.TakeWhile((x, i) => i < 2 || x.Flag).ToArray().Select(x => x.Number));
            Assert.Equal(ordered.Select(x => x.Money).SkipWhile(m => m < 3).Sum(), oq.Select(x => x.Money).SkipWhile(m => m < 3).Sum());

            //the last elements, all but them, and one counted from the end
            Assert.Equal(ordered.TakeLast(2).Select(x => x.Number), oq.TakeLast(2).ToArray().Select(x => x.Number));
            Assert.Equal(ordered.SkipLast(4).Select(x => x.Number), oq.SkipLast(4).ToArray().Select(x => x.Number));
            Assert.Equal(ordered.Select(x => x.Number).Take(^3..), oq.Select(x => x.Number).Take(^3..).ToArray());
            Assert.Equal(ordered.Select(x => x.Number).Take(..^3), oq.Select(x => x.Number).Take(..^3).ToArray());
            Assert.Equal(ordered.Select(x => x.Number).Take(1..3), oq.Select(x => x.Number).Take(1..3).ToArray());
            Assert.Equal(ordered[^2].Number, oq.ElementAt(^2).Number);
            Assert.Null(oq.ElementAtOrDefault(^100));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => oq.ElementAt(^100));
            Assert.Equal(ordered.TakeLast(3).OrderByDescending(x => x.Money).First().Number, oq.TakeLast(3).OrderByDescending(x => x.Money).First().Number);

            //the first element of each key
            Assert.Equal(ordered.DistinctBy(x => x.Flag).Select(x => x.Number), oq.DistinctBy(x => x.Flag).ToArray().Select(x => x.Number));
            Assert.Equal(ordered.DistinctBy(x => new { x.Kind, x.Flag }).Select(x => x.Number), oq.DistinctBy(x => new { x.Kind, x.Flag }).ToArray().Select(x => x.Number));
            Assert.Equal(ordered.DistinctBy(x => x.Maybe).Count(), oq.DistinctBy(x => x.Maybe).Count());
            var parents = lq.Select(l => l.ParityID);
            Assert.Equal(ordered.IntersectBy(lines.Select(l => l.ParityID), x => x.ID).Select(x => x.Number), oq.IntersectBy(parents, x => x.ID).ToArray().Select(x => x.Number));
            Assert.Equal(ordered.ExceptBy(lines.Select(l => l.ParityID), x => x.ID).Select(x => x.Number), oq.ExceptBy(parents, x => x.ID).ToArray().Select(x => x.Number));
            Assert.Equal(ordered.Where(x => x.Number < 3).UnionBy(models.Where(x => x.Number > 1).OrderByDescending(x => x.Number), x => x.Number % 3).Select(x => x.Number),
                q.Where(x => x.Number < 3).OrderBy(x => x.Number).UnionBy(q.Where(x => x.Number > 1).OrderByDescending(x => x.Number), x => x.Number % 3).ToArray().Select(x => x.Number));

            //one more element at either end
            Assert.Equal(ordered.Select(x => x.Number).Append(99), oq.Select(x => x.Number).Append(99).ToArray());
            Assert.Equal(ordered.Select(x => x.Number).Prepend(-1), oq.Select(x => x.Number).Prepend(-1).ToArray());
            Assert.Equal(-1, oq.Select(x => x.Number).Prepend(-1).First());
            Assert.Equal(ordered.Select(x => x.Name).Append("end"), oq.Select(x => x.Name).Append("end").ToArray());
            Assert.Equal(ordered.Select(x => $"{x.Number}:{x.Name}").Append("9:z"), oq.Select(x => new { x.Number, x.Name }).Append(new { Number = 9, Name = (string?)"z" }).ToArray().Select(r => $"{r.Number}:{r.Name}"));
            var added = new ParityModel { ID = Guid.NewGuid(), Number = 42, Name = "added", Money = 1.5m, Date = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), Moment = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), Kind = ParityKind.Large, Access = ParityAccess.ReadWrite, Clock = new TimeOnly(1, 2, 3), Day = new DateOnly(2020, 1, 2), Span = TimeSpan.FromHours(1) };
            var appended = q.Where(x => x.Number < 2).OrderBy(x => x.Number).Append(added).ToArray();
            Assert.Equal([0, 1, 42], appended.Select(x => x.Number));
            Assert.Equal(("added", ParityKind.Large, ParityAccess.ReadWrite, 1.5m), (appended[^1].Name, appended[^1].Kind, appended[^1].Access, appended[^1].Money));

            //elements paired by their places
            Assert.Equal(ordered.Zip(lines.OrderBy(l => l.Quantity).ThenBy(l => l.Price), (x, l) => $"{x.Number}:{l.Quantity}"), oq.Zip(lq.OrderBy(l => l.Quantity).ThenBy(l => l.Price), (x, l) => new { x.Number, l.Quantity }).ToArray().Select(r => $"{r.Number}:{r.Quantity}"));
            Assert.Equal(ordered.Select(x => x.Number).Zip(ordered.Select(x => x.Name)).Select(t => $"{t.First}:{t.Second}"), oq.Select(x => x.Number).Zip(oq.Select(x => x.Name)).ToArray().Select(t => $"{t.First}:{t.Second}"));
            Assert.Equal(3, oq.Zip(oq.Skip(3), (a, b) => a.Number + b.Number).Count());
            Assert.True(oq.Select(x => x.Number).SequenceEqual(q.OrderBy(x => x.Number).Select(x => x.Number)));
            Assert.False(oq.Select(x => x.Number).SequenceEqual(q.OrderByDescending(x => x.Number).Select(x => x.Number)));
            Assert.False(oq.Select(x => x.Number).SequenceEqual(oq.Take(3).Select(x => x.Number)));
            Assert.True(oq.Select(x => new { x.Number, x.Name }).SequenceEqual(oq.Select(x => new { x.Number, x.Name })));
            Assert.False(oq.Select(x => x.Name).SequenceEqual(oq.Select(x => x.Title)));
        }

        private static void AssertLookIns(IRepo repo, ParityModel[] models, ParityLineModel[] lines)
        {
            var big = repo.Query<ParityLineModel>().Where(l => l.Quantity > 2).Select(l => l.ParityID);
            var hasBig = lines.Where(l => l.Quantity > 2).Select(l => l.ParityID).ToHashSet();
            var values = repo.Query<ParityModel>().OrderBy(x => x.Number).Select(x => new { x.Number, Big = big.Contains(x.ID), NotBig = !big.Contains(x.ID) }).ToArray();
            foreach (var row in values)
            {
                Assert.Equal(hasBig.Contains(models.Single(m => m.Number == row.Number).ID), row.Big);
                Assert.Equal(!row.Big, row.NotBig);
            }
            Assert.Equal(models.OrderBy(x => x.Number).Select(x => hasBig.Contains(x.ID)), repo.Query<ParityModel>().OrderBy(x => x.Number).Select(x => big.Contains(x.ID)).ToArray());
            var grouped = repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Rows = g.Count(x => big.Contains(x.ID)) }).ToArray();
            foreach (var row in grouped)
                Assert.Equal(models.Count(x => x.Flag == row.Key && hasBig.Contains(x.ID)), row.Rows);

            //an update and a delete, including of the table looked in
            var marked = models.OrderBy(x => x.Number).First(x => hasBig.Contains(x.ID));
            var markedNumber = marked.Number;
            var justOne = repo.Query<ParityModel>().Where(x => x.Number == markedNumber).Select(x => x.ID);
            var note = new ParityNoteModel { ID = Guid.NewGuid(), LineID = lines[0].ID, Rank = 70, Text = "a", ReviewerID = marked.ID };
            repo.Create<ParityNoteModel>(note);
            try
            {
                Assert.Equal(1, repo.Update<ParityNoteModel>(x => x.ID == note.ID && justOne.Contains(x.ReviewerID!.Value), new ParityNoteModel { Text = "b" }, new Graph<ParityNoteModel>(x => x.Text)));
                Assert.Equal("b", repo.Query<ParityNoteModel>().Single(x => x.ID == note.ID).Text);
                var noteID = note.ID;
                var sameTable = repo.Query<ParityNoteModel>().Where(x => x.ID == noteID).Select(x => x.ID);
                Assert.Equal(1, repo.Update<ParityNoteModel>(x => sameTable.Contains(x.ID), new ParityNoteModel { Text = "c" }, new Graph<ParityNoteModel>(x => x.Text)));
                Assert.Equal("c", repo.Query<ParityNoteModel>().Single(x => x.ID == note.ID).Text);
                Assert.Equal(1, repo.Update<ParityNoteModel>(x => sameTable.Contains(x.ID), x => new ParityNoteModel { Rank = x.Rank + 1 }));
                Assert.Equal(71, repo.Query<ParityNoteModel>().Single(x => x.ID == note.ID).Rank);
                Assert.Equal(0, repo.Delete<ParityNoteModel>(x => sameTable.Contains(x.ID) && x.Rank == 1));
                Assert.Equal(1, repo.Delete<ParityNoteModel>(x => sameTable.Contains(x.ID)));
                note = null!;
            }
            finally
            {
                if (note is not null)
                    repo.Delete<ParityNoteModel>(note);
            }

            //a tuple key is filtered and ordered by its parts
            var ordered = repo.Query<ParityModel>().GroupBy(x => new ValueTuple<bool, int>(x.Flag, x.Number)).Select(g => new { g.Key, Rows = g.Count() }).Where(r => r.Key.Item2 > 1).OrderByDescending(r => r.Key.Item2).ToArray();
            Assert.Equal(models.Where(x => x.Number > 1).OrderByDescending(x => x.Number).Select(x => x.Number), ordered.Select(r => r.Key.Item2));
            Assert.All(ordered, r => Assert.Equal(models.Single(m => m.Number == r.Key.Item2).Flag, r.Key.Item1));
        }

        //what a query reads past its page, from its end, and from the rows of a relation
        private static void AssertShapes(IRepo repo, ParityModel[] models, ParityLineModel[] lines, ParityNoteModel[] notes)
        {
            var ordered = models.OrderBy(x => x.Number).ToArray();
            var q = repo.Query<ParityModel>().OrderBy(x => x.Number);
            int[] Numbers(IEnumerable<ParityModel> rows) => rows.Select(x => x.Number).ToArray();

            //a count or aggregate of a page
            Assert.Equal(2, q.Skip(1).Take(2).Count());
            Assert.Equal(2L, q.Take(2).LongCount());
            Assert.Equal(ordered.Length - 1, q.Skip(1).Count());
            Assert.Equal(0, q.Take(0).Count());
            Assert.Equal(ordered.Skip(1).Take(2).Sum(x => x.Number), q.Skip(1).Take(2).Sum(x => x.Number));
            Assert.Equal(ordered.Take(2).Sum(x => x.Number), q.Take(2).Sum(x => x.Number));
            Assert.Equal(ordered.Skip(1).Take(3).Min(x => x.Number), q.Skip(1).Take(3).Min(x => x.Number));
            Assert.Equal(ordered.Skip(1).Take(3).Max(x => x.Number), q.Skip(1).Take(3).Max(x => x.Number));
            Assert.Equal(ordered.Skip(1).Take(2).Average(x => x.Number), q.Skip(1).Take(2).Average(x => x.Number), 5);
            Assert.Equal(ordered.Take(2).Sum(x => x.Number), q.Take(2).Select(x => x.Number).Sum());
            Assert.True(q.Skip(1).Take(1).Any());
            Assert.False(q.Skip(ordered.Length).Any());
            Assert.False(q.Take(0).Any());
            Assert.Equal(1, repo.Query<ParityModel>().OrderBy(x => x.Flag).Select(x => x.Flag).Distinct().Take(1).Count());
            _ = Assert.Throws<NotSupportedException>(() => q.Take(3).Select(x => x.Flag).Distinct().Count());
            _ = Assert.Throws<NotSupportedException>(() => q.Skip(1).Count(x => x.Number > 0));

            //no rows asked for, however the page is written
            Assert.Empty(q.Take(0).ToArray());
            Assert.Empty(q.Skip(1).Take(0).ToArray());
            Assert.Equal(0, q.Skip(1).Take(0).Count());
            Assert.Equal(0, q.Skip(1).Take(0).Sum(x => x.Number));
            Assert.False(q.Skip(1).Take(0).Any());
            Assert.Null(q.Skip(1).Take(0).FirstOrDefault());
            Assert.Empty(repo.Query<ParityModel>(new Graph<ParityModel>(true, x => x.Lines)).OrderBy(x => x.Number).Take(0).ToArray());
            Assert.Empty(repo.Query<ParityModel>(new Graph<ParityModel>(true, x => x.Lines)).OrderBy(x => x.Number).Skip(1).Take(0).ToArray());
            Assert.Empty(repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Rows = g.Count() }).Take(0).ToArray());
            Assert.Empty(repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Rows = g.Count() }).OrderBy(r => r.Key).Skip(1).Take(0).ToArray());

            //the last of an order
            Assert.Equal(ordered[^1].Number, q.Last().Number);
            Assert.Equal(ordered[^1].Number, q.LastOrDefault()!.Number);
            Assert.Equal(ordered.Last(x => x.Number < 3).Number, q.Last(x => x.Number < 3).Number);
            Assert.Null(q.LastOrDefault(x => x.Number < -1000));
            _ = Assert.Throws<InvalidOperationException>(() => q.Last(x => x.Number < -1000));
            Assert.Equal(ordered[^1].Number, q.Select(x => new { x.Number }).Last().Number);
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<ParityModel>().Last());
            _ = Assert.Throws<NotSupportedException>(() => q.Take(2).Last());
            var lastGroup = repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Rows = g.Count() }).OrderBy(r => r.Key).Last();
            Assert.True(lastGroup.Key);

            //an element by its place
            Assert.Equal(ordered[2].Number, q.ElementAt(2).Number);
            Assert.Equal(ordered[3].Number, q.Skip(1).ElementAt(2).Number);
            Assert.Equal(ordered[1].Number, q.ElementAtOrDefault(1)!.Number);
            Assert.Null(q.ElementAtOrDefault(ordered.Length));
            Assert.Null(q.ElementAtOrDefault(-1));
            Assert.Null(q.Take(2).ElementAtOrDefault(2));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => q.ElementAt(ordered.Length));

            //the other way round
            Assert.Equal(Numbers(ordered.Reverse()), Numbers(q.Reverse().ToArray()));
            Assert.Equal(Numbers(ordered.Reverse().Take(2)), Numbers(q.Reverse().Take(2).ToArray()));
            Assert.Equal(Numbers(ordered), Numbers(q.Reverse().Reverse().ToArray()));
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<ParityModel>().Reverse().ToArray());
            _ = Assert.Throws<NotSupportedException>(() => q.Take(2).Reverse().ToArray());

            //a value among those selected
            var number = ordered[1].Number;
            Assert.True(repo.Query<ParityModel>().Select(x => x.Number).Contains(number));
            Assert.False(repo.Query<ParityModel>().Select(x => x.Number).Contains(-1000));
            Assert.False(repo.Query<ParityModel>().Where(x => x.Number != number).Select(x => x.Number).Contains(number));
            Assert.True(repo.Query<ParityModel>().Select(x => x.Name).Contains(ordered[1].Name));
            Assert.True(repo.Query<ParityModel>().Select(x => x.Number * 2).Contains(number * 2));
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<ParityModel>().Contains(ordered[0]));
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<ParityModel>().Select(x => x.Number).Take(1).Contains(number));

            //the rows of a relation of the models
            var withLines = ordered.Where(x => x.Number >= 2);
            var ids = withLines.SelectMany(m => lines.Where(l => l.ParityID == m.ID)).Select(l => l.ID).Order().ToArray();
            Assert.Equal(ids, repo.Query<ParityModel>().Where(x => x.Number >= 2).SelectMany(x => x.Lines!).ToArray().Select(l => l.ID).Order());
            Assert.Equal(lines.Where(l => l.Quantity > 2).Select(l => l.ID).Order(), repo.Query<ParityModel>().SelectMany(x => x.Lines!.Where(l => l.Quantity > 2)).ToArray().Select(l => l.ID).Order());
            Assert.Equal(lines.Select(l => l.Quantity).Order(), repo.Query<ParityModel>().SelectMany(x => x.Lines!.Select(l => l.Quantity)).ToArray().Order());
            var firstParent = ordered.First(x => lines.Any(l => l.ParityID == x.ID));
            Assert.Equal(lines.Where(l => l.ParityID == firstParent.ID).Select(l => l.ID).Order(), repo.Query<ParityModel>().Where(x => x.Lines!.Any()).OrderBy(x => x.Number).Take(1).SelectMany(x => x.Lines!).ToList().Select(l => l.ID).Order());
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<ParityModel>(new Graph<ParityModel>(true)).SelectMany(x => x.Lines!).ToArray());

            //a page of each model's rows
            int[] Quantities(ParityModel m) => lines.Where(l => l.ParityID == m.ID).Select(l => l.Quantity).Order().ToArray();
            var topOne = repo.Query<ParityModel>().Select(x => new { x.Number, Lines = x.Lines!.OrderBy(l => l.Quantity).Take(1).Select(l => l.Quantity).ToArray() }).ToArray();
            Assert.Equal(models.Length, topOne.Length);
            foreach (var row in topOne)
                Assert.Equal(Quantities(models.Single(m => m.Number == row.Number)).Take(1), row.Lines);
            var second = repo.Query<ParityModel>().Select(x => new { x.Number, Lines = x.Lines!.OrderBy(l => l.Quantity).Skip(1).Take(1).Select(l => l.Quantity).ToList() }).ToArray();
            foreach (var row in second)
                Assert.Equal(Quantities(models.Single(m => m.Number == row.Number)).Skip(1).Take(1), row.Lines);
            var afterFirst = repo.Query<ParityModel>().Select(x => new { x.Number, Lines = x.Lines!.OrderByDescending(l => l.Quantity).Skip(1).Select(l => l.Quantity).ToArray() }).ToArray();
            foreach (var row in afterFirst)
                Assert.Equal(Quantities(models.Single(m => m.Number == row.Number)).Reverse().Skip(1), row.Lines);
            var filtered = repo.Query<ParityModel>().Select(x => new { x.Number, Lines = x.Lines!.Where(l => l.Quantity > 1).OrderBy(l => l.Quantity).Take(1).ToArray() }).ToArray();
            foreach (var row in filtered)
                Assert.Equal(Quantities(models.Single(m => m.Number == row.Number)).Where(y => y > 1).Take(1), row.Lines.Select(l => l.Quantity));
            var anyOne = repo.Query<ParityModel>().Select(x => new { x.Number, Lines = x.Lines!.Take(1).ToArray() }).ToArray();
            foreach (var row in anyOne)
                Assert.Equal(Math.Min(1, Quantities(models.Single(m => m.Number == row.Number)).Length), row.Lines.Length);
            var nested = repo.Query<ParityModel>().Select(x => new { x.Number, Lines = x.Lines!.OrderBy(l => l.Quantity).Take(1).Select(l => new { l.Quantity, Notes = l.Notes!.OrderBy(n => n.Rank).Take(1).Select(n => n.Rank).ToArray() }).ToArray() }).ToArray();
            foreach (var row in nested)
            {
                foreach (var line in row.Lines)
                {
                    var source = lines.Single(l => l.ParityID == models.Single(m => m.Number == row.Number).ID && l.Quantity == line.Quantity);
                    Assert.Equal(notes.Where(n => n.LineID == source.ID).Select(n => n.Rank).Order().Take(1), line.Notes);
                }
            }
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<ParityModel>().Select(x => new { Lines = x.Lines!.Take(1).Where(l => l.Quantity > 0).ToArray() }).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<ParityModel>().Select(x => new { Lines = x.Lines!.Take(2).Skip(1).ToArray() }).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<ParityModel>().Select(x => new { Lines = x.Lines!.Select(l => l.Quantity).Take(1).ToArray() }).ToArray());


            //a condition and an order after a Select are written back onto the model
            var q2 = repo.Query<ParityModel>().OrderBy(x => x.Number);
            Assert.Equal(ordered.Where(x => x.Number * 2 > 2).Select(x => x.Number * 2), q2.Select(x => new { Twice = x.Number * 2 }).Where(r => r.Twice > 2).ToArray().Select(r => r.Twice));
            Assert.Equal(ordered.Where(x => x.Number > 1).Select(x => x.Number), q2.Select(x => x.Number).Where(n => n > 1).ToArray());
            Assert.Equal(ordered.OrderByDescending(x => x.Number).Select(x => x.Number), repo.Query<ParityModel>().Select(x => new { x.Number, x.Name }).OrderByDescending(r => r.Number).ToArray().Select(r => r.Number));
            Assert.Equal(ordered.OrderByDescending(x => x.Flag).ThenBy(x => x.Number).Select(x => x.Number), repo.Query<ParityModel>().Select(x => new { x.Number, x.Flag }).OrderByDescending(r => r.Flag).ThenBy(r => r.Number).ToArray().Select(r => r.Number));
            Assert.Equal(ordered.Where(x => x.Number == 2).Select(x => x.Number), q2.Select(x => new { Inner = new { x.Number } }).Where(r => r.Inner.Number == 2).ToArray().Select(r => r.Inner.Number));
            Assert.Equal(ordered.Where(x => x.Number > 0).Skip(1).Take(1).Select(x => x.Number), q2.Select(x => new { x.Number }).Where(r => r.Number > 0).Skip(1).Take(1).ToArray().Select(r => r.Number));
            Assert.Equal(ordered.Count(x => x.Number > 1), q2.Select(x => new { x.Number }).Count(r => r.Number > 1));
            Assert.Equal(ordered.First(x => x.Number > 1).Number, q2.Select(x => new { x.Number }).First(r => r.Number > 1).Number);
            Assert.Equal(ordered.Where(x => x.Name is not null && x.Name.Length > 0).Select(x => x.Name), q2.Select(x => x.Name).Where(n => n != null && n.Length > 0).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => q2.Select(x => new ParitySelectResult { Number = x.Number }).Where(r => r.LineCount > 0).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => q2.Select(x => new { x.Number }).Where(r => r.ToString() == "a").ToArray());
            _ = Assert.Throws<NotSupportedException>(() => q2.Select(x => new { x.Number, Rows = x.Lines!.Select(l => l.Quantity).ToArray() }).Where(r => r.Rows.Length > 0).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => q2.Select(x => new { x.Number }).Take(2).Where(r => r.Number > 0).ToArray());

            //a condition looking in another query
            var bigLines = repo.Query<ParityLineModel>().Where(l => l.Quantity > 2).Select(l => l.ParityID);
            var withBig = lines.Where(l => l.Quantity > 2).Select(l => l.ParityID).ToHashSet();
            Assert.Equal(ordered.Where(x => withBig.Contains(x.ID)).Select(x => x.Number), repo.Query<ParityModel>().Where(x => bigLines.Contains(x.ID)).OrderBy(x => x.Number).ToArray().Select(x => x.Number));
            Assert.Equal(ordered.Where(x => !withBig.Contains(x.ID)).Select(x => x.Number), repo.Query<ParityModel>().Where(x => !bigLines.Contains(x.ID)).OrderBy(x => x.Number).ToArray().Select(x => x.Number));
            Assert.Equal(ordered.Count(x => withBig.Contains(x.ID)), repo.Query<ParityModel>().Count(x => bigLines.Contains(x.ID)));
            Assert.Equal(ordered.Where(x => withBig.Contains(x.ID) && x.Number > 1).Select(x => x.Number), repo.Query<ParityModel>().Where(x => x.Number > 1 && repo.Query<ParityLineModel>().Where(l => l.Quantity > 2).Select(l => l.ParityID).Contains(x.ID)).OrderBy(x => x.Number).ToArray().Select(x => x.Number));
            var firstTwo = lines.OrderBy(l => l.Quantity).ThenBy(l => l.Price).Take(2).Select(l => l.ID).ToHashSet();
            var pagedLines = repo.Query<ParityLineModel>().OrderBy(l => l.Quantity).ThenBy(l => l.Price).Take(2).Select(l => l.ID);
            Assert.Equal(lines.Count(l => firstTwo.Contains(l.ID)), repo.Query<ParityLineModel>().Count(l => pagedLines.Contains(l.ID)));
            Assert.Equal(ordered.Length, repo.Query<ParityModel>().Count(x => repo.Query<ParityModel>().Select(y => y.ID).Distinct().Contains(x.ID)));
            var nullableNames = repo.Query<ParityModel>().Select(x => x.Name);
            Assert.Equal(ordered.Count(x => x.Name is not null), repo.Query<ParityModel>().Count(x => nullableNames.Contains(x.Name)));
            Assert.Equal(ordered.Count(x => x.Name is not null && models.All(y => y.Name != x.Name)), repo.Query<ParityModel>().Where(x => x.Name != null).Count(x => !nullableNames.Contains(x.Name)));
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<ParityModel>().Where(x => repo.Query<ParityLineModel>().Where(l => l.ParityID == x.ID).Select(l => l.ParityID).Contains(x.ID)).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<ParityModel>().Where(x => repo.Query<ParityLineModel>().Select(l => new { l.ParityID }).Contains(new { ParityID = x.ID })).ToArray());

            //the whole key of a group
            var byBoth = repo.Query<ParityModel>().GroupBy(x => new { x.Flag, Even = x.Number % 2 == 0 }).Select(g => new { g.Key, Rows = g.Count() }).ToArray();
            var expectedBoth = models.GroupBy(x => new { x.Flag, Even = x.Number % 2 == 0 }).ToArray();
            Assert.Equal(expectedBoth.Length, byBoth.Length);
            foreach (var group in expectedBoth)
                Assert.Equal(group.Count(), byBoth.Single(r => r.Key.Flag == group.Key.Flag && r.Key.Even == group.Key.Even).Rows);
            Assert.Equal(expectedBoth.Length, repo.Query<ParityModel>().GroupBy(x => new { x.Flag, Even = x.Number % 2 == 0 }).Select(g => g.Key).ToArray().Length);
            var keyOrdered = repo.Query<ParityModel>().GroupBy(x => new { x.Flag, x.Number }).Select(g => new { g.Key, Rows = g.Count() }).Where(r => r.Key.Number > 0).OrderByDescending(r => r.Key.Number).ToArray();
            Assert.Equal(ordered.Where(x => x.Number > 0).OrderByDescending(x => x.Number).Select(x => x.Number), keyOrdered.Select(r => r.Key.Number));
            var byTuple = repo.Query<ParityModel>().GroupBy(x => new ValueTuple<bool, int>(x.Flag, x.Number)).Select(g => new { g.Key, Rows = g.Count() }).ToArray();
            Assert.Equal(ordered.Select(x => x.Number), byTuple.Select(r => r.Key.Item2).Order());

            //what follows a SelectMany is a query of the related model
            var rows = repo.Query<ParityModel>().SelectMany(x => x.Lines!);
            Assert.Equal(lines.Length, rows.Count());
            Assert.Equal(lines.Sum(l => l.Quantity), rows.Sum(l => l.Quantity));
            Assert.Equal(lines.Where(l => l.Quantity > 2).Select(l => l.ID).Order(), rows.Where(l => l.Quantity > 2).ToArray().Select(l => l.ID).Order());
            Assert.Equal(lines.OrderByDescending(l => l.Quantity).ThenBy(l => l.Price).Select(l => l.Quantity), rows.OrderByDescending(l => l.Quantity).ThenBy(l => l.Price).Select(l => l.Quantity).ToArray());
            Assert.Equal(lines.OrderBy(l => l.Quantity).ThenBy(l => l.Price).Skip(1).Take(2).Select(l => l.Price), rows.OrderBy(l => l.Quantity).ThenBy(l => l.Price).Skip(1).Take(2).Select(l => l.Price).ToArray());
            Assert.Equal(lines.Max(l => l.Quantity), rows.OrderBy(l => l.Quantity).Last().Quantity);
            Assert.Equal(lines.Select(l => l.Quantity).Distinct().Order(), rows.Select(l => l.Quantity).Distinct().ToArray().Order());
            Assert.True(rows.Any(l => l.Quantity > 2));
            Assert.Equal(lines.Where(l => l.Quantity > 2).Sum(l => l.Quantity), repo.Query<ParityModel>().SelectMany(x => x.Lines!.Where(l => l.Quantity > 2)).Sum(l => l.Quantity));
            var perQuantity = rows.GroupBy(l => l.Quantity).Select(g => new { g.Key, Rows = g.Count() }).ToArray();
            Assert.Equal(lines.GroupBy(l => l.Quantity).Count(), perQuantity.Length);
            var overTwo = ordered.Where(x => x.Number > 2).Select(x => x.ID).ToHashSet();
            Assert.Equal(lines.Count(l => overTwo.Contains(l.ParityID)), repo.Query<ParityModel>().Where(x => x.Number > 2).SelectMany(x => x.Lines!).Count());
            var topParent = ordered.First(x => lines.Any(l => l.ParityID == x.ID));
            Assert.Equal(lines.Count(l => l.ParityID == topParent.ID), repo.Query<ParityModel>().Where(x => x.Lines!.Any()).OrderBy(x => x.Number).Take(1).SelectMany(x => x.Lines!).Count());
            var withParent = repo.Query<ParityModel>().SelectMany(x => x.Lines!, (x, l) => new { x.Number, l.Quantity }).ToArray();
            Assert.Equal(lines.Length, withParent.Length);
            foreach (var pair in withParent)
                Assert.Contains(lines, l => l.Quantity == pair.Quantity && l.ParityID == models.Single(m => m.Number == pair.Number).ID);
            Assert.Equal(lines.Select(l => l.Quantity).Order(), repo.Query<ParityModel>().SelectMany(x => x.Lines!, (x, l) => l.Quantity).ToArray().Order());
            var filteredPairs = repo.Query<ParityModel>().SelectMany(x => x.Lines!, (x, l) => new { x.Number, l.Quantity }).Where(r => r.Number > 2).OrderBy(r => r.Quantity).ToArray();
            Assert.Equal(lines.Where(l => models.Single(m => m.ID == l.ParityID).Number > 2).Select(l => l.Quantity).Order(), filteredPairs.Select(r => r.Quantity));
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<ParityModel>().SelectMany(x => x.Lines!.Take(1)).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<ParityModel>().Select(x => new { x.Number, x.Lines }).SelectMany(r => r.Lines!).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => rows.SelectMany(l => l.Notes!).ToArray());


            //one related row reads with the row in a join
            var linesWithParent = repo.Query<ParityLineModel>().Select(x => new { x.ID, x.Quantity, x.Parent }).ToArray();
            Assert.Equal(lines.Length, linesWithParent.Length);
            foreach (var row in linesWithParent)
            {
                var source = lines.Single(l => l.ID == row.ID);
                var parent = models.Single(m => m.ID == source.ParityID);
                Assert.Equal(source.Quantity, row.Quantity);
                Assert.Equal(parent.ID, row.Parent!.ID);
                Assert.Equal(parent.Number, row.Parent.Number);
                Assert.Equal(parent.Flag, row.Parent.Flag);
                Assert.Equal(parent.Money, row.Parent.Money);
                Assert.Equal(parent.Name, row.Parent.Name);
                Assert.Equal(parent.Date, row.Parent.Date);
                Assert.Null(row.Parent.Lines);
            }
            var partialParent = repo.Query<ParityLineModel>(new Graph<ParityLineModel>(x => x.Quantity, x => x.Parent!.Number)).OrderBy(x => x.Quantity).ThenBy(x => x.Price).ToArray();
            Assert.Equal(lines.OrderBy(l => l.Quantity).ThenBy(l => l.Price).Select(l => models.Single(m => m.ID == l.ParityID).Number), partialParent.Select(x => x.Parent!.Number));
            Assert.All(partialParent, x => Assert.Null(x.Parent!.Name));
            var pagedParent = repo.Query<ParityLineModel>(new Graph<ParityLineModel>(true, x => x.Parent)).Where(x => x.Parent!.Number > 1).OrderBy(x => x.Quantity).ThenBy(x => x.Price).Skip(1).Take(2).ToArray();
            Assert.Equal(lines.Where(l => models.Single(m => m.ID == l.ParityID).Number > 1).OrderBy(l => l.Quantity).ThenBy(l => l.Price).Skip(1).Take(2).Select(l => l.ID), pagedParent.Select(x => x.ID));
            Assert.All(pagedParent, x => Assert.Equal(x.ParityID, x.Parent!.ID));
            Assert.Equal(lines.Select(l => l.ParityID).Distinct().Order(), repo.Query<ParityLineModel>().Select(x => new { x.Parent }).Distinct().ToArray().Select(r => r.Parent!.ID).Order());
            //rows that read one related row themselves
            var noteRows = repo.Query<ParityNoteModel>().Select(x => new { x.ID, x.Line }).ToArray();
            Assert.Equal(notes.Length, noteRows.Length);
            Assert.All(noteRows, r => Assert.Equal(notes.Single(n => n.ID == r.ID).LineID, r.Line!.ID));
            var deepGraph = repo.Query<ParityNoteModel>(new Graph<ParityNoteModel>(x => x.ID, x => x.Line!.Quantity, x => x.Line!.Parent!.Number)).ToArray();
            Assert.Equal(notes.Length, deepGraph.Length);
            foreach (var note in deepGraph)
            {
                var line = lines.Single(l => l.ID == notes.Single(n => n.ID == note.ID).LineID);
                Assert.Equal(line.Quantity, note.Line!.Quantity);
                Assert.Equal(models.Single(m => m.ID == line.ParityID).Number, note.Line.Parent!.Number);
            }
            var lineRows = repo.Query<ParityModel>().Where(x => x.Lines!.Any()).Select(x => new { x.Number, Rows = x.Lines!.Select(l => new { l.Quantity, l.Parent }).ToArray() }).ToArray();
            Assert.All(lineRows, r => Assert.All(r.Rows, l => Assert.Equal(r.Number, l.Parent!.Number)));
            //a row with nothing to join keeps its place, with no related model
            var reviewed = new ParityNoteModel { ID = Guid.NewGuid(), LineID = lines[0].ID, Rank = 50, Text = "r", ReviewerID = ordered[1].ID };
            repo.Create<ParityNoteModel>(reviewed);
            try
            {
                var reviews = repo.Query<ParityNoteModel>().OrderBy(x => x.Rank).ThenBy(x => x.ID).Select(x => new { x.ID, x.Reviewer, x.Text, x.Rank }).ToArray();
                Assert.Equal(notes.Length + 1, reviews.Length);
                foreach (var row in reviews)
                {
                    if (row.ID == reviewed.ID)
                    {
                        Assert.Equal(ordered[1].Number, row.Reviewer!.Number);
                        Assert.Equal("r", row.Text);
                        Assert.Equal(50, row.Rank);
                    }
                    else
                    {
                        Assert.Null(row.Reviewer);
                        Assert.Equal(notes.Single(n => n.ID == row.ID).Text, row.Text);
                        Assert.Equal(notes.Single(n => n.ID == row.ID).Rank, row.Rank);
                    }
                }
                Assert.Equal(1, repo.Query<ParityNoteModel>().Count(x => x.Reviewer!.Number == ordered[1].Number));
                Assert.Equal(notes.Length + 1, repo.Query<ParityNoteModel>().Count(x => x.Reviewer!.Number != -1 || x.Rank >= 0));
                var reviewGraph = repo.Query<ParityNoteModel>(new Graph<ParityNoteModel>(true, x => x.Reviewer)).ToArray();
                Assert.Equal(ordered[1].Number, reviewGraph.Single(x => x.ID == reviewed.ID).Reviewer!.Number);
                Assert.All(reviewGraph.Where(x => x.ID != reviewed.ID), x => Assert.Null(x.Reviewer));
                Assert.Equal(notes.Single(n => n.ID == notes[0].ID).Text, reviewGraph.Single(x => x.ID == notes[0].ID).Text);
            }
            finally
            {
                repo.Delete<ParityNoteModel>(reviewed);
            }

            //a model related to another more than one way, each way being its own row
            var both = new ParityNoteModel { ID = Guid.NewGuid(), LineID = lines[0].ID, Rank = 60, Text = "b", ReviewerID = ordered[1].ID, SecondReviewerID = ordered[2].ID };
            repo.Create<ParityNoteModel>(both);
            try
            {
                var members = repo.Query<ParityNoteModel>().Where(x => x.ID == both.ID).Select(x => new { First = x.Reviewer!.Number, Second = x.SecondReviewer!.Number }).Single();
                Assert.Equal(ordered[1].Number, members.First);
                Assert.Equal(ordered[2].Number, members.Second);
                var wholes = repo.Query<ParityNoteModel>().Where(x => x.ID == both.ID).Select(x => new { x.Reviewer, x.SecondReviewer }).Single();
                Assert.Equal(ordered[1].Number, wholes.Reviewer!.Number);
                Assert.Equal(ordered[2].Number, wholes.SecondReviewer!.Number);
                var graphBoth = repo.Query<ParityNoteModel>(new Graph<ParityNoteModel>(true, x => x.Reviewer, x => x.SecondReviewer)).Single(x => x.ID == both.ID);
                Assert.Equal(ordered[1].Number, graphBoth.Reviewer!.Number);
                Assert.Equal(ordered[2].Number, graphBoth.SecondReviewer!.Number);
                var firstNumber = ordered[1].Number;
                var secondNumber = ordered[2].Number;
                Assert.Equal(1, repo.Query<ParityNoteModel>().Count(x => x.Reviewer!.Number == firstNumber && x.SecondReviewer!.Number == secondNumber));
                Assert.Equal(0, repo.Query<ParityNoteModel>().Count(x => x.Reviewer!.Number == secondNumber && x.SecondReviewer!.Number == firstNumber));
                Assert.Equal(both.ID, repo.Query<ParityNoteModel>().Where(x => x.SecondReviewer!.Number == secondNumber).OrderBy(x => x.Reviewer!.Number).ThenBy(x => x.SecondReviewer!.Number).First().ID);
                Assert.Equal(1, repo.Update<ParityNoteModel>(x => x.Reviewer!.Number == firstNumber && x.SecondReviewer!.Number == secondNumber, new ParityNoteModel { Text = "c" }, new Graph<ParityNoteModel>(x => x.Text)));
                Assert.Equal("c", repo.Query<ParityNoteModel>().Single(x => x.ID == both.ID).Text);
                Assert.Equal(1, repo.Delete<ParityNoteModel>(x => x.Reviewer!.Number == firstNumber && x.SecondReviewer!.Number == secondNumber));
                both = null!;
            }
            finally
            {
                if (both is not null)
                    repo.Delete<ParityNoteModel>(both);
            }

            //a model related to itself
            var selfOf = ordered[0];
            var pointing = ordered[1];
            pointing.SelfID = selfOf.ID;
            repo.Update(pointing, new Graph<ParityModel>(x => x.SelfID));
            try
            {
                var selfMembers = repo.Query<ParityModel>().Where(x => x.ID == pointing.ID).Select(x => new { x.Number, Other = x.Self!.Number }).Single();
                Assert.Equal(pointing.Number, selfMembers.Number);
                Assert.Equal(selfOf.Number, selfMembers.Other);
                var selfWhole = repo.Query<ParityModel>().Where(x => x.ID == pointing.ID).Select(x => new { x.Number, x.Self }).Single();
                Assert.Equal(selfOf.Number, selfWhole.Self!.Number);
                var selfGraph = repo.Query<ParityModel>(new Graph<ParityModel>(true, x => x.Self)).Single(x => x.ID == pointing.ID);
                Assert.Equal(pointing.Number, selfGraph.Number);
                Assert.Equal(selfOf.Number, selfGraph.Self!.Number);
                var selfOwn = selfOf.Number;
                Assert.Equal(pointing.Number, repo.Query<ParityModel>().Where(x => x.Self!.Number == selfOwn).Select(x => x.Number).Single());
                Assert.Null(repo.Query<ParityModel>().Where(x => x.ID == selfOf.ID).Select(x => new { x.Self }).Single().Self);
                Assert.Equal(lines.Any(l => l.ParityID == selfOf.ID) ? 1 : 0, repo.Query<ParityModel>().Count(x => x.Self!.Lines!.Any()));
                Assert.Equal(lines.Count(l => l.ParityID == selfOf.ID), repo.Query<ParityModel>().Where(x => x.ID == pointing.ID).Select(x => x.Self!.Lines!.Count()).Single());

                //a related collection through the relation is that row's, not the row of the statement
                var hasLines = ordered.First(m => lines.Any(l => l.ParityID == m.ID));
                var noLines = ordered.First(m => m.ID != pointing.ID && lines.All(l => l.ParityID != m.ID));
                noLines.SelfID = hasLines.ID;
                repo.Update(noLines, new Graph<ParityModel>(x => x.SelfID));
                try
                {
                    var linesOf = lines.Where(l => l.ParityID == hasLines.ID).ToArray();
                    var noLinesID = noLines.ID;
                    Assert.Equal(linesOf.Length, repo.Query<ParityModel>().Where(x => x.ID == noLinesID).Select(x => x.Self!.Lines!.Count()).Single());
                    Assert.Equal(linesOf.Count(l => l.Quantity > 1), repo.Query<ParityModel>().Where(x => x.ID == noLinesID).Select(x => x.Self!.Lines!.Count(l => l.Quantity > 1)).Single());
                    Assert.Equal(linesOf.Sum(l => l.Quantity), repo.Query<ParityModel>().Where(x => x.ID == noLinesID).Select(x => x.Self!.Lines!.Sum(l => l.Quantity)).Single());
                    Assert.Equal(linesOf.Min(l => l.Quantity), repo.Query<ParityModel>().Where(x => x.ID == noLinesID).Select(x => x.Self!.Lines!.OrderBy(l => l.Quantity).First().Quantity).Single());
                    Assert.True(repo.Query<ParityModel>().Any(x => x.ID == noLinesID && x.Self!.Lines!.Any()));
                    Assert.True(repo.Query<ParityModel>().Any(x => x.ID == noLinesID && x.Self!.Lines!.Any(l => l.Quantity > 0)));
                    Assert.False(repo.Query<ParityModel>().Any(x => x.ID == pointing.ID && x.Self!.Lines!.Any(l => l.Quantity > 0)));
                }
                finally
                {
                    noLines.SelfID = null;
                    repo.Update(noLines, new Graph<ParityModel>(x => x.SelfID));
                }
            }
            finally
            {
                pointing.SelfID = null;
                repo.Update(pointing, new Graph<ParityModel>(x => x.SelfID));
            }

            //an order by an aggregate of a relation
            Assert.Equal(Numbers(ordered.OrderByDescending(m => lines.Count(l => l.ParityID == m.ID)).ThenBy(m => m.Number)), Numbers(repo.Query<ParityModel>().OrderByDescending(x => x.Lines!.Count()).ThenBy(x => x.Number).ToArray()));
            Assert.Equal(Numbers(ordered.OrderBy(m => lines.Where(l => l.ParityID == m.ID).Sum(l => l.Quantity)).ThenBy(m => m.Number)), Numbers(repo.Query<ParityModel>().OrderBy(x => x.Lines!.Sum(l => l.Quantity)).ThenBy(x => x.Number).ToArray()));
        }

        private static async Task AssertShapesAsync(IRepo repo, ParityModel[] models, ParityLineModel[] lines)
        {
            var ordered = models.OrderBy(x => x.Number).ToArray();
            var q = repo.Query<ParityModel>().OrderBy(x => x.Number);

            //enums and a joined text longer than the limit MySQL has by default
            Assert.Equal(ordered.Where(x => x.Kind == ParityKind.Small).Select(x => x.Kind), await q.Where(x => x.Kind == ParityKind.Small).Select(x => x.Kind).ToArrayAsync());
            Assert.Equal(models.Max(x => x.Kind), await repo.Query<ParityModel>().MaxAsync(x => x.Kind));
            var longJoined = await repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Names = String.Join(",", g.Select(x => x.Title!.PadRight(800, '.'))) }).ToArrayAsync();
            foreach (var group in longJoined)
                Assert.Equal(models.Count(x => x.Flag == group.Key) * 801 - 1, group.Names.Length);
            Assert.Equal(1, await repo.UpdateAsync<ParityModel>(x => x.Number == 1 && x.Kind == models[1].Kind, x => new ParityModel { Kind = x.Kind }));
            Assert.Equal(ordered.Select(x => Convert.ToInt32(x.Number + 0.5)), await q.Select(x => Convert.ToInt32(x.Number + 0.5)).ToArrayAsync());

            Assert.Equal(2, await q.Skip(1).Take(2).CountAsync());
            Assert.Equal(ordered.Skip(1).Take(2).Sum(x => x.Number), await q.Skip(1).Take(2).SumAsync(x => x.Number));
            Assert.True(await q.Skip(1).AnyAsync());

            Assert.Equal(ordered[^1].Number, (await q.LastAsync()).Number);
            var highest = ordered[^1].Number;
            Assert.Equal(ordered[^2].Number, (await q.LastOrDefaultAsync(x => x.Number < highest))!.Number);
            Assert.Null(await q.LastOrDefaultAsync(x => x.Number < -1000));
            Assert.Equal(ordered[1].Number, (await q.ElementAtAsync(1)).Number);
            Assert.Null(await q.ElementAtOrDefaultAsync(ordered.Length));
            _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => q.ElementAtAsync(ordered.Length));
            Assert.True(await repo.Query<ParityModel>().Select(x => x.Number).ContainsAsync(ordered[0].Number));
            Assert.False(await repo.Query<ParityModel>().Select(x => x.Number).ContainsAsync(-1000));

            Assert.Equal(lines.Select(l => l.ID).Order(), (await repo.Query<ParityModel>().SelectMany(x => x.Lines!).ToArrayAsync()).Select(l => l.ID).Order());
            Assert.Equal(lines.Length, await repo.Query<ParityModel>().SelectMany(x => x.Lines!).CountAsync());
            Assert.Equal(lines.Where(l => l.Quantity > 2).Select(l => l.ID).Order(), (await repo.Query<ParityModel>().SelectMany(x => x.Lines!).Where(l => l.Quantity > 2).ToArrayAsync()).Select(l => l.ID).Order());
            var bigLines = repo.Query<ParityLineModel>().Where(l => l.Quantity > 2).Select(l => l.ParityID);
            Assert.Equal(models.Count(x => lines.Any(l => l.Quantity > 2 && l.ParityID == x.ID)), await repo.Query<ParityModel>().CountAsync(x => bigLines.Contains(x.ID)));
            Assert.Equal(ordered.Where(x => x.Number > 1).Select(x => x.Number), await q.Select(x => x.Number).Where(n => n > 1).ToArrayAsync());
            Assert.Equal(models.Length, (await repo.Query<ParityModel>().GroupBy(x => new { x.Flag, x.Number }).Select(g => new { g.Key, Rows = g.Count() }).ToArrayAsync()).Length);

            var set = await repo.Query<ParityModel>().Select(x => x.Number).ToHashSetAsync();
            Assert.Equal(models.Select(x => x.Number).ToHashSet(), set);
            Assert.Empty(await repo.Query<ParityModel>().Where(x => x.Name == "not there").Select(x => x.Name!).ToHashSetAsync(StringComparer.OrdinalIgnoreCase));
            var byID = await repo.Query<ParityModel>().ToDictionaryAsync(x => x.ID);
            Assert.Equal(models.Length, byID.Count);
            Assert.Equal(models[0].Number, byID[models[0].ID].Number);
            var numberByID = await repo.Query<ParityModel>().ToDictionaryAsync(x => x.ID, x => x.Number);
            Assert.Equal(models[0].Number, numberByID[models[0].ID]);
            var byName = await repo.Query<ParityModel>().Where(x => x.Name != null).ToDictionaryAsync(x => x.ID.ToString(), StringComparer.OrdinalIgnoreCase);
            Assert.True(byName.ContainsKey(models.First(x => x.Name is not null).ID.ToString().ToUpperInvariant()));
        }

        //a graph reads members and relations of the models, the query still running in the store
        private static void AssertGraph(IRepo repo, ParityModel[] models, ParityLineModel[] lines, ParityNoteModel[] notes)
        {
            ParityLineModel[] LinesOf(int number) => lines.Where(l => l.ParityID == models.Single(m => m.Number == number).ID).ToArray();
            void SameLines(ParityModel model)
            {
                var expected = LinesOf(model.Number);
                Assert.Equal(expected.Select(l => l.ID).Order(), model.Lines!.Select(l => l.ID).Order());
                foreach (var line in model.Lines!)
                {
                    var source = expected.Single(l => l.ID == line.ID);
                    Assert.Equal(source.Quantity, line.Quantity);
                    Assert.Equal(source.Price, line.Price);
                    Assert.Null(line.Parent);
                    Assert.Null(line.Notes);
                }
            }

            var wholeGraph = new Graph<ParityModel>(true, x => x.Lines);
            var whole = repo.Query<ParityModel>(wholeGraph).OrderBy(x => x.Number).ToArray();
            Assert.Equal(models.Select(x => x.Number).Order(), whole.Select(x => x.Number));
            foreach (var model in whole)
            {
                Assert.Equal(models.Single(m => m.Number == model.Number).Name, model.Name);
                SameLines(model);
            }

            var paged = repo.Query<ParityModel>(wholeGraph).Where(x => x.Number > 0).OrderByDescending(x => x.Number).Skip(1).Take(2).ToArray();
            Assert.Equal(models.Where(x => x.Number > 0).OrderByDescending(x => x.Number).Skip(1).Take(2).Select(x => x.Number), paged.Select(x => x.Number));
            foreach (var model in paged)
                SameLines(model);

            SameLines(repo.Query<ParityModel>(wholeGraph).OrderBy(x => x.Number).First(x => x.Number == 2));
            SameLines(repo.Query<ParityModel>(wholeGraph).Single(x => x.Number == 3));

            //no relation unless the graph names it, even with every member
            Assert.All(repo.Query<ParityModel>(new Graph<ParityModel>(true)).ToArray(), x => Assert.Null(x.Lines));

            var partial = repo.Query<ParityModel>(new Graph<ParityModel>(x => x.Number)).Single(x => x.Number == 1);
            Assert.Equal(1, partial.Number);
            Assert.Null(partial.Name);
            Assert.Null(partial.Lines);

            var childGraph = new Graph<ParityModel>(x => x.Number, x => x.Lines!.Select(l => l.Quantity));
            var signature = childGraph.Signature;
            var child = repo.Query<ParityModel>(childGraph).Single(x => x.Number == 1);
            Assert.Equal(LinesOf(1).Select(l => l.Quantity).Order(), child.Lines!.Select(l => l.Quantity).Order());
            Assert.All(child.Lines!, l => Assert.Equal(0m, l.Price));
            Assert.Equal(signature, childGraph.Signature);

            var nested = repo.Query<ParityModel>(new Graph<ParityModel>(x => x.Number, x => x.Lines!.Select(l => l.ID), x => x.Lines!.Select(l => l.Notes))).Single(x => x.Number == 2);
            Assert.Equal(LinesOf(2).Select(l => l.ID).Order(), nested.Lines!.Select(l => l.ID).Order());
            foreach (var line in nested.Lines!)
                Assert.Equal(notes.Where(n => n.LineID == line.ID).Select(n => n.ID).Order(), line.Notes!.Select(n => n.ID).Order());

            var withParent = repo.Query<ParityLineModel>(new Graph<ParityLineModel>(true, x => x.Parent)).ToArray();
            Assert.Equal(lines.Length, withParent.Length);
            foreach (var line in withParent)
                Assert.Equal(models.Single(m => m.ID == line.ParityID).Number, line.Parent!.Number);

            //the aggregates read what they read
            Assert.Equal(models.Length, repo.Query<ParityModel>(wholeGraph).Count());
            Assert.True(repo.Query<ParityModel>(wholeGraph).Any());
            Assert.Equal(models.Sum(x => x.Number), repo.Query<ParityModel>(wholeGraph).Sum(x => x.Number));

            _ = Assert.Throws<NotSupportedException>(() => repo.Query<ParityModel>(wholeGraph).Select(x => new { x.Number }).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => repo.Query<ParityModel>(wholeGraph).GroupBy(x => x.Flag).Select(g => new { g.Key, Rows = g.Count() }).ToArray());
        }

        private static async Task AssertGraphAsync(IRepo repo, ParityModel[] models, ParityLineModel[] lines)
        {
            var graph = new Graph<ParityModel>(true, x => x.Lines);
            var all = await repo.Query<ParityModel>(graph).OrderBy(x => x.Number).ToArrayAsync();
            Assert.Equal(models.Select(x => x.Number).Order(), all.Select(x => x.Number));
            Assert.Equal(lines.Select(l => l.ID).Order(), all.SelectMany(x => x.Lines!).Select(l => l.ID).Order());

            var first = await repo.Query<ParityModel>(graph).FirstOrDefaultAsync(x => x.Number == 1);
            Assert.Equal(lines.Where(l => l.ParityID == first!.ID).Select(l => l.ID).Order(), first!.Lines!.Select(l => l.ID).Order());

            var list = await repo.Query<ParityLineModel>(new Graph<ParityLineModel>(true, x => x.Parent)).ToListAsync();
            Assert.All(list, l => Assert.Equal(l.ParityID, l.Parent!.ID));
        }

        public static void TestSequence(ITransactStoreEngine engine)
        {
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<ParityModel>(engine));
            repo.AddProvider(new TransactStoreProvider<ParityLineModel>(engine));
            repo.AddProvider(new TransactStoreProvider<ParityNoteModel>(engine));
            if (engine is not MemoryEngine)
                repo.AddProvider(new TransactStoreProvider<ParityTextModel>(engine));
            var models = Models();
            foreach (var model in models)
                repo.Create(model);
            var lines = Lines(models);
            repo.Create<ParityLineModel>(lines);
            var notes = Notes(lines);
            repo.Create<ParityNoteModel>(notes);
            try
            {
                foreach (var (name, where) in Wheres())
                {
                    var compiled = where.Compile();
                    var expected = models.Where(compiled).Select(x => x.Number).Order().ToArray();
                    var actual = repo.Query<ParityModel>().Where(where).ToArray().Select(x => x.Number).Order().ToArray();
                    Assert.True(expected.SequenceEqual(actual), $"{name}: expected [{String.Join(",", expected)}] found [{String.Join(",", actual)}]");
                    Assert.True(expected.Length == repo.Query<ParityModel>().Count(where), $"{name} count");
                    Assert.True((expected.Length > 0) == repo.Query<ParityModel>().Any(where), $"{name} any");
                }

                AssertAggregates(repo, models);
                AssertAggregatesOfRelations(repo, models, lines);
                AssertProjections(repo, models, lines, notes);
                AssertRelationElements(repo, models, lines);
                AssertQueryable(repo, models, lines);
                AssertGraph(repo, models, lines, notes);
                AssertShapes(repo, models, lines, notes);
                AssertFunctions(repo, models, lines, engine is not MemoryEngine);
                AssertEnums(repo, models, engine is not MemoryEngine);
                AssertLookIns(repo, models, lines);
                AssertSets(repo, models, lines, engine is not MemoryEngine);
                AssertJoins(repo, models, lines, notes, engine is not MemoryEngine);
                AssertOperators(repo, models, lines);
                AssertPositions(repo, models, lines);

                //a member with a store name reads back, an excluded member isn't stored
                var stored = repo.Query<ParityModel>().SingleOrDefault(x => x.Number == 1)!;
                Assert.Equal("t1", stored.Title);
                Assert.Equal(0, stored.NotStored);

                var expectedOrder = models.OrderByDescending(x => x.Flag).ThenBy(x => x.Number).Skip(1).Take(3).Select(x => x.Number).ToArray();
                Assert.Equal(expectedOrder, repo.Query<ParityModel>().OrderByDescending(x => x.Flag).ThenBy(x => x.Number).Skip(1).Take(3).ToArray().Select(x => x.Number).ToArray());
                Assert.Equal(expectedOrder[0], repo.Query<ParityModel>().Where(x => x.Number > 0).OrderByDescending(x => x.Flag).ThenBy(x => x.Number).First().Number);
            }
            finally
            {
                repo.Delete<ParityNoteModel>(notes);
                repo.Delete<ParityLineModel>(lines);
                foreach (var model in models)
                    repo.Delete(model);
            }
        }

        //the key and the value reach through relations: a join for a single related model and a subquery for a collection
        private static void AssertAggregatesOfRelations(IRepo repo, ParityModel[] models, ParityLineModel[] lines)
        {
            var parents = models.ToDictionary(x => x.ID);
            int LineQuantities(ParityModel model) => lines.Where(l => l.ParityID == model.ID).Sum(l => l.Quantity);

            Assert.Equal(lines.Sum(l => l.Price * l.Quantity), repo.Query<ParityModel>().Sum(x => x.Lines!.Sum(l => l.Price * l.Quantity)));
            Assert.Equal(2, repo.Query<ParityModel>().Max(x => x.Lines!.Count()));
            AssertGroups("sum of lines by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Sum(LineQuantities))), repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Sum(x => x.Lines!.Sum(l => l.Quantity)) }).ToArray().Select(r => (r.Key, r.Value)).ToArray());

            Assert.Equal(lines.Sum(l => parents[l.ParityID].Number * l.Quantity), repo.Query<ParityLineModel>().Sum(x => x.Parent!.Number * x.Quantity));
            Assert.Equal(lines.Count(l => parents[l.ParityID].Flag), repo.Query<ParityLineModel>().Count(x => x.Parent!.Flag));
            AssertGroups("count of lines by parent name", lines.GroupBy(l => parents[l.ParityID].Name).Select(g => (g.Key, (long)g.Count())), repo.Query<ParityLineModel>().GroupBy(x => x.Parent!.Name).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
            AssertGroups("average price by parent flag", lines.GroupBy(l => parents[l.ParityID].Flag).Select(g => (g.Key, g.Average(l => l.Price))), repo.Query<ParityLineModel>().GroupBy(x => x.Parent!.Flag).Select(g => new { g.Key, Value = g.Average(x => x.Price) }).ToArray().Select(r => (r.Key, r.Value)).ToArray(), (a, b) => Math.Abs(a - b) < 0.000001m);
            AssertGroups("max quantity by parent number", lines.Where(l => parents[l.ParityID].Flag).GroupBy(l => parents[l.ParityID].Number).Select(g => (g.Key, g.Max(l => l.Quantity))), repo.Query<ParityLineModel>().Where(x => x.Parent!.Flag).GroupBy(x => x.Parent!.Number).Select(g => new { g.Key, Value = g.Max(x => x.Quantity) }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
        }

        //one row of a related collection after filtering, ordering, and paging it: a row that isn't there reads as null, in memory too
        private static void AssertRelationElements(IRepo repo, ParityModel[] models, ParityLineModel[] lines)
        {
            ParityLineModel[] LinesOf(ParityModel model) => lines.Where(l => l.ParityID == model.ID).ToArray();
            void Same(string name, Func<ParityModel, bool> expected, Expression<Func<ParityModel, bool>> where)
            {
                var expectedNumbers = models.Where(m => LinesOf(m).Length > 0 && expected(m)).Select(m => m.Number).Order().ToArray();
                var actual = repo.Query<ParityModel>().Where(where).ToArray().Select(x => x.Number).Order().ToArray();
                Assert.True(expectedNumbers.SequenceEqual(actual), $"{name}: expected [{String.Join(",", expectedNumbers)}] found [{String.Join(",", actual)}]");
            }

            Same("first ordered", m => LinesOf(m).OrderBy(l => l.Price).First().Quantity == 2, x => x.Lines!.OrderBy(l => l.Price).First().Quantity == 2);
            Same("average of rows", m => LinesOf(m).Average(l => l.Quantity) > 2, x => x.Lines!.Any() && x.Lines!.Average(l => l.Quantity) > 2);
            Same("sum of filtered rows", m => LinesOf(m).Where(l => l.Quantity > 1).Sum(l => l.Quantity) > 3, x => x.Lines!.Where(l => l.Quantity > 1).Sum(l => l.Quantity) > 3);
            Same("average of filtered rows", m => LinesOf(m).Any(l => l.Quantity > 1) && LinesOf(m).Where(l => l.Quantity > 1).Average(l => l.Quantity) >= 3, x => x.Lines!.Any(l => l.Quantity > 1) && x.Lines!.Where(l => l.Quantity > 1).Average(l => l.Quantity) >= 3);
            Same("any of filtered rows", m => LinesOf(m).Where(l => l.Quantity > 1).Any(l => l.Price < 2m), x => x.Lines!.Where(l => l.Quantity > 1).Any(l => l.Price < 2m));
            Same("count of filtered rows", m => LinesOf(m).Where(l => l.Quantity > 1).Count(l => l.Price < 3m) >= 1, x => x.Lines!.Where(l => l.Quantity > 1).Count(l => l.Price < 3m) >= 1);
            Assert.Equal(models.Where(m => LinesOf(m).Where(l => l.Quantity > 1).All(l => l.Price < 2m)).Select(m => m.Number).Order(), repo.Query<ParityModel>().Where(x => x.Lines!.Where(l => l.Quantity > 1).All(l => l.Price < 2m)).Select(x => x.Number).ToArray().Order());
            Same("last ordered", m => LinesOf(m).OrderBy(l => l.Quantity).Last().Price < 2m, x => x.Lines!.OrderBy(l => l.Quantity).Last().Price < 2m);
            Same("element at", m => LinesOf(m).OrderByDescending(l => l.Quantity).ElementAt(1).Quantity == 3, x => x.Lines!.OrderByDescending(l => l.Quantity).ElementAt(1).Quantity == 3);
            Same("filtered first", m => LinesOf(m).Any(l => l.Quantity > 2) && LinesOf(m).Where(l => l.Quantity > 2).OrderBy(l => l.Price).First().Quantity == 3, x => x.Lines!.Where(l => l.Quantity > 2).OrderBy(l => l.Price).First().Quantity == 3);
            Same("first with condition", m => LinesOf(m).OrderBy(l => l.Price).FirstOrDefault(l => l.Quantity > 1)!.Quantity == 2, x => x.Lines!.OrderBy(l => l.Price).FirstOrDefault(l => l.Quantity > 1)!.Quantity == 2);
            Same("skip then first", m => LinesOf(m).OrderBy(l => l.Price).Skip(1).First().Quantity == m.Number, x => x.Lines!.OrderBy(l => l.Price).Skip(1).First().Quantity == x.Number);
            Same("condition on the model", m => LinesOf(m).Where(l => l.Quantity == m.Number).OrderBy(l => l.Price).First().Price > 2m, x => x.Lines!.Where(l => l.Quantity == x.Number).OrderBy(l => l.Price).First().Price > 2m);
            Assert.Equal(5, repo.Query<ParityModel>().Count(x => x.Lines!.OrderBy(l => l.Price).First().Quantity != 2));

            //as a value, an order, and a projected member
            Assert.Equal(models.Where(m => LinesOf(m).Length > 0).Sum(m => LinesOf(m).OrderBy(l => l.Price).First().Quantity), repo.Query<ParityModel>().Sum(x => x.Lines!.OrderBy(l => l.Price).First().Quantity));
            Assert.Equal([4, 3, 2, 1], repo.Query<ParityModel>().Where(x => x.Lines!.Any()).OrderByDescending(x => x.Lines!.OrderBy(l => l.Price).First().Quantity).ToArray().Select(x => x.Number).ToArray());
            var rows = repo.Query<ParityModel>().Where(x => x.Lines!.Any()).Select(x => new ParitySelectResult { Number = x.Number, LineCount = x.Lines!.OrderBy(l => l.Price).First().Quantity }).ToArray();
            Assert.Equal([(1, 2), (2, 3), (3, 4), (4, 5)], rows.OrderBy(r => r.Number).Select(r => (r.Number, r.LineCount)).ToArray());
        }

        //every aggregate gives what LINQ gives over the same models, over all of them and per group
        private static void AssertAggregates(IRepo repo, ParityModel[] models)
        {
            Expression<Func<ParityModel, bool>> some = x => x.Number > 0 && x.Number < 5;
            Expression<Func<ParityModel, bool>> none = x => x.Number > 100;
            var someModels = models.Where(some.Compile()).ToArray();

            Assert.Equal(models.Sum(x => x.Number), repo.Query<ParityModel>().Sum(x => x.Number));
            Assert.Equal(models.Sum(x => x.Big), repo.Query<ParityModel>().Sum(x => x.Big));
            Assert.Equal(models.Sum(x => x.Money), repo.Query<ParityModel>().Sum(x => x.Money));
            Assert.Equal(models.Sum(x => x.Ratio), repo.Query<ParityModel>().Sum(x => x.Ratio), 9);
            Assert.Equal(models.Sum(x => x.Maybe), repo.Query<ParityModel>().Sum(x => x.Maybe));
            Assert.Equal(someModels.Sum(x => x.Number * 2 + 1), repo.Query<ParityModel>().Where(some).Sum(x => x.Number * 2 + 1));
            Assert.Equal(0, repo.Query<ParityModel>().Where(none).Sum(x => x.Number));
            Assert.Equal(0, repo.Query<ParityModel>().Where(none).Sum(x => x.Maybe));

            Assert.Equal(models.Average(x => x.Number), repo.Query<ParityModel>().Average(x => x.Number), 9);
            Assert.Equal(models.Average(x => x.Big), repo.Query<ParityModel>().Average(x => x.Big), 9);
            Assert.Equal(models.Average(x => x.Money), repo.Query<ParityModel>().Average(x => x.Money), 6);
            Assert.Equal(models.Average(x => x.Ratio), repo.Query<ParityModel>().Average(x => x.Ratio), 9);
            Assert.Equal(models.Average(x => x.Maybe)!.Value, repo.Query<ParityModel>().Average(x => x.Maybe)!.Value, 9);
            Assert.Null(repo.Query<ParityModel>().Where(none).Average(x => x.Maybe));
            _ = Assert.Throws<InvalidOperationException>(() => repo.Query<ParityModel>().Where(none).Average(x => x.Number));

            Assert.Equal(models.Min(x => x.Number), repo.Query<ParityModel>().Min(x => x.Number));
            Assert.Equal(models.Max(x => x.Money), repo.Query<ParityModel>().Max(x => x.Money));
            Assert.Equal(models.Min(x => x.Maybe), repo.Query<ParityModel>().Min(x => x.Maybe));
            Assert.Equal(models.Max(x => x.Name), repo.Query<ParityModel>().Max(x => x.Name));
            Assert.Equal(models.Min(x => x.Date), repo.Query<ParityModel>().Min(x => x.Date));
            Assert.Equal(someModels.Max(x => x.Date), repo.Query<ParityModel>().Where(some).Max(x => x.Date));
            Assert.Null(repo.Query<ParityModel>().Where(none).Max(x => x.Name));
            _ = Assert.Throws<InvalidOperationException>(() => repo.Query<ParityModel>().Where(none).Min(x => x.Number));

            AssertGroups("count by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, (long)g.Count())), repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
            AssertGroups("count by nullable", models.GroupBy(x => x.Other).Select(g => (g.Key, (long)g.Count())), repo.Query<ParityModel>().GroupBy(x => x.Other).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
            AssertGroups("count by string", models.GroupBy(x => x.Name).Select(g => (g.Key, (long)g.Count())), repo.Query<ParityModel>().GroupBy(x => x.Name).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
            AssertGroups("count by year", models.GroupBy(x => x.Date.Year).Select(g => (g.Key, (long)g.Count())), repo.Query<ParityModel>().GroupBy(x => x.Date.Year).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray());

            Assert.Equal(models.Select(x => x.Other).Distinct().Count(), repo.Query<ParityModel>().Select(x => x.Other).Distinct().Count());
            Assert.Equal(models.Where(x => x.Number > 0).Select(x => x.Flag).Distinct().Count(), repo.Query<ParityModel>().Where(x => x.Number > 0).Select(x => x.Flag).Distinct().Count());
            AssertGroups("distinct others by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, (long)g.Where(x => x.Other is not null).Select(x => x.Other).Distinct().Count())), repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = (long)g.Select(x => x.Other).Distinct().Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
            Assert.Equal(models.Select(x => x.Other).Distinct().Order().ToArray(), repo.Query<ParityModel>().Select(x => x.Other).Distinct().ToArray().Order().ToArray());
            Assert.Equal(models.Where(x => x.Flag).Select(x => x.Name).Distinct().Order().ToArray(), repo.Query<ParityModel>().Where(x => x.Flag).Select(x => x.Name).Distinct().ToArray().Order().ToArray());
            Assert.Equal(models.Select(x => (x.Flag, x.Other)).Distinct().Order().ToArray(), repo.Query<ParityModel>().Select(x => new { x.Flag, x.Other }).Distinct().ToArray().Select(k => (k.Flag, k.Other)).Order().ToArray());
            AssertGroups("count by two members", models.GroupBy(x => (x.Flag, x.Other)).Select(g => (g.Key, (long)g.Count())), repo.Query<ParityModel>().GroupBy(x => new { x.Flag, x.Other }).Select(g => new { g.Key.Flag, g.Key.Other, Value = (long)g.Count() }).ToArray().Select(r => ((r.Flag, r.Other), r.Value)).ToArray());
            AssertGroups("count some", someModels.GroupBy(x => x.Flag).Select(g => (g.Key, (long)g.Count())), repo.Query<ParityModel>().Where(some).GroupBy(x => x.Flag).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
            Assert.Empty(repo.Query<ParityModel>().Where(none).GroupBy(x => x.Flag).Select(g => new { g.Key, Value = (long)g.Count() }).ToArray().Select(r => (r.Key, r.Value)).ToArray());

            AssertGroups("sum by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Sum(x => x.Number))), repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Sum(x => x.Number) }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
            AssertGroups("sum money by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Sum(x => x.Money))), repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Sum(x => x.Money) }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
            AssertGroups("sum nullable by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Sum(x => x.Maybe))), repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Sum(x => x.Maybe) }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
            AssertGroups("average by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Average(x => x.Number))), repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Average(x => x.Number) }).ToArray().Select(r => (r.Key, r.Value)).ToArray(), (a, b) => Math.Abs(a - b) < 1e-9);
            AssertGroups("average money by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Average(x => x.Money))), repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Average(x => x.Money) }).ToArray().Select(r => (r.Key, r.Value)).ToArray(), (a, b) => Math.Abs(a - b) < 0.000001m);
            AssertGroups("min name by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Min(x => x.Name))), repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Min(x => x.Name) }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
            AssertGroups("max date by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Max(x => x.Date))), repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Max(x => x.Date) }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
            AssertGroups("max nullable by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Max(x => x.Maybe))), repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Max(x => x.Maybe) }).ToArray().Select(r => (r.Key, r.Value)).ToArray());
        }

        //a projection of each model and of each group gives what LINQ gives, with the where, order, and paging where they apply
        private static void AssertProjections(IRepo repo, ParityModel[] models, ParityLineModel[] lines, ParityNoteModel[] notes)
        {
            var parents = models.ToDictionary(x => x.ID);
            ParityLineModel[] LinesOf(ParityModel model) => lines.Where(l => l.ParityID == model.ID).ToArray();

            var selected = repo.Query<ParityModel>().Where(x => x.Number > 0).OrderByDescending(x => x.Number).Skip(1).Take(3).Select(x => new ParitySelectResult { ID = x.ID, Number = x.Number, Name = x.Name, Doubled = x.Number * 2, LineCount = x.Lines!.Count(), LineTotal = x.Lines!.Sum(l => l.Price * l.Quantity), Flagged = x.Flag, IsBig = x.Number > 2 }).ToArray();
            var expectedSelected = models.Where(x => x.Number > 0).OrderByDescending(x => x.Number).Skip(1).Take(3)
                .Select(x => (x.ID, x.Number, x.Name, x.Number * 2, LinesOf(x).Length, LinesOf(x).Sum(l => l.Price * l.Quantity), x.Flag, x.Number > 2)).ToArray();
            Assert.Equal(expectedSelected, selected.Select(r => (r.ID, r.Number, r.Name, r.Doubled, r.LineCount, r.LineTotal, r.Flagged, r.IsBig)).ToArray());

            var lineParents = repo.Query<ParityLineModel>().Select(x => new ParityLineGroupResult { ParentID = x.ParityID, ParentName = x.Parent!.Name }).ToArray();
            Assert.Equal(lines.Select(l => (l.ParityID, parents[l.ParityID].Name)).OrderBy(x => x.Item1).ThenBy(x => x.Item2).ToArray(), lineParents.Select(r => (r.ParentID, r.ParentName)).OrderBy(x => x.Item1).ThenBy(x => x.Item2).ToArray());

            //results built from the rows of a related collection, filtered and ordered, as a list and an array, with the models paged
            var nested = repo.Query<ParityModel>().Where(x => x.Number < 5).OrderBy(x => x.Number).Skip(1).Take(3).Select(x => new ParityNestedResult
            {
                Number = x.Number,
                Lines = x.Lines!.OrderByDescending(l => l.Price).Select(l => new ParityLineResult { Quantity = l.Quantity, Price = l.Price, ParentName = l.Parent!.Name }).ToList(),
                Cheap = x.Lines!.Where(l => l.Quantity > 1).OrderBy(l => l.Quantity).Select(l => new ParityLineResult { Quantity = l.Quantity }).ToArray(),
            }).ToArray();
            Assert.Equal([1, 2, 3], nested.Select(r => r.Number).ToArray());
            foreach (var result in nested)
            {
                var model = models.Single(m => m.Number == result.Number);
                Assert.Equal(LinesOf(model).OrderByDescending(l => l.Price).Select(l => (l.Quantity, l.Price, model.Name)).ToArray(), result.Lines!.Select(l => (l.Quantity, l.Price, l.ParentName)).ToArray());
                Assert.Equal(LinesOf(model).Where(l => l.Quantity > 1).OrderBy(l => l.Quantity).Select(l => l.Quantity).ToArray(), result.Cheap!.Select(l => l.Quantity).ToArray());
            }
            var none = repo.Query<ParityModel>().Where(x => x.Number == 0).Select(x => new ParityNestedResult { Number = x.Number, Lines = x.Lines!.Select(l => new ParityLineResult { Quantity = l.Quantity }).ToList() }).ToArray();
            Assert.Empty(Assert.Single(none).Lines!);

            //a row's result with a collection of its own, each level read with a statement of its own
            ParityNoteModel[] NotesOf(ParityLineModel line) => notes.Where(n => n.LineID == line.ID).ToArray();
            var deep = repo.Query<ParityModel>().Where(x => x.Number < 5).OrderBy(x => x.Number).Skip(1).Take(3).Select(x => new ParityNestedResult
            {
                Number = x.Number,
                Lines = x.Lines!.OrderBy(l => l.Quantity).Select(l => new ParityLineResult
                {
                    Quantity = l.Quantity,
                    Notes = l.Notes!.Where(n => n.Rank > 0).OrderByDescending(n => n.Rank).Select(n => new ParityNoteResult { Rank = n.Rank, Text = n.Text }).ToList(),
                }).ToList(),
            }).ToArray();
            Assert.Equal([1, 2, 3], deep.Select(r => r.Number).ToArray());
            foreach (var result in deep)
            {
                var model = models.Single(m => m.Number == result.Number);
                var expectedLines = LinesOf(model).OrderBy(l => l.Quantity).ToArray();
                Assert.Equal(expectedLines.Select(l => l.Quantity).ToArray(), result.Lines!.Select(l => l.Quantity).ToArray());
                for (var i = 0; i < expectedLines.Length; i++)
                    Assert.Equal(NotesOf(expectedLines[i]).Where(n => n.Rank > 0).OrderByDescending(n => n.Rank).Select(n => (n.Rank, n.Text)).ToArray(), result.Lines[i].Notes!.Select(n => (n.Rank, n.Text)).ToArray());
            }

            //a Guid key and a Guid grouped aggregate, which some stores keep as bytes
            var byParentID = repo.Query<ParityLineModel>().GroupBy(x => x.ParityID).Select(g => new ParityLineGroupResult { ParentID = g.Key, Lines = g.Count() }).ToArray();
            Assert.Equal(lines.GroupBy(l => l.ParityID).Select(g => (g.Key, g.Count())).OrderBy(x => x.Key).ToArray(), byParentID.Select(r => (r.ParentID, r.Lines)).OrderBy(x => x.Item1).ToArray());
            AssertGroups("sum quantity by parent id", lines.GroupBy(l => l.ParityID).Select(g => (g.Key, g.Sum(l => l.Quantity))), repo.Query<ParityLineModel>().GroupBy(x => x.ParityID).Select(g => new { g.Key, Value = g.Sum(x => x.Quantity) }).ToArray().Select(r => (r.Key, r.Value)).ToArray());

            var byFlag = repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new ParityGroupResult
            {
                Flag = g.Key,
                Rows = g.Count(),
                SumNumber = g.Sum(x => x.Number),
                AverageNumber = g.Average(x => x.Number),
                MaxMoney = g.Max(x => x.Money),
                MinName = g.Min(x => x.Name),
                LineQuantity = g.Sum(x => x.Lines!.Sum(l => l.Quantity)),
                Flagged = g.Count(x => x.Number > 2),
                DistinctOthers = g.Select(x => x.Other).Distinct().Count(),
            }).ToArray();
            Assert.Equal(2, byFlag.Length);
            foreach (var group in models.GroupBy(x => x.Flag))
            {
                var actual = byFlag.Single(r => r.Flag == group.Key);
                Assert.Equal(group.Count(), actual.Rows);
                Assert.Equal(group.Sum(x => x.Number), actual.SumNumber);
                Assert.Equal(group.Average(x => x.Number), actual.AverageNumber, 9);
                Assert.Equal(group.Max(x => x.Money), actual.MaxMoney);
                Assert.Equal(group.Min(x => x.Name), actual.MinName);
                Assert.Equal(group.Sum(x => LinesOf(x).Sum(l => l.Quantity)), actual.LineQuantity);
                Assert.Equal(group.Count(x => x.Number > 2), actual.Flagged);
                Assert.Equal(group.Where(x => x.Other is not null).Select(x => x.Other).Distinct().Count(), actual.DistinctOthers);
            }

            var byTwo = repo.Query<ParityModel>().GroupBy(x => new { x.Flag, x.Other }).Select(g => new ParityGroupResult { Flag = g.Key.Flag, Other = g.Key.Other, Rows = g.Count() }).ToArray();
            var expectedTwo = models.GroupBy(x => new { x.Flag, x.Other }).Select(g => (g.Key.Flag, g.Key.Other, g.Count())).OrderBy(x => x.Flag).ThenBy(x => x.Other).ToArray();
            Assert.Equal(expectedTwo, byTwo.Select(r => (r.Flag, r.Other, r.Rows)).OrderBy(x => x.Flag).ThenBy(x => x.Other).ToArray());

            //the having, the order, and the paging apply to the results
            var topOthers = repo.Query<ParityModel>().GroupBy(x => x.Other).Select(g => new ParityGroupResult { Other = g.Key, Rows = g.Count(), SumNumber = g.Sum(x => x.Number) }).Where(r => r.Rows >= 1 && r.SumNumber > 2).OrderByDescending(r => r.SumNumber).Take(2).ToArray();
            var expectedTop = models.GroupBy(x => x.Other).Select(g => (g.Key, g.Count(), g.Sum(x => x.Number))).Where(x => x.Item2 >= 1 && x.Item3 > 2).OrderByDescending(x => x.Item3).Take(2).ToArray();
            Assert.Equal(expectedTop, topOthers.Select(r => (r.Other, r.Rows, r.SumNumber)).ToArray());

            //a key through a relation
            var byParent = repo.Query<ParityLineModel>().GroupBy(x => x.Parent!.Name).Select(g => new ParityLineGroupResult { ParentName = g.Key, Lines = g.Count(), Revenue = g.Sum(l => l.Price * l.Quantity) }).ToArray();
            var expectedByParent = lines.GroupBy(l => parents[l.ParityID].Name).Select(g => (g.Key, g.Count(), g.Sum(l => l.Price * l.Quantity))).OrderBy(x => x.Key).ToArray();
            Assert.Equal(expectedByParent, byParent.Select(r => (r.ParentName, r.Lines, r.Revenue)).OrderBy(x => x.Item1).ToArray());

            Assert.Empty(repo.Query<ParityModel>().Where(x => x.Number > 100).GroupBy(x => x.Flag).Select(g => new ParityGroupResult { Flag = g.Key, Rows = g.Count() }).ToArray());
            Assert.Empty(repo.Query<ParityModel>().Where(x => x.Number > 100).Select(x => new ParitySelectResult { Number = x.Number }).ToArray());
        }

        //a LINQ query of the store gives what the same LINQ gives over the models, each call mapped onto a store operation
        private static void AssertQueryable(IRepo repo, ParityModel[] models, ParityLineModel[] lines)
        {
            var q = repo.Query<ParityModel>();
            var named = models.Where(x => x.Name != null).ToArray();

            //the models, filtered, ordered, and paged
            Assert.Equal(models.Where(x => x.Number < 3).Select(x => x.Number).Order(), q.Where(x => x.Number < 3).ToArray().Select(x => x.Number).Order());
            Assert.Equal(models.Where(x => x.Number > 0).Where(x => x.Flag || x.Number < 4).OrderByDescending(x => x.Number).Skip(1).Take(2).Select(x => x.Number), q.Where(x => x.Number > 0).Where(x => x.Flag || x.Number < 4).OrderByDescending(x => x.Number).Skip(1).Take(2).ToList().Select(x => x.Number));
            Assert.Equal(named.OrderBy(x => x.Flag).ThenByDescending(x => x.Name).Select(x => x.Number), q.Where(x => x.Name != null).OrderBy(x => x.Flag).ThenByDescending(x => x.Name).Select(x => x.Number));
            foreach (var model in q.Where(x => x.Lines!.Any()))
                Assert.Contains(model.Number, new[] { 1, 2, 3, 4 });

            //one model
            Assert.Equal(3, q.First(x => x.Number == 3).Number);
            Assert.Equal(2, q.Where(x => x.Number > 1).OrderBy(x => x.Number).First().Number);
            Assert.Equal(4, q.Where(x => x.Flag || x.Number > 3).OrderByDescending(x => x.Number).Skip(1).FirstOrDefault()!.Number);
            Assert.Null(q.FirstOrDefault(x => x.Number > 100));
            _ = Assert.Throws<InvalidOperationException>(() => q.First(x => x.Number > 100));
            Assert.Equal("cherry", q.Single(x => x.Number == 2).Name);
            Assert.Null(q.SingleOrDefault(x => x.Number > 100));

            //counts and conditions
            Assert.Equal(models.Count(x => x.Flag), q.Count(x => x.Flag));
            Assert.Equal(models.Count(x => x.Number > 1 && x.Flag), q.Where(x => x.Number > 1).Count(x => x.Flag));
            Assert.Equal(models.LongCount(), q.LongCount());
            Assert.True(q.Any(x => x.Number == 5));
            Assert.False(q.Where(x => x.Number > 100).Any());
            Assert.True(q.All(x => x.Number < 6));
            Assert.False(q.All(x => x.Flag));

            //aggregates, with the value in the call or selected before it
            Assert.Equal(models.Sum(x => x.Number), q.Sum(x => x.Number));
            Assert.Equal(models.Where(x => x.Flag).Sum(x => x.Money), q.Where(x => x.Flag).Select(x => x.Money).Sum());
            Assert.Equal(models.Max(x => x.Name), q.Max(x => x.Name));
            Assert.Equal(models.Min(x => x.Maybe), q.Select(x => x.Maybe).Min());
            Assert.Equal(models.Average(x => x.Number), q.Average(x => x.Number), 9);
            Assert.Equal(models.Where(x => x.Flag).Average(x => x.Money), q.Where(x => x.Flag).Average(x => x.Money), 6);
            _ = Assert.Throws<InvalidOperationException>(() => q.Where(x => x.Number > 100).Min(x => x.Number));

            //values, results, and the different values
            Assert.Equal(named.Where(x => x.Number < 5).OrderBy(x => x.Number).Select(x => x.Name!.ToUpper()), q.Where(x => x.Name != null && x.Number < 5).OrderBy(x => x.Number).Select(x => x.Name!.ToUpper()));
            Assert.Equal(named.OrderBy(x => x.Number).Select(x => (" " + x.Name + " ").TrimStart()), q.Where(x => x.Name != null).OrderBy(x => x.Number).Select(x => (" " + x.Name + " ").TrimStart()).ToArray());
            Assert.Equal(named.OrderBy(x => x.Number).Select(x => (" " + x.Name + " ").TrimEnd()), q.Where(x => x.Name != null).OrderBy(x => x.Number).Select(x => (" " + x.Name + " ").TrimEnd()).ToArray());
            Assert.Equal(models.Count(x => null == x.Maybe), q.Count(x => null == x.Maybe));
            Assert.Equal(models.Count(x => null != x.Maybe), q.Count(x => null != x.Maybe));
            Assert.Equal(models.Length - 2, q.Skip(2).ToArray().Length);
            Assert.Equal(models.Select(x => (float)x.Number).Order(), q.Select(x => (float)x.Number).ToArray().Order());
            Assert.Equal(models.OrderByDescending(x => x.Number).Select(x => x.Number * 2).First(), q.OrderByDescending(x => x.Number).Select(x => x.Number * 2).First());
            Assert.Equal(models.Select(x => x.Other).Distinct().Order(), q.Select(x => x.Other).Distinct().ToArray().Order());
            var results = q.Where(x => x.Number > 0).OrderBy(x => x.Number).Skip(1).Take(3).Select(x => new ParitySelectResult { Number = x.Number, Doubled = x.Number * 2, LineCount = x.Lines!.Count() }).ToArray();
            Assert.Equal(models.Where(x => x.Number > 0).OrderBy(x => x.Number).Skip(1).Take(3).Select(x => (x.Number, x.Number * 2, lines.Count(l => l.ParityID == x.ID))), results.Select(r => (r.Number, r.Doubled, r.LineCount)));
            Assert.Equal(2, q.Where(x => x.Number > 1).OrderBy(x => x.Number).Select(x => new ParitySelectResult { Number = x.Number }).First().Number);

            //groups, with their having, order, and paging after the Select
            var groups = q.Where(x => x.Number < 5).GroupBy(x => x.Other).Select(g => new ParityGroupResult { Other = g.Key, Rows = g.Count(), SumNumber = g.Sum(x => x.Number) }).Where(r => r.SumNumber > 0).OrderByDescending(r => r.SumNumber).ThenBy(r => r.Other).Take(2).ToArray();
            var expectedGroups = models.Where(x => x.Number < 5).GroupBy(x => x.Other).Select(g => (g.Key, g.Count(), g.Sum(x => x.Number))).Where(r => r.Item3 > 0).OrderByDescending(r => r.Item3).ThenBy(r => r.Key).Take(2).ToArray();
            Assert.Equal(expectedGroups, groups.Select(r => (r.Other, r.Rows, r.SumNumber)));

            //anonymous results, nested objects, and relations read whole, as rows selected from, or as values, each in one statement after the models
            ParityLineModel[] LinesOf(ParityModel model) => lines.Where(l => l.ParityID == model.ID).ToArray();
            var shaped = q.Where(x => x.Number > 0 && x.Number < 5).OrderBy(x => x.Number).Select(x => new
            {
                x.Number,
                Info = new { x.Name, Flag = x.Flag, Twice = x.Number * 2 },
                Lines = x.Lines!.Where(l => l.Quantity > 1).OrderByDescending(l => l.Price).Select(l => new { l.Quantity, l.Price, ParentName = l.Parent!.Name }).ToList(),
                Quantities = x.Lines!.OrderBy(l => l.Quantity).Select(l => l.Quantity).ToArray(),
                Whole = x.Lines!.OrderBy(l => l.Price).ToArray(),
            }).ToArray();
            Assert.Equal([1, 2, 3, 4], shaped.Select(r => r.Number));
            foreach (var row in shaped)
            {
                var model = models.Single(m => m.Number == row.Number);
                Assert.Equal((model.Name, model.Flag, model.Number * 2), (row.Info.Name, row.Info.Flag, row.Info.Twice));
                Assert.Equal(LinesOf(model).Where(l => l.Quantity > 1).OrderByDescending(l => l.Price).Select(l => (l.Quantity, l.Price, model.Name)), row.Lines.Select(l => (l.Quantity, l.Price, l.ParentName)));
                Assert.Equal(LinesOf(model).OrderBy(l => l.Quantity).Select(l => l.Quantity), row.Quantities);
                Assert.Equal(LinesOf(model).OrderBy(l => l.Price).Select(l => l.ID), row.Whole.Select(l => l.ID));
            }
            var lineRows = repo.Query<ParityLineModel>().Where(x => x.Quantity == 2).Select(x => new { x.Quantity, x.Parent, ParentNumber = x.Parent!.Number }).ToArray();
            Assert.Equal(2, lineRows.Length);
            foreach (var row in lineRows)
                Assert.Equal(row.ParentNumber, row.Parent!.Number);
            var noLines = q.Where(x => x.Number == 0).Select(x => new { x.Number, Lines = x.Lines!.Select(l => new { l.Quantity }).ToArray() }).Single();
            Assert.Empty(noLines.Lines);
            var twice = q.Where(x => x.Number == 2).Select(x => new { First = x.Name, Again = x.Name, x.Number, Quantities = x.Lines!.Select(l => l.Quantity).ToArray(), Id = x.ID }).Single();
            Assert.Equal(("cherry", "cherry", 2), (twice.First, twice.Again, twice.Number));
            Assert.Equal(LinesOf(models.Single(m => m.Number == 2)).Select(l => l.Quantity).Order(), twice.Quantities.Order());
            var pagedLines = q.Where(x => x.Number > 0).OrderByDescending(x => x.Flag).ThenBy(x => x.Number).Skip(1).Take(2).Select(x => new { x.Number, Quantities = x.Lines!.OrderBy(l => l.Quantity).Select(l => l.Quantity).ToArray() }).ToArray();
            Assert.Equal(models.Where(x => x.Number > 0).OrderByDescending(x => x.Flag).ThenBy(x => x.Number).Skip(1).Take(2).Select(x => x.Number), pagedLines.Select(r => r.Number));
            foreach (var row in pagedLines)
                Assert.Equal(LinesOf(models.Single(m => m.Number == row.Number)).OrderBy(l => l.Quantity).Select(l => l.Quantity), row.Quantities);
            var anonymousGroups = q.GroupBy(x => x.Flag).Select(g => new { g.Key, Rows = g.Count(), Total = g.Sum(x => x.Number) }).Where(r => r.Rows > 1).OrderByDescending(r => r.Total).ToArray();
            Assert.Equal(models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Count(), g.Sum(x => x.Number))).Where(r => r.Item2 > 1).OrderByDescending(r => r.Item3), anonymousGroups.Select(r => (r.Key, r.Rows, r.Total)));

            //what the store can't do is refused rather than done in memory
            _ = Assert.Throws<NotSupportedException>(() => q.Skip(1).Where(x => x.Flag).ToArray());
            _ = Assert.Throws<NotSupportedException>(() => q.Take(2).OrderBy(x => x.Number).ToArray());
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
            repo.AddProvider(new TransactStoreProvider<ParityNoteModel>(engine));
            var models = Models();
            foreach (var model in models)
                await repo.CreateAsync(model);
            var lines = Lines(models);
            await repo.CreateAsync<ParityLineModel>(lines);
            var notes = Notes(lines);
            await repo.CreateAsync<ParityNoteModel>(notes);
            try
            {
                foreach (var (name, where) in Wheres())
                {
                    var compiled = where.Compile();
                    var expected = models.Where(compiled).Select(x => x.Number).Order().ToArray();
                    var actual = (await repo.Query<ParityModel>().Where(where).ToArrayAsync()).Select(x => x.Number).Order().ToArray();
                    Assert.True(expected.SequenceEqual(actual), $"{name}: expected [{String.Join(",", expected)}] found [{String.Join(",", actual)}]");
                }

                Assert.Equal(models.Sum(x => x.Money), await repo.Query<ParityModel>().SumAsync(x => x.Money));
                Assert.Equal(models.Average(x => x.Number), await repo.Query<ParityModel>().AverageAsync(x => x.Number), 9);
                Assert.Equal(models.Max(x => x.Name), await repo.Query<ParityModel>().MaxAsync(x => x.Name));
                AssertGroups("async count by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, (long)g.Count())), (await repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = (long)g.Count() }).ToArrayAsync()).Select(r => (r.Key, r.Value)).ToArray());
                AssertGroups("async min by flag", models.GroupBy(x => x.Flag).Select(g => (g.Key, g.Min(x => x.Number))), (await repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new { g.Key, Value = g.Min(x => x.Number) }).ToArrayAsync()).Select(r => (r.Key, r.Value)).ToArray());
                Assert.Equal(lines.Sum(l => l.Price * l.Quantity), await repo.Query<ParityModel>().SumAsync(x => x.Lines!.Sum(l => l.Price * l.Quantity)));
                var setValues = await repo.Query<ParityModel>().Where(x => x.Number < 4).Select(x => x.Number).Union(repo.Query<ParityLineModel>().Select(l => l.Quantity)).ToArrayAsync();
                Assert.Equal(models.Where(x => x.Number < 4).Select(x => x.Number).Union(lines.Select(l => l.Quantity)).Order(), setValues.Order());
                Assert.Equal(models.Select(x => x.Number).Concat(lines.Select(l => l.Quantity)).Count(), await repo.Query<ParityModel>().Select(x => x.Number).Concat(repo.Query<ParityLineModel>().Select(l => l.Quantity)).CountAsync());
                var joinedValues = await repo.Query<ParityModel>().Join(repo.Query<ParityLineModel>(), x => x.ID, l => l.ParityID, (x, l) => new { x.Number, l.Quantity }).ToArrayAsync();
                Assert.Equal(models.Join(lines, x => x.ID, l => l.ParityID, (x, l) => $"{x.Number}:{l.Quantity}").Order(), joinedValues.Select(r => $"{r.Number}:{r.Quantity}").Order());
                Assert.Equal(lines.Max(l => l.Quantity), await repo.Query<ParityModel>().Join(repo.Query<ParityLineModel>(), x => x.ID, l => l.ParityID, (x, l) => new { x, l }).MaxAsync(r => r.l.Quantity));
                Assert.Equal(models.Length - 2, await repo.Query<ParityModel>().GroupJoin(repo.Query<ParityLineModel>(), x => x.ID, l => l.ParityID, (x, g) => new { x.Number, Count = g.Count() }).CountAsync(r => r.Count > 0));
                var selected = await repo.Query<ParityModel>().Where(x => x.Number < 2).Select(x => new ParitySelectResult { Number = x.Number, Doubled = x.Number * 2 }).ToArrayAsync();
                Assert.Equal(models.Where(x => x.Number < 2).Select(x => x.Number * 2).Order(), selected.Select(r => r.Doubled).Order());
                var grouped = await repo.Query<ParityModel>().GroupBy(x => x.Flag).Select(g => new ParityGroupResult { Flag = g.Key, Rows = g.Count() }).ToArrayAsync();
                Assert.Equal(models.Count(x => x.Flag), grouped.Single(r => r.Flag).Rows);

                await AssertGraphAsync(repo, models, lines);
                await AssertShapesAsync(repo, models, lines);

                var q = repo.Query<ParityModel>();
                Assert.Equal(models.Where(x => x.Number > 2).Select(x => x.Number).Order(), (await q.Where(x => x.Number > 2).ToArrayAsync()).Select(x => x.Number).Order());
                Assert.Equal(models.Where(x => x.Flag).Select(x => x.Number).Order(), (await q.Where(x => x.Flag).Select(x => x.Number).ToListAsync()).Order());
                Assert.Equal(models.Count(x => x.Flag), await q.CountAsync(x => x.Flag));
                Assert.Equal(1, (await q.OrderBy(x => x.Number).FirstAsync(x => x.Number > 0)).Number);
                Assert.Null(await q.FirstOrDefaultAsync(x => x.Number > 100));
                Assert.True(await q.AnyAsync(x => x.Number == 4));
                Assert.True(await q.AllAsync(x => x.Number >= 0));
                Assert.Equal(models.Sum(x => x.Money), await q.SumAsync(x => x.Money));
                Assert.Equal(models.Average(x => x.Number), await q.AverageAsync(x => x.Number), 9);
                Assert.Equal(models.Max(x => x.Date), await q.MaxAsync(x => x.Date));
                var firstResult = await q.Where(x => x.Number == 3).Select(x => new ParitySelectResult { Number = x.Number, Name = x.Name }).SingleAsync();
                Assert.Equal((3, (string?)null), (firstResult.Number, firstResult.Name));
            }
            finally
            {
                await repo.DeleteAsync<ParityNoteModel>(notes);
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
