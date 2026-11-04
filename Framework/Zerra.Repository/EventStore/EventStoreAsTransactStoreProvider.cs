// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Map;
using Zerra.Repository.Linq;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// A transactional store provider that reconstructs model state by replaying events from an <see cref="IEventStoreEngine"/>,
    /// enabling temporal queries over event-sourced data.
    /// </summary>
    /// <typeparam name="TModel">The model type managed by this provider.</typeparam>
    public sealed class EventStoreAsTransactStoreProvider<TModel> : RootTransactStoreProvider<TModel>
        where TModel : class, new()
    {
        private readonly ulong saveStateEvery;
        private readonly IEventStoreEngine engine;

        /// <summary>
        /// Initializes a new instance on <paramref name="engine"/>.
        /// </summary>
        /// <param name="engine">The engine to use, such as one from a data context's GetEngine. Providers given the same in-memory engine share its store.</param>
        /// <param name="saveStateEvery">The number of events after which the model's state is saved so reads replay from it instead of from the first event, 0 never saves the state.</param>
        public EventStoreAsTransactStoreProvider(IEventStoreEngine engine, ulong saveStateEvery = 100)
        {
            if (engine is null)
                throw new ArgumentNullException(nameof(engine));
            this.engine = engine;
            this.saveStateEvery = saveStateEvery;
        }

        /// <inheritdoc />
        protected override sealed bool SupportsTransactions => false;
        /// <inheritdoc/>
        protected override sealed object? Engine => engine;

        /// <inheritdoc/>
        protected override sealed IReadOnlyCollection<TModel> Many(LinqModelQuery query)
            => LinqHelper.Query(ReadModels(query.Where, null, null), query.Where, query.Order, query.Skip, query.Take).ToArray();
        /// <inheritdoc/>
        protected override sealed TModel? First(LinqModelQuery query)
            => LinqHelper.Query(ReadModels(query.Where, null, null), query.Where, query.Order, query.Skip, query.Take).FirstOrDefault();
        /// <inheritdoc/>
        protected override sealed TModel? Single(LinqModelQuery query)
            => LinqHelper.Query(ReadModels(query.Where, null, null), query.Where, query.Order, query.Skip, query.Take).SingleOrDefault();
        /// <inheritdoc/>
        protected override sealed long Count(LinqModelQuery query)
            => LinqHelper.Query(ReadModels(query.Where, null, null), query.Where, query.Order, query.Skip, query.Take).LongCount();
        /// <inheritdoc/>
        protected override sealed bool Any(LinqModelQuery query)
            => LinqHelper.Query(ReadModels(query.Where, null, null), query.Where, query.Order, query.Skip, query.Take).Any();

        /// <inheritdoc/>
        protected override sealed async Task<IReadOnlyCollection<TModel>> ManyAsync(LinqModelQuery query, CancellationToken cancellationToken)
            => LinqHelper.Query((await ReadModelsAsync(query.Where, null, null, cancellationToken)), query.Where, query.Order, query.Skip, query.Take).ToArray();
        /// <inheritdoc/>
        protected override sealed async Task<TModel?> FirstAsync(LinqModelQuery query, CancellationToken cancellationToken)
            => LinqHelper.Query((await ReadModelsAsync(query.Where, null, null, cancellationToken)), query.Where, query.Order, query.Skip, query.Take).FirstOrDefault();
        /// <inheritdoc/>
        protected override sealed async Task<TModel?> SingleAsync(LinqModelQuery query, CancellationToken cancellationToken)
            => LinqHelper.Query((await ReadModelsAsync(query.Where, null, null, cancellationToken)), query.Where, query.Order, query.Skip, query.Take).SingleOrDefault();
        /// <inheritdoc/>
        protected override sealed async Task<long> CountAsync(LinqModelQuery query, CancellationToken cancellationToken)
            => LinqHelper.Query((await ReadModelsAsync(query.Where, null, null, cancellationToken)), query.Where, query.Order, query.Skip, query.Take).LongCount();
        /// <inheritdoc/>
        protected override sealed async Task<bool> AnyAsync(LinqModelQuery query, CancellationToken cancellationToken)
            => LinqHelper.Query((await ReadModelsAsync(query.Where, null, null, cancellationToken)), query.Where, query.Order, query.Skip, query.Take).Any();

        /// <inheritdoc/>
        protected override sealed object? ExecuteLinq(LinqQuery query)
        {
            //runs over the current state of each model, or the states of its history, with the relations it reads loaded through their providers
            var temporal = query.Temporal;
            if (temporal is null)
                return Run(query, LoadLinqRelations(query, ReadModels(query.Where, null, null)));
            if (!temporal.Events)
                return Run(query, LoadLinqRelations(query, ReadModels(query.Where, temporal, HistoryLimit(query, query.Where))));

            var where = EventModelWhere(query.Where, out var whole);
            var eventModels = ReadEventModels(where, temporal, whole ? HistoryLimit(query, where) : null);
            if (eventModels.Count > 0)
            {
                OnQueryWithRelations(temporal.Graph);
                var models = OnGetWithRelations(eventModels.Select(x => x.Model).ToArray(), temporal.Graph);
                if (models.Count < eventModels.Count)
                    eventModels = eventModels.Where(x => models.Contains(x.Model)).ToList();
            }
            //the events are read for this query alone, and an event query loads only the relations its graph names
            return query.ExecuteRead(eventModels, false);
        }
        /// <inheritdoc/>
        protected override sealed async Task<object?> ExecuteLinqAsync(LinqQuery query, CancellationToken cancellationToken)
        {
            var temporal = query.Temporal;
            if (temporal is null)
                return Run(query, await LoadLinqRelationsAsync(query, await ReadModelsAsync(query.Where, null, null, cancellationToken), cancellationToken));
            if (!temporal.Events)
                return Run(query, await LoadLinqRelationsAsync(query, await ReadModelsAsync(query.Where, temporal, HistoryLimit(query, query.Where), cancellationToken), cancellationToken));

            var where = EventModelWhere(query.Where, out var whole);
            var eventModels = await ReadEventModelsAsync(where, temporal, whole ? HistoryLimit(query, where) : null, cancellationToken);
            if (eventModels.Count > 0)
            {
                OnQueryWithRelations(temporal.Graph);
                var models = await OnGetWithRelationsAsync(eventModels.Select(x => x.Model).ToArray(), temporal.Graph, cancellationToken);
                if (models.Count < eventModels.Count)
                    eventModels = eventModels.Where(x => models.Contains(x.Model)).ToList();
            }
            return query.ExecuteRead(eventModels, false);
        }
        /// <inheritdoc/>
        protected override sealed object ExecuteLinqStream(LinqQuery query) => query.StreamFrom(() => ExecuteLinq(query), null);
        /// <inheritdoc/>
        protected override sealed object ExecuteLinqStreamAsync(LinqQuery query, CancellationToken cancellationToken) => query.StreamFromAsync(cancellation => ExecuteLinqAsync(query, cancellation), null, cancellationToken);

        //the states are read for this query alone, so they're given as they are
        private static object? Run(LinqQuery query, IReadOnlyCollection<TModel> models) => query.ExecuteRead(models, false);

        //how many states of each model a query of history needs when it pages them in the order they are read: a where of identities alone keeps every state read
        private static int? HistoryLimit(LinqQuery query, LambdaExpression? where)
        {
            if (where is null || query.Order is not null || query.GroupBy is not null || query.Distinct || !IdentityOnly(where.Body, where.Parameters[0]))
                return null;
            if (query.Take.HasValue)
                return (query.Skip ?? 0) + query.Take.Value;
            if (query.Terminal == LinqTerminal.Any && !query.Skip.HasValue)
                return 1;
            return null;
        }
        private static bool IdentityOnly(Expression expression, ParameterExpression parameter)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                    {
                        var binary = (BinaryExpression)expression;
                        return IdentityOnly(binary.Left, parameter) && IdentityOnly(binary.Right, parameter);
                    }
                case ExpressionType.Equal:
                    {
                        var binary = (BinaryExpression)expression;
                        return (IsIdentity(binary.Left, parameter) && !LinqPlan.Uses(binary.Right, parameter)) || (IsIdentity(binary.Right, parameter) && !LinqPlan.Uses(binary.Left, parameter));
                    }
                case ExpressionType.Call:
                    {
                        var call = (MethodCallExpression)expression;
                        if (call.Method.Name != "Contains")
                            return false;
                        if (call.Object is null)
                            return call.Arguments.Count == 2 && IsIdentity(call.Arguments[1], parameter) && !LinqPlan.Uses(call.Arguments[0], parameter);
                        return call.Arguments.Count == 1 && IsIdentity(call.Arguments[0], parameter) && !LinqPlan.Uses(call.Object, parameter);
                    }
                default:
                    return false;
            }
        }
        private static bool IsIdentity(Expression expression, ParameterExpression parameter)
        {
            while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion)
                expression = conversion.Operand;
            if (expression is not MemberExpression member || member.Expression != parameter)
                return false;
            foreach (var identity in ModelAnalyzer.GetModel(typeof(TModel)).IdentityMembers)
            {
                if (identity.Name == member.Member.Name)
                    return true;
            }
            return false;
        }

        //an event query's condition names the models through x.Model; the parts of it on the model alone say which models' events to read, and all of it runs over the events
        private static Expression<Func<TModel, bool>>? EventModelWhere(LambdaExpression? where, out bool whole)
        {
            whole = true;
            if (where is null)
                return null;
            var model = Expression.Parameter(typeof(TModel), "x");
            var parts = new List<Expression>();
            var pending = new Stack<Expression>();
            pending.Push(where.Body);
            while (pending.Count > 0)
            {
                var part = pending.Pop();
                if (part.NodeType == ExpressionType.AndAlso)
                {
                    pending.Push(((BinaryExpression)part).Right);
                    pending.Push(((BinaryExpression)part).Left);
                    continue;
                }
                var rewriter = new EventModelRewriter(where.Parameters[0], model);
                var rewritten = rewriter.Visit(part);
                if (rewriter.ReadsEvent)
                    whole = false;
                else
                    parts.Add(rewritten);
            }
            if (parts.Count == 0)
                return null;
            var body = parts[0];
            for (var i = 1; i < parts.Count; i++)
                body = Expression.AndAlso(body, parts[i]);
            return Expression.Lambda<Func<TModel, bool>>(body, model);
        }
        private sealed class EventModelRewriter(ParameterExpression eventParameter, ParameterExpression model) : ExpressionVisitor
        {
            public bool ReadsEvent { get; private set; }
            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression == eventParameter && node.Member.Name == nameof(EventModel<TModel>.Model))
                    return model;
                return base.VisitMember(node);
            }
            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node == eventParameter)
                    ReadsEvent = true;
                return node;
            }
        }

        //the current state of each model, or the states of its history in the range; a limit keeps the first that many of each model's states in their order
        private List<TModel> ReadModels(LambdaExpression? where, LinqTemporal? temporal, int? limit)
        {
            var ids = GetIDs(where);
            var atTo = temporal is null || (temporal.Order == TemporalOrder.Newest && limit == 1);
            var models = new List<TModel>();
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);
                var (modelState, modelEventNumber) = ReadModelState(id, temporal, atTo);
                var eventDatas = engine.ReadBackwards(streamName, temporal?.NumberTo, null, modelEventNumber, null, temporal?.DateTo);
                LoadModels(eventDatas, modelState, temporal, limit, atTo, models);
            }
            return models;
        }
        private async Task<List<TModel>> ReadModelsAsync(LambdaExpression? where, LinqTemporal? temporal, int? limit, CancellationToken cancellationToken)
        {
            var ids = GetIDs(where);
            var atTo = temporal is null || (temporal.Order == TemporalOrder.Newest && limit == 1);
            var models = new List<TModel>();
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);
                var (modelState, modelEventNumber) = await ReadModelStateAsync(id, temporal, atTo, cancellationToken);
                var eventDatas = await engine.ReadBackwardsAsync(streamName, temporal?.NumberTo, null, modelEventNumber, null, temporal?.DateTo, cancellationToken);
                LoadModels(eventDatas, modelState, temporal, limit, atTo, models);
            }
            return models;
        }
        private List<EventModel<TModel>> ReadEventModels(LambdaExpression? where, LinqTemporal temporal, int? limit)
        {
            var ids = GetIDs(where);
            var atTo = temporal.Order == TemporalOrder.Newest && limit == 1;
            var eventModels = new List<EventModel<TModel>>();
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);
                var (modelState, modelEventNumber) = ReadModelState(id, temporal, atTo);
                //a deleted model's saved state has no model, and its events before the delete replay from the start
                if (modelState is null)
                    modelEventNumber = null;
                var eventDatas = engine.ReadBackwards(streamName, temporal.NumberTo, null, modelEventNumber, null, temporal.DateTo);
                LoadEventModels(eventDatas, modelState, temporal, limit, atTo, eventModels);
            }
            return eventModels;
        }
        private async Task<List<EventModel<TModel>>> ReadEventModelsAsync(LambdaExpression? where, LinqTemporal temporal, int? limit, CancellationToken cancellationToken)
        {
            var ids = GetIDs(where);
            var atTo = temporal.Order == TemporalOrder.Newest && limit == 1;
            var eventModels = new List<EventModel<TModel>>();
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);
                var (modelState, modelEventNumber) = await ReadModelStateAsync(id, temporal, atTo, cancellationToken);
                if (modelState is null)
                    modelEventNumber = null;
                var eventDatas = await engine.ReadBackwardsAsync(streamName, temporal.NumberTo, null, modelEventNumber, null, temporal.DateTo, cancellationToken);
                LoadEventModels(eventDatas, modelState, temporal, limit, atTo, eventModels);
            }
            return eventModels;
        }

        private static bool PastTo(LinqTemporal? temporal, EventStoreEventData eventData)
            => temporal is not null && ((temporal.DateTo.HasValue && temporal.DateTo.Value < eventData.Date) || (temporal.NumberTo.HasValue && temporal.NumberTo.Value < eventData.Number));
        private static bool InRange(LinqTemporal? temporal, EventStoreEventData eventData)
            => temporal is null || (!temporal.DateFrom.HasValue && !temporal.NumberFrom.HasValue) || (temporal.DateFrom.HasValue && eventData.Date >= temporal.DateFrom.Value) || (temporal.NumberFrom.HasValue && eventData.Number >= temporal.NumberFrom.Value);

        //the events read are newest first and replay oldest first; atTo keeps only the state the last of them leaves, when that event is in the range
        private void LoadModels(EventStoreEventData[] eventDatas, TModel? modelState, LinqTemporal? temporal, int? limit, bool atTo, List<TModel> models)
        {
            if (modelState is null && eventDatas.Length == 0)
                return;

            modelState ??= (TModel)base.ModelTypeDetail.Creator();

            if (atTo)
            {
                EventStoreEventData? last = null;
                for (var i = eventDatas.Length - 1; i >= 0; i--)
                {
                    var eventData = eventDatas[i];
                    //after a terminating event the model no longer exists, so it has no states
                    if (eventData.Deleted)
                        return;
                    if (PastTo(temporal, eventData))
                        break;

                    var eventModel = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
                    if (eventModel is null)
                        throw new Exception("Failed to deserialize Model");

                    Mapper.MapTo(eventModel.Model, modelState, eventModel.Graph);
                    last = eventData;
                }
                if (temporal is null || (last is not null && InRange(temporal, last)))
                    models.Add(modelState);
                return;
            }

            //newest first with a limit keeps the last states in the range, so they are counted first and only those copied
            var keepFrom = 0;
            if (temporal!.Order == TemporalOrder.Newest && limit.HasValue)
            {
                var inRange = 0;
                for (var i = eventDatas.Length - 1; i >= 0; i--)
                {
                    var eventData = eventDatas[i];
                    if (eventData.Deleted)
                        return;
                    if (PastTo(temporal, eventData))
                        break;
                    if (InRange(temporal, eventData))
                        inRange++;
                }
                keepFrom = inRange - limit.Value;
            }

            var start = models.Count;
            var index = 0;
            for (var i = eventDatas.Length - 1; i >= 0; i--)
            {
                var eventData = eventDatas[i];
                //after a terminating event the model no longer exists, so it has no states
                if (eventData.Deleted)
                {
                    models.RemoveRange(start, models.Count - start);
                    return;
                }
                if (PastTo(temporal, eventData))
                    break;

                var eventModel = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
                if (eventModel is null)
                    throw new Exception("Failed to deserialize Model");

                Mapper.MapTo(eventModel.Model, modelState, eventModel.Graph);

                if (!InRange(temporal, eventData) || index++ < keepFrom)
                    continue;
                models.Add(Mapper.Copy(modelState));
                if (temporal.Order == TemporalOrder.Oldest && models.Count - start == limit)
                    break;
            }
            if (temporal.Order == TemporalOrder.Newest)
                models.Reverse(start, models.Count - start);
        }
        private void LoadEventModels(EventStoreEventData[] eventDatas, TModel? modelState, LinqTemporal temporal, int? limit, bool atTo, List<EventModel<TModel>> eventModels)
        {
            if (modelState is null && eventDatas.Length == 0)
                return;

            modelState ??= (TModel)base.ModelTypeDetail.Creator();

            if (atTo)
            {
                EventStoreEventModelData<TModel>? lastData = null;
                EventStoreEventData? last = null;
                for (var i = eventDatas.Length - 1; i >= 0; i--)
                {
                    var eventData = eventDatas[i];
                    if (PastTo(temporal, eventData))
                        break;

                    //a terminating event changes nothing, its model is the state it deleted, and one stored before they carried data has none
                    if (eventData.Deleted && eventData.Data.IsEmpty)
                        lastData = new();
                    else
                        lastData = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
                    if (lastData is null)
                        throw new Exception("Failed to deserialize Model");

                    if (!eventData.Deleted)
                        Mapper.MapTo(lastData.Model, modelState, lastData.Graph);
                    last = eventData;
                }
                if (last is not null && InRange(temporal, last))
                    eventModels.Add(NewEventModel(last, lastData!, modelState));
                return;
            }

            var keepFrom = 0;
            if (temporal.Order == TemporalOrder.Newest && limit.HasValue)
            {
                var inRange = 0;
                for (var i = eventDatas.Length - 1; i >= 0; i--)
                {
                    var eventData = eventDatas[i];
                    if (PastTo(temporal, eventData))
                        break;
                    if (InRange(temporal, eventData))
                        inRange++;
                }
                keepFrom = inRange - limit.Value;
            }

            var start = eventModels.Count;
            var index = 0;
            for (var i = eventDatas.Length - 1; i >= 0; i--)
            {
                var eventData = eventDatas[i];
                if (PastTo(temporal, eventData))
                    break;

                EventStoreEventModelData<TModel>? eventModelData;
                if (eventData.Deleted && eventData.Data.IsEmpty)
                    eventModelData = new();
                else
                    eventModelData = EventStoreCommon.Deserialize<EventStoreEventModelData<TModel>>(eventData.Data.Span);
                if (eventModelData is null)
                    throw new Exception("Failed to deserialize Model");

                if (!eventData.Deleted)
                    Mapper.MapTo(eventModelData.Model, modelState, eventModelData.Graph);

                if (!InRange(temporal, eventData) || index++ < keepFrom)
                    continue;
                eventModels.Add(NewEventModel(eventData, eventModelData, Mapper.Copy(modelState)));
                if (temporal.Order == TemporalOrder.Oldest && eventModels.Count - start == limit)
                    break;
            }
            if (temporal.Order == TemporalOrder.Newest)
                eventModels.Reverse(start, eventModels.Count - start);
        }
        private static EventModel<TModel> NewEventModel(EventStoreEventData eventData, EventStoreEventModelData<TModel> eventModelData, TModel model)
        {
            return new EventModel<TModel>()
            {
                EventID = eventData.EventID,
                EventName = eventData.EventName,
                Date = eventData.Date,
                Number = eventData.Number,
                Deleted = eventData.Deleted,

                Model = model,

                ModelChange = eventModelData.Model,
                GraphChange = eventModelData.Graph,
                Source = eventModelData.Source,
                SourceType = eventModelData.SourceType
            };
        }

        private (TModel?, ulong?) ReadModelState(object id, LinqTemporal? temporal, bool atTo)
        {
            ulong? numberAtOrBefore;
            DateTime? dateBefore = null;
            DateTime? dateAtOrBefore = null;
            if (!atTo)
            {
                //the states from the From bound on are read, the replay starts before it
                if (!temporal!.NumberFrom.HasValue && !temporal.DateFrom.HasValue)
                    return (null, null);
                numberAtOrBefore = temporal.NumberFrom;
                dateBefore = temporal.DateFrom;
            }
            else
            {
                numberAtOrBefore = temporal?.NumberTo;
                dateAtOrBefore = temporal?.DateTo;
            }

            var streamName = EventStoreCommon.GetStateStreamName<TModel>(id);
            var bounded = numberAtOrBefore.HasValue || dateBefore.HasValue || dateAtOrBefore.HasValue;
            var eventDatas = engine.ReadBackwards(streamName, null, bounded ? null : 1, null, null, null);

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
        private async Task<(TModel?, ulong?)> ReadModelStateAsync(object id, LinqTemporal? temporal, bool atTo, CancellationToken cancellationToken)
        {
            ulong? numberAtOrBefore;
            DateTime? dateBefore = null;
            DateTime? dateAtOrBefore = null;
            if (!atTo)
            {
                if (!temporal!.NumberFrom.HasValue && !temporal.DateFrom.HasValue)
                    return (null, null);
                numberAtOrBefore = temporal.NumberFrom;
                dateBefore = temporal.DateFrom;
            }
            else
            {
                numberAtOrBefore = temporal?.NumberTo;
                dateAtOrBefore = temporal?.DateTo;
            }

            var streamName = EventStoreCommon.GetStateStreamName<TModel>(id);
            var bounded = numberAtOrBefore.HasValue || dateBefore.HasValue || dateAtOrBefore.HasValue;
            var eventDatas = await engine.ReadBackwardsAsync(streamName, null, bounded ? null : 1, null, null, null, cancellationToken);

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

        private static object[] GetIDs(LambdaExpression? where)
        {
            if (where is null)
                throw new NotSupportedException("No identity clauses found in the query. These are required for event stores.");

            var type = typeof(TModel);

            var identityProperties = ModelAnalyzer.GetIdentityPropertyNames(type);
            object[] ids;
            if (identityProperties.Length == 1)
            {
                var propertyValues = LinqValueExtractor.Extract(where, type, identityProperties[0]);
                var idSingle = propertyValues[identityProperties[0]].ToArray();
                if (idSingle.Any(x => x is null))
                    throw new Exception($"Model {type.Name} missing Identity");
                ids = idSingle!;
            }
            else
            {
                var propertyValues = LinqValueExtractor.Extract(where, type, identityProperties);
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

            var eventNumber = engine.Append(@event.ID, @event.Name, streamName, null, create ? EventStoreState.NotExisting : EventStoreState.Existing, data);

            if (saveStateEvery > 0 && eventNumber > 0 && eventNumber % saveStateEvery == 0)
            {
                var thisEventData = engine.ReadBackwards(streamName, eventNumber, 1, null, null, null)[0];

                //read from this provider, the state is saved as stored and not as the layers above it change it
                var where = ModelAnalyzer.GetIdentityExpression<TModel>(id);
                var modelState = ReadModels(where, new LinqTemporal(modelType, false, TemporalOrder.Newest, null, null, null, eventNumber, null), 1).Single();

                SaveModelState(id, modelState, eventNumber, thisEventData.Date);
            }
        }
        /// <inheritdoc/>
        protected override sealed void DeleteModel(PersistEvent @event, object[] ids)
        {
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);

                var eventStoreModel = new EventStoreEventModelData<TModel>()
                {
                    Source = @event.Source,
                    SourceType = @event.Source?.GetType().Name
                };

                var data = EventStoreCommon.Serialize(eventStoreModel);

                var eventNumber = engine.Terminate(@event.ID, @event.Name, streamName, null, EventStoreState.Existing, data);

                SaveModelState(id, null, eventNumber, null);
            }
        }

        /// <inheritdoc/>
        protected override sealed async Task PersistModelAsync(PersistEvent @event, object model, Graph? graph, bool create, CancellationToken cancellationToken)
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

            var eventNumber = await engine.AppendAsync(@event.ID, @event.Name, streamName, null, create ? EventStoreState.NotExisting : EventStoreState.Existing, data, cancellationToken);

            if (saveStateEvery > 0 && eventNumber > 0 && eventNumber % saveStateEvery == 0)
            {
                var thisEventData = (await engine.ReadBackwardsAsync(streamName, eventNumber, 1, null, null, null, cancellationToken))[0];

                var where = ModelAnalyzer.GetIdentityExpression<TModel>(id);
                var modelState = (await ReadModelsAsync(where, new LinqTemporal(modelType, false, TemporalOrder.Newest, null, null, null, eventNumber, null), 1, cancellationToken)).Single();

                await SaveModelStateAsync(id, modelState, eventNumber, thisEventData.Date, cancellationToken);
            }
        }
        /// <inheritdoc/>
        protected override sealed async Task DeleteModelAsync(PersistEvent @event, object[] ids, CancellationToken cancellationToken)
        {
            foreach (var id in ids)
            {
                var streamName = EventStoreCommon.GetStreamName<TModel>(id);

                var eventStoreModel = new EventStoreEventModelData<TModel>()
                {
                    Source = @event.Source,
                    SourceType = @event.Source?.GetType().Name
                };

                var data = EventStoreCommon.Serialize(eventStoreModel);

                var eventNumber = await engine.TerminateAsync(@event.ID, @event.Name, streamName, null, EventStoreState.Existing, data, cancellationToken);

                await SaveModelStateAsync(id, null, eventNumber, null, cancellationToken);
            }
        }

        /// <inheritdoc/>
        protected override sealed int UpdateModelsWhere(PersistEvent @event, LambdaExpression where, object model, Graph graph)
        {
            //each matching stream gets the change as an update event, the condition names the identities as every event store query does
            var ids = GetIDsWhere(where);
            foreach (var id in ids)
            {
                var copy = Mapper.Copy((TModel)model);
                ModelAnalyzer.SetIdentity(modelType, copy, id);
                PersistModel(@event, copy, graph, false);
            }
            return ids.Length;
        }
        /// <inheritdoc/>
        protected override sealed int UpdateModelsWhere(PersistEvent @event, LambdaExpression where, LambdaExpression set)
        {
            //each matching stream gets the values computed from its current state as an update event
            var models = LinqHelper.Query(ReadModels(where, null, null), where, null, null, null).ToArray();
            var sets = ProjectionMember.FromSet(set);
            var getters = new Func<TModel, object?>[sets.Length];
            var names = new string[sets.Length];
            for (var i = 0; i < sets.Length; i++)
            {
                getters[i] = AggregateCommon.CompileBoxed<TModel>(sets[i].Expression!, sets[i].Parameter!, false);
                names[i] = sets[i].MemberDetail.Name;
            }
            var graph = new Graph(names);
            foreach (var model in models)
            {
                var copy = new TModel();
                ModelAnalyzer.SetIdentity(modelType, copy, ModelAnalyzer.GetIdentity(modelType, model));
                for (var i = 0; i < sets.Length; i++)
                    sets[i].MemberDetail.SetterBoxed!(copy, getters[i](model));
                PersistModel(@event, copy, graph, false);
            }
            return models.Length;
        }
        /// <inheritdoc/>
        protected override sealed int DeleteModelsWhere(PersistEvent @event, LambdaExpression where)
        {
            var ids = GetIDsWhere(where);
            DeleteModel(@event, ids);
            return ids.Length;
        }
        /// <inheritdoc/>
        protected override sealed async Task<int> UpdateModelsWhereAsync(PersistEvent @event, LambdaExpression where, object model, Graph graph, CancellationToken cancellationToken)
        {
            var ids = await GetIDsWhereAsync(where, cancellationToken);
            foreach (var id in ids)
            {
                var copy = Mapper.Copy((TModel)model);
                ModelAnalyzer.SetIdentity(modelType, copy, id);
                await PersistModelAsync(@event, copy, graph, false, cancellationToken);
            }
            return ids.Length;
        }
        /// <inheritdoc/>
        protected override sealed async Task<int> UpdateModelsWhereAsync(PersistEvent @event, LambdaExpression where, LambdaExpression set, CancellationToken cancellationToken)
        {
            //each matching stream gets the values computed from its current state as an update event
            var models = LinqHelper.Query(await ReadModelsAsync(where, null, null, cancellationToken), where, null, null, null).ToArray();
            var sets = ProjectionMember.FromSet(set);
            var getters = new Func<TModel, object?>[sets.Length];
            var names = new string[sets.Length];
            for (var i = 0; i < sets.Length; i++)
            {
                getters[i] = AggregateCommon.CompileBoxed<TModel>(sets[i].Expression!, sets[i].Parameter!, false);
                names[i] = sets[i].MemberDetail.Name;
            }
            var graph = new Graph(names);
            foreach (var model in models)
            {
                var copy = new TModel();
                ModelAnalyzer.SetIdentity(modelType, copy, ModelAnalyzer.GetIdentity(modelType, model));
                for (var i = 0; i < sets.Length; i++)
                    sets[i].MemberDetail.SetterBoxed!(copy, getters[i](model));
                await PersistModelAsync(@event, copy, graph, false, cancellationToken);
            }
            return models.Length;
        }
        /// <inheritdoc/>
        protected override sealed async Task<int> DeleteModelsWhereAsync(PersistEvent @event, LambdaExpression where, CancellationToken cancellationToken)
        {
            var ids = await GetIDsWhereAsync(where, cancellationToken);
            await DeleteModelAsync(@event, ids, cancellationToken);
            return ids.Length;
        }
        private object[] GetIDsWhere(LambdaExpression where)
        {
            var models = ReadModels(where, null, null);
            return LinqHelper.Query(models, where, null, null, null).Select(x => ModelAnalyzer.GetIdentity(modelType, x)).ToArray();
        }
        private async Task<object[]> GetIDsWhereAsync(LambdaExpression where, CancellationToken cancellationToken)
        {
            var models = await ReadModelsAsync(where, null, null, cancellationToken);
            return LinqHelper.Query(models, where, null, null, null).Select(x => ModelAnalyzer.GetIdentity(modelType, x)).ToArray();
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

            _ = engine.Append(Guid.NewGuid(), "StoreState", streamName, null, EventStoreState.Any, data);
        }
        private async Task SaveModelStateAsync(object id, TModel? model, ulong eventNumber, DateTime? date, CancellationToken cancellationToken)
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

            _ = await engine.AppendAsync(Guid.NewGuid(), "StoreState", streamName, null, EventStoreState.Any, data, cancellationToken);
        }
    }
}
