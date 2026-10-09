// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;

namespace Zerra.Repository.Test
{
    //the same reads against every IEventStoreEngine, each bound is inclusive and a stream longer than a page is read across pages
    public static class EventStoreEngineTest
    {
        private sealed class Calls
        {
            public required Func<Guid, string, string, ulong?, EventStoreState, byte[], Task<ulong>> Append { get; init; }
            public required Func<Guid, string, string, ulong?, EventStoreState, Task<ulong>> Terminate { get; init; }
            public required Func<string, ulong?, long?, ulong?, DateTime?, DateTime?, Task<EventStoreEventData[]>> Read { get; init; }
            public required Func<string, ulong?, long?, ulong?, DateTime?, DateTime?, Task<EventStoreEventData[]>> ReadBackwards { get; init; }
        }

        public static Task TestAsync(IEventStoreEngine engine) => TestCalls(new Calls()
        {
            Append = engine.AppendAsync,
            Terminate = engine.TerminateAsync,
            Read = engine.ReadAsync,
            ReadBackwards = engine.ReadBackwardsAsync,
        });

        public static Task TestSync(IEventStoreEngine engine) => TestCalls(new Calls()
        {
            Append = (id, name, stream, number, state, data) => Task.FromResult(engine.Append(id, name, stream, number, state, data)),
            Terminate = (id, name, stream, number, state) => Task.FromResult(engine.Terminate(id, name, stream, number, state)),
            Read = (stream, start, count, end, startDate, endDate) => Task.FromResult(engine.Read(stream, start, count, end, startDate, endDate)),
            ReadBackwards = (stream, start, count, end, startDate, endDate) => Task.FromResult(engine.ReadBackwards(stream, start, count, end, startDate, endDate)),
        });

