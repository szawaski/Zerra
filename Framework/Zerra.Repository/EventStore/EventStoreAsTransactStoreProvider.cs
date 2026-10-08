// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Map;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// A transactional store provider that reconstructs model state by replaying events from an <see cref="IEventStoreEngine"/>,
    /// enabling temporal queries over event-sourced data.
    /// </summary>
    /// <typeparam name="TModel">The model type managed by this provider.</typeparam>
    public class EventStoreAsTransactStoreProvider<TModel> : RootTransactStoreProvider<TModel>
        where TModel : class, new()
    {
        private readonly ulong saveStateEvery;

        /// <summary>
        /// Gets the number of events after which the model's state is saved so reads replay from it instead of from the first event. Defaults to 100, 0 never saves the state.
        /// </summary>
        protected virtual ulong SaveStateEvery => saveStateEvery;

        /// <summary>
        /// Gets the underlying event store engine used for reading and appending events.
        /// </summary>
        protected readonly IEventStoreEngine Engine;

        /// <summary>
        /// Initializes a new instance on <paramref name="engine"/>.
        /// </summary>
        /// <param name="engine">The engine to use, such as one from a data context's GetEngine. Providers given the same in-memory engine share its store.</param>
        /// <param name="saveStateEvery">The number of events after which the model's state is saved so reads replay from it instead of from the first event, 0 never saves the state.</param>
        public EventStoreAsTransactStoreProvider(IEventStoreEngine engine, ulong saveStateEvery = 100)
        {
            if (engine is null)
                throw new ArgumentNullException(nameof(engine));
            this.Engine = engine;
            this.saveStateEvery = saveStateEvery;
        }

        /// <inheritdoc/>
        protected override sealed IReadOnlyCollection<TModel> Many(Query query)
        {
            var models = ReadModels(query);

            var queriedSet = models.Query(query);

            var selectedArray = queriedSet.ToArray();

            return selectedArray;
        }
        /// <inheritdoc/>
        protected override sealed TModel? First(Query query)
        {
            var models = ReadModels(query);

            var queriedSet = models.Query(query);

            var selected = queriedSet.FirstOrDefault();

            return selected;
        }
        /// <inheritdoc/>
        protected override sealed TModel? Single(Query query)
        {
            var models = ReadModels(query);

            var queriedSet = models.Query(query);

            var selected = queriedSet.SingleOrDefault();

            return selected;
        }
        /// <inheritdoc/>
        protected override sealed long Count(Query query)
        {
            var models = ReadModels(query);

            var queriedSet = models.Query(query);

            long count = queriedSet.Count();

            return count;
        }
        /// <inheritdoc/>
        protected override sealed bool Any(Query query)
        {
            var models = ReadModels(query);

            var queriedSet = models.Query(query);

            var any = queriedSet.Any();

            return any;
        }
        /// <inheritdoc/>
        protected override sealed IReadOnlyCollection<EventModel<TModel>> EventMany(Query query)
        {
            var eventModels = ReadEventModels(query);

            var models = eventModels.Select(x => x.Model);

            var queriedSet = models.Query(query);

            var selectedArray = queriedSet.ToArray();

            var eventModelSelectedArray = eventModels.Where(x => selectedArray.Contains(x.Model)).ToArray();

            return eventModelSelectedArray;
        }
        /// <inheritdoc/>
        protected override sealed EventModel<TModel>? EventFirst(Query query)
        {
            var eventModels = ReadEventModels(query);

            var models = eventModels.Select(x => x.Model);

            var queriedSet = models.Query(query);

            var selected = queriedSet.FirstOrDefault();
            if (selected is null)
                return null;

            var eventModelSelected = eventModels.FirstOrDefault(x => x.Model == selected);

            return eventModelSelected;
        }
        /// <inheritdoc/>
        protected override sealed EventModel<TModel>? EventSingle(Query query)
        {
            var eventModels = ReadEventModels(query);

            var models = eventModels.Select(x => x.Model);

            var queriedSet = models.Query(query);

            var selected = queriedSet.FirstOrDefault();
            if (selected is null)
                return null;

            var eventModelSelected = eventModels.SingleOrDefault(x => x.Model == selected);

            return eventModelSelected;
        }
        /// <inheritdoc/>
        protected override sealed long EventCount(Query query)
        {
            var eventModels = ReadEventModels(query);

            var models = eventModels.Select(x => x.Model);

            var queriedSet = models.Query(query);

            long count = queriedSet.Count();

            return count;
        }
        /// <inheritdoc/>
        protected override sealed bool EventAny(Query query)
        {
            var eventModels = ReadEventModels(query);

            var models = eventModels.Select(x => x.Model);

            var queriedSet = models.Query(query);

            var any = queriedSet.Any();

            return any;
        }

        /// <inheritdoc/>
        protected override sealed async Task<IReadOnlyCollection<TModel>> ManyAsync(Query query)
        {
            var models = await ReadModelsAsync(query);

            var queriedSet = models.Query(query);

            var selectedArray = queriedSet.ToArray();

            return selectedArray;
        }
        /// <inheritdoc/>
        protected override sealed async Task<TModel?> FirstAsync(Query query)
        {
            var models = await ReadModelsAsync(query);

            var queriedSet = models.Query(query);

            var selected = queriedSet.FirstOrDefault();

            return selected;
        }
        /// <inheritdoc/>
        protected override sealed async Task<TModel?> SingleAsync(Query query)
        {
            var models = await ReadModelsAsync(query);

            var queriedSet = models.Query(query);

            var selected = queriedSet.SingleOrDefault();

            return selected;
        }
        /// <inheritdoc/>
        protected override sealed async Task<long> CountAsync(Query query)
        {
            var models = await ReadModelsAsync(query);

            var queriedSet = models.Query(query);

            long count = queriedSet.Count();

            return count;
        }
        /// <inheritdoc/>
        protected override sealed async Task<bool> AnyAsync(Query query)
        {
            var models = await ReadModelsAsync(query);

            var queriedSet = models.Query(query);

            var any = queriedSet.Any();

            return any;
        }
        /// <inheritdoc/>
        protected override sealed async Task<IReadOnlyCollection<EventModel<TModel>>> EventManyAsync(Query query)
        {
            var eventModels = await ReadEventModelsAsync(query);

            var models = eventModels.Select(x => x.Model);

            var queriedSet = models.Query(query);

            var selectedArray = queriedSet.ToArray();

            var eventModelSelectedArray = eventModels.Where(x => selectedArray.Contains(x.Model)).ToArray();

            return eventModelSelectedArray;
        }
        /// <inheritdoc/>
        protected override sealed async Task<EventModel<TModel>?> EventFirstAsync(Query query)
        {
            var eventModels = await ReadEventModelsAsync(query);

            var models = eventModels.Select(x => x.Model);

            var queriedSet = models.Query(query);

            var selected = queriedSet.FirstOrDefault();
            if (selected is null)
                return null;

            var eventModelSelected = eventModels.FirstOrDefault(x => x.Model == selected);

            return eventModelSelected;
        }
        /// <inheritdoc/>
        protected override sealed async Task<EventModel<TModel>?> EventSingleAsync(Query query)
        {
            var eventModels = await ReadEventModelsAsync(query);

            var models = eventModels.Select(x => x.Model);

            var queriedSet = models.Query(query);

            var selected = queriedSet.FirstOrDefault();
            if (selected is null)
                return null;

            var eventModelSelected = eventModels.SingleOrDefault(x => x.Model == selected);

            return eventModelSelected;
        }
        /// <inheritdoc/>
        protected override sealed async Task<long> EventCountAsync(Query query)
        {
            var eventModels = await ReadEventModelsAsync(query);

            var models = eventModels.Select(x => x.Model);

            var queriedSet = models.Query(query);

            long count = queriedSet.Count();

            return count;
        }
        /// <inheritdoc/>
        protected override sealed async Task<bool> EventAnyAsync(Query query)
        {
            var eventModels = await ReadEventModelsAsync(query);

            var models = eventModels.Select(x => x.Model);

            var queriedSet = models.Query(query);

            var any = queriedSet.Any();

            return any;
        }

        private ICollection<TModel> ReadModels(Query query)
        {
            var many = query.Operation == QueryOperation.Many || query.Operation == QueryOperation.Count;
            var ids = GetIDs(query);

            var models = new List<TModel>();
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);

                var (modelState, modelEventNumber) = ReadModelState(id, query, many);

                var eventDatas = Engine.ReadBackwards(streamName, query.TemporalNumberTo, null, modelEventNumber, null, query.TemporalDateTo);

                var items = LoadModelsFromEventDatas(eventDatas, modelState, many, query);

                models.AddRange(items);
            }
            return models;
        }
        private async Task<ICollection<TModel>> ReadModelsAsync(Query query)
        {
            var many = query.Operation == QueryOperation.Many || query.Operation == QueryOperation.Count;
            var ids = GetIDs(query);

            var models = new List<TModel>();
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);

                var (modelState, modelEventNumber) = await ReadModelStateAsync(id, query, many);

                var eventDatas = await Engine.ReadBackwardsAsync(streamName, query.TemporalNumberTo, null, modelEventNumber, null, query.TemporalDateTo);

                var items = LoadModelsFromEventDatas(eventDatas, modelState, many, query);

                models.AddRange(items);
            }
            return models;
        }

        private ICollection<EventModel<TModel>> ReadEventModels(Query query)
        {
            var many = query.Operation == QueryOperation.EventMany || query.Operation == QueryOperation.EventCount;
            var ids = GetIDs(query);

            var models = new List<EventModel<TModel>>();
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);

                var (modelState, modelEventNumber) = ReadModelState(id, query, many);

                var eventDatas = Engine.ReadBackwards(streamName, query.TemporalNumberTo, null, modelEventNumber, null, query.TemporalDateTo);

                var items = LoadEventModelsFromEventDatas(eventDatas, modelState, many, query);

                models.AddRange(items);
            }
            return models;
        }
        private async Task<ICollection<EventModel<TModel>>> ReadEventModelsAsync(Query query)
        {
            var many = query.Operation == QueryOperation.EventMany || query.Operation == QueryOperation.EventCount;
            var ids = GetIDs(query);

            var models = new List<EventModel<TModel>>();
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);

                var (modelState, modelEventNumber) = await ReadModelStateAsync(id, query, many);

                var eventDatas = await Engine.ReadBackwardsAsync(streamName, query.TemporalNumberTo, null, modelEventNumber, null, query.TemporalDateTo);

                var items = LoadEventModelsFromEventDatas(eventDatas, modelState, many, query);

                models.AddRange(items);
            }
            return models;
        }

        private ICollection<TModel> LoadModelsFromEventDatas(EventStoreEventData[] eventDatas, TModel? modelState, bool many, Query query)
        {
            if (modelState is null && eventDatas.Length == 0)
                return Array.Empty<TModel>();

            modelState ??= (TModel)base.ModelTypeDetail.Creator();

            if (many)
            {
                switch (query.TemporalOrder ?? TemporalOrder.Newest)
                {
                    case TemporalOrder.Newest:
                        {
                            var modelStates = new List<TModel>();
                            foreach (var eventData in eventDatas.AsEnumerable().Reverse())
                            {
                                //a terminating event carries no data, the model no longer exists
                                if (eventData.Deleted)
                                    return Array.Empty<TModel>();

                                var eventModel = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
                                if (eventModel is null)
                                    throw new Exception("Failed to deserialize Model");

                                Mapper.MapTo(eventModel.Model, modelState, eventModel.Graph);

                                if (query.TemporalDateTo.HasValue && query.TemporalDateTo.Value < eventData.Date)
                                    break;
                                if (query.TemporalNumberTo.HasValue && query.TemporalNumberTo.Value < eventData.Number)
                                    break;
                                if ((!query.TemporalDateFrom.HasValue && !query.TemporalNumberFrom.HasValue) || (query.TemporalDateFrom.HasValue && eventData.Date >= query.TemporalDateFrom.Value) || (query.TemporalNumberFrom.HasValue && eventData.Number >= query.TemporalNumberFrom.Value))
                                {
                                    var copy = Mapper.Copy(modelState);
                                    modelStates.Add(copy);
                                }
                            }

                            if (query.TemporalSkip.HasValue && query.TemporalTake.HasValue)
                            {
                                modelStates.Reverse();
                                return modelStates.Skip(query.TemporalSkip.Value).Take(query.TemporalTake.Value).Reverse().ToArray();
                            }
                            else if (query.TemporalSkip.HasValue)
                            {
                                modelStates.Reverse();
                                return modelStates.Skip(query.TemporalSkip.Value).Reverse().ToArray();
                            }
                            else if (query.TemporalTake.HasValue)
                            {
                                modelStates.Reverse();
                                return modelStates.Take(query.TemporalTake.Value).Reverse().ToArray();
                            }
                            else
                            {
                                return modelStates;
                            }
                        }
                    case TemporalOrder.Oldest:
                        {
                            var modelStates = new List<TModel>();
                            var skipCount = 0;
                            foreach (var eventData in eventDatas.AsEnumerable().Reverse())
                            {
                                //a terminating event carries no data, the model no longer exists
                                if (eventData.Deleted)
                                    return Array.Empty<TModel>();

                                var eventModel = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
                                if (eventModel is null)
                                    throw new Exception("Failed to deserialize Model");

                                Mapper.MapTo(eventModel.Model, modelState, eventModel.Graph);

                                if (query.TemporalDateTo.HasValue && query.TemporalDateTo.Value < eventData.Date)
                                    break;
                                if (query.TemporalNumberTo.HasValue && query.TemporalNumberTo.Value < eventData.Number)
                                    break;
                                if ((!query.TemporalDateFrom.HasValue && !query.TemporalNumberFrom.HasValue) || (query.TemporalDateFrom.HasValue && eventData.Date >= query.TemporalDateFrom.Value) || (query.TemporalNumberFrom.HasValue && eventData.Number >= query.TemporalNumberFrom.Value))
                                {
                                    if (!query.TemporalSkip.HasValue || query.TemporalSkip.Value <= skipCount)
                                    {
                                        var copy = Mapper.Copy(modelState);
                                        modelStates.Add(copy);
                                        if (query.TemporalTake.HasValue && query.TemporalTake.Value == modelStates.Count)
                                            break;
                                    }
                                    else
                                    {
                                        skipCount++;
                                    }
                                }
                            }
                            return modelStates;
                        }
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                switch (query.TemporalOrder ?? TemporalOrder.Newest)
                {
                    case TemporalOrder.Newest:
                        {
                            foreach (var eventData in eventDatas.AsEnumerable().Reverse())
                            {
                                //a terminating event carries no data, the model no longer exists
                                if (eventData.Deleted)
                                    return Array.Empty<TModel>();

                                var eventModel = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
                                if (eventModel is null)
                                    throw new Exception("Failed to deserialize Model");

                                Mapper.MapTo(eventModel.Model, modelState, eventModel.Graph);

                                if (query.TemporalDateTo.HasValue && query.TemporalDateTo.Value < eventData.Date)
                                    break;
                                if (query.TemporalNumberTo.HasValue && query.TemporalNumberTo.Value < eventData.Number)
                                    break;
                            }
                            return [modelState];
                        }
                    case TemporalOrder.Oldest:
                        {
                            foreach (var eventData in eventDatas.AsEnumerable().Reverse())
                            {
                                //a terminating event carries no data, the model no longer exists
                                if (eventData.Deleted)
                                    return Array.Empty<TModel>();

                                var eventModel = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
                                if (eventModel is null)
                                    throw new Exception("Failed to deserialize Model");

                                Mapper.MapTo(eventModel.Model, modelState, eventModel.Graph);

                                if ((!query.TemporalDateFrom.HasValue && !query.TemporalNumberFrom.HasValue) || (query.TemporalDateFrom.HasValue && eventData.Date >= query.TemporalDateFrom.Value) || (query.TemporalNumberFrom.HasValue && eventData.Number >= query.TemporalNumberFrom.Value))
                                    break;
                            }
                            return [modelState];
                        }
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        private ICollection<EventModel<TModel>> LoadEventModelsFromEventDatas(EventStoreEventData[] eventDatas, TModel? modelState, bool many, Query query)
        {
            if (modelState is null && eventDatas.Length == 0)
                return Array.Empty<EventModel<TModel>>();

            modelState ??= (TModel)base.ModelTypeDetail.Creator();

            if (many)
            {
                switch (query.TemporalOrder ?? TemporalOrder.Newest)
                {
                    case TemporalOrder.Newest:
                        {
                            var eventModels = new List<EventModel<TModel>>();
                            foreach (var eventData in eventDatas.AsEnumerable().Reverse())
                            {
                                //a terminating event carries no data, there is nothing to replay from it
                                if (eventData.Deleted)
                                    continue;

                                var eventModelData = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
                                if (eventModelData is null)
                                    throw new Exception("Failed to deserialize Model");

                                Mapper.MapTo(eventModelData.Model, modelState, eventModelData.Graph);

                                if (query.TemporalDateTo.HasValue && query.TemporalDateTo.Value < eventData.Date)
                                    break;
                                if (query.TemporalNumberTo.HasValue && query.TemporalNumberTo.Value < eventData.Number)
                                    break;
                                if ((!query.TemporalDateFrom.HasValue && !query.TemporalNumberFrom.HasValue) || (query.TemporalDateFrom.HasValue && eventData.Date >= query.TemporalDateFrom.Value) || (query.TemporalNumberFrom.HasValue && eventData.Number >= query.TemporalNumberFrom.Value))
                                {
                                    var copy = Mapper.Copy(modelState);
                                    var eventModel = new EventModel<TModel>()
                                    {
                                        EventID = eventData.EventID,
                                        EventName = eventData.EventName,
                                        Date = eventData.Date,
                                        Number = eventData.Number,
                                        Deleted = eventData.Deleted,

                                        Model = copy,

                                        ModelChange = eventModelData.Model,
                                        GraphChange = eventModelData.Graph,
                                        Source = eventModelData.Source,
                                        SourceType = eventModelData.SourceType
                                    };
                                    eventModels.Add(eventModel);
                                }
                            }
                            if (query.TemporalSkip.HasValue && query.TemporalTake.HasValue)
                            {
                                eventModels.Reverse();
                                return eventModels.Skip(query.TemporalSkip.Value).Take(query.TemporalTake.Value).Reverse().ToArray();
                            }
                            else if (query.TemporalSkip.HasValue)
                            {
                                eventModels.Reverse();
                                return eventModels.Skip(query.TemporalSkip.Value).Reverse().ToArray();
                            }
                            else if (query.TemporalTake.HasValue)
                            {
                                eventModels.Reverse();
                                return eventModels.Take(query.TemporalTake.Value).Reverse().ToArray();
                            }
                            else
                            {
                                return eventModels;
                            }
                        }
                    case TemporalOrder.Oldest:
                        {
                            var eventModels = new List<EventModel<TModel>>();
                            var skipCount = 0;
                            foreach (var eventData in eventDatas.AsEnumerable().Reverse())
                            {
                                //a terminating event carries no data, there is nothing to replay from it
                                if (eventData.Deleted)
                                    continue;

                                var eventModelData = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
                                if (eventModelData is null)
                                    throw new Exception("Failed to deserialize Model");

                                Mapper.MapTo(eventModelData.Model, modelState, eventModelData.Graph);

                                if (query.TemporalDateTo.HasValue && query.TemporalDateTo.Value < eventData.Date)
                                    break;
                                if (query.TemporalNumberTo.HasValue && query.TemporalNumberTo.Value < eventData.Number)
                                    break;
                                if ((!query.TemporalDateFrom.HasValue && !query.TemporalNumberFrom.HasValue) || (query.TemporalDateFrom.HasValue && eventData.Date >= query.TemporalDateFrom.Value) || (query.TemporalNumberFrom.HasValue && eventData.Number >= query.TemporalNumberFrom.Value))
                                {
                                    if (!query.TemporalSkip.HasValue || query.TemporalSkip.Value <= skipCount)
                                    {
                                        var copy = Mapper.Copy(modelState);
                                        var eventModel = new EventModel<TModel>()
                                        {
                                            EventID = eventData.EventID,
                                            EventName = eventData.EventName,
                                            Date = eventData.Date,
                                            Number = eventData.Number,
                                            Deleted = eventData.Deleted,

                                            Model = copy,

                                            ModelChange = eventModelData.Model,
                                            GraphChange = eventModelData.Graph,
                                            Source = eventModelData.Source,
                                            SourceType = eventModelData.SourceType
                                        };
                                        eventModels.Add(eventModel);
                                        if (query.TemporalTake.HasValue && query.TemporalTake.Value == eventModels.Count)
                                            break;
                                    }
                                    else
                                    {
                                        skipCount++;
                                    }
                                }
                            }
                            return eventModels;
                        }
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                switch (query.TemporalOrder ?? TemporalOrder.Newest)
                {
                    case TemporalOrder.Newest:
                        {
                            EventStoreEventModelData<TModel>? eventModelData = null;
                            EventStoreEventData? thisEventData = null;
                            foreach (var eventData in eventDatas.AsEnumerable().Reverse())
                            {
                                //a terminating event carries no data, there is nothing to replay from it
                                if (eventData.Deleted)
                                    continue;

                                thisEventData = eventData;
                                eventModelData = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
                                if (eventModelData is null)
                                    throw new Exception("Failed to deserialize Model");

                                Mapper.MapTo(eventModelData.Model, modelState, eventModelData.Graph);

                                if (query.TemporalDateTo.HasValue && query.TemporalDateTo.Value < eventData.Date)
                                    break;
                                if (query.TemporalNumberTo.HasValue && query.TemporalNumberTo.Value < eventData.Number)
                                    break;
                            }

                            if (thisEventData is null || eventModelData is null)
                                return Array.Empty<EventModel<TModel>>();

                            var eventModel = new EventModel<TModel>()
                            {
                                EventID = thisEventData.EventID,
                                EventName = thisEventData.EventName,
                                Date = thisEventData.Date,
                                Number = thisEventData.Number,
                                Deleted = thisEventData.Deleted,

                                Model = modelState,

                                ModelChange = eventModelData.Model,
                                GraphChange = eventModelData.Graph,
                                Source = eventModelData.Source,
                                SourceType = eventModelData.SourceType
                            };
                            return [eventModel];
                        }
                    case TemporalOrder.Oldest:
                        {
                            EventStoreEventModelData<TModel>? eventModelData = null;
                            EventStoreEventData? thisEventData = null;
                            foreach (var eventData in eventDatas.AsEnumerable().Reverse())
                            {
                                //a terminating event carries no data, there is nothing to replay from it
                                if (eventData.Deleted)
                                    continue;

                                thisEventData = eventData;
                                eventModelData = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
                                if (eventModelData is null)
                                    throw new Exception("Failed to deserialize Model");

                                Mapper.MapTo(eventModelData.Model, modelState, eventModelData.Graph);

                                if ((!query.TemporalDateFrom.HasValue && !query.TemporalNumberFrom.HasValue) || (query.TemporalDateFrom.HasValue && eventData.Date >= query.TemporalDateFrom.Value) || (query.TemporalNumberFrom.HasValue && eventData.Number >= query.TemporalNumberFrom.Value))
                                    break;
                            }

                            if (thisEventData is null || eventModelData is null)
                                return Array.Empty<EventModel<TModel>>();

                            var eventModel = new EventModel<TModel>()
                            {
                                EventID = thisEventData.EventID,
                                EventName = thisEventData.EventName,
                                Date = thisEventData.Date,
                                Number = thisEventData.Number,
                                Deleted = thisEventData.Deleted,

                                Model = modelState,

                                ModelChange = eventModelData.Model,
                                GraphChange = eventModelData.Graph,
                                Source = eventModelData.Source,
                                SourceType = eventModelData.SourceType
                            };
                            return [eventModel];
                        }
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        //the newest saved state from before what the query reads, the events are replayed from the event it was saved at
        private (TModel?, ulong?) ReadModelState(object id, Query query, bool many)
        {
            ulong? numberAtOrBefore;
            DateTime? dateBefore = null;
            DateTime? dateAtOrBefore = null;
            if (many || query.TemporalOrder == TemporalOrder.Oldest)
            {
                //the states from the From bound on are read, the replay starts before it
                if (!query.TemporalNumberFrom.HasValue && !query.TemporalDateFrom.HasValue)
                    return (null, null);
                numberAtOrBefore = query.TemporalNumberFrom;
                dateBefore = query.TemporalDateFrom;
            }
            else
            {
                numberAtOrBefore = query.TemporalNumberTo;
                dateAtOrBefore = query.TemporalDateTo;
            }

            var streamName = EventStoreCommon.GetStateStreamName<TModel>(id);
            var bounded = numberAtOrBefore.HasValue || dateBefore.HasValue || dateAtOrBefore.HasValue;
            var eventDatas = Engine.ReadBackwards(streamName, null, bounded ? null : 1, null, null, null);

            foreach (var eventData in eventDatas)
            {
                var eventState = EventStoreCommon.Deserialize<EvenStoreStateData<TModel>>(eventData.Data.Span);
                if (eventState is null)
                    throw new Exception("Failed to deserialize Model");

                if (!eventState.Number.HasValue)
                    continue;
                if (numberAtOrBefore.HasValue && eventState.Number.Value > numberAtOrBefore.Value)
                    continue;
                if (dateBefore.HasValue && (!eventState.Date.HasValue || eventState.Date.Value >= dateBefore.Value))
                    continue;
                if (dateAtOrBefore.HasValue && (!eventState.Date.HasValue || eventState.Date.Value > dateAtOrBefore.Value))
                    continue;

                return (eventState.Model, eventState.Number);
            }

            return (null, null);
        }
        private async Task<(TModel?, ulong?)> ReadModelStateAsync(object id, Query query, bool many)
        {
            ulong? numberAtOrBefore;
            DateTime? dateBefore = null;
            DateTime? dateAtOrBefore = null;
            if (many || query.TemporalOrder == TemporalOrder.Oldest)
            {
                if (!query.TemporalNumberFrom.HasValue && !query.TemporalDateFrom.HasValue)
                    return (null, null);
                numberAtOrBefore = query.TemporalNumberFrom;
                dateBefore = query.TemporalDateFrom;
            }
            else
            {
                numberAtOrBefore = query.TemporalNumberTo;
                dateAtOrBefore = query.TemporalDateTo;
            }

            var streamName = EventStoreCommon.GetStateStreamName<TModel>(id);
            var bounded = numberAtOrBefore.HasValue || dateBefore.HasValue || dateAtOrBefore.HasValue;
            var eventDatas = await Engine.ReadBackwardsAsync(streamName, null, bounded ? null : 1, null, null, null);

            foreach (var eventData in eventDatas)
            {
                var eventState = EventStoreCommon.Deserialize<EvenStoreStateData<TModel>>(eventData.Data.Span);
                if (eventState is null)
                    throw new Exception("Failed to deserialize Model");

                if (!eventState.Number.HasValue)
                    continue;
                if (numberAtOrBefore.HasValue && eventState.Number.Value > numberAtOrBefore.Value)
                    continue;
                if (dateBefore.HasValue && (!eventState.Date.HasValue || eventState.Date.Value >= dateBefore.Value))
                    continue;
                if (dateAtOrBefore.HasValue && (!eventState.Date.HasValue || eventState.Date.Value > dateAtOrBefore.Value))
                    continue;

                return (eventState.Model, eventState.Number);
            }

            return (null, null);
        }

        private static object[] GetIDs(Query query)
        {
            if (query.Where is null)
                throw new NotSupportedException("No identity clauses found in the query. These are required for event stores.");

            var type = typeof(TModel);

            var identityProperties = ModelAnalyzer.GetIdentityPropertyNames(type);
            object[] ids;
            if (identityProperties.Length == 1)
            {
                var propertyValues = LinqValueExtractor.Extract(query.Where, type, identityProperties[0]);
                var idSingle = propertyValues[identityProperties[0]].ToArray();
                if (idSingle.Any(x => x is null))
                    throw new Exception($"Model {type.Name} missing Identity");
                ids = idSingle!;
            }
            else
            {
                var propertyValues = LinqValueExtractor.Extract(query.Where, type, identityProperties);
                var idSets = propertyValues.Select(x => x.Value.ToArray()).ToArray();
                if (idSets.Any(x => x.Any(y => y is null)))
                    throw new Exception($"Model {type.Name} missing Identity");
                ids = CalculatePermutations(idSets!);
            }

            if (ids.Length == 0)
                throw new NotSupportedException("No identity clauses found in the query. These are required for event stores.");

            return ids;
        }
        private static object[] CalculatePermutations(object[][] sets)
        {
            var list = new List<object>();
            foreach (var set in sets)
            {
                if (set.Length == 0)
                    return [];
            }

            var indexes = new int[sets.Length];
            while (true)
            {
                var permutation = new object[sets.Length];
                for (var i = 0; i < sets.Length; i++)
                    permutation[i] = sets[i][indexes[i]];
                list.Add(permutation);

                var indexer = 0;
                while (indexer < indexes.Length && ++indexes[indexer] == sets[indexer].Length)
                {
                    indexes[indexer] = 0;
                    indexer++;
                }
                if (indexer == indexes.Length)
                    return list.ToArray();
            }
        }

        /// <inheritdoc/>
        protected override sealed void PersistModel(PersistEvent @event, object model, Graph? graph, bool create)
        {
            var id = ModelAnalyzer.GetIdentity(modelType, model);
            if (id is null)
                throw new NotSupportedException("No identity on model. These are required for event stores.");

            var streamName = EventStoreCommon.GetStreamName<TModel>(id);

            var eventStoreModel = new EventStoreEventModelData<TModel>()
            {
                Source = @event.Source,
                SourceType = @event.Source?.GetType().Name,
                Model = (TModel)model,
                Graph = graph
            };

            var data = EventStoreCommon.Serialize(eventStoreModel);

            var eventNumber = Engine.Append(@event.ID, @event.Name, streamName, null, create ? EventStoreState.NotExisting : EventStoreState.Existing, data);

            var saveStateEvery = SaveStateEvery;
            if (saveStateEvery > 0 && eventNumber > 0 && eventNumber % saveStateEvery == 0)
            {
                var thisEventData = Engine.ReadBackwards(streamName, eventNumber, 1, null, null, null)[0];

                //read from this provider, the state is saved as stored and not as the layers above it change it
                var where = ModelAnalyzer.GetIdentityExpression<TModel>(id);
                var stateQuery = new Query(QueryOperation.Single, modelType, TemporalOrder.Newest, null, null, null, eventNumber, null, null, where, null, null, null, null);
                var modelState = ReadModels(stateQuery).Single();

                SaveModelState(id, modelState, eventNumber, thisEventData.Date);
            }
        }
        /// <inheritdoc/>
        protected override sealed void DeleteModel(PersistEvent @event, object[] ids)
        {
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);

                var eventNumber = Engine.Terminate(@event.ID, "Delete", streamName, null, EventStoreState.Existing);

                SaveModelState(id, null, eventNumber, null);
            }
        }

        /// <inheritdoc/>
        protected override sealed async Task PersistModelAsync(PersistEvent @event, object model, Graph? graph, bool create)
        {
            var id = ModelAnalyzer.GetIdentity(modelType, model);
            if (id is null)
                throw new NotSupportedException("No identity on model. These are required for event stores.");

            var streamName = EventStoreCommon.GetStreamName<TModel>(id);

            var eventStoreModel = new EventStoreEventModelData<TModel>()
            {
                Source = @event.Source,
                SourceType = @event.Source?.GetType().Name,
                Model = (TModel)model,
                Graph = graph
            };

            var data = EventStoreCommon.Serialize(eventStoreModel);

            var eventNumber = await Engine.AppendAsync(@event.ID, @event.Name, streamName, null, create ? EventStoreState.NotExisting : EventStoreState.Existing, data);

            var saveStateEvery = SaveStateEvery;
            if (saveStateEvery > 0 && eventNumber > 0 && eventNumber % saveStateEvery == 0)
            {
                var thisEventData = (await Engine.ReadBackwardsAsync(streamName, eventNumber, 1, null, null, null))[0];

                var where = ModelAnalyzer.GetIdentityExpression<TModel>(id);
                var stateQuery = new Query(QueryOperation.Single, modelType, TemporalOrder.Newest, null, null, null, eventNumber, null, null, where, null, null, null, null);
                var modelState = (await ReadModelsAsync(stateQuery)).Single();

                await SaveModelStateAsync(id, modelState, eventNumber, thisEventData.Date);
            }
        }
        /// <inheritdoc/>
        protected override sealed async Task DeleteModelAsync(PersistEvent @event, object[] ids)
        {
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);

                var eventNumber = await Engine.TerminateAsync(@event.ID, "Delete", streamName, null, EventStoreState.Existing);

                await SaveModelStateAsync(id, null, eventNumber, null);
            }
        }

        private void SaveModelState(object id, TModel? model, ulong eventNumber, DateTime? date)
        {
            var streamName = EventStoreCommon.GetStateStreamName<TModel>(id);

            var eventState = new EvenStoreStateData<TModel>()
            {
                Model = model,
                Number = eventNumber,
                Date = date,
                Deleted = model is null
            };
            var data = EventStoreCommon.Serialize(eventState);

            _ = Engine.Append(Guid.NewGuid(), "StoreState", streamName, null, EventStoreState.Any, data);
        }
        private async Task SaveModelStateAsync(object id, TModel? model, ulong eventNumber, DateTime? date)
        {
            var streamName = EventStoreCommon.GetStateStreamName<TModel>(id);

            var eventState = new EvenStoreStateData<TModel>()
            {
                Model = model,
                Number = eventNumber,
                Date = date,
                Deleted = model is null
            };
            var data = EventStoreCommon.Serialize(eventState);

            _ = await Engine.AppendAsync(Guid.NewGuid(), "StoreState", streamName, null, EventStoreState.Any, data);
        }
    }
}
