// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;

namespace Zerra.Repository.Test
{
    public static class AggregateTest
    {
        /// <summary>
        /// Exercises an <see cref="AggregateRoot"/> against an <see cref="IEventStoreEngine"/>. An aggregate applies an event to itself and appends it
        /// to its own stream, so the state of a fresh instance comes only from replaying that stream. These events are the aggregate's state,
        /// not CQRS events: they stay in the stream and never reach the bus, so no bus is needed here.
        /// </summary>
        public static async Task TestSequenceAsync<T>()
            where T : DataContext, new()
        {
            var eventStore = GetEventStore<T>();

            var id = Guid.NewGuid();
            var aggregate = new TestAggregate(id, eventStore);

            //nothing has been appended so there is nothing to replay
            Assert.False(await aggregate.Rebuild());
            Assert.False(aggregate.IsCreated);
            Assert.False(aggregate.IsDeleted);
            Assert.Null(aggregate.LastEventNumber);
            Assert.Null(aggregate.LastEventDate);
            Assert.Null(aggregate.LastEventName);

            //an append stores the event, then applies it to this instance
            await aggregate.Append(new TestAggregateCreated() { Name = "First", Amount = 10 });
            Assert.Equal("First", aggregate.Name);
            Assert.Equal(10, aggregate.Amount);
            Assert.Equal(1, aggregate.AppliedCount);
            Assert.True(aggregate.IsCreated);
            Assert.Equal(0UL, aggregate.LastEventNumber);
            Assert.Equal(nameof(TestAggregateCreated), aggregate.LastEventName);
            Assert.NotNull(aggregate.LastEventDate);

            await aggregate.Append(new TestAggregateAmountAdded() { Amount = 5 });
            Assert.Equal(15, aggregate.Amount);
            Assert.Equal(2, aggregate.AppliedCount);

            await aggregate.Append(new TestAggregateRenamed() { Name = "Second" });
            Assert.Equal("Second", aggregate.Name);
            Assert.Equal(3, aggregate.AppliedCount);
            Assert.Equal(2UL, aggregate.LastEventNumber);

            //the appends advanced this instance past its own events, so a rebuild has nothing new to apply
            Assert.False(await aggregate.Rebuild());
            Assert.Equal(3, aggregate.AppliedCount);

            //a fresh instance has no state until it is rebuilt
            var rebuilt = new TestAggregate(id, eventStore);
            Assert.Null(rebuilt.Name);
            Assert.Equal(0, rebuilt.Amount);

            Assert.True(await rebuilt.Rebuild());
            Assert.True(rebuilt.IsCreated);
            Assert.False(rebuilt.IsDeleted);
            Assert.Equal("Second", rebuilt.Name);
            Assert.Equal(15, rebuilt.Amount);
            Assert.Equal(3, rebuilt.AppliedCount);
            Assert.Equal(2UL, rebuilt.LastEventNumber);
            Assert.Equal(nameof(TestAggregateRenamed), rebuilt.LastEventName);
            Assert.NotNull(rebuilt.LastEventDate);

            //a rebuild only replays what it has not seen
            Assert.False(await rebuilt.Rebuild());
            Assert.Equal(3, rebuilt.AppliedCount);

            //one event at a time
            var stepped = new TestAggregate(id, eventStore);

            Assert.True(await stepped.RebuildOneEvent());
            Assert.Equal("First", stepped.Name);
            Assert.Equal(10, stepped.Amount);
            Assert.Equal(0UL, stepped.LastEventNumber);
            Assert.Equal(nameof(TestAggregateCreated), stepped.LastEventName);

            Assert.True(await stepped.RebuildOneEvent());
            Assert.Equal(15, stepped.Amount);
            Assert.Equal(1UL, stepped.LastEventNumber);
            Assert.Equal(nameof(TestAggregateAmountAdded), stepped.LastEventName);

            Assert.True(await stepped.RebuildOneEvent());
            Assert.Equal("Second", stepped.Name);
            Assert.Equal(2UL, stepped.LastEventNumber);

            Assert.False(await stepped.RebuildOneEvent());
            Assert.Equal(3, stepped.AppliedCount);

            //rebuilding up to an event number
            var toNumber = new TestAggregate(id, eventStore);
            Assert.True(await toNumber.Rebuild(1));
            Assert.Equal("First", toNumber.Name);
            Assert.Equal(15, toNumber.Amount);
            Assert.Equal(1UL, toNumber.LastEventNumber);
            Assert.Equal(2, toNumber.AppliedCount);

            //rebuilding up to a date
            var createdDate = rebuilt.LastEventDate!.Value;
            var toDate = new TestAggregate(id, eventStore);
            Assert.True(await toDate.Rebuild(null, createdDate));
            Assert.Equal(3, toDate.AppliedCount);
            Assert.Equal(2UL, toDate.LastEventNumber);

            //a delete terminates the stream
            await aggregate.Delete(new TestAggregateRemoved() { Reason = "Done" });
            Assert.True(aggregate.IsDeleted);
            Assert.Equal("Done", aggregate.RemovedReason);
            Assert.Equal(4, aggregate.AppliedCount);
            Assert.Equal(3UL, aggregate.LastEventNumber);
            Assert.Equal(nameof(TestAggregateRemoved), aggregate.LastEventName);

            //the stream is closed, nothing more can be appended
            _ = await Assert.ThrowsAnyAsync<Exception>(() => aggregate.Append(new TestAggregateAmountAdded() { Amount = 1 }));

            //a fresh instance that has replayed the deletion cannot append either
            var replayedDeleted = new TestAggregate(id, eventStore);
            Assert.True(await replayedDeleted.Rebuild());
            _ = await Assert.ThrowsAnyAsync<Exception>(() => replayedDeleted.Append(new TestAggregateAmountAdded() { Amount = 1 }));

            //the terminating event carries no data, a rebuild sees the deletion but has nothing to apply from it
            var rebuiltDeleted = new TestAggregate(id, eventStore);
            Assert.True(await rebuiltDeleted.Rebuild());
            Assert.True(rebuiltDeleted.IsCreated);
            Assert.True(rebuiltDeleted.IsDeleted);
            Assert.Equal("Second", rebuiltDeleted.Name);
            Assert.Equal(15, rebuiltDeleted.Amount);
            Assert.Equal(3, rebuiltDeleted.AppliedCount);
            Assert.Null(rebuiltDeleted.RemovedReason);
            Assert.Equal(3UL, rebuiltDeleted.LastEventNumber);
            Assert.Equal(nameof(TestAggregateRemoved), rebuiltDeleted.LastEventName);

            //each aggregate has its own stream
            var another = new TestAggregate(Guid.NewGuid(), eventStore);
            Assert.False(await another.Rebuild());
            await another.Append(new TestAggregateCreated() { Name = "Another", Amount = 1 });

            var rebuiltAnother = new TestAggregate(another.ID, eventStore);
            Assert.True(await rebuiltAnother.Rebuild());
            Assert.Equal("Another", rebuiltAnother.Name);
            Assert.Equal(1, rebuiltAnother.Amount);
            Assert.Equal(0UL, rebuiltAnother.LastEventNumber);

            //a different aggregate type takes the same event with its own method, and the same id is a different stream
            var otherType = new TestOtherAggregate(another.ID, eventStore);
            Assert.False(await otherType.Rebuild());
            await otherType.Append(new TestAggregateCreated() { Name = "Shared", Amount = 7 });

            var rebuiltOtherType = new TestOtherAggregate(another.ID, eventStore);
            Assert.True(await rebuiltOtherType.Rebuild());
            Assert.Equal("Other-Shared", rebuiltOtherType.Label);
            Assert.Equal(0UL, rebuiltOtherType.LastEventNumber);

            //the stream of the first aggregate is untouched
            var rebuiltAnotherAgain = new TestAggregate(another.ID, eventStore);
            Assert.True(await rebuiltAnotherAgain.Rebuild());
            Assert.Equal("Another", rebuiltAnotherAgain.Name);
            Assert.Equal(1, rebuiltAnotherAgain.Amount);
            Assert.Equal(0UL, rebuiltAnotherAgain.LastEventNumber);
        }

