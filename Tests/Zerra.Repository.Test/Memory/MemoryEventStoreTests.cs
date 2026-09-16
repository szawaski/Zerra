// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test.Memory
{
    public class MemoryEventStoreTests
    {
        //streams are shared by every engine instance so each test uses its own stream name
        private static string NewStreamName() => $"Stream_{Guid.NewGuid():N}";

        [Fact]
        public void Append_NumbersFromZeroAndReadsBack()
        {
            var engine = new MemoryEngine();
            var streamName = NewStreamName();

            var eventID = Guid.NewGuid();
            Assert.Equal(0UL, engine.Append(eventID, "Created", streamName, null, EventStoreState.NotExisting, [1, 2, 3]));
            Assert.Equal(1UL, engine.Append(Guid.NewGuid(), "Updated", streamName, null, EventStoreState.Existing, [4, 5, 6]));
            Assert.Equal(2UL, engine.Append(Guid.NewGuid(), "Updated", streamName, 1, EventStoreState.Any, [7, 8, 9]));

            var eventDatas = engine.Read(streamName, null, null, null, null, null);

            Assert.Equal(3, eventDatas.Length);
            Assert.Equal(eventID, eventDatas[0].EventID);
            Assert.Equal("Created", eventDatas[0].EventName);
            Assert.Equal<byte>([1, 2, 3], eventDatas[0].Data.ToArray());
            Assert.False(eventDatas[0].Deleted);
            Assert.Equal([0UL, 1UL, 2UL], eventDatas.Select(x => x.Number));
        }

        [Fact]
        public async Task AppendAsync_NumbersFromZeroAndReadsBack()
        {
            var engine = new MemoryEngine();
            var streamName = NewStreamName();

            Assert.Equal(0UL, await engine.AppendAsync(Guid.NewGuid(), "Created", streamName, null, EventStoreState.NotExisting, [1]));
            Assert.Equal(1UL, await engine.AppendAsync(Guid.NewGuid(), "Updated", streamName, null, EventStoreState.Existing, [2]));

            var eventDatas = await engine.ReadAsync(streamName, null, null, null, null, null);

            Assert.Equal([0UL, 1UL], eventDatas.Select(x => x.Number));
        }

        [Fact]
        public void Read_MissingStream_IsEmpty()
        {
            var engine = new MemoryEngine();

            Assert.Empty(engine.Read(NewStreamName(), null, null, null, null, null));
            Assert.Empty(engine.ReadBackwards(NewStreamName(), null, null, null, null, null));
        }

        [Fact]
        public void Append_ExpectedStateNotMet_Throws()
        {
            var engine = new MemoryEngine();
            var streamName = NewStreamName();

            _ = Assert.Throws<InvalidOperationException>(() => engine.Append(Guid.NewGuid(), "Updated", streamName, null, EventStoreState.Existing, [1]));

            _ = engine.Append(Guid.NewGuid(), "Created", streamName, null, EventStoreState.NotExisting, [1]);

            _ = Assert.Throws<InvalidOperationException>(() => engine.Append(Guid.NewGuid(), "Created", streamName, null, EventStoreState.NotExisting, [2]));
        }

        [Fact]
        public void Append_ExpectedEventNumberNotMet_Throws()
        {
            var engine = new MemoryEngine();
            var streamName = NewStreamName();

            _ = engine.Append(Guid.NewGuid(), "Created", streamName, null, EventStoreState.NotExisting, [1]);

            //the stream is at 0, not 1
            _ = Assert.Throws<InvalidOperationException>(() => engine.Append(Guid.NewGuid(), "Updated", streamName, 1, EventStoreState.Any, [2]));

            Assert.Equal(1UL, engine.Append(Guid.NewGuid(), "Updated", streamName, 0, EventStoreState.Any, [2]));
        }

        [Fact]
        public void Terminate_MarksDeletedAndClosesTheStream()
        {
            var engine = new MemoryEngine();
            var streamName = NewStreamName();

            _ = engine.Append(Guid.NewGuid(), "Created", streamName, null, EventStoreState.NotExisting, [1]);
            Assert.Equal(1UL, engine.Terminate(Guid.NewGuid(), "Delete", streamName, null, EventStoreState.Existing));

            var eventDatas = engine.Read(streamName, null, null, null, null, null);

            Assert.Equal(2, eventDatas.Length);
            Assert.False(eventDatas[0].Deleted);
            Assert.True(eventDatas[1].Deleted);
            Assert.True(eventDatas[1].Data.IsEmpty);

            _ = Assert.Throws<InvalidOperationException>(() => engine.Append(Guid.NewGuid(), "Updated", streamName, null, EventStoreState.Any, [2]));
            _ = Assert.Throws<InvalidOperationException>(() => engine.Terminate(Guid.NewGuid(), "Delete", streamName, null, EventStoreState.Any));
        }

        [Fact]
        public async Task TerminateAsync_MarksDeletedAndClosesTheStream()
        {
            var engine = new MemoryEngine();
            var streamName = NewStreamName();

            _ = await engine.AppendAsync(Guid.NewGuid(), "Created", streamName, null, EventStoreState.NotExisting, [1]);
            Assert.Equal(1UL, await engine.TerminateAsync(Guid.NewGuid(), "Delete", streamName, null, EventStoreState.Existing));

            var eventDatas = await engine.ReadAsync(streamName, null, null, null, null, null);

            Assert.True(eventDatas[1].Deleted);
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => engine.AppendAsync(Guid.NewGuid(), "Updated", streamName, null, EventStoreState.Any, [2]));
        }

        [Fact]
        public void Read_FiltersByNumber()
        {
            var engine = new MemoryEngine();
            var streamName = NewStreamName();
            for (var i = 0; i < 5; i++)
                _ = engine.Append(Guid.NewGuid(), "Updated", streamName, null, EventStoreState.Any, [(byte)i]);

            Assert.Equal([2UL, 3UL, 4UL], engine.Read(streamName, 2, null, null, null, null).Select(x => x.Number));
            Assert.Equal([0UL, 1UL, 2UL], engine.Read(streamName, null, null, 2, null, null).Select(x => x.Number));
            Assert.Equal([1UL, 2UL], engine.Read(streamName, 1, 2, null, null, null).Select(x => x.Number));
            Assert.Empty(engine.Read(streamName, 5, null, null, null, null));
        }

        [Fact]
        public void ReadBackwards_FiltersByNumber()
        {
            var engine = new MemoryEngine();
            var streamName = NewStreamName();
            for (var i = 0; i < 5; i++)
                _ = engine.Append(Guid.NewGuid(), "Updated", streamName, null, EventStoreState.Any, [(byte)i]);

            Assert.Equal([4UL, 3UL, 2UL, 1UL, 0UL], engine.ReadBackwards(streamName, null, null, null, null, null).Select(x => x.Number));
            Assert.Equal([2UL, 1UL, 0UL], engine.ReadBackwards(streamName, 2, null, null, null, null).Select(x => x.Number));
            Assert.Equal([4UL, 3UL, 2UL], engine.ReadBackwards(streamName, null, null, 2, null, null).Select(x => x.Number));
            Assert.Equal([3UL], engine.ReadBackwards(streamName, 3, 1, null, null, null).Select(x => x.Number));
        }

        [Fact]
        public async Task Read_FiltersByDate()
        {
            var engine = new MemoryEngine();
            var streamName = NewStreamName();

            _ = engine.Append(Guid.NewGuid(), "Created", streamName, null, EventStoreState.NotExisting, [0]);
            await Task.Delay(20, TestContext.Current.CancellationToken);
            var middle = DateTime.UtcNow;
            await Task.Delay(20, TestContext.Current.CancellationToken);
            _ = engine.Append(Guid.NewGuid(), "Updated", streamName, null, EventStoreState.Existing, [1]);

            Assert.Equal([0UL], engine.Read(streamName, null, null, null, null, middle).Select(x => x.Number));
            Assert.Equal([1UL], engine.Read(streamName, null, null, null, middle, null).Select(x => x.Number));
            Assert.Equal([1UL], engine.ReadBackwards(streamName, null, null, null, middle, null).Select(x => x.Number));
            Assert.Equal([0UL], engine.ReadBackwards(streamName, null, null, null, null, middle).Select(x => x.Number));
        }

        [Fact]
        public void Read_DoesNotHandOutTheStoredEvents()
        {
            var engine = new MemoryEngine();
            var streamName = NewStreamName();
            _ = engine.Append(Guid.NewGuid(), "Created", streamName, null, EventStoreState.NotExisting, [1]);

            var eventData = engine.Read(streamName, null, null, null, null, null)[0];
            eventData.EventName = "Changed";

            Assert.Equal("Created", engine.Read(streamName, null, null, null, null, null)[0].EventName);
        }

        [Fact]
        public void Engine_IsAnEventStoreEngineFromTheDataContext()
        {
            var context = new MemoryTestDataContext();

            Assert.True(context.TryGetEngine(out var engine));
            _ = Assert.IsAssignableFrom<IEventStoreEngine>(engine);
        }
    }
}
