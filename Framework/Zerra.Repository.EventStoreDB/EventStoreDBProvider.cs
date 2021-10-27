﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using EventStore.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zerra.Repository.EventStore;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.EventStoreDB
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

        public ulong Append(Guid eventID, string eventName, string streamName, ulong? expectedEventNumber, EventStoreState expectedState, byte[] data)
        {
            throw new NotSupportedException($"{nameof(EventStoreDBProvider)} does not support synchronous operations");
        }
        public ulong Terminate(Guid eventID, string eventName, string streamName, ulong? expectedEventNumber, EventStoreState expectedState)
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

        public async Task<ulong> AppendAsync(Guid eventID, string eventName, string streamName, ulong? expectedEventNumber, EventStoreState expectedState, byte[] data)
        {
            var eventData = new EventData(Uuid.FromGuid(eventID), eventName, data);

            if (expectedEventNumber.HasValue)
            {
                var revision = new StreamRevision(expectedEventNumber.Value);

                var writeResult = await client.AppendToStreamAsync(streamName, revision, new EventData[] { eventData });
                return writeResult.NextExpectedStreamRevision;
            }
            else
            {
                StreamState state;
                switch(expectedState)
                {
                    case EventStoreState.Any: state = StreamState.Any; break;
                    case EventStoreState.NotExisting: state = StreamState.NoStream; break;
                    case EventStoreState.Existing: state = StreamState.StreamExists; break;
                    default: throw new NotImplementedException();
                }
 
                var writeResult = await client.AppendToStreamAsync(streamName, state, new EventData[] { eventData });
                return writeResult.NextExpectedStreamRevision;
            }
        }
        public async Task<ulong> TerminateAsync(Guid eventID, string eventName, string streamName, ulong? expectedEventNumber, EventStoreState expectedState)
        {
            var eventData = new EventData(Uuid.FromGuid(eventID), "Delete", null, null);

            if (expectedEventNumber.HasValue)
            {
                var revision = new StreamRevision(expectedEventNumber.Value);

                var writeResult = await client.AppendToStreamAsync(streamName, revision, new EventData[] { eventData });
                await client.TombstoneAsync(streamName, revision);
                return writeResult.NextExpectedStreamRevision;

            }
            else
            {
                StreamState state;
                switch (expectedState)
                {
                    case EventStoreState.Any: state = StreamState.Any; break;
                    case EventStoreState.NotExisting: state = StreamState.NoStream; break;
                    case EventStoreState.Existing: state = StreamState.StreamExists; break;
                    default: throw new NotImplementedException();
                }

                var writeResult = await client.AppendToStreamAsync(streamName, state, new EventData[] { eventData });
                await client.TombstoneAsync(streamName, state);
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

        public bool DetectIsDataSource()
        {
            var eventData = new EventData(Uuid.NewUuid(), "DetectIsDataSource", Array.Empty<byte>());

            try
            {
                client.AppendToStreamAsync("DetectIsDataSource", StreamState.Any, new EventData[] { eventData })
                    .GetAwaiter().GetResult();

                return true;
            }
            catch { }
            return false;
        }

        public void BuildStoreFromModels(ICollection<ModelDetail> modelDetail)
        {
            //nada
        }
    }
}
