// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Concrete transact store provider that executes queries and persist operations against a <typeparamref name="TContext"/> data context using an <see cref="ITransactStoreEngine"/>.
    /// </summary>
    /// <typeparam name="TContext">The data context type that supplies the engine.</typeparam>
    /// <typeparam name="TModel">The model type managed by this provider.</typeparam>
    public abstract class TransactStoreProvider<TContext, TModel> : RootTransactStoreProvider<TModel>
        where TContext : DataContext, new()
        where TModel : class, new()
    {
        private const int deleteBatchSizeSingleIdentity = 1028;
        private const int deleteBatchSizeManyIdentity = 256;
        private const string defaultEventName = "Transact Store";

        private readonly int deleteBatchSize;
        /// <summary>The engine used to execute queries and persist operations against the underlying data store.</summary>
        protected readonly ITransactStoreEngine Engine;

        /// <summary>Initializes a new instance of <see cref="TransactStoreProvider{TContext, TModel}"/>, resolving the <see cref="ITransactStoreEngine"/> from a new <typeparamref name="TContext"/> instance.</summary>
        public TransactStoreProvider()
        {
            this.deleteBatchSize = ModelDetail.IdentityProperties.Count == 1 ? deleteBatchSizeSingleIdentity : deleteBatchSizeManyIdentity;
            var context = new TContext();
            if (!context.TryGetEngine(out var engine))
                throw new Exception($"{typeof(TContext).Name} could not produce an engine of {typeof(ITransactStoreEngine).Name}");
            if (engine is not ITransactStoreEngine transactStoreEngine)
                throw new Exception($"{typeof(TContext).Name} produced an engine of {engine.GetType().Name} which is not a {typeof(ITransactStoreEngine).Name}");
            this.Engine = transactStoreEngine;
        }

        /// <inheritdoc/>
        protected override sealed IReadOnlyCollection<TModel> QueryMany(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var models = Engine.ExecuteQueryToModelMany<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return models;
        }
        /// <inheritdoc/>
        protected override sealed TModel? QueryFirst(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var model = Engine.ExecuteQueryToModelFirst<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return model;
        }
        /// <inheritdoc/>
        protected override sealed TModel? QuerySingle(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var model = Engine.ExecuteQueryToModelSingle<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return model;
        }
        /// <inheritdoc/>
        protected override sealed long QueryCount(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var count = Engine.ExecuteQueryCount(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return count;
        }
        /// <inheritdoc/>
        protected override sealed bool QueryAny(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var any = Engine.ExecuteQueryAny(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return any;
        }
        /// <inheritdoc/>
        protected override sealed IReadOnlyCollection<EventModel<TModel>> QueryEventMany(Query query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed EventModel<TModel>? QueryEventFirst(Query query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed EventModel<TModel>? QueryEventSingle(Query query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed long QueryEventCount(Query query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed bool QueryEventAny(Query query) => throw new NotSupportedException("Event queries not supported with this provider");

        /// <inheritdoc/>
        protected override sealed Task<IReadOnlyCollection<TModel>> QueryManyAsync(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var models = Engine.ExecuteQueryToModelManyAsync<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return models;
        }
        /// <inheritdoc/>
        protected override sealed Task<TModel?> QueryFirstAsync(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var model = Engine.ExecuteQueryToModelFirstAsync<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return model;
        }
        /// <inheritdoc/>
        protected override sealed Task<TModel?> QuerySingleAsync(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var model = Engine.ExecuteQueryToModelSingleAsync<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return model;
        }
        /// <inheritdoc/>
        protected override sealed Task<long> QueryCountAsync(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var count = Engine.ExecuteQueryCountAsync(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return count;
        }
        /// <inheritdoc/>
        protected override sealed Task<bool> QueryAnyAsync(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var any = Engine.ExecuteQueryAnyAsync(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return any;
        }
        /// <inheritdoc/>
        protected override sealed Task<IReadOnlyCollection<EventModel<TModel>>> QueryEventManyAsync(Query query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed Task<EventModel<TModel>?> QueryEventFirstAsync(Query query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed Task<EventModel<TModel>?> QueryEventSingleAsync(Query query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed Task<long> QueryEventCountAsync(Query query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed Task<bool> QueryEventAnyAsync(Query query) => throw new NotSupportedException("Event queries not supported with this provider");

        /// <inheritdoc/>
        protected override sealed void PersistModel(PersistEvent @event, object model, Graph? graph, bool create)
        {
            if (create)
            {
                var autoGeneratedCount = ModelDetail.IdentityAutoGeneratedProperties.Count;

                if (autoGeneratedCount == 1)
                {
                    var rows = Engine.ExecuteInsertGetIdentities(model, graph, ModelDetail);
                    if (rows.Count != 1)
                        throw new Exception($"Insert failed: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                    var identity = rows.First();
                    var modelPropertyInfo = ModelDetail.IdentityAutoGeneratedProperties[0];
                    identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                    modelPropertyInfo.SetterBoxed(model, identity);

                }
                else if (autoGeneratedCount > 1)
                {
                    var rows = Engine.ExecuteInsertGetIdentities(model, graph, ModelDetail);
                    if (rows.Count != 1)
                        throw new Exception($"Insert failed: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                    var identities = (IList<object>)rows.First();
                    var i = 0;
                    foreach (var modelPropertyInfo in ModelDetail.IdentityAutoGeneratedProperties)
                    {
                        var identity = modelPropertyInfo.GetterBoxed(identities[i])!;
                        identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                        modelPropertyInfo.SetterBoxed(model, identity);
                        i++;
                    }
                }
                else
                {
                    var rows = Engine.ExecuteInsert(model, graph, ModelDetail);
                    if (rows == 0)
                        throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                }
            }
            else
            {
                var rowsAffected = Engine.ExecuteUpdate(model, graph, ModelDetail);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }

        /// <inheritdoc/>
        protected override sealed void DeleteModel(PersistEvent @event, object[] ids)
        {
            for (var i = 0; i <= ids.Length; i += deleteBatchSize)
            {
                var deleteIds = ids.Skip(i).Take(deleteBatchSize).ToArray();
                var rowsAffected = Engine.ExecuteDelete(deleteIds, ModelDetail);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }

        /// <inheritdoc/>
        protected override sealed async Task PersistModelAsync(PersistEvent @event, object model, Graph? graph, bool create)
        {
            if (create)
            {
                var autoGeneratedCount = ModelDetail.IdentityAutoGeneratedProperties.Count;

                if (autoGeneratedCount == 1)
                {
                    var rows = await Engine.ExecuteInsertGetIdentitiesAsync(model, graph, ModelDetail);
                    if (rows.Count != 1)
                        throw new Exception($"Insert failed: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                    var identity = rows.First();
                    var modelPropertyInfo = ModelDetail.IdentityAutoGeneratedProperties[0];
                    identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                    modelPropertyInfo.SetterBoxed(model, identity);

                }
                else if (autoGeneratedCount > 1)
                {
                    var rows = await Engine.ExecuteInsertGetIdentitiesAsync(model, graph, ModelDetail);
                    if (rows.Count != 1)
                        throw new Exception($"Insert failed: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                    var identities = (IList<object>)rows.First();
                    var i = 0;
                    foreach (var modelPropertyInfo in ModelDetail.IdentityAutoGeneratedProperties)
                    {
                        var identity = modelPropertyInfo.GetterBoxed(identities[i])!;
                        identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                        modelPropertyInfo.SetterBoxed(model, identity);
                        i++;
                    }
                }
                else
                {
                    var rows = await Engine.ExecuteInsertAsync(model, graph, ModelDetail);
                    if (rows == 0)
                        throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                }
            }
            else
            {
                var rowsAffected = await Engine.ExecuteUpdateAsync(model, graph, ModelDetail);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }

        /// <inheritdoc/>
        protected override sealed async Task DeleteModelAsync(PersistEvent @event, object[] ids)
        {
            for (var i = 0; i <= ids.Length; i += deleteBatchSize)
            {
                var deleteIds = ids.Skip(i).Take(deleteBatchSize).ToArray();
                var rowsAffected = await Engine.ExecuteDeleteAsync(deleteIds, ModelDetail);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }
    }
}