// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Zerra.Map;
using Zerra.Reflection;
using Zerra.Repository.Linq;
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
            return ProviderRelation?.GetWhereExpressionIncludingBase(graph);
        }

        private static LambdaExpression? CompressWhere(LambdaExpression? expression)
        {
            return expression;
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
        public override sealed object? Query(LinqQuery query)
        {
            RefuseCompressedReads(query);
            //the query carries its result types, so the converted where is swapped onto it for the call and the original put back
            var where = query.Where;
            query.Where = CompressWhere(where);
            try
            {
                return DecompressResult(query, NextProvider.Query(query));
            }
            finally
            {
                query.Where = where;
            }
        }
        /// <inheritdoc/>
        public override sealed async Task<object?> QueryAsync(LinqQuery query, CancellationToken cancellationToken)
        {
            RefuseCompressedReads(query);
            var where = query.Where;
            query.Where = CompressWhere(where);
            try
            {
                return DecompressResult(query, await NextProvider.QueryAsync(query, cancellationToken));
            }
            finally
            {
                query.Where = where;
            }
        }
        /// <inheritdoc/>
        public override sealed object QueryStream(LinqQuery query)
        {
            RefuseCompressedReads(query);
            LambdaExpression? where = null;
            var elements = query.StreamFrom(() =>
            {
                where = query.Where;
                query.Where = CompressWhere(where);
                return NextProvider.QueryStream(query);
            }, () => query.Where = where);
            if (!query.ReturnsModels)
                return elements;
            return DecompressStream((IEnumerable<TModel>)elements, query.Graph);
        }
        private IEnumerable<TModel> DecompressStream(IEnumerable<TModel> models, Graph? graph)
        {
            foreach (var model in models)
                yield return DecompressModel(model, graph, true);
        }
        /// <inheritdoc/>
        public override sealed object QueryStreamAsync(LinqQuery query, CancellationToken cancellationToken)
        {
            RefuseCompressedReads(query);
            LambdaExpression? where = null;
            var elements = query.StreamFromAsync(cancellation =>
            {
                where = query.Where;
                query.Where = CompressWhere(where);
                return Task.FromResult<object?>(NextProvider.QueryStreamAsync(query, cancellation));
            }, () => query.Where = where, cancellationToken);
            if (!query.ReturnsModels)
                return elements;
            return DecompressStreamAsync((IAsyncEnumerable<TModel>)elements, query.Graph, cancellationToken);
        }
        private async IAsyncEnumerable<TModel> DecompressStreamAsync(IAsyncEnumerable<TModel> models, Graph? graph, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var model in models.WithCancellation(cancellationToken))
                yield return DecompressModel(model, graph, true);
        }
        private object? DecompressResult(LinqQuery query, object? result)
        {
            switch (result)
            {
                case IReadOnlyCollection<TModel> models when query.ReturnsModels:
                    return models.Count == 0 ? models : DecompressModels(models, query.Graph, true).Cast<TModel>().ToArray();
                case TModel model:
                    return DecompressModel(model, query.Graph, true);
                case ICollection<EventModel<TModel>> eventModels:
                    {
                        if (eventModels.Count == 0)
                            return eventModels;
                        var models = DecompressModels(eventModels.Select(x => x.Model).ToArray(), query.Graph, true).GetEnumerator();
                        foreach (var eventModel in eventModels)
                        {
                            _ = models.MoveNext();
                            eventModel.Model = (TModel)models.Current;
                        }
                        return eventModels;
                    }
                case EventModel<TModel> eventModel:
                    eventModel.Model = DecompressModel(eventModel.Model, query.Graph, true);
                    return eventModel;
                default:
                    return result;
            }
        }
        private void RefuseCompressedReads(LinqQuery query)
        {
            if (!this.Enabled)
                return;
            var properties = CompressionCommon.GetModelCompressableProperties(typeof(TModel), this.Properties);
            if (LinqMemberFinder.ReadsMember(query.GroupBy, properties) || LinqMemberFinder.ReadsMember(query.Value, properties) || LinqMemberFinder.ReadsMember(query.Select, typeof(TModel), properties))
                throw new NotSupportedException($"{GetType().Name} cannot project or calculate on a compressed property");
            if (query.Order is null)
                return;
            foreach (var order in query.Order.OrderExpressions)
            {
                if (LinqMemberFinder.ReadsMember(order.Expression, properties))
                    throw new NotSupportedException($"{GetType().Name} cannot order by a compressed property");
            }
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
                if (models.Length > 0)
                {
                    var modelType = models[0].GetType();
                    for (var i = 0; i < models.Length; i++)
                    {
                        var model = models[i];
                        copy[i] = model.Copy(modelType);
                    }
                }
                models = copy;
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
            NextProvider.Persist(new Persist(persist.Operation, persist.Event, persist.ModelType, compressedModels, null, persist.Graph));

            for (var i = 0; i < persist.Models.Length; i++)
            {
                var identity = ModelAnalyzer.GetIdentity(modelType, compressedModels[i]);
                ModelAnalyzer.SetIdentity(modelType, persist.Models[i], identity);
            }
        }
        /// <inheritdoc/>
        public override sealed void Update(Persist persist)
        {
            if (persist.Set is not null)
            {
                if (this.Enabled)
                {
                    var properties = CompressionCommon.GetModelCompressableProperties(typeof(TModel), this.Properties);
                    if (LinqMemberFinder.ReadsMember(persist.Where, properties))
                        throw new NotSupportedException($"{GetType().Name} cannot select the rows to change by a compressed property");
                    if (LinqMemberFinder.ReadsMember(persist.Set, properties) || LinqMemberFinder.SetsMember(persist.Set, properties))
                        throw new NotSupportedException($"{GetType().Name} cannot compute a value from or set a compressed property in the store");
                }
                var forwarded = new Persist(persist.Operation, persist.Event, persist.ModelType, persist.Where!, persist.Set);
                NextProvider.Persist(forwarded);
                persist.RowsAffected = forwarded.RowsAffected;
                return;
            }
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var compressedModels = CompressModels(persist.Models, persist.Graph, true);
            if (persist.Where is not null)
            {
                if (this.Enabled && LinqMemberFinder.ReadsMember(persist.Where, CompressionCommon.GetModelCompressableProperties(typeof(TModel), this.Properties)))
                    throw new NotSupportedException($"{GetType().Name} cannot select the rows to change by a compressed property");
                var forwarded = new Persist(persist.Operation, persist.Event, persist.ModelType, persist.Where, compressedModels.Cast<object>().First(), persist.Graph);
                NextProvider.Persist(forwarded);
                persist.RowsAffected = forwarded.RowsAffected;
                return;
            }
            NextProvider.Persist(new Persist(persist.Operation, persist.Event, persist.ModelType, compressedModels, null, persist.Graph));
        }
        /// <inheritdoc/>
        public override sealed void Delete(Persist persist)
        {
            if (this.Enabled && persist.Where is not null && LinqMemberFinder.ReadsMember(persist.Where, CompressionCommon.GetModelCompressableProperties(typeof(TModel), this.Properties)))
                throw new NotSupportedException($"{GetType().Name} cannot select the rows to change by a compressed property");
            NextProvider.Persist(persist);
        }
        /// <inheritdoc/>
        public override sealed async Task CreateAsync(Persist persist, CancellationToken cancellationToken)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var compressedModels = CompressModels(persist.Models, persist.Graph, true);
            await NextProvider.PersistAsync(new Persist(persist.Operation, persist.Event, persist.ModelType, compressedModels, null, persist.Graph), cancellationToken);

            for (var i = 0; i < persist.Models.Length; i++)
            {
                var identity = ModelAnalyzer.GetIdentity(modelType, compressedModels[i]);
                ModelAnalyzer.SetIdentity(modelType, persist.Models[i], identity);
            }
        }
        /// <inheritdoc/>
        public override sealed async Task UpdateAsync(Persist persist, CancellationToken cancellationToken)
        {
            if (persist.Set is not null)
            {
                if (this.Enabled)
                {
                    var properties = CompressionCommon.GetModelCompressableProperties(typeof(TModel), this.Properties);
                    if (LinqMemberFinder.ReadsMember(persist.Where, properties))
                        throw new NotSupportedException($"{GetType().Name} cannot select the rows to change by a compressed property");
                    if (LinqMemberFinder.ReadsMember(persist.Set, properties) || LinqMemberFinder.SetsMember(persist.Set, properties))
                        throw new NotSupportedException($"{GetType().Name} cannot compute a value from or set a compressed property in the store");
                }
                var forwarded = new Persist(persist.Operation, persist.Event, persist.ModelType, persist.Where!, persist.Set);
                await NextProvider.PersistAsync(forwarded, cancellationToken);
                persist.RowsAffected = forwarded.RowsAffected;
                return;
            }
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var compressedModels = CompressModels(persist.Models, persist.Graph, true);
            if (persist.Where is not null)
            {
                if (this.Enabled && LinqMemberFinder.ReadsMember(persist.Where, CompressionCommon.GetModelCompressableProperties(typeof(TModel), this.Properties)))
                    throw new NotSupportedException($"{GetType().Name} cannot select the rows to change by a compressed property");
                var forwarded = new Persist(persist.Operation, persist.Event, persist.ModelType, persist.Where, compressedModels.Cast<object>().First(), persist.Graph);
                await NextProvider.PersistAsync(forwarded, cancellationToken);
                persist.RowsAffected = forwarded.RowsAffected;
                return;
            }
            await NextProvider.PersistAsync(new Persist(persist.Operation, persist.Event, persist.ModelType, compressedModels, null, persist.Graph), cancellationToken);
        }
        /// <inheritdoc/>
        public override sealed Task DeleteAsync(Persist persist, CancellationToken cancellationToken)
        {
            if (this.Enabled && persist.Where is not null && LinqMemberFinder.ReadsMember(persist.Where, CompressionCommon.GetModelCompressableProperties(typeof(TModel), this.Properties)))
                throw new NotSupportedException($"{GetType().Name} cannot select the rows to change by a compressed property");
            return NextProvider.PersistAsync(persist, cancellationToken);
        }
    }
}
