// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public class EventStoreAsTransactStoreProvider<TContext, TModel> : RootTransactStoreProvider<TModel>
        where TContext : DataContext
        where TModel : class, new()
    {
        protected virtual ulong SaveStateEvery => 100;

        protected readonly IEventStoreEngine Engine;

        public EventStoreAsTransactStoreProvider()
        {
            var context = Instantiator.GetSingle<TContext>();
            this.Engine = context.InitializeEngine<IEventStoreEngine>();
        }

        protected override sealed ICollection<TModel> QueryMany(Query<TModel> query)
        {
            var models = ReadModels(query);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            var selectedArray = queriedSet.ToArray();

            return selectedArray;
        }
        protected override sealed TModel QueryFirst(Query<TModel> query)
        {
            var models = ReadModels(query);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            var selected = queriedSet.FirstOrDefault();

            return selected;
        }
        protected override sealed TModel QuerySingle(Query<TModel> query)
        {
            var selector = query.Graph.GenerateSelect<TModel>();

            var models = ReadModels(query);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            var select = queriedSet.Select(selector);

            var selected = select.SingleOrDefault();

            return selected;
        }
        protected override sealed long QueryCount(Query<TModel> query)
        {
            var models = ReadModels(query);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            long count = queriedSet.Count();

            return count;
        }
        protected override sealed bool QueryAny(Query<TModel> query)
        {
            var models = ReadModels(query);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            var any = queriedSet.Any();

            return any;
        }
        protected override sealed ICollection<EventModel<TModel>> QueryEventMany(Query<TModel> query)
        {
            var eventModels = ReadEventModels(query);

            var models = eventModels.Select(x => x.Model);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            var selectedArray = queriedSet.ToArray();

            var eventModelSelectedArray = eventModels.Where(x => selectedArray.Contains(x.Model)).ToArray();

            return eventModelSelectedArray;
        }
        protected override sealed EventModel<TModel> QueryEventFirst(Query<TModel> query)
        {
            var eventModels = ReadEventModels(query);

            var models = eventModels.Select(x => x.Model);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            var selected = queriedSet.FirstOrDefault();

            var eventModelSelected = eventModels.First(x => x.Model == selected);

            return eventModelSelected;
        }
        protected override sealed EventModel<TModel> QueryEventSingle(Query<TModel> query)
        {
            var eventModels = ReadEventModels(query);

            var models = eventModels.Select(x => x.Model);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            var selected = queriedSet.FirstOrDefault();

            var eventModelSelected = eventModels.Single(x => x.Model == selected);

            return eventModelSelected;
        }
        protected override sealed long QueryEventCount(Query<TModel> query)
        {
            var eventModels = ReadEventModels(query);

            var models = eventModels.Select(x => x.Model);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            long count = queriedSet.Count();

            return count;
        }
        protected override sealed bool QueryEventAny(Query<TModel> query)
        {
            var eventModels = ReadEventModels(query);

            var models = eventModels.Select(x => x.Model);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            var any = queriedSet.Any();

            return any;
        }

        protected override sealed async Task<ICollection<TModel>> QueryManyAsync(Query<TModel> query)
        {
            var models = await ReadModelsAsync(query);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            var selectedArray = queriedSet.ToArray();

            return selectedArray;
        }
        protected override sealed async Task<TModel> QueryFirstAsync(Query<TModel> query)
        {
            var models = await ReadModelsAsync(query);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            var selected = queriedSet.FirstOrDefault();

            return selected;
        }
        protected override sealed async Task<TModel> QuerySingleAsync(Query<TModel> query)
        {
            var selector = query.Graph.GenerateSelect<TModel>();

            var models = await ReadModelsAsync(query);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            var select = queriedSet.Select(selector);

            var selected = select.SingleOrDefault();

            return selected;
        }
        protected override sealed async Task<long> QueryCountAsync(Query<TModel> query)
        {
            var models = await ReadModelsAsync(query);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            long count = queriedSet.Count();

            return count;
        }
        protected override sealed async Task<bool> QueryAnyAsync(Query<TModel> query)
        {
            var models = await ReadModelsAsync(query);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            var any = queriedSet.Any();

            return any;
        }
        protected override sealed async Task<ICollection<EventModel<TModel>>> QueryEventManyAsync(Query<TModel> query)
        {
            var eventModels = await ReadEventModelsAsync(query);

            var models = eventModels.Select(x => x.Model);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            var selectedArray = queriedSet.ToArray();

            var eventModelSelectedArray = eventModels.Where(x => selectedArray.Contains(x.Model)).ToArray();

            return eventModelSelectedArray;
        }
        protected override sealed async Task<EventModel<TModel>> QueryEventFirstAsync(Query<TModel> query)
        {
            var eventModels = await ReadEventModelsAsync(query);

            var models = eventModels.Select(x => x.Model);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            var selected = queriedSet.FirstOrDefault();

            var eventModelSelected = eventModels.First(x => x.Model == selected);

            return eventModelSelected;
        }
        protected override sealed async Task<EventModel<TModel>> QueryEventSingleAsync(Query<TModel> query)
        {
            var eventModels = await ReadEventModelsAsync(query);

            var models = eventModels.Select(x => x.Model);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            var selected = queriedSet.FirstOrDefault();

            var eventModelSelected = eventModels.Single(x => x.Model == selected);

            return eventModelSelected;
        }
        protected override sealed async Task<long> QueryEventCountAsync(Query<TModel> query)
        {
            var eventModels = await ReadEventModelsAsync(query);

            var models = eventModels.Select(x => x.Model);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            long count = queriedSet.Count();

            return count;
        }
        protected override sealed async Task<bool> QueryEventAnyAsync(Query<TModel> query)
        {
            var eventModels = await ReadEventModelsAsync(query);

            var models = eventModels.Select(x => x.Model);

            var set = models.AsQueryable();

            var queriedSet = set.Query(query);

            var any = queriedSet.Any();

            return any;
        }

        private ICollection<TModel> ReadModels(Query<TModel> query)
        {
            var many = query.Operation == QueryOperation.Many || query.Operation == QueryOperation.Count;
            var ids = GetIDs(query);

            var models = new List<TModel>();
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);

                var (modelState, modelEventNumber) = ReadModelState(id, query.TemporalOrder, query.TemporalDateFrom, query.TemporalNumberFrom);

                var eventDatas = Engine.ReadBackwards(streamName, null, null, modelEventNumber + 1, null, query.TemporalDateTo);

                var items =  LoadModelsFromEventDatas(eventDatas, modelState, many, query);

                models.AddRange(items);
            }
            return models;
        }
        private async Task<ICollection<TModel>> ReadModelsAsync(Query<TModel> query)
        {
            var many = query.Operation == QueryOperation.Many || query.Operation == QueryOperation.Count;
            var ids = GetIDs(query);

            var models = new List<TModel>();
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);

                var (modelState, modelEventNumber) = await ReadModelStateAsync(id, query.TemporalOrder, query.TemporalDateFrom, query.TemporalNumberFrom);

                var eventDatas = await Engine.ReadBackwardsAsync(streamName, null, null, modelEventNumber + 1, null, query.TemporalDateTo);

                var items = LoadModelsFromEventDatas(eventDatas, modelState, many, query);

                models.AddRange(items);
            }
            return models;
        }

        private ICollection<EventModel<TModel>> ReadEventModels(Query<TModel> query)
        {
            var many = query.Operation == QueryOperation.EventMany || query.Operation == QueryOperation.EventCount;
            var ids = GetIDs(query);

            var models = new List<EventModel<TModel>>();
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);

                var (modelState, modelEventNumber) = ReadModelState(id, query.TemporalOrder, query.TemporalDateFrom, query.TemporalNumberFrom);

                var eventDatas = Engine.ReadBackwards(streamName, null, null, modelEventNumber + 1 ?? query.TemporalNumberTo, null, query.TemporalDateTo);

                var items = LoadEventModelsFromEventDatas(eventDatas, modelState, many, query);

                models.AddRange(items);
            }
            return models;
        }
        private async Task<ICollection<EventModel<TModel>>> ReadEventModelsAsync(Query<TModel> query)
        {
            var many = query.Operation == QueryOperation.EventMany || query.Operation == QueryOperation.EventCount;
            var ids = GetIDs(query);

            var models = new List<EventModel<TModel>>();
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);

                var (modelState, modelEventNumber) = await ReadModelStateAsync(id, query.TemporalOrder, query.TemporalDateFrom, query.TemporalNumberFrom);

                var eventDatas = await Engine.ReadBackwardsAsync(streamName, null, null, modelEventNumber + 1 ?? query.TemporalNumberTo, null, query.TemporalDateTo);

                var items = LoadEventModelsFromEventDatas(eventDatas, modelState, many, query);

                models.AddRange(items);
            }
            return models;
        }

        private ICollection<TModel> LoadModelsFromEventDatas(EventStoreEventData[] eventDatas, TModel modelState, bool many, Query<TModel> query)
        {
            if (modelState == null && eventDatas.Length == 0)
                return Array.Empty<TModel>();

            modelState ??= Instantiator.Create<TModel>();

            if (many)
            {
                switch (query.TemporalOrder ?? TemporalOrder.Newest)
                {
                    case TemporalOrder.Newest:
                        {
                            var modelStates = new List<TModel>();
                            foreach (var eventData in eventDatas.Reverse())
                            {
                                var eventModel = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
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
                            foreach (var eventData in eventDatas.Reverse())
                            {
                                var eventModel = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
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
                            foreach (var eventData in eventDatas.Reverse())
                            {
                                var eventModel = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
                                Mapper.MapTo(eventModel.Model, modelState, eventModel.Graph);

                                if (query.TemporalDateTo.HasValue && query.TemporalDateTo.Value < eventData.Date)
                                    break;
                                if (query.TemporalNumberTo.HasValue && eventData.Number < query.TemporalNumberTo.Value)
                                    break;
                            }
                            return new TModel[] { modelState };
                        }
                    case TemporalOrder.Oldest:
                        {
                            foreach (var eventData in eventDatas.Reverse())
                            {
                                var eventModel = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
                                Mapper.MapTo(eventModel.Model, modelState, eventModel.Graph);

                                if (query.TemporalDateFrom.HasValue && eventData.Date >= query.TemporalDateFrom.Value)
                                    break;
                                if (query.TemporalNumberFrom.HasValue && eventData.Number >= query.TemporalNumberFrom.Value)
                                    break;
                            }
                            return new TModel[] { modelState };
                        }
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        private ICollection<EventModel<TModel>> LoadEventModelsFromEventDatas(EventStoreEventData[] eventDatas, TModel modelState, bool many, Query<TModel> query)
        {
            if (modelState == null && eventDatas.Length == 0)
                return Array.Empty<EventModel<TModel>>();

            modelState ??= Instantiator.Create<TModel>();

            if (many)
            {
                switch (query.TemporalOrder ?? TemporalOrder.Newest)
                {
                    case TemporalOrder.Newest:
                        {
                            var eventModels = new List<EventModel<TModel>>();
                            foreach (var eventData in eventDatas.Reverse())
                            {
                                var eventModelData = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
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
                            foreach (var eventData in eventDatas.Reverse())
                            {
                                var eventModelData = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
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
                            EventStoreEventModelData<TModel> eventModelData = null;
                            EventStoreEventData thisEventData = null;
                            foreach (var eventData in eventDatas.Reverse())
                            {
                                thisEventData = eventData;
                                eventModelData = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
                                Mapper.MapTo(eventModelData.Model, modelState, eventModelData.Graph);

                                if (query.TemporalDateTo.HasValue && query.TemporalDateTo.Value < eventData.Date)
                                    break;
                                if (!query.TemporalNumberTo.HasValue && eventData.Number < query.TemporalNumberTo.Value)
                                    break;
                            }
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
                            return new EventModel<TModel>[] { eventModel };
                        }
                    case TemporalOrder.Oldest:
                        {
                            EventStoreEventModelData<TModel> eventModelData = null;
                            EventStoreEventData thisEventData = null;
                            foreach (var eventData in eventDatas.Reverse())
                            {
                                thisEventData = eventData;
                                eventModelData = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
                                Mapper.MapTo(eventModelData.Model, modelState, eventModelData.Graph);

                                if (!query.TemporalDateFrom.HasValue || eventData.Date >= query.TemporalDateFrom)
                                    break;
                                if (!query.TemporalNumberFrom.HasValue || eventData.Number >= query.TemporalNumberFrom.Value)
                                    break;
                            }
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
                            return new EventModel<TModel>[] { eventModel };
                        }
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private (TModel, ulong?) ReadModelState(object id, TemporalOrder? temporalOrder, DateTime? temporalDateFrom, ulong? temporalNumberFrom)
        {
            if (temporalOrder != TemporalOrder.Newest && !temporalDateFrom.HasValue && !temporalNumberFrom.HasValue)
                return (null, null);

            var streamName = EventStoreCommon.GetStateStreamName<TModel>(id);

            TModel model = null;
            ulong? modelEventNumber = null;

            long? count = null;
            if (temporalOrder == TemporalOrder.Newest)
                count = 1;

            if (temporalOrder == TemporalOrder.Newest || temporalDateFrom.HasValue || temporalNumberFrom.HasValue)
            {
                var eventData = (Engine.ReadBackwards(streamName, null, count, temporalNumberFrom, null, temporalDateFrom)).LastOrDefault();
                if (eventData != null)
                {
                    var eventState = EventStoreCommon.Deserialize<EvenStoreStateData<TModel>>(eventData.Data.Span);
                    modelEventNumber = eventState.Number;
                    model = eventState.Model;
                }
            }

            return (model, modelEventNumber);
        }
        private async Task<(TModel, ulong?)> ReadModelStateAsync(object id, TemporalOrder? temporalOrder, DateTime? temporalDateFrom, ulong? temporalNumberFrom)
        {
            if (temporalOrder != TemporalOrder.Newest && !temporalDateFrom.HasValue && !temporalNumberFrom.HasValue)
                return (null, null);

            var streamName = EventStoreCommon.GetStateStreamName<TModel>(id);

            TModel model = null;
            ulong? modelEventNumber = null;

            long? count = null;
            if (temporalOrder == TemporalOrder.Newest)
                count = 1;

            if (temporalOrder == TemporalOrder.Newest || temporalDateFrom.HasValue || temporalNumberFrom.HasValue)
            {
                var eventData = (await Engine.ReadBackwardsAsync(streamName, null, count, temporalNumberFrom, null, temporalDateFrom)).LastOrDefault();
                if (eventData != null)
                {
                    var eventState = EventStoreCommon.Deserialize<EvenStoreStateData<TModel>>(eventData.Data.Span);
                    modelEventNumber = eventState.Number;
                    model = eventState.Model;
                }
            }

            return (model, modelEventNumber);
        }

        private object[] GetIDs(Query<TModel> query)
        {
            var identityProperties = ModelAnalyzer.GetIdentityPropertyNames(typeof(TModel));
            object[] ids = null;
            if (identityProperties.Length == 1)
            {
                var propertyValues = LinqValueExtractor.Extract(query.Where, typeof(TModel), identityProperties[0]);
                ids = propertyValues[identityProperties[0]].ToArray();
            }
            else
            {
                var propertyValues = LinqValueExtractor.Extract(query.Where, typeof(TModel), identityProperties);
                var idSets = propertyValues.Select(x => x.Value.ToArray()).ToArray();
                ids = CalculatePermutations(idSets);
            }

            if (ids.Length == 0)
            {
                throw new NotSupportedException("No identity clauses found in the query. These are required for event stores.");
            }

            return ids;
        }
        private object[] CalculatePermutations(IList<object[]> sets)
        {
            var list = new List<object>();

            var indexer = 0;
            var indexes = new int[sets.Count];
            while (true)
            {
                var permutation = new List<object>();
                for (var x = 0; x < sets.Count; x++)
                {
                    var index = indexes[x];
                    var value = sets[x][index];
                    permutation.Add(value);
                }
                list.Add(permutation.ToArray());

                while (true)
                {
                    if (indexes[indexer] < sets[indexer].Length)
                    {
                        indexes[indexer]++;
                        indexer = 0;
                        break;
                    }
                    else
                    {
                        indexes[indexer] = 0;
                        indexer++;
                        if (indexer >= indexes.Length)
                            break;
                    }
                }

                if (indexer >= indexes.Length)
                    break;
            }

            return list.ToArray();
        }

        protected override sealed void PersistModel(PersistEvent @event, TModel model, Graph<TModel> graph, bool create)
        {
            var id = ModelAnalyzer.GetIdentity(model);
            var streamName = EventStoreCommon.GetStreamName<TModel>(id);

            var eventStoreModel = new EventStoreEventModelData<TModel>()
            {
                Source = @event.Source,
                SourceType = @event.Source?.GetType().Name,
                Model = model,
                Graph = graph
            };

            var data = EventStoreCommon.Serialize(eventStoreModel);

            var eventNumber = Engine.Append(@event.ID, @event.Name, streamName, null, create ? EventStoreState.NotExisting : EventStoreState.Existing, data);

            if (eventNumber > 0 && eventNumber % SaveStateEvery == 0)
            {
                var thisEventData = Engine.ReadBackwards(streamName, eventNumber, 1, null, null, null)[0];

                var where = ModelAnalyzer.GetIdentityExpression<TModel>(id);

                var eventStates = Repo.Query(new EventQueryMany<TModel>(thisEventData.Date, thisEventData.Date, where));
                var eventState = eventStates.Where(x => x.Number == eventNumber).Single();

                SaveModelState(id, eventState.Model, eventState.Number);
            }
        }
        protected override sealed void DeleteModel(PersistEvent @event, object[] ids)
        {
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);

                var eventNumber = Engine.Terminate(@event.ID, "Delete", streamName, null, EventStoreState.Existing);

                SaveModelState(id, null, eventNumber);
            }
        }

        protected override sealed async Task PersistModelAsync(PersistEvent @event, TModel model, Graph<TModel> graph, bool create)
        {
            var id = ModelAnalyzer.GetIdentity(model);
            var streamName = EventStoreCommon.GetStreamName<TModel>(id);

            var eventStoreModel = new EventStoreEventModelData<TModel>()
            {
                Source = @event.Source,
                SourceType = @event.Source?.GetType().Name,
                Model = model,
                Graph = graph
            };

            var data = EventStoreCommon.Serialize(eventStoreModel);

            var eventNumber = await Engine.AppendAsync(@event.ID, @event.Name, streamName, null, create ? EventStoreState.NotExisting : EventStoreState.Existing, data);

            if (eventNumber > 0 && eventNumber % SaveStateEvery == 0)
            {
                var thisEventData = (await Engine.ReadBackwardsAsync(streamName, eventNumber, 1, null, null, null))[0];

                var where = ModelAnalyzer.GetIdentityExpression<TModel>(id);

                var eventStates = await Repo.QueryAsync(new EventQueryMany<TModel>(thisEventData.Date, thisEventData.Date, where));
                var eventState = eventStates.Where(x => x.Number == eventNumber).Single();

                await SaveModelStateAsync(id, eventState.Model, eventState.Number);
            }
        }
        protected override sealed async Task DeleteModelAsync(PersistEvent @event, object[] ids)
        {
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);

                var eventNumber = await Engine.TerminateAsync(@event.ID, "Delete", streamName, null, EventStoreState.Existing);

                await SaveModelStateAsync(id, null, eventNumber);
            }
        }

        private void SaveModelState(object id, TModel model, ulong eventNumber)
        {
            var streamName = EventStoreCommon.GetStateStreamName<TModel>(id);

            var eventState = new EvenStoreStateData<TModel>()
            {
                Model = model,
                Number = eventNumber,
                Deleted = model == null
            };
            var data = EventStoreCommon.Serialize(eventState);

            _ = Engine.Append(Guid.NewGuid(), "StoreState", streamName, null, EventStoreState.Any, data);
        }
        private async Task SaveModelStateAsync(object id, TModel model, ulong eventNumber)
        {
            var streamName = EventStoreCommon.GetStateStreamName<TModel>(id);

            var eventState = new EvenStoreStateData<TModel>()
            {
                Model = model,
                Number = eventNumber,
                Deleted = model == null
            };
            var data = EventStoreCommon.Serialize(eventState);

            _ = await Engine.AppendAsync(Guid.NewGuid(), "StoreState", streamName, null, EventStoreState.Any, data);
        }
    }
}