// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zerra.IO;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Sql
{
    public class SqlProvider<TContext, TModel> : BaseDataProvider<TModel>
        where TContext : DataContext
        where TModel : class, new()
    {
        private const int deleteBatchSize = 250;
        private const string defaultEventName = "Transact SQL";

        protected readonly ISqlEngine Engine;

        private static ISqlEngine engineCache = null;
        private static readonly object engineCacheLock = new object();
        private ISqlEngine GetEngine()
        {
            if (engineCache == null)
            {
                lock (engineCacheLock)
                {
                    if (engineCache == null)
                    {
                        var context = Instantiator.GetSingleInstance<TContext>();
                        engineCache = context.InitializeEngine<ISqlEngine>();
                    }
                }
            }
            return engineCache;
        }

        public SqlProvider()
        {
            this.Engine = GetEngine();
        }

        protected override sealed ICollection<TModel> QueryMany(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.Many, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);

            var models = Engine.ExecuteSqlQueryToModelMany<TModel>(sql, ModelDetail);
            return models;
        }
        protected override sealed TModel QueryFirst(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.First, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);

            var model = Engine.ExecuteSqlQueryToModelFirst<TModel>(sql, ModelDetail);
            return model;
        }
        protected override sealed TModel QuerySingle(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.Single, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);

            var model = Engine.ExecuteSqlQueryToModelSingle<TModel>(sql, ModelDetail);
            return model;
        }
        protected override sealed long QueryCount(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.Count, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);

            var count = Engine.ExecuteSqlQueryCount(sql);
            return count;
        }
        protected override sealed bool QueryAny(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.Any, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);

            var any = Engine.ExecuteSqlQueryAny(sql);
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
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.Many, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);

            var models = Engine.ExecuteSqlQueryToModelManyAsync<TModel>(sql, ModelDetail);
            return models;
        }
        protected override sealed Task<TModel> QueryFirstAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.First, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);

            var model = Engine.ExecuteSqlQueryToModelFirstAsync<TModel>(sql, ModelDetail);
            return model;
        }
        protected override sealed Task<TModel> QuerySingleAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.Single, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);

            var model = Engine.ExecuteSqlQueryToModelSingleAsync<TModel>(sql, ModelDetail);
            return model;
        }
        protected override sealed Task<long> QueryCountAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.Count, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);

            var count = Engine.ExecuteSqlQueryCountAsync(sql);
            return count;
        }
        protected override sealed Task<bool> QueryAnyAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
                throw new NotSupportedException("Temporal queries not supported with this provider");

            var sql = Engine.ConvertToSql(QueryOperation.Any, query.Where, query.Order, query.Skip, query.Take, query.Graph, ModelDetail);

            var any = Engine.ExecuteSqlQueryAnyAsync(sql);
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
                var sql = Engine.GenerateSqlInsert(model, graph, ModelDetail);

                var autoGeneratedCount = ModelDetail.IdentityAutoGeneratedProperties.Count;

                if (autoGeneratedCount == 1)
                {
                    var rows = Engine.ExecuteSqlQuery(sql);
                    if (rows.Count != 1)
                        throw new Exception($"Insert failed: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                    var identity = rows.First();
                    var modelPropertyInfo = ModelDetail.IdentityAutoGeneratedProperties[0];
                    identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                    modelPropertyInfo.Setter(model, identity);

                }
                else if (autoGeneratedCount > 1)
                {
                    var rows = Engine.ExecuteSqlQuery(sql);
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
                    var rowsAffected = Engine.ExecuteSql(sql);
                    if (rowsAffected == 0)
                        throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                }
            }
            else
            {
                var sql = Engine.GenerateSqlUpdate(model, graph, ModelDetail);
                var rowsAffected = Engine.ExecuteSql(sql);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }

        protected override sealed void DeleteModel(PersistEvent @event, object[] ids)
        {
            for (var i = 0; i <= ids.Length; i += deleteBatchSize)
            {
                var deleteIds = ids.Skip(i).Take(deleteBatchSize).ToArray();
                var sql = Engine.GenerateSqlDelete(deleteIds, ModelDetail);
                var rowsAffected = Engine.ExecuteSql(sql);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }

        protected override sealed async Task PersistModelAsync(PersistEvent @event, TModel model, Graph<TModel> graph, bool create)
        {
            if (create)
            {
                var sql = Engine.GenerateSqlInsert(model, graph, ModelDetail);

                int autoGeneratedCount = ModelDetail.IdentityAutoGeneratedProperties.Count;

                if (autoGeneratedCount == 1)
                {
                    var rows = await Engine.ExecuteSqlQueryAsync(sql);
                    if (rows.Count != 1)
                        throw new Exception($"Insert failed: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                    var identity = rows.First();
                    var modelPropertyInfo = ModelDetail.IdentityAutoGeneratedProperties[0];
                    identity = TypeAnalyzer.Convert(identity, modelPropertyInfo.Type);
                    modelPropertyInfo.Setter(model, identity);

                }
                else if (autoGeneratedCount > 1)
                {
                    var rows = await Engine.ExecuteSqlQueryAsync(sql);
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
                    var rowsAffected = await Engine.ExecuteSqlAsync(sql);
                    if (rowsAffected == 0)
                        throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
                }
            }
            else
            {
                var sql = Engine.GenerateSqlUpdate(model, graph, ModelDetail);
                var rowsAffected = await Engine.ExecuteSqlAsync(sql);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }

        protected override sealed async Task DeleteModelAsync(PersistEvent @event, object[] ids)
        {
            for (var i = 0; i <= ids.Length; i += deleteBatchSize)
            {
                var deleteIds = ids.Skip(i).Take(deleteBatchSize).ToArray();
                var sql = Engine.GenerateSqlDelete(deleteIds, ModelDetail);
                var rowsAffected = await Engine.ExecuteSqlAsync(sql);
                if (rowsAffected == 0)
                    throw new Exception($"No rows affected: {(String.IsNullOrWhiteSpace(@event.Name) ? defaultEventName : @event.Name)}");
            }
        }
    }
}