        private static async Task TestCalls(Calls engine)
        {
            var stream = $"EngineTest-{Guid.NewGuid():N}";

            //a missing stream reads as empty
            Assert.Empty(await engine.Read(stream, null, null, null, null, null));
            Assert.Empty(await engine.ReadBackwards(stream, null, null, null, null, null));

            //Existing needs the stream, NotExisting needs it missing
            _ = await Assert.ThrowsAnyAsync<Exception>(() => engine.Append(Guid.NewGuid(), "E", stream, null, EventStoreState.Existing, [0]));
            Assert.Equal(0UL, await engine.Append(Guid.NewGuid(), "E0", stream, null, EventStoreState.NotExisting, [0]));
            _ = await Assert.ThrowsAnyAsync<Exception>(() => engine.Append(Guid.NewGuid(), "E", stream, null, EventStoreState.NotExisting, [0]));
            Assert.Equal(1UL, await engine.Append(Guid.NewGuid(), "E1", stream, null, EventStoreState.Existing, [1]));
            Assert.Equal(2UL, await engine.Append(Guid.NewGuid(), "E2", stream, null, EventStoreState.Any, [2]));

            //an expected number is the number of the last event
            _ = await Assert.ThrowsAnyAsync<Exception>(() => engine.Append(Guid.NewGuid(), "E", stream, 1, EventStoreState.Any, [0]));
            for (ulong i = 3; i < 61; i++)
                Assert.Equal(i, await engine.Append(Guid.NewGuid(), $"E{i}", stream, i - 1, EventStoreState.Any, [(byte)i]));

            var all = await engine.Read(stream, null, null, null, null, null);
            Assert.Equal(61, all.Length);
            for (var i = 0; i < all.Length; i++)
            {
                Assert.Equal((ulong)i, all[i].Number);
                Assert.Equal($"E{i}", all[i].EventName);
                Assert.Equal([(byte)i], all[i].Data.ToArray());
                Assert.False(all[i].Deleted);
            }

            await AssertRead(engine, stream, all, 10, null, null, null, null);
            await AssertRead(engine, stream, all, 10, 5, null, null, null);
            await AssertRead(engine, stream, all, null, null, 40, null, null);
            await AssertRead(engine, stream, all, 10, null, 40, null, null);
            await AssertRead(engine, stream, all, null, 30, null, null, null);
            await AssertRead(engine, stream, all, 5, 50, null, null, null);
            await AssertRead(engine, stream, all, 59, 10, null, null, null);
            await AssertRead(engine, stream, all, 100, null, null, null, null);
            await AssertRead(engine, stream, all, null, null, null, all[20].Date, null);
            await AssertRead(engine, stream, all, null, null, null, null, all[45].Date);
            await AssertRead(engine, stream, all, null, null, null, all[20].Date, all[45].Date);
            await AssertRead(engine, stream, all, null, 7, null, all[20].Date, null);

            await AssertReadBackwards(engine, stream, all, null, null, null, null, null);
            await AssertReadBackwards(engine, stream, all, 50, null, null, null, null);
            await AssertReadBackwards(engine, stream, all, 50, 5, null, null, null);
            await AssertReadBackwards(engine, stream, all, null, null, 10, null, null);
            await AssertReadBackwards(engine, stream, all, 50, null, 10, null, null);
            await AssertReadBackwards(engine, stream, all, null, 30, null, null, null);
            await AssertReadBackwards(engine, stream, all, null, 1, null, null, null);
            await AssertReadBackwards(engine, stream, all, 0, 5, null, null, null);
            await AssertReadBackwards(engine, stream, all, null, null, null, all[20].Date, null);
            await AssertReadBackwards(engine, stream, all, null, null, null, null, all[45].Date);
            await AssertReadBackwards(engine, stream, all, null, null, null, all[20].Date, all[45].Date);
            await AssertReadBackwards(engine, stream, all, null, 7, null, null, all[45].Date);

            //a stream that ends exactly on a page boundary
            var pages = $"EngineTest-{Guid.NewGuid():N}";
            for (ulong i = 0; i < 50; i++)
                _ = await engine.Append(Guid.NewGuid(), $"E{i}", pages, null, EventStoreState.Any, [(byte)i]);
            var pagesAll = await engine.Read(pages, null, null, null, null, null);
            Assert.Equal(50, pagesAll.Length);
            await AssertReadBackwards(engine, pages, pagesAll, null, null, null, null, null);
            await AssertReadBackwards(engine, pages, pagesAll, 24, null, null, null, null);

            //termination writes a deleted marker as the last event
            Assert.Equal(61UL, await engine.Terminate(Guid.NewGuid(), "Deleted", stream, 60, EventStoreState.Any));
            var last = await engine.ReadBackwards(stream, null, 1, null, null, null);
            Assert.Single(last);
            Assert.Equal(61UL, last[0].Number);
            Assert.True(last[0].Deleted);
            Assert.Equal("Deleted", last[0].EventName);

            var terminated = $"EngineTest-{Guid.NewGuid():N}";
            _ = await engine.Append(Guid.NewGuid(), "E0", terminated, null, EventStoreState.Any, [0]);
            _ = await Assert.ThrowsAnyAsync<Exception>(() => engine.Terminate(Guid.NewGuid(), "Deleted", terminated, null, EventStoreState.NotExisting));
            Assert.Equal(1UL, await engine.Terminate(Guid.NewGuid(), "Deleted", terminated, null, EventStoreState.Existing));
            Assert.True((await engine.Read(terminated, null, null, null, null, null))[1].Deleted);
        }

        private static async Task AssertRead(Calls engine, string stream, EventStoreEventData[] all, ulong? start, long? count, ulong? end, DateTime? startDate, DateTime? endDate)
        {
            var expected = all.Where(x => (!start.HasValue || x.Number >= start.Value) && (!end.HasValue || x.Number <= end.Value) && (!startDate.HasValue || x.Date >= startDate.Value) && (!endDate.HasValue || x.Date <= endDate.Value));
            if (count.HasValue)
                expected = expected.Take((int)count.Value);
            var read = await engine.Read(stream, start, count, end, startDate, endDate);
            Assert.Equal(expected.Select(x => x.Number), read.Select(x => x.Number));
        }

        private static async Task AssertReadBackwards(Calls engine, string stream, EventStoreEventData[] all, ulong? start, long? count, ulong? end, DateTime? startDate, DateTime? endDate)
        {
            var expected = all.Reverse().Where(x => (!start.HasValue || x.Number <= start.Value) && (!end.HasValue || x.Number >= end.Value) && (!startDate.HasValue || x.Date >= startDate.Value) && (!endDate.HasValue || x.Date <= endDate.Value));
            if (count.HasValue)
                expected = expected.Take((int)count.Value);
            var read = await engine.ReadBackwards(stream, start, count, end, startDate, endDate);
            Assert.Equal(expected.Select(x => x.Number), read.Select(x => x.Number));
        }
    }
}
