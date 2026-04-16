// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Zerra.Map;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract partial class BaseTransactStoreCompressionProvider<TNextProviderInterface, TModel> : BaseTransactStoreLayerProvider<TNextProviderInterface, TModel>
        where TNextProviderInterface : ITransactStoreProvider<TModel>
        where TModel : class, new()
    {
        public virtual bool Enabled { get { return true; } }
        public virtual Graph<TModel>? Properties { get { return null; } }

        public BaseTransactStoreCompressionProvider(TNextProviderInterface nextProvider)
            : base(nextProvider)
        {
        }

        public override sealed Expression? GetWhereExpressionIncludingBase(Graph? graph)
        {
            return base.GetWhereExpressionIncludingBase(graph);
        }

        private static Expression? CompressWhere(Expression? expression)
        {
            return expression;
        }
        private static QueryOrder? CompressOrder(QueryOrder? order)
        {
            return order;
        }

        public object DecompressModel(object model, Graph? graph, bool newCopy)
        {
            if (!this.Enabled)
                return model;

            var properties = CompressionCommon.GetModelCompressableProperties(typeof(TModel), this.Properties);
            if (properties.Length == 0)
                return model;

            graph = graph is null ? null : new Graph<TModel>(graph);

            if (newCopy)
                model = Mapper.Map<TModel, TModel>((TModel)model, graph);

            foreach (var property in properties)
            {
                if (graph is null || graph.HasMember(property.Name))
                {
                    if (property.TypeDetail.CoreType == CoreType.String)
                    {
                        var compressed = (string?)property.GetterBoxed(model);
                        if (compressed is not null)
                        {
                            try
                            {
                                var plain = CompressionCommon.DecompressGZip(compressed);
                                property.SetterBoxed(model, plain);
                            }
                            catch { }
                        }
                    }
                    else if (property.Type == typeof(byte[]))
                    {
                        var compressed = (byte[]?)property.GetterBoxed(model);
                        if (compressed is not null)
                        {
                            try
                            {
                                var plain = CompressionCommon.DecompressGZip(compressed);
                                property.SetterBoxed(model, plain);
                            }
                            catch { }
                        }
                    }
                }
            }

            return model;
        }
        public IEnumerable DecompressModels(IEnumerable models, Graph? graph, bool newCopy)
        {
            if (!this.Enabled || graph is null)
                return models;

            var properties = CompressionCommon.GetModelCompressableProperties(typeof(TModel), this.Properties);
            if (properties.Length == 0)
                return models;

            graph = graph is null ? null : new Graph<TModel>(graph);

            if (newCopy)
                models = Mapper.Map<IEnumerable<TModel>, TModel[]>((IEnumerable<TModel>)models, graph);

            foreach (var model in models)
            {
                foreach (var property in properties)
                {
                    if (graph is null || graph.HasMember(property.Name))
                    {
                        if (property.TypeDetail.CoreType == CoreType.String)
                        {
                            var compressed = (string?)property.GetterBoxed(model);
                            if (compressed is not null)
                            {
                                try
                                {
                                    var plain = CompressionCommon.DecompressGZip(compressed);
                                    property.SetterBoxed(model, plain);
                                }
                                catch { }
                            }
                        }
                        else if (property.Type == typeof(byte[]))
                        {
                            var compressed = (byte[]?)property.GetterBoxed(model);
                            if (compressed is not null)
                            {
                                try
                                {
                                    var plain = CompressionCommon.DecompressGZip(compressed);
                                    property.SetterBoxed(model, plain);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }

            return models;
        }

        public override sealed void OnQueryIncludingBase(Graph? graph)
        {
            ProviderRelation?.OnQueryIncludingBase(graph);
        }

        public override sealed IEnumerable OnGetIncludingBase(IEnumerable models, Graph? graph)
        {
            var returnModels1 = DecompressModels(models, graph, false);
            if (ProviderRelation is null)
                return returnModels1;

            var returnModels2 = ProviderRelation.OnGetIncludingBase(returnModels1, graph);
            return returnModels2;
        }
        public override sealed async Task<IEnumerable> OnGetIncludingBaseAsync(IEnumerable models, Graph? graph)
        {
            var returnModels1 = DecompressModels(models, graph, false);
            if (ProviderRelation is null)
                return returnModels1;

            var returnModels2 = await ProviderRelation.OnGetIncludingBaseAsync(returnModels1, graph);
            return returnModels2;
        }

        public override sealed object Many(Query query)
        {
            var whereCompressed = CompressWhere(query.Where);
            var orderCompressed = CompressOrder(query.Order);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var compressedModels = (IReadOnlyCollection<TModel>)NextProvider.Query(appenedQuery)!;

            if (compressedModels.Count == 0)
                return compressedModels;

            var models = DecompressModels(compressedModels, query.Graph, true);
            return models;
        }
        public override sealed object? First(Query query)
        {
            var whereCompressed = CompressWhere(query.Where);
            var orderCompressed = CompressOrder(query.Order);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var compressedModels = (TModel?)NextProvider.Query(appenedQuery);

            if (compressedModels is null)
                return null;

            var model = DecompressModel(compressedModels, query.Graph, true);
            return model;
        }
        public override sealed object? Single(Query query)
        {
            var whereCompressed = CompressWhere(query.Where);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;

            var compressedModels = (TModel?)NextProvider.Query(appenedQuery);

            if (compressedModels is null)
                return null;

            var model = DecompressModel(compressedModels, query.Graph, true);
            return model;
        }
        public override sealed object Count(Query query)
        {
            var whereCompressed = CompressWhere(query.Where);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;

            var count = NextProvider.Query(appenedQuery)!;
            return count;
        }
        public override sealed object Any(Query query)
        {
            var whereCompressed = CompressWhere(query.Where);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;

            var any = NextProvider.Query(appenedQuery)!;
            return any;
        }
        public override sealed object EventMany(Query query)
        {
            var whereCompressed = CompressWhere(query.Where);
            var orderCompressed = CompressOrder(query.Order);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var eventModels = (ICollection<EventModel<TModel>>)NextProvider.Query(appenedQuery)!;

            if (eventModels.Count == 0)
                return eventModels;

            var compressedModels = eventModels.Select(x => x.Model).ToArray();
            var models = DecompressModels(compressedModels, query.Graph, true);
            var i = 0;
            foreach (var eventModel in eventModels)
            {
                eventModel.Model = compressedModels[i];
                i++;
            }

            return eventModels;
        }

        public override sealed async Task<object?> ManyAsync(Query query)
        {
            var whereCompressed = CompressWhere(query.Where);
            var orderCompressed = CompressOrder(query.Order);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var compressedModels = (IReadOnlyCollection<TModel>)(await NextProvider.QueryAsync(appenedQuery))!;

            if (compressedModels.Count == 0)
                return compressedModels;

            var models = DecompressModels(compressedModels, query.Graph, true);
            return models;
        }
        public override sealed async Task<object?> FirstAsync(Query query)
        {
            var whereCompressed = CompressWhere(query.Where);
            var orderCompressed = CompressOrder(query.Order);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var compressedModels = (TModel?)await NextProvider.QueryAsync(appenedQuery);

            if (compressedModels is null)
                return null;

            var model = DecompressModel(compressedModels, query.Graph, true);
            return model;
        }
        public override sealed async Task<object?> SingleAsync(Query query)
        {
            var whereCompressed = CompressWhere(query.Where);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;

            var compressedModels = (TModel?)await NextProvider.QueryAsync(appenedQuery);

            if (compressedModels is null)
                return null;

            var model = DecompressModel(compressedModels, query.Graph, true);
            return model;
        }
        public override sealed Task<object?> CountAsync(Query query)
        {
            var whereCompressed = CompressWhere(query.Where);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;

            var count = NextProvider.QueryAsync(appenedQuery);
            return count;
        }
        public override sealed Task<object?> AnyAsync(Query query)
        {
            var whereCompressed = CompressWhere(query.Where);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;

            var any = NextProvider.QueryAsync(appenedQuery);
            return any;
        }
        public override sealed async Task<object?> EventManyAsync(Query query)
        {
            var whereCompressed = CompressWhere(query.Where);
            var orderCompressed = CompressOrder(query.Order);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var eventModels = (ICollection<EventModel<TModel>>)(await NextProvider.QueryAsync(appenedQuery))!;

            if (eventModels.Count == 0)
                return eventModels;

            var compressedModels = eventModels.Select(x => x.Model).ToArray();
            var models = DecompressModels(compressedModels, query.Graph, true);
            var i = 0;
            foreach (var eventModel in eventModels)
            {
                eventModel.Model = compressedModels[i];
                i++;
            }

            return eventModels;
        }

        private object[] CompressModels(object[] models, Graph? graph, bool newCopy)
        {
            if (!this.Enabled)
                return models;

            var properties = CompressionCommon.GetModelCompressableProperties(typeof(TModel), this.Properties);
            if (properties.Length == 0)
                return models;

            if (graph is not null)
            {
                graph = new Graph<TModel>(graph);

                //add identites for copying
                graph.AddMembers(ModelAnalyzer.GetIdentityPropertyNames(typeof(TModel)));
            }

            if (newCopy)
                models = Mapper.Map<object[], object[]>(models, graph);

            foreach (var model in models)
            {
                foreach (var property in properties)
                {
                    if (graph is null || graph.HasMember(property.Name))
                    {
                        if (property.TypeDetail.CoreType == CoreType.String)
                        {
                            var plain = (string?)property.GetterBoxed(model);
                            if (plain is not null)
                            {
                                var compressed = CompressionCommon.CompressGZip(plain);
                                property.SetterBoxed(model, compressed);
                            }
                        }
                        else if (property.Type == typeof(byte[]))
                        {
                            var plain = (byte[]?)property.GetterBoxed(model);
                            if (plain is not null)
                            {
                                var compressed = CompressionCommon.CompressGZip(plain);
                                property.SetterBoxed(model, compressed);
                            }
                        }
                    }
                }
            }
            return models;
        }

        public override sealed void Create(Persist persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var compressedModels = CompressModels(persist.Models, persist.Graph, true);
            NextProvider.Persist(new Create(persist.Event, compressedModels, persist.Graph));

            for (var i = 0; i < persist.Models.Length; i++)
            {
                var identity = ModelAnalyzer.GetIdentity(modelType, compressedModels[i]);
                ModelAnalyzer.SetIdentity(modelType, persist.Models[i], identity);
            }
        }
        public override sealed void Update(Persist persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var compressedModels = CompressModels(persist.Models, persist.Graph, true);
            NextProvider.Persist(new Update(persist.Event, compressedModels, persist.Graph));
        }
        public override sealed void Delete(Persist persist)
        {
            NextProvider.Persist(persist);
        }

        public override sealed async Task CreateAsync(Persist persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var compressedModels = CompressModels(persist.Models, persist.Graph, true);
            await NextProvider.PersistAsync(new Create(persist.Event, compressedModels, persist.Graph));

            for (var i = 0; i < persist.Models.Length; i++)
            {
                var identity = ModelAnalyzer.GetIdentity(modelType, compressedModels[i]);
                ModelAnalyzer.SetIdentity(modelType, persist.Models[i], identity);
            }
        }
        public override sealed Task UpdateAsync(Persist persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return Task.CompletedTask;

            var compressedModels = CompressModels(persist.Models, persist.Graph, true);
            return NextProvider.PersistAsync(new Update(persist.Event, compressedModels, persist.Graph));
        }
        public override sealed Task DeleteAsync(Persist persist)
        {
            return NextProvider.PersistAsync(persist);
        }
    }
}
