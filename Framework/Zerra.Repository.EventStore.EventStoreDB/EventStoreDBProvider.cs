// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using EventStore.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Zerra.Repository.EventStore.EventStoreDB
{
    public class EventStoreDBProvider : IEventStoreEngine, IDisposable
    {
        private const int maxPerQuery = 25;
        public static int SaveStateEvery => 100;

        private readonly EventStoreClient client;
        public EventStoreDBProvider(string connectionString, bool insecure)
        {
            var settings = new EventStoreClientSettings
            {
                ConnectivitySettings =
                {
                    Address = new Uri(connectionString),
                    Insecure = insecure
                }
            };
            client = new EventStoreClient(settings);
        }

        public ulong Append(EventStoreAppend eventStoreAppend)
        {
            throw new NotSupportedException($"{nameof(EventStoreDBProvider)} does not support synchronous operations");
        }
        public ulong Terminate(EventStoreTerminate eventStoreTerminate)
        {
            throw new NotSupportedException($"{nameof(EventStoreDBProvider)} does not support synchronous operations");
        }
        public EventStoreEventData[] Read(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate)
        {
            throw new NotSupportedException($"{nameof(EventStoreDBProvider)} does not support synchronous operations");
        }
        public EventStoreEventData[] ReadBackwards(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate)
        {
            throw new NotSupportedException($"{nameof(EventStoreDBProvider)} does not support synchronous operations");
        }

        public async Task<ulong> AppendAsync(EventStoreAppend eventStoreAppend)
        {
            var eventData = new EventData(Uuid.FromGuid(eventStoreAppend.EventID), eventStoreAppend.EventName, eventStoreAppend.Data);

            if (eventStoreAppend.ExpectedEventNumber.HasValue)
            {
                var revision = new StreamRevision(eventStoreAppend.ExpectedEventNumber.Value);

                var writeResult = await client.AppendToStreamAsync(eventStoreAppend.StreamName, revision, new EventData[] { eventData });
                return writeResult.NextExpectedStreamRevision;
            }
            else
            {
                StreamState state;
                switch(eventStoreAppend.ExpectedState)
                {
                    case EventStoreState.Any: state = StreamState.Any; break;
                    case EventStoreState.NotExisting: state = StreamState.NoStream; break;
                    case EventStoreState.Existing: state = StreamState.StreamExists; break;
                    default: throw new NotImplementedException();
                }
 
                var writeResult = await client.AppendToStreamAsync(eventStoreAppend.StreamName, state, new EventData[] { eventData });
                return writeResult.NextExpectedStreamRevision;
            }
        }
        public async Task<ulong> TerminateAsync(EventStoreTerminate eventStoreTerminate)
        {
            var eventData = new EventData(Uuid.FromGuid(eventStoreTerminate.EventID), "Delete", null, null);

            if (eventStoreTerminate.ExpectedEventNumber.HasValue)
            {
                var revision = new StreamRevision(eventStoreTerminate.ExpectedEventNumber.Value);

                var writeResult = await client.AppendToStreamAsync(eventStoreTerminate.StreamName, revision, new EventData[] { eventData });
                await client.TombstoneAsync(eventStoreTerminate.StreamName, revision);
                return writeResult.NextExpectedStreamRevision;

            }
            else
            {
                StreamState state;
                switch (eventStoreTerminate.ExpectedState)
                {
                    case EventStoreState.Any: state = StreamState.Any; break;
                    case EventStoreState.NotExisting: state = StreamState.NoStream; break;
                    case EventStoreState.Existing: state = StreamState.StreamExists; break;
                    default: throw new NotImplementedException();
                }

                var writeResult = await client.AppendToStreamAsync(eventStoreTerminate.StreamName, state, new EventData[] { eventData });
                await client.TombstoneAsync(eventStoreTerminate.StreamName, state);
                return writeResult.NextExpectedStreamRevision;
            }
        }
        public async Task<EventStoreEventData[]> ReadAsync(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate)
        {
            var models = new List<EventStoreEventData>();

            var streamStart = startEventNumber.HasValue ? new StreamPosition(startEventNumber.Value) : StreamPosition.Start;
            long remaining = eventCount ?? (long)(endEventNumber.HasValue ? endEventNumber.Value - (startEventNumber ?? 0) + 1 : maxPerQuery);
            int streamCount = endEventDate.HasValue ? maxPerQuery : (remaining <= maxPerQuery ? (int)remaining : maxPerQuery);

            if (streamCount == 0)
                return Array.Empty<EventStoreEventData>();

            DateTime currentDate = DateTime.MinValue;
            ulong currentEventNumber = 0;

            for (; ; )
            {
                ResolvedEvent[] streamEvents;
                try
                {
                    streamEvents = await client.ReadStreamAsync(Direction.Forwards, streamName, streamStart, streamCount).ToArrayAsync();
                }
                catch (StreamNotFoundException)
                {
                    return Array.Empty<EventStoreEventData>();
                }

                if (streamEvents.Length == 0)
                    break;

                foreach (var streamEvent in streamEvents)
                {
                    currentDate = streamEvent.Event.Created;
                    currentEventNumber = streamEvent.Event.EventNumber.ToUInt64();

                    if (endEventNumber.HasValue && currentEventNumber > endEventNumber.Value)
                        break;

                    if (endEventDate.HasValue && currentDate > endEventDate.Value)
                        break;

                    if (!startEventDate.HasValue || currentDate >= startEventDate.Value)
                    {
                        var model = new EventStoreEventData
                        {
                            EventName = streamEvent.Event.EventType,
                            EventID = streamEvent.Event.EventId.ToGuid(),
                            Data = streamEvent.Event.Data,
                            Deleted = streamEvent.Event.Data.IsEmpty,
                            Date = streamEvent.Event.Created,
                            Number = streamEvent.Event.EventNumber
                        };
                        models.Add(model);

                        if (eventCount.HasValue && models.Count == eventCount.Value)
                            break;

                        if (eventCount.HasValue)
                            remaining--;
                    }

                    streamStart = streamEvent.Event.EventNumber + 1;
                }

                if (endEventNumber.HasValue && currentEventNumber >= endEventNumber.Value)
                    break;

                if (endEventDate.HasValue && currentDate >= endEventDate.Value)
                    break;

                if (eventCount.HasValue && models.Count >= eventCount.Value)
                    break;

                if (streamEvents.Length < maxPerQuery)
                    break;

                streamCount = endEventDate.HasValue ? maxPerQuery : (remaining <= maxPerQuery ? (int)remaining : maxPerQuery);
            }

            return models.ToArray();
        }
        public async Task<EventStoreEventData[]> ReadBackwardsAsync(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate)
        {
            var models = new List<EventStoreEventData>();

            long remaining = eventCount ?? maxPerQuery;
            var streamStart = startEventNumber.HasValue ? new StreamPosition(startEventNumber.Value) : StreamPosition.End;
            int streamCount = endEventDate.HasValue ? maxPerQuery : (remaining <= maxPerQuery ? (int)remaining : maxPerQuery);

            DateTime currentDate = default;
            ulong currentEventNumber = default;

            for (; ; )
            {
                ResolvedEvent[] streamEvents;
                try
                {
                    streamEvents = await client.ReadStreamAsync(Direction.Backwards, streamName, streamStart, streamCount).ToArrayAsync();
                }
                catch (StreamNotFoundException)
                {
                    return Array.Empty<EventStoreEventData>();
                }

                if (streamEvents.Length == 0)
                    break;

                foreach (var streamEvent in streamEvents)
                {
                    currentDate = streamEvent.Event.Created;
                    currentEventNumber = streamEvent.Event.EventNumber;

                    if (endEventNumber.HasValue && currentEventNumber < endEventNumber.Value)
                        break;

                    if (startEventDate.HasValue && currentDate < startEventDate.Value)
                        break;

                    if (!endEventDate.HasValue || currentDate <= endEventDate.Value)
                    {
                        var model = new EventStoreEventData
                        {
                            EventName = streamEvent.Event.EventType,
                            EventID = streamEvent.Event.EventId.ToGuid(),
                            Data = streamEvent.Event.Data,
                            Deleted = streamEvent.Event.Data.IsEmpty,
                            Date = streamEvent.Event.Created,
                            Number = streamEvent.Event.EventNumber
                        };
                        models.Add(model);

                        if (eventCount.HasValue && models.Count == eventCount.Value)
                            break;

                        if (eventCount.HasValue)
                            remaining--;
                    }

                    streamStart = streamEvent.Event.EventNumber - 1;
                }

                if (endEventNumber.HasValue && currentEventNumber < endEventNumber.Value)
                    break;

                if (startEventDate.HasValue && currentDate < startEventDate.Value)
                    break;

                if (eventCount.HasValue && models.Count == eventCount.Value)
                    break;

                if (streamEvents.Length < maxPerQuery)
                    break;

                streamCount = endEventDate.HasValue ? maxPerQuery : (remaining <= maxPerQuery ? (int)remaining : maxPerQuery);
            }

            return models.ToArray();
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