        /// <summary>
        /// Appending with event number validation enforces optimistic concurrency, so an append from a stale instance is rejected.
        /// </summary>
        public static async Task TestConcurrencyAsync<T>()
            where T : DataContext, new()
        {
            var eventStore = GetEventStore<T>();

            var id = Guid.NewGuid();

            //the first append validates that the stream does not exist yet
            var aggregate = new TestAggregate(id, eventStore);
            await aggregate.Append(new TestAggregateCreated() { Name = "First", Amount = 10 }, true);

            //a second create of the same aggregate is rejected
            var duplicate = new TestAggregate(id, eventStore);
            _ = await Assert.ThrowsAnyAsync<Exception>(() => duplicate.Append(new TestAggregateCreated() { Name = "Duplicate", Amount = 1 }, true));

            //a rejected append is not applied
            Assert.Null(duplicate.Name);
            Assert.Equal(0, duplicate.AppliedCount);
            Assert.False(duplicate.IsCreated);
            Assert.Null(duplicate.LastEventNumber);

            //an instance that has replayed the stream knows the event number it is appending after
            var current = new TestAggregate(id, eventStore);
            Assert.True(await current.Rebuild());
            await current.Append(new TestAggregateAmountAdded() { Amount = 5 }, true);
            Assert.Equal(15, current.Amount);
            Assert.Equal(1UL, current.LastEventNumber);

            //the append advanced the instance, so it can append again without a rebuild
            await current.Append(new TestAggregateAmountAdded() { Amount = 1 }, true);
            Assert.Equal(2UL, current.LastEventNumber);

            //a stale instance is still on the event before it, so its append is rejected
            var stale = new TestAggregate(id, eventStore);
            Assert.True(await stale.Rebuild(0));
            Assert.Equal(0UL, stale.LastEventNumber);
            _ = await Assert.ThrowsAnyAsync<Exception>(() => stale.Append(new TestAggregateAmountAdded() { Amount = 100 }, true));
            Assert.Equal(10, stale.Amount);
            Assert.Equal(1, stale.AppliedCount);
            Assert.Equal(0UL, stale.LastEventNumber);

            //catching up lets it append again
            Assert.True(await stale.Rebuild());
            Assert.Equal(2UL, stale.LastEventNumber);
            await stale.Append(new TestAggregateAmountAdded() { Amount = 100 }, true);
            Assert.Equal(116, stale.Amount);
            Assert.Equal(4, stale.AppliedCount);

            var final = new TestAggregate(id, eventStore);
            Assert.True(await final.Rebuild());
            Assert.Equal(116, final.Amount);
            Assert.Equal(3UL, final.LastEventNumber);

            //an event the aggregate has no apply method for is rejected before it is stored
            var noMethod = new TestOtherAggregate(Guid.NewGuid(), eventStore);
            _ = await Assert.ThrowsAnyAsync<Exception>(() => noMethod.Append(new TestAggregateAmountAdded() { Amount = 1 }));
            Assert.False(await new TestOtherAggregate(noMethod.ID, eventStore).Rebuild());
        }

        private static IEventStoreEngine GetEventStore<T>()
            where T : DataContext, new()
        {
            var context = new T();
            if (!context.TryGetEngine(out var engine))
                throw new Exception($"{typeof(T).Name} could not produce an engine");
            if (engine is not IEventStoreEngine eventStoreEngine)
                throw new Exception($"{typeof(T).Name} produced an engine of {engine.GetType().Name} which is not a {nameof(IEventStoreEngine)}");
            return eventStoreEngine;
        }
    }
}
