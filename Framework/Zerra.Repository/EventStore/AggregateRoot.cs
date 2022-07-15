// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;
using Zerra.Collections;
using Zerra.CQRS;
using Zerra.Reflection;

namespace Zerra.Repository
{
    public abstract class AggregateRoot
    {
        private static Type typeCache = null;
        private static readonly object typeCacheLock = new object();
        private Type GetAggregateType()
        {
            if (typeCache == null)
            {
                lock (typeCacheLock)
                {
                    if (typeCache == null)
                    {
                        typeCache = this.GetType();
                    }
                }
            }
            return typeCache;
        }

        private static IEventStoreEngine engineCache = null;
        private static readonly object engineCacheLock = new object();
        private IEventStoreEngine GetEngine()
        {
            if (engineCache == null)
            {
                lock (engineCacheLock)
                {
                    if (engineCache == null)
                    {
                        var aggregateType = GetAggregateType();
                        var iEventStoreContextProviderType = typeof(IAggregateRootContextProvider<>);
                        var iEventStoreContextProviderGenericType = TypeAnalyzer.GetGenericType(iEventStoreContextProviderType, aggregateType);
                        var providerType = Discovery.GetImplementationType(iEventStoreContextProviderGenericType);
                        var provider = (IContextProvider)Instantiator.CreateInstance(providerType);
                        var context = provider.GetContext();
                        engineCache = context.InitializeEngine<IEventStoreEngine>();
                    }
                }
            }
            return engineCache;
        }

        public Guid ID { get; private set; }
        public ulong? LastEventNumber { get; private set; }
        public DateTime? LastEventDate { get; private set; }
        public string LastEventName { get; private set; }
        public bool IsCreated { get; private set; }
        public bool IsDeleted { get; private set; }

        private readonly string streamName;
        private readonly IEventStoreEngine eventStore;

        protected AggregateRoot(Guid id)
        {
            this.eventStore = GetEngine();
            this.ID = id;
            this.streamName = $"{GetAggregateType().FullName}-{id}";
        }

        public async Task Append<TEvent>(TEvent @event, bool validateEventNumber = false) where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            var eventName = eventType.Name;

            await ApplyEvent(@event, eventType);

            var eventBytes = EventStoreCommon.Serialize(@event);
            _ = await this.eventStore.AppendAsync(Guid.NewGuid(), eventName, streamName, validateEventNumber ? LastEventNumber : null, validateEventNumber ? (LastEventNumber.HasValue ? EventStoreState.NotExisting : EventStoreState.Existing) : EventStoreState.Any, eventBytes);

            await Bus.DispatchAsync(@event);
        }

        public async Task Delete<TEvent>(TEvent @event, bool validateEventNumber = false) where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            var eventName = eventType.Name;

            await ApplyEvent(@event, eventType);

            _ = await this.eventStore.TerminateAsync(Guid.NewGuid(), eventName, streamName, validateEventNumber ? LastEventNumber : null, validateEventNumber ? (LastEventNumber.HasValue ? EventStoreState.NotExisting : EventStoreState.Existing) : EventStoreState.Any);

            await Bus.DispatchAsync(@event);
        }

        public Task<bool> RebuildOneEvent()
        {
            return Rebuild(LastEventNumber.HasValue ? LastEventNumber.Value + 1 : 0, null);
        }
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
                await ApplyEvent(eventModel, eventModel.GetType());
            }
            return true;
        }

        private static readonly ConcurrentFactoryDictionary<Type, MethodDetail> methodCache = new ConcurrentFactoryDictionary<Type, MethodDetail>();
        private Task ApplyEvent(IEvent @event, Type eventType)
        {
            var methodDetail = methodCache.GetOrAdd(eventType, (t) =>
            {
                var aggregateType = GetAggregateType();
                var aggregateTypeDetail = TypeAnalyzer.GetTypeDetail(aggregateType);
                MethodDetail methodDetail = null;
                foreach (var method in aggregateTypeDetail.MethodDetails)
                {
                    if (!method.MethodInfo.IsStatic && method.ParametersInfo.Count == 1 && method.ParametersInfo[0].ParameterType == t)
                    {
                        if (methodDetail != null)
                            throw new Exception($"Multiple aggregate event methods found in {aggregateType.Name} to accept {t.Name}");
                        methodDetail = method;
                    }
                }
                if (methodDetail == null)
                    throw new Exception($"No aggregate event methods found in {aggregateType.Name} to accept {t.Name}");
                return methodDetail;
            });

            return methodDetail.CallerAsync(this, new object[] { @event });
        }
    }
}