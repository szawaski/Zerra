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
    public class TransactStoreProvider<TContext, TModel> : RootTransactStoreProvider<TModel>
        where TContext : DataContext
        where TModel : class, new()
    {
        private const int deleteBatchSize = 250;
        private const string defaultEventName = "Transact Store";

        protected readonly ITransactStoreEngine Engine;

        public TransactStoreProvider()
        {
            var context = Instantiator.GetSingleInstance<TContext>();
            this.Engine = context.InitializeEngine<ITransactStoreEngine>();
        }

        protected override sealed ICollection<TModel> QueryMany(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var models = Engine.ExecuteQueryToModelMany<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return models;
        }
        protected override sealed TModel QueryFirst(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var model = Engine.ExecuteQueryToModelFirst<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return model;
        }
        protected override sealed TModel QuerySingle(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var model = Engine.ExecuteQueryToModelSingle<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return model;
        }
        protected override sealed long QueryCount(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var count = Engine.ExecuteQueryCount(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return count;
        }
        protected override sealed bool QueryAny(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var any = Engine.ExecuteQueryAny(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return any;
        }
        protected override sealed ICollection<EventModel<TModel>> QueryEventMany(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");
        protected override sealed EventModel<TModel> QueryEventFirst(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");
        protected override sealed EventModel<TModel> QueryEventSingle(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");
        protected override sealed long QueryEventCount(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");
        protected override sealed bool QueryEventAny(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");

        protected override sealed Task<ICollection<TModel>> QueryManyAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var models = Engine.ExecuteQueryToModelManyAsync<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return models;
        }
        protected override sealed Task<TModel> QueryFirstAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var model = Engine.ExecuteQueryToModelFirstAsync<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return model;
        }
        protected override sealed Task<TModel> QuerySingleAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var model = Engine.ExecuteQueryToModelSingleAsync<TModel>(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return model;
        }
        protected override sealed Task<long> QueryCountAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var count = Engine.ExecuteQueryCountAsync(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return count;
        }
        protected override sealed Task<bool> QueryAnyAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException($"Temporal queries not supported with {nameof(TransactStoreProvider<TContext, TModel>)}");

            var any = Engine.ExecuteQueryAnyAsync(query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);
            return any;
        }
        protected override sealed Task<ICollection<EventModel<TModel>>> QueryEventManyAsync(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");
        protected override sealed Task<EventModel<TModel>> QueryEventFirstAsync(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");
        protected override sealed Task<EventModel<TModel>> QueryEventSingleAsync(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");
        protected override sealed Task<long> QueryEventCountAsync(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");
        protected override sealed Task<bool> QueryEventAnyAsync(Query<TModel> query) => throw new NotSupportedException("Event queries not supported with this provider");

        protected override sealed void PersistModel(PersistEvent @event, TModel model, Graph<TModel> graph, bool create)
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
                    modelPropertyInfo.Setter(model, identity);

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
                        object identity = modelPropertyInfo.Getter(identities[i]);
                        identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                        modelPropertyInfo.Setter(model, identity);
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

        protected override sealed async Task PersistModelAsync(PersistEvent @event, TModel model, Graph<TModel> graph, bool create)
        {
            if (create)
            {
                int autoGeneratedCount = ModelDetail.IdentityAutoGeneratedProperties.Count;

                if (autoGeneratedCount == 1)
                {
                    var rows = await Engine.ExecuteInsertGetIdentitiesAsync(model, graph, ModelDetail);
                    if (rows.Count != 1)
                        throw new Exception($"Insert failed: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                    var identity = rows.First();
                    var modelPropertyInfo = ModelDetail.IdentityAutoGeneratedProperties[0];
                    identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                    modelPropertyInfo.Setter(model, identity);

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
                        object identity = modelPropertyInfo.Getter(identities[i]);
                        identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                        modelPropertyInfo.Setter(model, identity);
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