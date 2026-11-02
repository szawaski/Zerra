// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
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
        protected override sealed object? Engine => engine;
        /// <inheritdoc/>
        protected override sealed bool ReadsInStore => true;

        /// <inheritdoc/>
        protected override sealed IReadOnlyCollection<TModel> Many(LinqModelQuery query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");
            return (IReadOnlyCollection<TModel>)engine.ExecuteLinq<TModel, TModel>(new LinqQuery<TModel, TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, LinqTerminal.Sequence), ModelTypeDetail)!;
        }
        /// <inheritdoc/>
        protected override sealed TModel? First(LinqModelQuery query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");
            return ((IReadOnlyList<TModel>)engine.ExecuteLinq<TModel, TModel>(new LinqQuery<TModel, TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, LinqTerminal.FirstOrDefault), ModelTypeDetail)!).FirstOrDefault();
        }
        /// <inheritdoc/>
        protected override sealed TModel? Single(LinqModelQuery query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");
            return ((IReadOnlyList<TModel>)engine.ExecuteLinq<TModel, TModel>(new LinqQuery<TModel, TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, LinqTerminal.SingleOrDefault), ModelTypeDetail)!).SingleOrDefault();
        }
        /// <inheritdoc/>
        protected override sealed long Count(LinqModelQuery query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");
            return (long)engine.ExecuteLinq<TModel, long>(new LinqQuery<TModel, long>(query.Where, query.Order, query.Skip, query.Take, query.Graph, LinqTerminal.LongCount), ModelTypeDetail)!;
        }
        /// <inheritdoc/>
        protected override sealed bool Any(LinqModelQuery query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");
            return (bool)engine.ExecuteLinq<TModel, bool>(new LinqQuery<TModel, bool>(query.Where, query.Order, query.Skip, query.Take, query.Graph, LinqTerminal.Any), ModelTypeDetail)!;
        }
        /// <inheritdoc/>
        protected override sealed object? ExecuteLinq(LinqQuery query) => query.Execute(engine, ModelTypeDetail);
        /// <inheritdoc/>
        protected override sealed Task<object?> ExecuteLinqAsync(LinqQuery query, CancellationToken cancellationToken) => query.ExecuteAsync(engine, ModelTypeDetail, cancellationToken);
        /// <inheritdoc/>
        protected override sealed IReadOnlyCollection<EventModel<TModel>> EventMany(LinqModelQuery query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed EventModel<TModel>? EventFirst(LinqModelQuery query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed EventModel<TModel>? EventSingle(LinqModelQuery query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed long EventCount(LinqModelQuery query) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed bool EventAny(LinqModelQuery query) => throw new NotSupportedException("Event queries not supported with this provider");

        /// <inheritdoc/>
        protected override sealed async Task<IReadOnlyCollection<TModel>> ManyAsync(LinqModelQuery query, CancellationToken cancellationToken)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");
            return (IReadOnlyCollection<TModel>)(await engine.ExecuteLinqAsync<TModel, TModel>(new LinqQuery<TModel, TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, LinqTerminal.Sequence), ModelTypeDetail, cancellationToken))!;
        }
        /// <inheritdoc/>
        protected override sealed async Task<TModel?> FirstAsync(LinqModelQuery query, CancellationToken cancellationToken)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");
            return ((IReadOnlyList<TModel>)(await engine.ExecuteLinqAsync<TModel, TModel>(new LinqQuery<TModel, TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, LinqTerminal.FirstOrDefault), ModelTypeDetail, cancellationToken))!).FirstOrDefault();
        }
        /// <inheritdoc/>
        protected override sealed async Task<TModel?> SingleAsync(LinqModelQuery query, CancellationToken cancellationToken)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");
            return ((IReadOnlyList<TModel>)(await engine.ExecuteLinqAsync<TModel, TModel>(new LinqQuery<TModel, TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, LinqTerminal.SingleOrDefault), ModelTypeDetail, cancellationToken))!).SingleOrDefault();
        }
        /// <inheritdoc/>
        protected override sealed async Task<long> CountAsync(LinqModelQuery query, CancellationToken cancellationToken)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");
            return (long)(await engine.ExecuteLinqAsync<TModel, long>(new LinqQuery<TModel, long>(query.Where, query.Order, query.Skip, query.Take, query.Graph, LinqTerminal.LongCount), ModelTypeDetail, cancellationToken))!;
        }
        /// <inheritdoc/>
        protected override sealed async Task<bool> AnyAsync(LinqModelQuery query, CancellationToken cancellationToken)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TModel>)}");
            return (bool)(await engine.ExecuteLinqAsync<TModel, bool>(new LinqQuery<TModel, bool>(query.Where, query.Order, query.Skip, query.Take, query.Graph, LinqTerminal.Any), ModelTypeDetail, cancellationToken))!;
        }
        /// <inheritdoc/>
        protected override sealed Task<IReadOnlyCollection<EventModel<TModel>>> EventManyAsync(LinqModelQuery query, CancellationToken cancellationToken) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed Task<EventModel<TModel>?> EventFirstAsync(LinqModelQuery query, CancellationToken cancellationToken) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed Task<EventModel<TModel>?> EventSingleAsync(LinqModelQuery query, CancellationToken cancellationToken) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed Task<long> EventCountAsync(LinqModelQuery query, CancellationToken cancellationToken) => throw new NotSupportedException("Event queries not supported with this provider");
        /// <inheritdoc/>
        protected override sealed Task<bool> EventAnyAsync(LinqModelQuery query, CancellationToken cancellationToken) => throw new NotSupportedException("Event queries not supported with this provider");

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
        protected override sealed int UpdateModelsWhere(PersistEvent @event, LambdaExpression where, object model, Graph graph)
        {
            return engine.ExecuteUpdateWhere<TModel>((TModel)model, graph, where, ModelTypeDetail);
        }
        /// <inheritdoc/>
        protected override sealed int UpdateModelsWhere(PersistEvent @event, LambdaExpression where, LambdaExpression set)
        {
            return engine.ExecuteUpdateWhere<TModel>(set, where, ModelTypeDetail);
        }
        /// <inheritdoc/>
        protected override sealed int DeleteModelsWhere(PersistEvent @event, LambdaExpression where)
        {
            return engine.ExecuteDeleteWhere<TModel>(where, ModelTypeDetail);
        }

        /// <inheritdoc/>
        protected override sealed async Task PersistModelAsync(PersistEvent @event, object model, Graph? graph, bool create, CancellationToken cancellationToken)
        {
            if (create)
            {
                var autoGeneratedCount = ModelTypeDetail.IdentityAutoGeneratedMembers.Count;

                if (autoGeneratedCount == 1)
                {
                    var identity = await engine.ExecuteInsertGetIdentitiesAsync<TModel>((TModel)model, graph, ModelTypeDetail, cancellationToken);
                    var modelPropertyInfo = ModelTypeDetail.IdentityAutoGeneratedMembers[0];
                    identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                    modelPropertyInfo.SetterBoxed(model, identity);

                }
                else if (autoGeneratedCount > 1)
                {
                    var identities = (IList<object>)await engine.ExecuteInsertGetIdentitiesAsync<TModel>((TModel)model, graph, ModelTypeDetail, cancellationToken);
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
                    var success = await engine.ExecuteInsertAsync<TModel>((TModel)model, graph, ModelTypeDetail, cancellationToken);
                    if (!success)
                        throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                }
            }
            else
            {
                var success = await engine.ExecuteUpdateAsync<TModel>((TModel)model, graph, ModelTypeDetail, cancellationToken);
                if (!success)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }

        /// <inheritdoc/>
        protected override sealed async Task DeleteModelAsync(PersistEvent @event, object[] ids, CancellationToken cancellationToken)
        {
            for (var i = 0; i < ids.Length; i += deleteBatchSize)
            {
                var deleteIds = ids.Skip(i).Take(deleteBatchSize).ToArray();
                var rowsAffected = await engine.ExecuteDeleteAsync<TModel>(deleteIds, ModelTypeDetail, cancellationToken);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }
        /// <inheritdoc/>
        protected override sealed Task<int> UpdateModelsWhereAsync(PersistEvent @event, LambdaExpression where, object model, Graph graph, CancellationToken cancellationToken)
        {
            return engine.ExecuteUpdateWhereAsync<TModel>((TModel)model, graph, where, ModelTypeDetail, cancellationToken);
        }
        /// <inheritdoc/>
        protected override sealed Task<int> UpdateModelsWhereAsync(PersistEvent @event, LambdaExpression where, LambdaExpression set, CancellationToken cancellationToken)
        {
            return engine.ExecuteUpdateWhereAsync<TModel>(set, where, ModelTypeDetail, cancellationToken);
        }
        /// <inheritdoc/>
        protected override sealed Task<int> DeleteModelsWhereAsync(PersistEvent @event, LambdaExpression where, CancellationToken cancellationToken)
        {
            return engine.ExecuteDeleteWhereAsync<TModel>(where, ModelTypeDetail, cancellationToken);
        }
    }
}