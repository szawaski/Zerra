// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Repository.Reflection;
using Zerra.Logging;
using KurrentDB.Client;

namespace Zerra.Repository.KurrentDB
{
    /// <summary>
    /// KurrentDB implementation of the event store engine that provides event sourcing capabilities.
    /// </summary>
    public sealed class KurrentDBEngine : IEventStoreEngine, IDisposable
    {
        private const int maxPerQuery = 25;
        private const int saveStateEvery = 100;

        private readonly KurrentDBClient client;
        /// <summary>
        /// Initializes a new instance of the <see cref="KurrentDBEngine"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string for the KurrentDB instance.</param>
        /// <param name="insecure">Whether to use an insecure connection (without TLS/SSL).</param>
        public KurrentDBEngine(string connectionString, bool insecure)
        {
            var settings = new KurrentDBClientSettings
            {
                ConnectivitySettings =
                {
                    Address = new Uri(connectionString),
                    Insecure = insecure
                }
            };
            client = new KurrentDBClient(settings);
        }

        /// <inheritdoc/>
        public ulong Append(Guid eventID, string eventName, string streamName, ulong? expectedEventNumber, EventStoreState expectedState, byte[] data)
        {
            throw new NotSupportedException($"{nameof(KurrentDBEngine)} does not support synchronous operations");
        }
        /// <inheritdoc/>
        public ulong Terminate(Guid eventID, string eventName, string streamName, ulong? expectedEventNumber, EventStoreState expectedState)
        {
            throw new NotSupportedException($"{nameof(KurrentDBEngine)} does not support synchronous operations");
        }
        /// <inheritdoc/>
        public EventStoreEventData[] Read(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate)
        {
            throw new NotSupportedException($"{nameof(KurrentDBEngine)} does not support synchronous operations");
        }
        /// <inheritdoc/>
        public EventStoreEventData[] ReadBackwards(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate)
        {
            throw new NotSupportedException($"{nameof(KurrentDBEngine)} does not support synchronous operations");
        }

        /// <inheritdoc/>
        public async Task<ulong> AppendAsync(Guid eventID, string eventName, string streamName, ulong? expectedEventNumber, EventStoreState expectedState, byte[] data)
        {
            var eventData = new EventData(Uuid.FromGuid(eventID), eventName, data);

            if (expectedEventNumber.HasValue)
            {
                var revision = (StreamState)expectedEventNumber.Value;

                var writeResult = await client.AppendToStreamAsync(streamName, revision, [eventData]);
                return (ulong)writeResult.NextExpectedStreamState.ToInt64();
            }
            else
            {
                var state = expectedState switch
                {
                    EventStoreState.Any => StreamState.Any,
                    EventStoreState.NotExisting => StreamState.NoStream,
                    EventStoreState.Existing => StreamState.StreamExists,
                    _ => throw new NotImplementedException(),
                };
                var writeResult = await client.AppendToStreamAsync(streamName, state, [eventData]);
                return (ulong)writeResult.NextExpectedStreamState.ToInt64();
            }
        }
        /// <inheritdoc/>
        public async Task<ulong> TerminateAsync(Guid eventID, string eventName, string streamName, ulong? expectedEventNumber, EventStoreState expectedState)
        {
            var eventData = new EventData(Uuid.FromGuid(eventID), "Delete", null, null);

            if (expectedEventNumber.HasValue)
            {
                var revision = (StreamState)expectedEventNumber.Value;

                var writeResult = await client.AppendToStreamAsync(streamName, revision, [eventData]);
                _ = await client.TombstoneAsync(streamName, revision);
                return (ulong)writeResult.NextExpectedStreamState.ToInt64();

            }
            else
            {
                var state = expectedState switch
                {
                    EventStoreState.Any => StreamState.Any,
                    EventStoreState.NotExisting => StreamState.NoStream,
                    EventStoreState.Existing => StreamState.StreamExists,
                    _ => throw new NotImplementedException(),
                };
                var writeResult = await client.AppendToStreamAsync(streamName, state, [eventData]);
                _ = await client.TombstoneAsync(streamName, state);
                return (ulong)writeResult.NextExpectedStreamState.ToInt64();
            }
        }
        /// <inheritdoc/>
        public async Task<EventStoreEventData[]> ReadAsync(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate)
        {
            var models = new List<EventStoreEventData>();

            var streamStart = startEventNumber.HasValue ? new StreamPosition(startEventNumber.Value) : StreamPosition.Start;
            var remaining = eventCount ?? (long)(endEventNumber.HasValue ? endEventNumber.Value - (startEventNumber ?? 0) + 1 : maxPerQuery);
            var streamCount = endEventDate.HasValue ? maxPerQuery : (remaining <= maxPerQuery ? (int)remaining : maxPerQuery);

            if (streamCount == 0)
                return Array.Empty<EventStoreEventData>();

            var currentDate = DateTime.MinValue;
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
        /// <inheritdoc/>
        public async Task<EventStoreEventData[]> ReadBackwardsAsync(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate)
        {
            var models = new List<EventStoreEventData>();

            var remaining = eventCount ?? maxPerQuery;
            var streamStart = startEventNumber.HasValue ? new StreamPosition(startEventNumber.Value) : StreamPosition.End;
            var streamCount = endEventDate.HasValue ? maxPerQuery : (remaining <= maxPerQuery ? (int)remaining : maxPerQuery);

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

        /// <inheritdoc/>
        public void Dispose()
        {
            client.Dispose();
        }

        /// <inheritdoc/>
        public bool ValidateDataSource()
        {
            var eventData = new EventData(Uuid.NewUuid(), "ValidateDataSource", Array.Empty<byte>());

            try
            {
                Task.Run(() => client.AppendToStreamAsync("ValidateDataSource", StreamState.Any, [eventData])).GetAwaiter().GetResult();

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"{nameof(KurrentDBEngine)} failed to validate", ex);
            }
            return false;
        }

        /// <inheritdoc/>
        public IDataStoreGenerationPlan BuildStoreGenerationPlan(bool create, bool update, bool delete, ICollection<ModelDetail> modelDetail)
        {
            return new EmptyDataStoreGenerationPlan();
        }
    }
}
