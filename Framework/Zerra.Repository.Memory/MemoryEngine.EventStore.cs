// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;

namespace Zerra.Repository.Memory
{
    public sealed partial class MemoryEngine
    {
        //per instance, like the rows in MemoryEngine.TransactStore.cs: two engines never see each other's streams
        private readonly ConcurrentFactoryDictionary<string, MemoryEventStream> streams = new();

        //streams are append only so an event number is always the index of that event in the list
        private sealed class MemoryEventStream
        {
            public readonly List<EventStoreEventData> Events = new();
            public bool Terminated;
        }

        /// <inheritdoc/>
        public ulong Append(Guid eventID, string eventName, string streamName, ulong? expectedEventNumber, EventStoreState expectedState, byte[] data)
        {
            var stream = streams.GetOrAdd(streamName, static () => new MemoryEventStream());
            lock (stream)
            {
                if (stream.Terminated)
                    throw new InvalidOperationException($"Stream {streamName} has been terminated");

                if (expectedEventNumber.HasValue)
                {
                    if (stream.Events.Count == 0 || stream.Events[^1].Number != expectedEventNumber.Value)
                        throw new InvalidOperationException($"Stream {streamName} was not at the expected event number {expectedEventNumber.Value}");
                }
                else
                {
                    switch (expectedState)
                    {
                        case EventStoreState.Any:
                            break;
                        case EventStoreState.NotExisting:
                            if (stream.Events.Count > 0)
                                throw new InvalidOperationException($"Stream {streamName} already exists");
                            break;
                        case EventStoreState.Existing:
                            if (stream.Events.Count == 0)
                                throw new InvalidOperationException($"Stream {streamName} does not exist");
                            break;
                        default: throw new NotImplementedException();
                    }
                }

                var number = (ulong)stream.Events.Count;
                stream.Events.Add(new EventStoreEventData()
                {
                    EventID = eventID,
                    EventName = eventName,
                    Data = data,
                    Deleted = false,
                    Date = DateTime.UtcNow,
                    Number = number
                });
                return number;
            }
        }
        /// <inheritdoc/>
        public ulong Terminate(Guid eventID, string eventName, string streamName, ulong? expectedEventNumber, EventStoreState expectedState)
        {
            var stream = streams.GetOrAdd(streamName, static () => new MemoryEventStream());
            lock (stream)
            {
                if (stream.Terminated)
                    throw new InvalidOperationException($"Stream {streamName} has been terminated");

                if (expectedEventNumber.HasValue)
                {
                    if (stream.Events.Count == 0 || stream.Events[^1].Number != expectedEventNumber.Value)
                        throw new InvalidOperationException($"Stream {streamName} was not at the expected event number {expectedEventNumber.Value}");
                }
                else
                {
                    switch (expectedState)
                    {
                        case EventStoreState.Any:
                            break;
                        case EventStoreState.NotExisting:
                            if (stream.Events.Count > 0)
                                throw new InvalidOperationException($"Stream {streamName} already exists");
                            break;
                        case EventStoreState.Existing:
                            if (stream.Events.Count == 0)
                                throw new InvalidOperationException($"Stream {streamName} does not exist");
                            break;
                        default: throw new NotImplementedException();
                    }
                }

                //the marker has no data which flags it deleted, no more events may be appended after it
                var number = (ulong)stream.Events.Count;
                stream.Events.Add(new EventStoreEventData()
                {
                    EventID = eventID,
                    EventName = eventName,
                    Data = default,
                    Deleted = true,
                    Date = DateTime.UtcNow,
                    Number = number
                });
                stream.Terminated = true;
                return number;
            }
        }
        /// <inheritdoc/>
        public EventStoreEventData[] Read(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate)
        {
            if (!streams.TryGetValue(streamName, out var stream))
                return Array.Empty<EventStoreEventData>();

            var models = new List<EventStoreEventData>();
            lock (stream)
            {
                var index = 0;
                if (startEventNumber.HasValue)
                {
                    if (startEventNumber.Value >= (ulong)stream.Events.Count)
                        return Array.Empty<EventStoreEventData>();
                    index = (int)startEventNumber.Value;
                }

                for (; index < stream.Events.Count; index++)
                {
                    var streamEvent = stream.Events[index];

                    if (endEventNumber.HasValue && streamEvent.Number > endEventNumber.Value)
                        break;

                    if (endEventDate.HasValue && streamEvent.Date > endEventDate.Value)
                        break;

                    if (startEventDate.HasValue && streamEvent.Date < startEventDate.Value)
                        continue;

                    models.Add(new EventStoreEventData()
                    {
                        EventID = streamEvent.EventID,
                        EventName = streamEvent.EventName,
                        Data = streamEvent.Data,
                        Deleted = streamEvent.Deleted,
                        Date = streamEvent.Date,
                        Number = streamEvent.Number
                    });

                    if (eventCount.HasValue && models.Count == eventCount.Value)
                        break;
                }
            }

            return models.ToArray();
        }
        /// <inheritdoc/>
        public EventStoreEventData[] ReadBackwards(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate)
        {
            if (!streams.TryGetValue(streamName, out var stream))
                return Array.Empty<EventStoreEventData>();

            var models = new List<EventStoreEventData>();
            lock (stream)
            {
                if (stream.Events.Count == 0)
                    return Array.Empty<EventStoreEventData>();

                var index = stream.Events.Count - 1;
                if (startEventNumber.HasValue && startEventNumber.Value < (ulong)index)
                    index = (int)startEventNumber.Value;

                for (; index >= 0; index--)
                {
                    var streamEvent = stream.Events[index];

                    if (endEventNumber.HasValue && streamEvent.Number < endEventNumber.Value)
                        break;

                    if (startEventDate.HasValue && streamEvent.Date < startEventDate.Value)
                        break;

                    if (endEventDate.HasValue && streamEvent.Date > endEventDate.Value)
                        continue;

                    models.Add(new EventStoreEventData()
                    {
                        EventID = streamEvent.EventID,
                        EventName = streamEvent.EventName,
                        Data = streamEvent.Data,
                        Deleted = streamEvent.Deleted,
                        Date = streamEvent.Date,
                        Number = streamEvent.Number
                    });

                    if (eventCount.HasValue && models.Count == eventCount.Value)
                        break;
                }
            }

            return models.ToArray();
        }

