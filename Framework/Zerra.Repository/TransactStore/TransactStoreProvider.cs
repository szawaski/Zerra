// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Concrete transact store provider that executes queries and persist operations against a <typeparamref name="TContext"/> data context using an <see cref="ITransactStoreEngine"/>.
    /// </summary>
    /// <typeparam name="TContext">The data context type that supplies the engine.</typeparam>
    /// <typeparam name="TModel">The model type managed by this provider.</typeparam>
    public class TransactStoreProvider<TContext, TModel> : RootTransactStoreProvider<TModel>
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
            this.deleteBatchSize = ModelTypeDetail.IdentityProperties.Count == 1 ? deleteBatchSizeSingleIdentity : deleteBatchSizeManyIdentity;
            var context = new TContext();
            if (!context.TryGetEngine(out var engine))
                throw new Exception($"{typeof(TContext).Name} could not produce an engine of {typeof(ITransactStoreEngine).Name}");
            if (engine is not ITransactStoreEngine transactStoreEngine)
                throw new Exception($"{typeof(TContext).Name} produced an engine of {engine.GetType().Name} which is not a {typeof(ITransactStoreEngine).Name}");
            this.Engine = transactStoreEngine;
        }

        /// <inheritdoc/>
        protected override sealed IReadOnlyCollection<TModel> Many(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var models = Engine.ExecuteMany<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return models;
        }
        /// <inheritdoc/>
        protected override sealed TModel? First(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var model = Engine.ExecuteFirst<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return model;
        }
        /// <inheritdoc/>
        protected override sealed TModel? Single(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var model = Engine.ExecuteSingle<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return model;
        }
        /// <inheritdoc/>
        protected override sealed long Count(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var count = Engine.ExecuteCount(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return count;
        }
        /// <inheritdoc/>
        protected override sealed bool Any(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var any = Engine.ExecuteAny(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return any;
        }
        /// <inheritdoc/>
        protected override sealed IReadOnlyCollection<EventModel<TModel>> EventMany(Query query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed EventModel<TModel>? EventFirst(Query query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed EventModel<TModel>? EventSingle(Query query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed long EventCount(Query query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed bool EventAny(Query query) => throw new NotSupportedException("Event queries not supported with this provider");

        /// <inheritdoc/>
        protected override sealed Task<IReadOnlyCollection<TModel>> ManyAsync(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var models = Engine.ExecuteManyAsync<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return models;
        }
        /// <inheritdoc/>
        protected override sealed Task<TModel?> FirstAsync(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var model = Engine.ExecuteFirstAsync<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return model;
        }
        /// <inheritdoc/>
        protected override sealed Task<TModel?> SingleAsync(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var model = Engine.ExecuteSingleAsync<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return model;
        }
        /// <inheritdoc/>
        protected override sealed Task<long> CountAsync(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var count = Engine.ExecuteCountAsync(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return count;
        }
        /// <inheritdoc/>
        protected override sealed Task<bool> AnyAsync(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var any = Engine.ExecuteAnyAsync(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return any;
        }
        /// <inheritdoc/>
        protected override sealed Task<IReadOnlyCollection<EventModel<TModel>>> EventManyAsync(Query query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed Task<EventModel<TModel>?> EventFirstAsync(Query query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed Task<EventModel<TModel>?> EventSingleAsync(Query query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed Task<long> EventCountAsync(Query query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed Task<bool> EventAnyAsync(Query query) => throw new NotSupportedException("Event queries not supported with this provider");

        /// <inheritdoc/>
        protected override sealed void PersistModel(PersistEvent @event, object model, Graph? graph, bool create)
        {
            if (create)
            {
                var autoGeneratedCount = ModelTypeDetail.IdentityAutoGeneratedProperties.Count;

                if (autoGeneratedCount == 1)
                {
                    var rows = Engine.ExecuteInsertGetIdentities(model, graph, ModelTypeDetail);
                    if (rows.Count != 1)
                        throw new Exception($"Insert failed: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                    var identity = rows.First();
                    var modelPropertyInfo = ModelTypeDetail.IdentityAutoGeneratedProperties[0];
                    identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                    modelPropertyInfo.SetterBoxed(model, identity);

                }
                else if (autoGeneratedCount > 1)
                {
                    var rows = Engine.ExecuteInsertGetIdentities(model, graph, ModelTypeDetail);
                    if (rows.Count != 1)
                        throw new Exception($"Insert failed: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                    var identities = (IList<object>)rows.First();
                    var i = 0;
                    foreach (var modelPropertyInfo in ModelTypeDetail.IdentityAutoGeneratedProperties)
                    {
                        var identity = modelPropertyInfo.GetterBoxed(identities[i])!;
                        identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                        modelPropertyInfo.SetterBoxed(model, identity);
                        i++;
                    }
                }
                else
                {
                    var rows = Engine.ExecuteInsert(model, graph, ModelTypeDetail);
                    if (rows == 0)
                        throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                }
            }
            else
            {
                var rowsAffected = Engine.ExecuteUpdate(model, graph, ModelTypeDetail);
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
                var rowsAffected = Engine.ExecuteDelete(deleteIds, ModelTypeDetail);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }

        /// <inheritdoc/>
        protected override sealed async Task PersistModelAsync(PersistEvent @event, object model, Graph? graph, bool create)
        {
            if (create)
            {
                var autoGeneratedCount = ModelTypeDetail.IdentityAutoGeneratedProperties.Count;

                if (autoGeneratedCount == 1)
                {
                    var rows = await Engine.ExecuteInsertGetIdentitiesAsync(model, graph, ModelTypeDetail);
                    if (rows.Count != 1)
                        throw new Exception($"Insert failed: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                    var identity = rows.First();
                    var modelPropertyInfo = ModelTypeDetail.IdentityAutoGeneratedProperties[0];
                    identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                    modelPropertyInfo.SetterBoxed(model, identity);

                }
                else if (autoGeneratedCount > 1)
                {
                    var rows = await Engine.ExecuteInsertGetIdentitiesAsync(model, graph, ModelTypeDetail);
                    if (rows.Count != 1)
                        throw new Exception($"Insert failed: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                    var identities = (IList<object>)rows.First();
                    var i = 0;
                    foreach (var modelPropertyInfo in ModelTypeDetail.IdentityAutoGeneratedProperties)
                    {
                        var identity = modelPropertyInfo.GetterBoxed(identities[i])!;
                        identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                        modelPropertyInfo.SetterBoxed(model, identity);
                        i++;
                    }
                }
                else
                {
                    var rows = await Engine.ExecuteInsertAsync(model, graph, ModelTypeDetail);
                    if (rows == 0)
                        throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                }
            }
            else
            {
                var rowsAffected = await Engine.ExecuteUpdateAsync(model, graph, ModelTypeDetail);
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
                var rowsAffected = await Engine.ExecuteDeleteAsync(deleteIds, ModelTypeDetail);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }
    }
}