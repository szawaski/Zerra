// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;
using Zerra.CQRS;
using Zerra.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Base class for event-sourced aggregate roots. Manages event appending, deletion, and state rebuilding from an event store.
    /// </summary>
    public abstract class AggregateRoot
    {
        private static Type? typeCache = null;
        private static readonly Lock typeCacheLock = new();
        private Type GetAggregateType()
        {
            if (typeCache is null)
            {
                lock (typeCacheLock)
                {
                    typeCache ??= this.GetType();
                }
            }
            return typeCache;
        }

        /// <summary>
        /// Gets or sets the unique identifier of the aggregate.
        /// </summary>
        public Guid ID { get; set; }
        /// <summary>
        /// Gets the event number of the last applied event, or <see langword="null"/> if no events have been applied.
        /// </summary>
        public ulong? LastEventNumber { get; private set; }
        /// <summary>
        /// Gets the date of the last applied event, or <see langword="null"/> if no events have been applied.
        /// </summary>
        public DateTime? LastEventDate { get; private set; }
        /// <summary>
        /// Gets the name of the last applied event, or <see langword="null"/> if no events have been applied.
        /// </summary>
        public string? LastEventName { get; private set; }
        /// <summary>
        /// Gets a value indicating whether the aggregate has been created by replaying at least one event.
        /// </summary>
        public bool IsCreated { get; private set; }
        /// <summary>
        /// Gets a value indicating whether the aggregate has been deleted via a terminating event.
        /// </summary>
        public bool IsDeleted { get; private set; }

        private readonly string streamName;
        private readonly IEventStoreEngine eventStore;

        /// <summary>
        /// Initializes a new instance of <see cref="AggregateRoot"/> with the specified identifier and event store engine.
        /// </summary>
        /// <param name="id">The unique identifier of the aggregate.</param>
        /// <param name="eventStore">The event store engine used to persist and read events.</param>
        protected AggregateRoot(Guid id, IEventStoreEngine eventStore)
        {
            this.eventStore = eventStore;
            this.ID = id;
            this.streamName = $"{GetAggregateType().FullName}-{id}";
        }

        /// <summary>
        /// Applies and persists an event to the aggregate, then dispatches it on the bus.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="event">The event to append.</param>
        /// <param name="validateEventNumber">When <see langword="true"/>, enforces optimistic concurrency by validating the expected event number.</param>
        public async Task Append<TEvent>(TEvent @event, bool validateEventNumber = false) where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            var eventName = eventType.Name;

            await ApplyEvent(@event, eventType);

            var eventBytes = EventStoreCommon.Serialize(@event);
            _ = await this.eventStore.AppendAsync(Guid.NewGuid(), eventName, streamName, validateEventNumber ? LastEventNumber : null, validateEventNumber ? (LastEventNumber.HasValue ? EventStoreState.NotExisting : EventStoreState.Existing) : EventStoreState.Any, eventBytes);

            await Bus.DispatchAsync(@event);
        }

        /// <summary>
        /// Applies and persists a terminating event that marks the aggregate as deleted, then dispatches it on the bus.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="event">The event to use as the deletion marker.</param>
        /// <param name="validateEventNumber">When <see langword="true"/>, enforces optimistic concurrency by validating the expected event number.</param>
        public async Task Delete<TEvent>(TEvent @event, bool validateEventNumber = false) where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            var eventName = eventType.Name;

            await ApplyEvent(@event, eventType);

            _ = await this.eventStore.TerminateAsync(Guid.NewGuid(), eventName, streamName, validateEventNumber ? LastEventNumber : null, validateEventNumber ? (LastEventNumber.HasValue ? EventStoreState.NotExisting : EventStoreState.Existing) : EventStoreState.Any);

            await Bus.DispatchAsync(@event);
        }

        /// <summary>
        /// Rebuilds the aggregate state by replaying the next single event after the last applied event.
        /// </summary>
        /// <returns><see langword="true"/> if an event was found and applied; otherwise, <see langword="false"/>.</returns>
        public Task<bool> RebuildOneEvent()
        {
            return Rebuild(LastEventNumber.HasValue ? LastEventNumber.Value + 1 : 0, null);
        }
        /// <summary>
        /// Rebuilds the aggregate state by replaying events from the event store up to an optional maximum event number or date.
        /// </summary>
        /// <param name="maxEventNumber">The maximum event number to replay up to, or <see langword="null"/> for no limit.</param>
        /// <param name="maxEventDate">The maximum event date to replay up to, or <see langword="null"/> for no limit.</param>
        /// <returns><see langword="true"/> if one or more events were found and applied; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> Rebuild(ulong? maxEventNumber = null, DateTime? maxEventDate = null)
        {
            var startEventNumber = LastEventNumber.HasValue ? LastEventNumber + 1 : null;

            //TODO error handle if aggregate doesn't exist?????
            var eventDatas = await this.eventStore.ReadAsync(streamName, startEventNumber, null, maxEventNumber, null, maxEventDate);
            if (eventDatas.Length == 0)
                return false;

            if (!IsCreated)
                IsCreated = true;

            foreach (var eventData in eventDatas)
            {
                this.LastEventNumber = eventData.Number;
                this.LastEventDate = eventData.Date;
                this.LastEventName = eventData.EventName;
                if (eventData.Deleted)
                    this.IsDeleted = true;
                var eventModel = EventStoreCommon.Deserialize<IEvent>(eventData.Data.Span);
                if (eventModel is null)
                    throw new Exception("Failed to deserialize Model");
                await ApplyEvent(eventModel, eventModel.GetType());
            }
            return true;
        }

        private static readonly ConcurrentFactoryDictionary<Type, MethodDetail> methodCache = new();
        private Task ApplyEvent(IEvent @event, Type eventType)
        {
            var methodDetail = methodCache.GetOrAdd(eventType, (eventType) =>
            {
                var aggregateType = GetAggregateType();
                var aggregateTypeDetail = TypeAnalyzer.GetTypeDetail(aggregateType);
                MethodDetail? methodDetail = null;
                foreach (var method in aggregateTypeDetail.Methods)
                {
                    if (!method.IsStatic && method.Parameters.Count == 1 && method.Parameters[0].Type == eventType)
                    {
                        if (methodDetail is not null)
                            throw new Exception($"Multiple aggregate event methods found in {aggregateType.Name} to accept {eventType.Name}");
                        methodDetail = method;
                    }
                }
                if (methodDetail is null)
                    throw new Exception($"No aggregate event methods found in {aggregateType.Name} to accept {eventType.Name}");
                return methodDetail;
            });

            return (Task)methodDetail.CallerBoxed!(this, [@event])!;
        }
    }
}