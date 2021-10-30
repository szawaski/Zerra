﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Zerra.Providers;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract partial class BaseTransactStoreCompressionProvider<TNextProviderInterface, TModel> : BaseTransactStoreLayerProvider<TNextProviderInterface, TModel>, ICompressionProvider
        where TNextProviderInterface : ITransactStoreProvider<TModel>
        where TModel : class, new()
    {
        public virtual bool Enabled { get { return true; } }
        public virtual Graph<TModel> Properties { get { return null; } }

        public override sealed Expression<Func<TModel, bool>> GetWhereExpressionIncludingBase(Graph<TModel> graph)
        {
            return base.GetWhereExpressionIncludingBase(graph);
        }

        private static Expression<Func<TModel, bool>> CompressWhere(Expression<Func<TModel, bool>> expression)
        {
            return expression;
        }
        private static QueryOrder<TModel> CompressOrder(QueryOrder<TModel> order)
        {
            return order;
        }

        public TModel DecompressModel(TModel model, Graph<TModel> graph, bool newCopy)
        {
            if (!this.Enabled)
                return model;

            var properties = CompressionCommon.GetModelCompressableProperties(typeof(TModel), this.Properties);
            if (properties.Length == 0)
                return model;

            graph = new Graph<TModel>(graph);

            if (newCopy)
                model = Mapper.Map<TModel, TModel>(model, graph);

            foreach (var property in properties)
            {
                if (graph.HasLocalProperty(property.Name))
                {
                    if (property.TypeDetail.CoreType == CoreType.String)
                    {
                        string compressed = (string)property.Getter(model);
                        if (compressed != null)
                        {
                            try
                            {
                                string plain = CompressionCommon.DecompressGZip(compressed);
                                property.Setter(model, plain);
                            }
                            catch { }
                        }
                    }
                    else if (property.Type == typeof(byte[]))
                    {
                        byte[] compressed = (byte[])property.Getter(model);
                        if (compressed != null)
                        {
                            try
                            {
                                byte[] plain = CompressionCommon.DecompressGZip(compressed);
                                property.Setter(model, plain);
                            }
                            catch { }
                        }
                    }
                }
            }

            return model;
        }
        public ICollection<TModel> DecompressModels(ICollection<TModel> models, Graph<TModel> graph, bool newCopy)
        {
            if (!this.Enabled)
                return models;

            var properties = CompressionCommon.GetModelCompressableProperties(typeof(TModel), this.Properties);
            if (properties.Length == 0)
                return models;

            graph = new Graph<TModel>(graph);

            if (newCopy)
                models = Mapper.Map<ICollection<TModel>, TModel[]>(models, graph);

            foreach (TModel model in models)
            {
                foreach (var property in properties)
                {
                    if (graph.HasLocalProperty(property.Name))
                    {
                        if (property.TypeDetail.CoreType == CoreType.String)
                        {
                            string compressed = (string)property.Getter(model);
                            if (compressed != null)
                            {
                                try
                                {
                                    string plain = CompressionCommon.DecompressGZip(compressed);
                                    property.Setter(model, plain);
                                }
                                catch { }
                            }
                        }
                        else if (property.Type == typeof(byte[]))
                        {
                            byte[] compressed = (byte[])property.Getter(model);
                            if (compressed != null)
                            {
                                try
                                {
                                    byte[] plain = CompressionCommon.DecompressGZip(compressed);
                                    property.Setter(model, plain);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }

            return models;
        }

        public override sealed void OnQueryIncludingBase(Graph<TModel> graph)
        {
            ProviderRelation.OnQueryIncludingBase(graph);
        }

        public override sealed ICollection<TModel> OnGetIncludingBase(ICollection<TModel> models, Graph<TModel> graph)
        {
            ICollection<TModel> returnModels1 = DecompressModels(models, graph, false);
            ICollection<TModel> returnModels2 = ProviderRelation.OnGetIncludingBase(returnModels1, graph);
            return returnModels2;
        }
        public override sealed async Task<ICollection<TModel>> OnGetIncludingBaseAsync(ICollection<TModel> models, Graph<TModel> graph)
        {
            ICollection<TModel> returnModels1 = DecompressModels(models, graph, false);
            ICollection<TModel> returnModels2 = await ProviderRelation.OnGetIncludingBaseAsync(returnModels1, graph);
            return returnModels2;
        }

        public override sealed object Many(Query<TModel> query)
        {
            var whereCompressed = CompressWhere(query.Where);
            var orderCompressed = CompressOrder(query.Order);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var compressedModels = (ICollection<TModel>)(NextProvider.Query(appenedQuery));

            if (compressedModels.Count == 0)
                return compressedModels;

            var models = DecompressModels(compressedModels, query.Graph, true);
            return models;
        }
        public override sealed object First(Query<TModel> query)
        {
            var whereCompressed = CompressWhere(query.Where);
            var orderCompressed = CompressOrder(query.Order);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var compressedModels = (TModel)(NextProvider.Query(appenedQuery));

            if (compressedModels == null)
                return null;

            var model = DecompressModel(compressedModels, query.Graph, true);
            return model;
        }
        public override sealed object Single(Query<TModel> query)
        {
            var whereCompressed = CompressWhere(query.Where);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;

            var compressedModels = (TModel)(NextProvider.Query(appenedQuery));

            if (compressedModels == null)
                return null;

            var model = DecompressModel(compressedModels, query.Graph, true);
            return model;
        }
        public override sealed object Count(Query<TModel> query)
        {
            var whereCompressed = CompressWhere(query.Where);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;

            var count = NextProvider.Query(appenedQuery);
            return count;
        }
        public override sealed object Any(Query<TModel> query)
        {
            var whereCompressed = CompressWhere(query.Where);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;

            var any = NextProvider.Query(appenedQuery);
            return any;
        }
        public override sealed object EventMany(Query<TModel> query)
        {
            var whereCompressed = CompressWhere(query.Where);
            var orderCompressed = CompressOrder(query.Order);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var eventModels = (ICollection<EventModel<TModel>>)(NextProvider.Query(appenedQuery));

            if (eventModels.Count == 0)
                return eventModels;

            var compressedModels = eventModels.Select(x => x.Model).ToArray();
            var models = DecompressModels(compressedModels, query.Graph, true);
            int i = 0;
            foreach (var eventModel in eventModels)
            {
                eventModel.Model = compressedModels[i];
                i++;
            }

            return eventModels;
        }

        public override sealed async Task<object> ManyAsync(Query<TModel> query)
        {
            var whereCompressed = CompressWhere(query.Where);
            var orderCompressed = CompressOrder(query.Order);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var compressedModels = (ICollection<TModel>)(await NextProvider.QueryAsync(appenedQuery));

            if (compressedModels.Count == 0)
                return compressedModels;

            var models = DecompressModels(compressedModels, query.Graph, true);
            return models;
        }
        public override sealed async Task<object> FirstAsync(Query<TModel> query)
        {
            var whereCompressed = CompressWhere(query.Where);
            var orderCompressed = CompressOrder(query.Order);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var compressedModels = (TModel)(await NextProvider.QueryAsync(appenedQuery));

            if (compressedModels == null)
                return null;

            var model = DecompressModel(compressedModels, query.Graph, true);
            return model;
        }
        public override sealed async Task<object> SingleAsync(Query<TModel> query)
        {
            var whereCompressed = CompressWhere(query.Where);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;

            var compressedModels = (TModel)(await NextProvider.QueryAsync(appenedQuery));

            if (compressedModels == null)
                return null;

            var model = DecompressModel(compressedModels, query.Graph, true);
            return model;
        }
        public override sealed Task<object> CountAsync(Query<TModel> query)
        {
            var whereCompressed = CompressWhere(query.Where);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;

            var count = NextProvider.QueryAsync(appenedQuery);
            return count;
        }
        public override sealed Task<object> AnyAsync(Query<TModel> query)
        {
            var whereCompressed = CompressWhere(query.Where);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;

            var any = NextProvider.QueryAsync(appenedQuery);
            return any;
        }
        public override sealed async Task<object> EventManyAsync(Query<TModel> query)
        {
            var whereCompressed = CompressWhere(query.Where);
            var orderCompressed = CompressOrder(query.Order);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Where = whereCompressed;
            appenedQuery.Order = orderCompressed;

            var eventModels = (ICollection<EventModel<TModel>>)(await NextProvider.QueryAsync(appenedQuery));

            if (eventModels.Count == 0)
                return eventModels;

            var compressedModels = eventModels.Select(x => x.Model).ToArray();
            var models = DecompressModels(compressedModels, query.Graph, true);
            int i = 0;
            foreach (var eventModel in eventModels)
            {
                eventModel.Model = compressedModels[i];
                i++;
            }

            return eventModels;
        }

        private TModel[] CompressModels(TModel[] models, Graph<TModel> graph, bool newCopy)
        {
            if (!this.Enabled)
                return models;

            var properties = CompressionCommon.GetModelCompressableProperties(typeof(TModel), this.Properties);
            if (properties.Length == 0)
                return models;

            graph = new Graph<TModel>(graph);

            //add identites for copying
            graph.AddProperties(ModelAnalyzer.GetIdentityPropertyNames(typeof(TModel)));

            if (newCopy)
                models = Mapper.Map<TModel[], TModel[]>(models, graph);

            foreach (TModel model in models)
            {
                foreach (var property in properties)
                {
                    if (graph.HasLocalProperty(property.Name))
                    {
                        if (property.TypeDetail.CoreType == CoreType.String)
                        {
                            string plain = (string)property.Getter(model);
                            if (plain != null)
                            {
                                string compressed = CompressionCommon.CompressGZip(plain);
                                property.Setter(model, compressed);
                            }
                        }
                        else if (property.Type == typeof(byte[]))
                        {
                            byte[] plain = (byte[])property.Getter(model);
                            if (plain != null)
                            {
                                byte[] compressed = CompressionCommon.CompressGZip(plain);
                                property.Setter(model, compressed);
                            }
                        }
                    }
                }
            }
            return models;
        }

        public override sealed void Create(Persist<TModel> persist)
        {
            if (persist.Models.Length == 0)
                return;

            TModel[] compressedModels = CompressModels(persist.Models, persist.Graph, true);
            NextProvider.Persist(new Create<TModel>(persist.Event, compressedModels, persist.Graph));

            for (var i = 0; i < persist.Models.Length; i++)
            {
                object identity = ModelAnalyzer.GetIdentity(compressedModels[i]);
                ModelAnalyzer.SetIdentity(persist.Models[i], identity);
            }
        }
        public override sealed void Update(Persist<TModel> persist)
        {
            if (persist.Models.Length == 0)
                return;

            ICollection<TModel> compressedModels = CompressModels(persist.Models, persist.Graph, true);
            NextProvider.Persist(new Update<TModel>(persist.Event, compressedModels, persist.Graph));
        }
        public override sealed void Delete(Persist<TModel> persist)
        {
            NextProvider.Persist(persist);
        }

        public override sealed async Task CreateAsync(Persist<TModel> persist)
        {
            if (persist.Models.Length == 0)
                return;

            TModel[] compressedModels = CompressModels(persist.Models, persist.Graph, true);
            await NextProvider.PersistAsync(new Create<TModel>(persist.Event, compressedModels, persist.Graph));

            for (var i = 0; i < persist.Models.Length; i++)
            {
                object identity = ModelAnalyzer.GetIdentity(compressedModels[i]);
                ModelAnalyzer.SetIdentity(persist.Models[i], identity);
            }
        }
        public override sealed Task UpdateAsync(Persist<TModel> persist)
        {
            if (persist.Models.Length == 0)
                return Task.CompletedTask;

            ICollection<TModel> compressedModels = CompressModels(persist.Models, persist.Graph, true);
            return NextProvider.PersistAsync(new Update<TModel>(persist.Event, compressedModels, persist.Graph));
        }
        public override sealed Task DeleteAsync(Persist<TModel> persist)
        {
            return NextProvider.PersistAsync(persist);
        }
    }
}