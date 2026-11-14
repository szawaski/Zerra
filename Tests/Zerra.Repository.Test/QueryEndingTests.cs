// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    //each Async ending gives what its LINQ ending gives over the same models
    public class QueryEndingTests
    {
        [Entity("EndingModel")]
        public sealed class EndingModel
        {
            [Identity]
            public Guid ID { get; set; }
            public int Int { get; set; }
            public int? IntNullable { get; set; }
            public long Long { get; set; }
            public long? LongNullable { get; set; }
            public decimal Decimal { get; set; }
            public decimal? DecimalNullable { get; set; }
            public double Double { get; set; }
            public double? DoubleNullable { get; set; }
            public float Float { get; set; }
            public float? FloatNullable { get; set; }
        }

        [Fact]
        public async Task EveryEnding()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var repo = Repo.New();
            repo.AddProvider(new TransactStoreProvider<EndingModel>(new MemoryEngine()));
            var models = Enumerable.Range(1, 6).Select(i => new EndingModel
            {
                ID = Guid.NewGuid(),
                Int = i,
                IntNullable = i % 2 == 0 ? i : null,
                Long = i * 10L,
                LongNullable = i % 3 == 0 ? null : i * 10L,
                Decimal = i * 1.5m,
                DecimalNullable = i % 2 == 0 ? null : i * 1.5m,
                Double = i * 0.25,
                DoubleNullable = i % 2 == 0 ? i * 0.25 : null,
                Float = i * 0.5f,
                FloatNullable = i % 3 == 0 ? i * 0.5f : null,
            }).ToArray();
            await repo.CreateManyAsync<EndingModel>(models, cancellationToken);
            var q = repo.Query<EndingModel>();

            Assert.Equal(models.Sum(x => x.Int), await q.SumAsync(x => x.Int, cancellationToken));
            Assert.Equal(models.Sum(x => x.IntNullable), await q.SumAsync(x => x.IntNullable, cancellationToken));
            Assert.Equal(models.Sum(x => x.Long), await q.SumAsync(x => x.Long, cancellationToken));
            Assert.Equal(models.Sum(x => x.LongNullable), await q.SumAsync(x => x.LongNullable, cancellationToken));
            Assert.Equal(models.Sum(x => x.Decimal), await q.SumAsync(x => x.Decimal, cancellationToken));
            Assert.Equal(models.Sum(x => x.DecimalNullable), await q.SumAsync(x => x.DecimalNullable, cancellationToken));
            Assert.Equal(models.Sum(x => x.Double), await q.SumAsync(x => x.Double, cancellationToken));
            Assert.Equal(models.Sum(x => x.DoubleNullable), await q.SumAsync(x => x.DoubleNullable, cancellationToken));
            Assert.Equal(models.Sum(x => x.Float), await q.SumAsync(x => x.Float, cancellationToken));
            Assert.Equal(models.Sum(x => x.FloatNullable), await q.SumAsync(x => x.FloatNullable, cancellationToken));

            Assert.Equal(models.Average(x => x.Int), await q.AverageAsync(x => x.Int, cancellationToken));
            Assert.Equal(models.Average(x => x.IntNullable), await q.AverageAsync(x => x.IntNullable, cancellationToken));
            Assert.Equal(models.Average(x => x.Long), await q.AverageAsync(x => x.Long, cancellationToken));
            Assert.Equal(models.Average(x => x.LongNullable), await q.AverageAsync(x => x.LongNullable, cancellationToken));
            Assert.Equal(models.Average(x => x.Decimal), await q.AverageAsync(x => x.Decimal, cancellationToken));
            Assert.Equal(models.Average(x => x.DecimalNullable), await q.AverageAsync(x => x.DecimalNullable, cancellationToken));
            Assert.Equal(models.Average(x => x.Double), await q.AverageAsync(x => x.Double, cancellationToken));
            Assert.Equal(models.Average(x => x.DoubleNullable), await q.AverageAsync(x => x.DoubleNullable, cancellationToken));
            Assert.Equal(models.Average(x => x.Float), await q.AverageAsync(x => x.Float, cancellationToken));
            Assert.Equal(models.Average(x => x.FloatNullable), await q.AverageAsync(x => x.FloatNullable, cancellationToken));

            Assert.Equal(models.Min(x => x.Long), await q.MinAsync(x => x.Long, cancellationToken));
            Assert.Equal(models.Max(x => x.DoubleNullable), await q.MaxAsync(x => x.DoubleNullable, cancellationToken));
            Assert.Equal(models.LongCount(), await q.LongCountAsync(cancellationToken));
            Assert.Equal(models.LongCount(x => x.Int > 2), await q.LongCountAsync(x => x.Int > 2, cancellationToken));

            var ordered = q.OrderBy(x => x.Int);
            Assert.Equal(6, (await ordered.LastAsync(cancellationToken)).Int);
            Assert.Equal(4, (await ordered.LastAsync(x => x.Int < 5, cancellationToken)).Int);
            Assert.Equal(6, (await ordered.LastOrDefaultAsync(cancellationToken))!.Int);
            Assert.Null(await ordered.LastOrDefaultAsync(x => x.Int > 10, cancellationToken));

            //per group, as the memory engine aggregates them
            var groups = (await q.GroupBy(x => x.Int % 2).Select(g => new { g.Key, F = g.Sum(x => x.Float), FN = g.Sum(x => x.FloatNullable), D = g.Sum(x => x.Double), DN = g.Average(x => x.DoubleNullable), M = g.Average(x => x.Decimal), MN = g.Sum(x => x.DecimalNullable), L = g.Average(x => x.LongNullable), I = g.Average(x => x.IntNullable) }).ToArrayAsync(cancellationToken)).OrderBy(g => g.Key).ToArray();
            var expectedGroups = models.GroupBy(x => x.Int % 2).Select(g => new { g.Key, F = g.Sum(x => x.Float), FN = g.Sum(x => x.FloatNullable), D = g.Sum(x => x.Double), DN = g.Average(x => x.DoubleNullable), M = g.Average(x => x.Decimal), MN = g.Sum(x => x.DecimalNullable), L = g.Average(x => x.LongNullable), I = g.Average(x => x.IntNullable) }).OrderBy(g => g.Key).ToArray();
            Assert.Equal(expectedGroups, groups);

            var byInt = await q.ToDictionaryAsync(x => x.Int, x => x.Long, EqualityComparer<int>.Default, cancellationToken);
            Assert.Equal(models.ToDictionary(x => x.Int, x => x.Long), byInt);
        }
    }
}