        /// <inheritdoc/>
        public Task<ulong> AppendAsync(Guid eventID, string eventName, string streamName, ulong? expectedEventNumber, EventStoreState expectedState, byte[] data)
        {
            var stream = streams.GetOrAdd(streamName, static () => new MemoryEventStream());
            lock (stream)
            {
                if (stream.Terminated)
                    throw new InvalidOperationException($"Stream {streamName} has been terminated");

                if (expectedEventNumber.HasValue)
                {
                    if (stream.Events.Count == 0 || stream.Events[^1].Number != expectedEventNumber.Value)
                        throw new InvalidOperationException($"Stream {streamName} was not at the expected event number {expectedEventNumber.Value}");
                }
                else
                {
                    switch (expectedState)
                    {
                        case EventStoreState.Any:
                            break;
                        case EventStoreState.NotExisting:
                            if (stream.Events.Count > 0)
                                throw new InvalidOperationException($"Stream {streamName} already exists");
                            break;
                        case EventStoreState.Existing:
                            if (stream.Events.Count == 0)
                                throw new InvalidOperationException($"Stream {streamName} does not exist");
                            break;
                        default: throw new NotImplementedException();
                    }
                }

                var number = (ulong)stream.Events.Count;
                stream.Events.Add(new EventStoreEventData()
                {
                    EventID = eventID,
                    EventName = eventName,
                    Data = data,
                    Deleted = false,
                    Date = DateTime.UtcNow,
                    Number = number
                });
                return Task.FromResult(number);
            }
        }
        /// <inheritdoc/>
        public Task<ulong> TerminateAsync(Guid eventID, string eventName, string streamName, ulong? expectedEventNumber, EventStoreState expectedState)
        {
            var stream = streams.GetOrAdd(streamName, static () => new MemoryEventStream());
            lock (stream)
            {
                if (stream.Terminated)
                    throw new InvalidOperationException($"Stream {streamName} has been terminated");

                if (expectedEventNumber.HasValue)
                {
                    if (stream.Events.Count == 0 || stream.Events[^1].Number != expectedEventNumber.Value)
                        throw new InvalidOperationException($"Stream {streamName} was not at the expected event number {expectedEventNumber.Value}");
                }
                else
                {
                    switch (expectedState)
                    {
                        case EventStoreState.Any:
                            break;
                        case EventStoreState.NotExisting:
                            if (stream.Events.Count > 0)
                                throw new InvalidOperationException($"Stream {streamName} already exists");
                            break;
                        case EventStoreState.Existing:
                            if (stream.Events.Count == 0)
                                throw new InvalidOperationException($"Stream {streamName} does not exist");
                            break;
                        default: throw new NotImplementedException();
                    }
                }

                //the marker has no data which flags it deleted, no more events may be appended after it
                var number = (ulong)stream.Events.Count;
                stream.Events.Add(new EventStoreEventData()
                {
                    EventID = eventID,
                    EventName = eventName,
                    Data = default,
                    Deleted = true,
                    Date = DateTime.UtcNow,
                    Number = number
                });
                stream.Terminated = true;
                return Task.FromResult(number);
            }
        }
        /// <inheritdoc/>
        public Task<EventStoreEventData[]> ReadAsync(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate)
        {
            if (!streams.TryGetValue(streamName, out var stream))
                return Task.FromResult(Array.Empty<EventStoreEventData>());

            var models = new List<EventStoreEventData>();
            lock (stream)
            {
                var index = 0;
                if (startEventNumber.HasValue)
                {
                    if (startEventNumber.Value >= (ulong)stream.Events.Count)
                        return Task.FromResult(Array.Empty<EventStoreEventData>());
                    index = (int)startEventNumber.Value;
                }

                for (; index < stream.Events.Count; index++)
                {
                    var streamEvent = stream.Events[index];

                    if (endEventNumber.HasValue && streamEvent.Number > endEventNumber.Value)
                        break;

                    if (endEventDate.HasValue && streamEvent.Date > endEventDate.Value)
                        break;

                    if (startEventDate.HasValue && streamEvent.Date < startEventDate.Value)
                        continue;

                    models.Add(new EventStoreEventData()
                    {
                        EventID = streamEvent.EventID,
                        EventName = streamEvent.EventName,
                        Data = streamEvent.Data,
                        Deleted = streamEvent.Deleted,
                        Date = streamEvent.Date,
                        Number = streamEvent.Number
                    });

                    if (eventCount.HasValue && models.Count == eventCount.Value)
                        break;
                }
            }

            return Task.FromResult(models.ToArray());
        }
        /// <inheritdoc/>
        public Task<EventStoreEventData[]> ReadBackwardsAsync(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate)
        {
            if (!streams.TryGetValue(streamName, out var stream))
                return Task.FromResult(Array.Empty<EventStoreEventData>());

            var models = new List<EventStoreEventData>();
            lock (stream)
            {
                if (stream.Events.Count == 0)
                    return Task.FromResult(Array.Empty<EventStoreEventData>());

                var index = stream.Events.Count - 1;
                if (startEventNumber.HasValue && startEventNumber.Value < (ulong)index)
                    index = (int)startEventNumber.Value;

                for (; index >= 0; index--)
                {
                    var streamEvent = stream.Events[index];

                    if (endEventNumber.HasValue && streamEvent.Number < endEventNumber.Value)
                        break;

                    if (startEventDate.HasValue && streamEvent.Date < startEventDate.Value)
                        break;

                    if (endEventDate.HasValue && streamEvent.Date > endEventDate.Value)
                        continue;

                    models.Add(new EventStoreEventData()
                    {
                        EventID = streamEvent.EventID,
                        EventName = streamEvent.EventName,
                        Data = streamEvent.Data,
                        Deleted = streamEvent.Deleted,
                        Date = streamEvent.Date,
                        Number = streamEvent.Number
                    });

                    if (eventCount.HasValue && models.Count == eventCount.Value)
                        break;
                }
            }

            return Task.FromResult(models.ToArray());
        }
    }
}
