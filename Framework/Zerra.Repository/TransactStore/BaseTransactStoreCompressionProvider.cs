// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Zerra.Map;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Abstract layer provider that transparently compresses and decompresses model properties using GZip.
    /// </summary>
    /// <typeparam name="TNextProviderInterface">The type of the next provider in the chain.</typeparam>
    /// <typeparam name="TModel">The model type managed by this provider.</typeparam>
    public abstract partial class BaseTransactStoreCompressionProvider<TNextProviderInterface, TModel> : BaseTransactStoreLayerProvider<TNextProviderInterface, TModel>
        where TNextProviderInterface : ITransactStoreProvider<TModel>
        where TModel : class, new()
    {
        /// <summary>Gets a value indicating whether compression is enabled. Defaults to <see langword="true"/>.</summary>
        public virtual bool Enabled { get { return true; } }
        /// <summary>Gets an optional graph that restricts which model properties are compressed. When <see langword="null"/>, all eligible properties are compressed.</summary>
        public virtual Graph<TModel>? Properties { get { return null; } }

        /// <summary>Initializes a new instance of <see cref="BaseTransactStoreCompressionProvider{TNextProviderInterface, TModel}"/> with the next provider in the chain.</summary>
        /// <param name="nextProvider">The next provider to delegate operations to after compression/decompression.</param>
        public BaseTransactStoreCompressionProvider(TNextProviderInterface nextProvider)
            : base(nextProvider)
        {
        }

        /// <inheritdoc/>
        public override sealed LambdaExpression? GetWhereExpressionIncludingBase(Graph? graph)
        {
            return base.GetWhereExpressionIncludingBase(graph);
        }

        private static LambdaExpression? CompressWhere(LambdaExpression? expression)
        {
            return expression;
        }
        private static QueryOrder? CompressOrder(QueryOrder? order)
        {
            return order;
        }

        /// <summary>Decompresses the compressed properties of a single model.</summary>
        /// <param name="model">The model whose properties should be decompressed.</param>
        /// <param name="graph">The graph specifying which members to decompress, or <see langword="null"/> to decompress all eligible members.</param>
        /// <param name="newCopy"><see langword="true"/> to decompress a copy of the model; <see langword="false"/> to decompress in place.</param>
        /// <returns>The model with decompressed properties.</returns>
        public TModel DecompressModel(TModel model, Graph? graph, bool newCopy)
        {
            if (!this.Enabled)
                return model;

            var properties = CompressionCommon.GetModelCompressableProperties(typeof(TModel), this.Properties);
            if (properties.Length == 0)
                return model;

            graph = graph is null ? null : new Graph<TModel>(graph);

            if (newCopy)
                model = model.Copy();

            foreach (var property in properties)
            {
                if (graph is null || graph.HasMember(property.Name))
                {
                    if (property.TypeDetail.CoreType == CoreType.String)
                    {
                        var compressed = (string?)property.GetterBoxed!(model);
                        if (compressed is not null)
                        {
                            try
                            {
                                var plain = CompressionCommon.DecompressGZip(compressed);
                                property.SetterBoxed!(model, plain);
                            }
                            catch { }
                        }
                    }
                    else if (property.Type == typeof(byte[]))
                    {
                        var compressed = (byte[]?)property.GetterBoxed!(model);
                        if (compressed is not null)
                        {
                            try
                            {
                                var plain = CompressionCommon.DecompressGZip(compressed);
                                property.SetterBoxed!(model, plain);
                            }
                            catch { }
                        }
                    }
                }
            }

            return model;
        }
        /// <summary>Decompresses the compressed properties of a collection of models.</summary>
        /// <param name="models">The models whose properties should be decompressed.</param>
        /// <param name="graph">The graph specifying which members to decompress, or <see langword="null"/> to decompress all eligible members.</param>
        /// <param name="newCopy"><see langword="true"/> to decompress copies of the models; <see langword="false"/> to decompress in place.</param>
        /// <returns>The models with decompressed properties.</returns>
        public IEnumerable DecompressModels(IEnumerable models, Graph? graph, bool newCopy)
        {
            if (!this.Enabled)
                return models;

            var properties = CompressionCommon.GetModelCompressableProperties(typeof(TModel), this.Properties);
            if (properties.Length == 0)
                return models;

            graph = graph is null ? null : new Graph<TModel>(graph);

            if (newCopy)
            {
                var copies = new List<TModel>();
                foreach(TModel model in models)
                    copies.Add(model.Copy());
                models = copies;
            }

            foreach (var model in models)
            {
                foreach (var property in properties)
                {
                    if (graph is null || graph.HasMember(property.Name))
                    {
                        if (property.TypeDetail.CoreType == CoreType.String)
                        {
                            var compressed = (string?)property.GetterBoxed!(model);
                            if (compressed is not null)
                            {
                                try
                                {
                                    var plain = CompressionCommon.DecompressGZip(compressed);
                                    property.SetterBoxed!(model, plain);
                                }
                                catch { }
                            }
                        }
                        else if (property.Type == typeof(byte[]))
                        {
                            var compressed = (byte[]?)property.GetterBoxed!(model);
                            if (compressed is not null)
                            {
                                try
                                {
                                    var plain = CompressionCommon.DecompressGZip(compressed);
                                    property.SetterBoxed!(model, plain);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }

            return models;
        }

        /// <inheritdoc/>
        public override sealed void OnQueryIncludingBase(Graph? graph)
        {
            ProviderRelation?.OnQueryIncludingBase(graph);
        }

        /// <inheritdoc/>
        public override sealed IEnumerable OnGetIncludingBase(IEnumerable models, Graph? graph)
        {
            var returnModels1 = DecompressModels(models, graph, false);
            if (ProviderRelation is null)
                return returnModels1;

            var returnModels2 = ProviderRelation.OnGetIncludingBase(returnModels1, graph);
            return returnModels2;
        }
        /// <inheritdoc/>
        public override sealed async Task<IEnumerable> OnGetIncludingBaseAsync(IEnumerable models, Graph? graph)
        {
            var returnModels1 = DecompressModels(models, graph, false);
            if (ProviderRelation is null)
                return returnModels1;

            var returnModels2 = await ProviderRelation.OnGetIncludingBaseAsync(returnModels1, graph);
            return returnModels2;
        }

        /// <inheritdoc/>
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
        /// <inheritdoc/>
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
        /// <inheritdoc/>
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
        /// <inheritdoc/>
        public override sealed object Count(Query query)
        {
            var whereCompressed = CompressWhere(query.Where);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;

            var count = NextProvider.Query(appenedQuery)!;
            return count;
        }
        /// <inheritdoc/>
        public override sealed object Any(Query query)
        {
            var whereCompressed = CompressWhere(query.Where);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;

            var any = NextProvider.Query(appenedQuery)!;
            return any;
        }
        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
        /// <inheritdoc/>
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
        /// <inheritdoc/>
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
        /// <inheritdoc/>
        public override sealed Task<object?> CountAsync(Query query)
        {
            var whereCompressed = CompressWhere(query.Where);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;

            var count = NextProvider.QueryAsync(appenedQuery);
            return count;
        }
        /// <inheritdoc/>
        public override sealed Task<object?> AnyAsync(Query query)
        {
            var whereCompressed = CompressWhere(query.Where);

            var appenedQuery = new Query(query);
            appenedQuery.Where = whereCompressed;

            var any = NextProvider.QueryAsync(appenedQuery);
            return any;
        }
        /// <inheritdoc/>
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
            {
                var copy = new object[models.Length];
                for (var i = 0; i < models.Length; i++)
                    copy[i] = models[i].CopyObject();
            }

            foreach (var model in models)
            {
                foreach (var property in properties)
                {
                    if (graph is null || graph.HasMember(property.Name))
                    {
                        if (property.TypeDetail.CoreType == CoreType.String)
                        {
                            var plain = (string?)property.GetterBoxed!(model);
                            if (plain is not null)
                            {
                                var compressed = CompressionCommon.CompressGZip(plain);
                                property.SetterBoxed!(model, compressed);
                            }
                        }
                        else if (property.Type == typeof(byte[]))
                        {
                            var plain = (byte[]?)property.GetterBoxed!(model);
                            if (plain is not null)
                            {
                                var compressed = CompressionCommon.CompressGZip(plain);
                                property.SetterBoxed!(model, compressed);
                            }
                        }
                    }
                }
            }
            return models;
        }

        /// <inheritdoc/>
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
        /// <inheritdoc/>
        public override sealed void Update(Persist persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var compressedModels = CompressModels(persist.Models, persist.Graph, true);
            NextProvider.Persist(new Update(persist.Event, compressedModels, persist.Graph));
        }
        /// <inheritdoc/>
        public override sealed void Delete(Persist persist)
        {
            NextProvider.Persist(persist);
        }

        /// <inheritdoc/>
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
        /// <inheritdoc/>
        public override sealed Task UpdateAsync(Persist persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return Task.CompletedTask;

            var compressedModels = CompressModels(persist.Models, persist.Graph, true);
            return NextProvider.PersistAsync(new Update(persist.Event, compressedModels, persist.Graph));
        }
        /// <inheritdoc/>
        public override sealed Task DeleteAsync(Persist persist)
        {
            return NextProvider.PersistAsync(persist);
        }
    }
}
