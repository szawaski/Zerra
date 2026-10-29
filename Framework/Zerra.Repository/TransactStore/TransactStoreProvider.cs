// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Concrete transact store provider that executes queries and persist operations using an <see cref="ITransactStoreEngine"/>.
    /// </summary>
    /// <typeparam name="TModel">The model type managed by this provider.</typeparam>
    public sealed class TransactStoreProvider<TModel> : RootTransactStoreProvider<TModel>
        where TModel : class, new()
    {
        private const int deleteBatchSizeSingleIdentity = 1028;
        private const int deleteBatchSizeManyIdentity = 256;
        private const string defaultEventName = "Transact Store";

        private readonly int deleteBatchSize;
        private readonly ITransactStoreEngine engine;

        /// <summary>Initializes a new instance of <see cref="TransactStoreProvider{TModel}"/> on <paramref name="engine"/>.</summary>
        /// <param name="engine">The engine to use, such as one from a data context's GetEngine. Providers given the same in-memory engine share its store.</param>
        public TransactStoreProvider(ITransactStoreEngine engine)
        {
            if (engine is null)
                throw new ArgumentNullException(nameof(engine));
            this.deleteBatchSize = ModelTypeDetail.IdentityMembers.Count == 1 ? deleteBatchSizeSingleIdentity : deleteBatchSizeManyIdentity;
            this.engine = engine;
        }

        /// <inheritdoc/>
        protected override sealed IReadOnlyCollection<TModel> Many(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");

            var models = engine.ExecuteMany<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return models;
        }
        /// <inheritdoc/>
        protected override sealed TModel? First(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");

            var model = engine.ExecuteFirst<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return model;
        }
        /// <inheritdoc/>
        protected override sealed TModel? Single(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");

            var model = engine.ExecuteSingle<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return model;
        }
        /// <inheritdoc/>
        protected override sealed long Count(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");

            var count = engine.ExecuteCount<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return count;
        }
        /// <inheritdoc/>
        protected override sealed bool Any(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");

            var any = engine.ExecuteAny<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
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
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");

            var models = engine.ExecuteManyAsync<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return models;
        }
        /// <inheritdoc/>
        protected override sealed Task<TModel?> FirstAsync(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");

            var model = engine.ExecuteFirstAsync<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return model;
        }
        /// <inheritdoc/>
        protected override sealed Task<TModel?> SingleAsync(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");

            var model = engine.ExecuteSingleAsync<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return model;
        }
        /// <inheritdoc/>
        protected override sealed Task<long> CountAsync(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");

            var count = engine.ExecuteCountAsync<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
            return count;
        }
        /// <inheritdoc/>
        protected override sealed Task<bool> AnyAsync(Query query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");

            var any = engine.ExecuteAnyAsync<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelTypeDetail);
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
                var autoGeneratedCount = ModelTypeDetail.IdentityAutoGeneratedMembers.Count;

                if (autoGeneratedCount == 1)
                {
                    var identity = engine.ExecuteInsertGetIdentities<TModel>((TModel)model, graph, ModelTypeDetail);
                    var modelPropertyInfo = ModelTypeDetail.IdentityAutoGeneratedMembers[0];
                    identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                    modelPropertyInfo.SetterBoxed(model, identity);
                }
                else if (autoGeneratedCount > 1)
                {
                    var identities = (IList<object>)engine.ExecuteInsertGetIdentities<TModel>((TModel)model, graph, ModelTypeDetail);
                    var i = 0;
                    foreach (var modelPropertyInfo in ModelTypeDetail.IdentityAutoGeneratedMembers)
                    {
                        var identity = identities[i];
                        identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                        modelPropertyInfo.SetterBoxed(model, identity);
                        i++;
                    }
                }
                else
                {
                    var success = engine.ExecuteInsert<TModel>((TModel)model, graph, ModelTypeDetail);
                    if (!success)
                        throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                }
            }
            else
            {
                var success = engine.ExecuteUpdate<TModel>((TModel)model, graph, ModelTypeDetail);
                if (!success)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }

        /// <inheritdoc/>
        protected override sealed void DeleteModel(PersistEvent @event, object[] ids)
        {
            for (var i = 0; i < ids.Length; i += deleteBatchSize)
            {
                var deleteIds = ids.Skip(i).Take(deleteBatchSize).ToArray();
                var rowsAffected = engine.ExecuteDelete<TModel>(deleteIds, ModelTypeDetail);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }

        /// <inheritdoc/>
        protected override sealed async Task PersistModelAsync(PersistEvent @event, object model, Graph? graph, bool create)
        {
            if (create)
            {
                var autoGeneratedCount = ModelTypeDetail.IdentityAutoGeneratedMembers.Count;

                if (autoGeneratedCount == 1)
                {
                    var identity = await engine.ExecuteInsertGetIdentitiesAsync<TModel>((TModel)model, graph, ModelTypeDetail);
                    var modelPropertyInfo = ModelTypeDetail.IdentityAutoGeneratedMembers[0];
                    identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                    modelPropertyInfo.SetterBoxed(model, identity);

                }
                else if (autoGeneratedCount > 1)
                {
                    var identities = (IList<object>)await engine.ExecuteInsertGetIdentitiesAsync<TModel>((TModel)model, graph, ModelTypeDetail);
                    var i = 0;
                    foreach (var modelPropertyInfo in ModelTypeDetail.IdentityAutoGeneratedMembers)
                    {
                        var identity = identities[i];
                        identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                        modelPropertyInfo.SetterBoxed(model, identity);
                        i++;
                    }
                }
                else
                {
                    var success = await engine.ExecuteInsertAsync<TModel>((TModel)model, graph, ModelTypeDetail);
                    if (!success)
                        throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                }
            }
            else
            {
                var success = await engine.ExecuteUpdateAsync<TModel>((TModel)model, graph, ModelTypeDetail);
                if (!success)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }

        /// <inheritdoc/>
        protected override sealed async Task DeleteModelAsync(PersistEvent @event, object[] ids)
        {
            for (var i = 0; i < ids.Length; i += deleteBatchSize)
            {
                var deleteIds = ids.Skip(i).Take(deleteBatchSize).ToArray();
                var rowsAffected = await engine.ExecuteDeleteAsync<TModel>(deleteIds, ModelTypeDetail);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }
    }
}