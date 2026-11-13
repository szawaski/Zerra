// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zerra.Collections;
using Zerra.Reflection;
using Zerra.Repository.Linq;
using Zerra.Repository.Reflection;
using Zerra.Repository.Transactions;

namespace Zerra.Repository
{
    /// <summary>
    /// Abstract base provider for transactional store operations, supporting query, persist, and relation linking for <typeparamref name="TModel"/>.
    /// </summary>
    /// <typeparam name="TModel">The model type managed by this provider.</typeparam>
    public abstract partial class RootTransactStoreProvider<TModel> : BaseStore, ITransactStoreProvider<TModel>, IProviderRelation
        where TModel : class, new()
    {
        /// <summary>The <see cref="Type"/> of <typeparamref name="TModel"/>.</summary>
        protected static readonly Type modelType = typeof(TModel);
        /// <summary>The analyzed model metadata for <typeparamref name="TModel"/>.</summary>
        protected static readonly ModelDetail modelTypeDetail = ModelAnalyzer.GetModel(typeof(TModel));
        private static readonly Type objectType = typeof(object);
        private static readonly Type listType = typeof(List<>);
        private static readonly Type hashSetObjectType = typeof(HashSet<object>);
        private static readonly MethodInfo hashSetContainsMethod = typeof(HashSet<object>).GetMethods().First(m => m.Name == nameof(HashSet<>.Contains));

        /// <summary>The analyzed model metadata for <typeparamref name="TModel"/>.</summary>
        protected ModelDetail ModelTypeDetail => modelTypeDetail;

        /// <summary>Gets or sets the memory the models of a stream may hold while the relations it reads from other engines load for them, which happens for each chunk of them that fits. 1 MB by default.</summary>
        public int StreamChunkBytes { get; set; } = 1024 * 1024;

        /// <summary>Initializes the provider, which needs the model to be an entity.</summary>
        protected RootTransactStoreProvider()
        {
            if (!modelTypeDetail.IsDataSourceEntity)
                throw new Exception($"{typeof(TModel).Name} does not have an EntityAttribute");
        }

        /// <summary>Gets the <see cref="Type"/> of the model managed by this provider.</summary>
        protected Type ModelType => modelType;

        private readonly ConcurrentFactoryDictionary<Type, ITransactStoreProvider?> relatedProviders = new();
        private ITransactStoreProvider? GetRelatedProvider(Type type)
        {
            var relatedProvider = relatedProviders.GetOrAdd(type, this, static (type, root) =>
            {
                var propertyType = type;
                var propertyTypeDetails = TypeAnalyzer.GetTypeDetail(propertyType);
                if (propertyTypeDetails.HasIEnumerableGeneric)
                    propertyType = propertyTypeDetails.InnerType!;

                if (!propertyType.IsClass)
                    return null;

                if (root.Repo.TryGetProvider(propertyType, out var provider))
                    return provider;

                return null;
            });
            return relatedProvider;
        }

        /// <summary>Builds a combined where expression from related providers based on the given graph.</summary>
        /// <param name="graph">The graph specifying which members to include.</param>
        /// <returns>A combined where expression, or <see langword="null"/> if none applies.</returns>
        public Expression<Func<TModel, bool>>? GetWhereExpression(Graph? graph)
        {
            Expression<Func<TModel, bool>>? whereExpression = null;

            foreach (var property in ModelTypeDetail.RelatedMembers)
            {
                if (graph is not null && graph.HasMemberExplicitly(property.Name))
                {
                    var relatedProvider = GetRelatedProvider(property.Type);

                    if (relatedProvider is not null)
                    {
                        var relatedGraph = graph.GetChildGraph(property.Name);

                        LambdaExpression? returnWhereExpression = null;
                        if (relatedProvider is IProviderRelation relatedProviderGeneric)
                        {
                            returnWhereExpression = relatedProviderGeneric.GetWhereExpressionIncludingBase(relatedGraph);
                        }

                        if (returnWhereExpression is not null)
                        {
                            whereExpression ??= x => true;
                            whereExpression = LinqAppender.AppendExpressionOnMember(whereExpression, property.MemberInfo, returnWhereExpression);
                        }
                    }
                }
            }

            return whereExpression;
        }
        /// <summary>Returns the where expression for this provider including base type considerations.</summary>
        /// <param name="graph">The graph specifying which members to include.</param>
        /// <returns>A lambda where expression, or <see langword="null"/> if none applies.</returns>
        public LambdaExpression? GetWhereExpressionIncludingBase(Graph? graph)
        {
            return GetWhereExpression(graph);
        }

        /// <summary>Adds the identity and foreign-key members the graph's relations are matched by, for relations loaded through their own providers.</summary>
        /// <param name="graph">The graph to augment with relation members.</param>
        private protected void AddRelationKeys(Graph? graph)
        {
            if (graph is not null && !graph.IsEmpty && ModelTypeDetail.RelatedMembers.Count > 0)
            {
                foreach (var modelPropertyInfo in ModelTypeDetail.RelatedMembers)
                {
                    if (graph.HasMemberExplicitly(modelPropertyInfo.Name))
                    {
                        if (!modelPropertyInfo.IsEnumerable)
                        {
                            //related single
                            if (modelPropertyInfo.ForeignIdentities is null)
                                throw new Exception($"Model {modelPropertyInfo.Type.Name} missing Foreign Identity");
                            graph.AddMembers(modelPropertyInfo.ForeignIdentities);
                        }
                        else
                        {
                            //related many
                            var identityNames = ModelAnalyzer.GetIdentityPropertyNames(modelType);
                            graph.AddMembers(identityNames);
                        }
                    }
                }
            }
        }

        /// <summary>Populates related model properties on the retrieved models using query linking and/or event linking.</summary>
        /// <param name="models">The models whose relations should be populated.</param>
        /// <param name="graph">The graph specifying which members to populate.</param>
        /// <returns>The models with related properties populated.</returns>
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Only interface members reach MakeArrayType, models are reference types so the array code is shared.")]
        public IReadOnlyCollection<TModel> OnGetWithRelations(IReadOnlyCollection<TModel> models, Graph? graph)
        {
            var returnModels = models;

            if (graph is not null && !graph.IsEmpty && ModelTypeDetail.RelatedMembers.Count > 0)
            {
                //Get related
                //var tasks = new HashSet<Task>();
                foreach (var modelPropertyInfo in ModelTypeDetail.RelatedMembers)
                {
                    if (graph.HasMemberExplicitly(modelPropertyInfo.Name))
                    {
                        //var task = Task.Run(() =>
                        //{
                        if (!modelPropertyInfo.IsEnumerable)
                        {
                            //related single
                            var relatedType = modelPropertyInfo.ActualType;
                            var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            _ = ForeignKeyMembers(ModelTypeDetail, modelPropertyInfo, relatedModelInfo);

                            var foreignIdentities = new List<object>();
                            foreach (var model in returnModels)
                            {
                                var foreignIdentity = KeyOrNull(ModelAnalyzer.GetForeignIdentity(modelType, modelPropertyInfo.ForeignIdentityNames!, model));
                                if (foreignIdentity is not null)
                                    foreignIdentities.Add(foreignIdentity);
                            }

                            var queryExpression = KeysCondition(relatedModelInfo, relatedModelInfo.IdentityMembers, foreignIdentities);

                            if (relatedGraph is not null)
                            {
                                var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                                relatedGraph.AddMembers(relatedIdentityPropertyNames);
                            }

                            var query = new LinqModelQuery(relatedType, LinqTerminal.Sequence, queryExpression, null, null, null, relatedGraph);

                            var relatedModels = (IEnumerable)Repo.Query(query)!;

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (var relatedModel in relatedModels)
                            {
                                var relatedIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedModel);
                                if (relatedIdentity is not null)
                                    relatedModelIdentities.Add(relatedModel, relatedIdentity);
                            }

                            foreach (var model in returnModels)
                            {
                                var foreignIdentity = ModelAnalyzer.GetForeignIdentity(modelType, modelPropertyInfo.ForeignIdentityNames!, model);
                                foreach (var relatedModel in relatedModelIdentities)
                                {
                                    if (ModelAnalyzer.CompareIdentities(foreignIdentity, relatedModel.Value))
                                    {
                                        modelPropertyInfo.SetterBoxed(model, relatedModel.Key);
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //related many
                            var relatedType = modelPropertyInfo.ActualType;
                            var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            var relatedForeignIdentityMembers = ForeignKeyMembers(relatedModelInfo, modelPropertyInfo, ModelTypeDetail);

                            relatedGraph?.AddMembers(modelPropertyInfo.ForeignIdentities!);

                            var foreignIdentities = new List<object>();
                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(modelType, model);
                                if (identity is not null)
                                    foreignIdentities.Add(identity);
                            }

                            var queryExpression = KeysCondition(relatedModelInfo, relatedForeignIdentityMembers, foreignIdentities);

                            if (relatedGraph is not null)
                            {
                                var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                                var allNames = relatedIdentityPropertyNames.Concat(modelPropertyInfo.ForeignIdentities!);
                                relatedGraph.AddMembers(allNames);
                            }

                            var query = new LinqModelQuery(relatedType, LinqTerminal.Sequence, queryExpression, null, null, null, relatedGraph);

                            var relatedModels = (IEnumerable)Repo.Query(query)!;

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (var relatedModel in relatedModels)
                            {
                                var relatedIdentity = ModelAnalyzer.GetForeignIdentity(relatedType, modelPropertyInfo.ForeignIdentityNames!, relatedModel);
                                if (relatedIdentity is not null)
                                    relatedModelIdentities.Add(relatedModel, relatedIdentity);
                            }

                            //collections with a creator (List<T>) are filled directly, arrays and the interfaces an array satisfies are filled as arrays
                            var memberTypeDetail = modelPropertyInfo.MemberDetail.TypeDetail;
                            if (memberTypeDetail.HasCreator)
                            {
                                foreach (var model in returnModels)
                                {
                                    var identity = ModelAnalyzer.GetIdentity(modelType, model);
                                    var relatedForModel = (IList)memberTypeDetail.CreatorBoxed!();
                                    foreach (var relatedModel in relatedModelIdentities)
                                    {
                                        if (ModelAnalyzer.CompareIdentities(identity, relatedModel.Value))
                                            _ = relatedForModel.Add(relatedModel.Key);
                                    }
                                    modelPropertyInfo.SetterBoxed(model, relatedForModel);
                                }
                            }
                            else
                            {
                                var arrayType = modelPropertyInfo.Type.IsArray ? modelPropertyInfo.Type : relatedType.MakeArrayType();
                                var matches = new List<object>();
                                foreach (var model in returnModels)
                                {
                                    var identity = ModelAnalyzer.GetIdentity(modelType, model);
                                    matches.Clear();
                                    foreach (var relatedModel in relatedModelIdentities)
                                    {
                                        if (ModelAnalyzer.CompareIdentities(identity, relatedModel.Value))
                                            matches.Add(relatedModel.Key);
                                    }
                                    var relatedForModel = Array.CreateInstanceFromArrayType(arrayType, matches.Count);
                                    for (var i = 0; i < matches.Count; i++)
                                        relatedForModel.SetValue(matches[i], i);
                                    modelPropertyInfo.SetterBoxed(model, relatedForModel);
                                }
                            }
                        }
                        //});
                        //tasks.Add(task);
                    }
                }

                //Task.WaitAll(tasks.ToArray());
            }

            return returnModels;
        }
        /// <summary>Asynchronously populates related model properties on the retrieved models using query linking and/or event linking.</summary>
        /// <param name="models">The models whose relations should be populated.</param>
        /// <param name="graph">The graph specifying which members to populate.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task representing the asynchronous operation, containing the models with related properties populated.</returns>
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Only interface members reach MakeArrayType, models are reference types so the array code is shared.")]
        public async Task<IReadOnlyCollection<TModel>> OnGetWithRelationsAsync(IReadOnlyCollection<TModel> models, Graph? graph, CancellationToken cancellationToken = default)
        {
            var returnModels = models;

            if (graph is not null && !graph.IsEmpty && ModelTypeDetail.RelatedMembers.Count > 0)
            {
                //Get related
                //var tasks = new List<Task>();
                foreach (var modelPropertyInfo in ModelTypeDetail.RelatedMembers)
                {
                    if (graph.HasMemberExplicitly(modelPropertyInfo.Name))
                    {
                        //var task = Task.Run(async () =>
                        //{
                        if (!modelPropertyInfo.IsEnumerable)
                        {
                            //related single
                            var relatedType = modelPropertyInfo.ActualType;
                            var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            _ = ForeignKeyMembers(ModelTypeDetail, modelPropertyInfo, relatedModelInfo);

                            var foreignIdentities = new List<object>();
                            foreach (var model in returnModels)
                            {
                                var foreignIdentity = KeyOrNull(ModelAnalyzer.GetForeignIdentity(modelType, modelPropertyInfo.ForeignIdentityNames!, model));
                                if (foreignIdentity is not null)
                                    foreignIdentities.Add(foreignIdentity);
                            }

                            var queryExpression = KeysCondition(relatedModelInfo, relatedModelInfo.IdentityMembers, foreignIdentities);

                            if (relatedGraph is not null)
                            {
                                var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                                relatedGraph.AddMembers(relatedIdentityPropertyNames);
                            }

                            var query = new LinqModelQuery(relatedType, LinqTerminal.Sequence, queryExpression, null, null, null, relatedGraph);

                            var relatedModels = (IEnumerable)(await Repo.QueryAsync(query, cancellationToken))!;

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (var relatedModel in relatedModels)
                            {
                                var relatedIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedModel);
                                if (relatedIdentity is not null)
                                    relatedModelIdentities.Add(relatedModel, relatedIdentity);
                            }

                            foreach (var model in returnModels)
                            {
                                var foreignIdentity = ModelAnalyzer.GetForeignIdentity(modelType, modelPropertyInfo.ForeignIdentityNames!, model);
                                foreach (var relatedModel in relatedModelIdentities)
                                {
                                    if (ModelAnalyzer.CompareIdentities(foreignIdentity, relatedModel.Value))
                                    {
                                        modelPropertyInfo.SetterBoxed(model, relatedModel.Key);
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //related many
                            var relatedType = modelPropertyInfo.ActualType;
                            var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            var relatedForeignIdentityMembers = ForeignKeyMembers(relatedModelInfo, modelPropertyInfo, ModelTypeDetail);

                            relatedGraph?.AddMembers(modelPropertyInfo.ForeignIdentities!);

                            var foreignIdentities = new List<object>();
                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(modelType, model);
                                if (identity is not null)
                                    foreignIdentities.Add(identity);
                            }

                            var queryExpression = KeysCondition(relatedModelInfo, relatedForeignIdentityMembers, foreignIdentities);

                            if (relatedGraph is not null)
                            {
                                var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                                var allNames = relatedIdentityPropertyNames.Concat(modelPropertyInfo.ForeignIdentities!);
                                relatedGraph.AddMembers(allNames);
                            }

                            var query = new LinqModelQuery(relatedType, LinqTerminal.Sequence, queryExpression, null, null, null, relatedGraph);

                            var relatedModels = (IEnumerable)(await Repo.QueryAsync(query, cancellationToken))!;

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (var relatedModel in relatedModels)
                            {
                                var relatedIdentity = ModelAnalyzer.GetForeignIdentity(relatedType, modelPropertyInfo.ForeignIdentityNames!, relatedModel);
                                if (relatedIdentity is not null)
                                    relatedModelIdentities.Add(relatedModel, relatedIdentity);
                            }

                            //collections with a creator (List<T>) are filled directly, arrays and the interfaces an array satisfies are filled as arrays
                            var memberTypeDetail = modelPropertyInfo.MemberDetail.TypeDetail;
                            if (memberTypeDetail.HasCreator)
                            {
                                foreach (var model in returnModels)
                                {
                                    var identity = ModelAnalyzer.GetIdentity(modelType, model);
                                    var relatedForModel = (IList)memberTypeDetail.CreatorBoxed!();
                                    foreach (var relatedModel in relatedModelIdentities)
                                    {
                                        if (ModelAnalyzer.CompareIdentities(identity, relatedModel.Value))
                                            _ = relatedForModel.Add(relatedModel.Key);
                                    }
                                    modelPropertyInfo.SetterBoxed(model, relatedForModel);
                                }
                            }
                            else
                            {
                                var arrayType = modelPropertyInfo.Type.IsArray ? modelPropertyInfo.Type : relatedType.MakeArrayType();
                                var matches = new List<object>();
                                foreach (var model in returnModels)
                                {
                                    var identity = ModelAnalyzer.GetIdentity(modelType, model);
                                    matches.Clear();
                                    foreach (var relatedModel in relatedModelIdentities)
                                    {
                                        if (ModelAnalyzer.CompareIdentities(identity, relatedModel.Value))
                                            matches.Add(relatedModel.Key);
                                    }
                                    var relatedForModel = Array.CreateInstanceFromArrayType(arrayType, matches.Count);
                                    for (var i = 0; i < matches.Count; i++)
                                        relatedForModel.SetValue(matches[i], i);
                                    modelPropertyInfo.SetterBoxed(model, relatedForModel);
                                }
                            }
                        }
                        //});
                        //tasks.Add(task);
                    }
                }

                //await Task.WhenAll(tasks.ToArray());
            }

            return returnModels;
        }

        /// <summary>Gives the models read as a relation in another model's graph query as they are, as the store read them.</summary>
        /// <param name="models">The models read.</param>
        /// <param name="graph">The graph of the relation.</param>
        /// <returns>The models.</returns>
        public IEnumerable OnGetIncludingBase(IEnumerable models, Graph? graph) => models;

        /// <inheritdoc/>
        public object? Query(LinqQuery query)
        {
            //the repository sends a set or a join here when every query of it runs as it is in this engine
            if (query.Combines)
                return ExecuteLinq(query);
            if (query.Temporal is not null)
                return QueryTemporal(query);
            if (query is LinqModelQuery modelQuery)
            {
                if (ModelTypeDetail.RelatedMembers.Count > 0)
                    query.Where = SplitCrossEngineWhere(query.Where);
                return query.Terminal switch
                {
                    LinqTerminal.Sequence => QueryMany(modelQuery),
                    LinqTerminal.FirstOrDefault => QueryFirst(modelQuery),
                    LinqTerminal.SingleOrDefault => QuerySingle(modelQuery),
                    LinqTerminal.LongCount => QueryCount(modelQuery),
                    LinqTerminal.Any => QueryAny(modelQuery),
                    _ => throw new NotSupportedException($"{query.Terminal} doesn't load models"),
                };
            }
            if (query.HasSubquery)
            {
                query.Where = ReadForeignSubqueries(query.Where);
                query.Select = ReadForeignSubqueries(query.Select);
            }
            if (ModelTypeDetail.RelatedMembers.Count > 0)
            {
                var graph = CrossEngineLinqGraph(query);
                if (graph is not null)
                    return query.Execute((IReadOnlyCollection<TModel>)Query(new LinqModelQuery(modelType, LinqTerminal.Sequence, query.Where, null, null, null, graph))!, false);
                query.Where = SplitCrossEngineWhere(query.Where);
                if (GraphLayers(query, out var treats))
                    return treats ? OnGetLayers(query, ExecuteLinq(query)) : ExecuteLinq(query);
            }
            return ExecuteLinq(query);
        }
        /// <inheritdoc/>
        public async Task<object?> QueryAsync(LinqQuery query, CancellationToken cancellationToken)
        {
            if (query.Combines)
                return await ExecuteLinqAsync(query, cancellationToken);
            if (query.Temporal is not null)
                return await QueryTemporalAsync(query, cancellationToken);
            if (query is LinqModelQuery modelQuery)
            {
                if (ModelTypeDetail.RelatedMembers.Count > 0)
                    query.Where = await SplitCrossEngineWhereAsync(query.Where, cancellationToken);
                return query.Terminal switch
                {
                    LinqTerminal.Sequence => await QueryManyAsync(modelQuery, cancellationToken),
                    LinqTerminal.FirstOrDefault => await QueryFirstAsync(modelQuery, cancellationToken),
                    LinqTerminal.SingleOrDefault => await QuerySingleAsync(modelQuery, cancellationToken),
                    LinqTerminal.LongCount => await QueryCountAsync(modelQuery, cancellationToken),
                    LinqTerminal.Any => await QueryAnyAsync(modelQuery, cancellationToken),
                    _ => throw new NotSupportedException($"{query.Terminal} doesn't load models"),
                };
            }
            if (query.HasSubquery)
            {
                query.Where = await ReadForeignSubqueriesAsync(query.Where, cancellationToken);
                query.Select = await ReadForeignSubqueriesAsync(query.Select, cancellationToken);
            }
            if (ModelTypeDetail.RelatedMembers.Count > 0)
            {
                var graph = CrossEngineLinqGraph(query);
                if (graph is not null)
                    return query.Execute((IReadOnlyCollection<TModel>)(await QueryAsync(new LinqModelQuery(modelType, LinqTerminal.Sequence, query.Where, null, null, null, graph), cancellationToken))!, false);
                query.Where = await SplitCrossEngineWhereAsync(query.Where, cancellationToken);
                if (GraphLayers(query, out var treats))
                    return treats ? OnGetLayers(query, await ExecuteLinqAsync(query, cancellationToken)) : await ExecuteLinqAsync(query, cancellationToken);
            }
            return await ExecuteLinqAsync(query, cancellationToken);
        }
        /// <inheritdoc/>
        public object QueryStream(LinqQuery query)
        {
            if (query is LinqModelQuery)
                throw new NotSupportedException("Models are loaded by their provider");
            if (query.Terminal != LinqTerminal.Sequence)
                throw new NotSupportedException($"A stream gives the elements of a query, not its {query.Terminal}");
            if (query.Temporal is not null)
                return query.StreamFrom(() => QueryTemporal(query), null);
            if (query.Combines || (!query.HasSubquery && ModelTypeDetail.RelatedMembers.Count == 0))
                return ExecuteLinqStream(query);
            return query.StreamFrom(() => OpenStream(query), null);
        }
        //the history of models is read by the store over the states it reads, which load the relations the query reads themselves
        private object? QueryTemporal(LinqQuery query)
        {
            if (query.HasSubquery)
            {
                query.Where = ReadForeignSubqueries(query.Where);
                query.Select = ReadForeignSubqueries(query.Select);
            }
            return ExecuteLinq(query);
        }
        private async Task<object?> QueryTemporalAsync(LinqQuery query, CancellationToken cancellationToken)
        {
            if (query.HasSubquery)
            {
                query.Where = await ReadForeignSubqueriesAsync(query.Where, cancellationToken);
                query.Select = await ReadForeignSubqueriesAsync(query.Select, cancellationToken);
            }
            return await ExecuteLinqAsync(query, cancellationToken);
        }
        private object? OpenStream(LinqQuery query)
        {
            if (query.HasSubquery)
            {
                query.Where = ReadForeignSubqueries(query.Where);
                query.Select = ReadForeignSubqueries(query.Select);
            }
            if (ModelTypeDetail.RelatedMembers.Count > 0)
            {
                var graph = CrossEngineLinqGraph(query);
                if (graph is not null)
                {
                    if (StreamsWithRelations(query))
                    {
                        query.Where = SplitCrossEngineWhere(query.Where);
                        return StreamWithRelations(query, graph);
                    }
                    return query.Execute((IReadOnlyCollection<TModel>)Query(new LinqModelQuery(modelType, LinqTerminal.Sequence, query.Where, null, null, null, graph))!, false);
                }
                query.Where = SplitCrossEngineWhere(query.Where);
                if (GraphLayers(query, out var treats) && treats)
                    return OnGetLayersStream((IEnumerable<TModel>)ExecuteLinqStream(query), query.Graph!);
            }
            return ExecuteLinqStream(query);
        }
        /// <inheritdoc/>
        public object QueryStreamAsync(LinqQuery query, CancellationToken cancellationToken)
        {
            if (query is LinqModelQuery)
                throw new NotSupportedException("Models are loaded by their provider");
            if (query.Terminal != LinqTerminal.Sequence)
                throw new NotSupportedException($"A stream gives the elements of a query, not its {query.Terminal}");
            if (query.Temporal is not null)
                return query.StreamFromAsync(cancellation => QueryTemporalAsync(query, cancellation), null, cancellationToken);
            if (query.Combines || (!query.HasSubquery && ModelTypeDetail.RelatedMembers.Count == 0))
                return ExecuteLinqStreamAsync(query, cancellationToken);
            return query.StreamFromAsync(cancellation => OpenStreamAsync(query, cancellation), null, cancellationToken);
        }
        private async Task<object?> OpenStreamAsync(LinqQuery query, CancellationToken cancellationToken)
        {
            if (query.HasSubquery)
            {
                query.Where = await ReadForeignSubqueriesAsync(query.Where, cancellationToken);
                query.Select = await ReadForeignSubqueriesAsync(query.Select, cancellationToken);
            }
            if (ModelTypeDetail.RelatedMembers.Count > 0)
            {
                var graph = CrossEngineLinqGraph(query);
                if (graph is not null)
                {
                    if (StreamsWithRelations(query))
                    {
                        query.Where = await SplitCrossEngineWhereAsync(query.Where, cancellationToken);
                        return StreamWithRelationsAsync(query, graph, cancellationToken);
                    }
                    return query.Execute((IReadOnlyCollection<TModel>)(await QueryAsync(new LinqModelQuery(modelType, LinqTerminal.Sequence, query.Where, null, null, null, graph), cancellationToken))!, false);
                }
                query.Where = await SplitCrossEngineWhereAsync(query.Where, cancellationToken);
                if (GraphLayers(query, out var treats) && treats)
                    return OnGetLayersStreamAsync((IAsyncEnumerable<TModel>)ExecuteLinqStreamAsync(query, cancellationToken), query.Graph!, cancellationToken);
            }
            return ExecuteLinqStreamAsync(query, cancellationToken);
        }
        //models whose graph reads relations on other engines stream when nothing else reads them; in a transaction a chunk's relations would wait on the connection the stream holds, so the models load first
        private bool StreamsWithRelations(LinqQuery query)
        {
            if (!query.ReturnsModels || !query.GraphRelations || query.Distinct || RepoTransaction.Current is not null)
                return false;
            if (query.Order is null)
                return true;
            var finder = new GraphFinder(this, new Graph());
            foreach (var order in query.Order)
                finder.Read(order.Expression);
            return !finder.Crosses;
        }
        private IEnumerable<TModel> StreamWithRelations(LinqQuery query, Graph graph)
        {
            AddRelationKeys(graph);
            var models = (IEnumerable<TModel>)ExecuteLinqStream(new LinqQuery<TModel, TModel>(query.Where, query.Order, query.Skip, query.Take, graph, LinqTerminal.Sequence));
            var size = new LinqStreamSize(ModelTypeDetail);
            var relationBytes = size.EstimateRelations(graph);
            var chunk = new List<TModel>();
            var bytes = 0L;
            foreach (var model in models)
            {
                chunk.Add(model);
                bytes += size.Columns(model) + relationBytes;
                if (bytes < StreamChunkBytes)
                    continue;
                var loaded = OnGetWithRelations(chunk, graph);
                var loadedBytes = 0L;
                foreach (var related in loaded)
                    loadedBytes += size.Relations(related);
                relationBytes = loadedBytes / chunk.Count;
                foreach (var related in loaded)
                    yield return related;
                chunk = new List<TModel>(chunk.Count);
                bytes = 0;
            }
            if (chunk.Count > 0)
            {
                foreach (var related in OnGetWithRelations(chunk, graph))
                    yield return related;
            }
        }
        private async IAsyncEnumerable<TModel> StreamWithRelationsAsync(LinqQuery query, Graph graph, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            AddRelationKeys(graph);
            var models = (IAsyncEnumerable<TModel>)ExecuteLinqStreamAsync(new LinqQuery<TModel, TModel>(query.Where, query.Order, query.Skip, query.Take, graph, LinqTerminal.Sequence), cancellationToken);
            var size = new LinqStreamSize(ModelTypeDetail);
            var relationBytes = size.EstimateRelations(graph);
            var chunk = new List<TModel>();
            var bytes = 0L;
            await foreach (var model in models.WithCancellation(cancellationToken))
            {
                chunk.Add(model);
                bytes += size.Columns(model) + relationBytes;
                if (bytes < StreamChunkBytes)
                    continue;
                var loaded = await OnGetWithRelationsAsync(chunk, graph, cancellationToken);
                var loadedBytes = 0L;
                foreach (var related in loaded)
                    loadedBytes += size.Relations(related);
                relationBytes = loadedBytes / chunk.Count;
                foreach (var related in loaded)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return related;
                }
                chunk = new List<TModel>(chunk.Count);
                bytes = 0;
            }
            if (chunk.Count > 0)
            {
                foreach (var related in await OnGetWithRelationsAsync(chunk, graph, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return related;
                }
            }
        }
        //a graph read in the statement takes the condition of each layer on a relation, and the layer treats the rows after
        private bool GraphLayers(LinqQuery query, out bool treats)
        {
            treats = false;
            if (!query.GraphRelations || !query.ReturnsModels || !HasLayers(query.Graph!, ModelTypeDetail, ref treats))
                return false;
            var graph = new LinqLayeredGraph(query.Graph!, relationWhere ??= (type, graph) => GetRelatedProvider(type) is IProviderRelation { ReadsInStore: false } layer ? layer.GetWhereExpressionIncludingBase(graph) : null);
            if (treats)
                AddLayerKeys(graph, ModelTypeDetail);
            query.Graph = graph;
            return true;
        }
        private Func<Type, Graph?, LambdaExpression?>? relationWhere;
        private bool HasLayers(Graph graph, ModelDetail model, ref bool treats)
        {
            var any = false;
            foreach (var member in model.RelatedMembers)
            {
                if (!graph.HasMemberExplicitly(member.Name))
                    continue;
                if (GetRelatedProvider(member.ActualType) is IProviderRelation { ReadsInStore: false } layer)
                {
                    any = true;
                    if (layer.TreatsReadModels)
                        treats = true;
                }
                var child = graph.GetChildGraph(member.Name);
                if (child is not null && HasLayers(child, ModelAnalyzer.GetModel(member.ActualType), ref treats))
                    any = true;
            }
            return any;
        }
        //the keys a relation through a layer is matched by are read, so the models its OnGet gives can be set on their parents; the graphs of the relations are copied first, as they're the caller's
        private void AddLayerKeys(Graph graph, ModelDetail model)
        {
            foreach (var member in model.RelatedMembers)
            {
                if (!graph.HasMemberExplicitly(member.Name))
                    continue;
                var child = graph.GetChildGraph(member.Name);
                if (child is not null)
                {
                    child = new Graph(child);
                    graph.AddOrReplaceChildGraph(member.Name, child);
                }
                if (GetRelatedProvider(member.ActualType) is IProviderRelation { ReadsInStore: false, TreatsReadModels: true })
                {
                    if (member.IsEnumerable)
                    {
                        graph.AddMembers(ModelAnalyzer.GetIdentityPropertyNames(model.Type));
                        child?.AddMembers(member.ForeignIdentities!);
                    }
                    else
                    {
                        graph.AddMembers(member.ForeignIdentities!);
                        child?.AddMembers(ModelAnalyzer.GetIdentityPropertyNames(member.ActualType));
                    }
                }
                if (child is not null)
                    AddLayerKeys(child, ModelAnalyzer.GetModel(member.ActualType));
            }
        }
        private object? OnGetLayers(LinqQuery query, object? result)
        {
            switch (result)
            {
                case IReadOnlyCollection<TModel> models:
                    if (models.Count > 0)
                        OnGetLayers(models, ModelTypeDetail, query.Graph!);
                    break;
                case TModel model:
                    OnGetLayers(new TModel[] { model }, ModelTypeDetail, query.Graph!);
                    break;
            }
            return result;
        }
        //deepest first, so a layer sees its rows with their relations treated, as reading them through it would give them
        private void OnGetLayers(IEnumerable parents, ModelDetail model, Graph graph)
        {
            foreach (var member in model.RelatedMembers)
            {
                if (!graph.HasMemberExplicitly(member.Name))
                    continue;
                var relatedGraph = graph.GetChildGraph(member.Name);
                var relatedModel = ModelAnalyzer.GetModel(member.ActualType);
                var layer = GetRelatedProvider(member.ActualType) as IProviderRelation;
                var treats = layer is { ReadsInStore: false, TreatsReadModels: true };
                var below = relatedGraph is not null && relatedModel.RelatedMembers.Count > 0 && TreatsBelow(relatedGraph, relatedModel);
                if (!treats && !below)
                    continue;
                var related = new List<object>();
                foreach (var parent in parents)
                {
                    var value = member.GetterBoxed(parent);
                    if (value is null)
                        continue;
                    if (member.IsEnumerable)
                    {
                        foreach (var element in (IEnumerable)value)
                            related.Add(element!);
                    }
                    else
                    {
                        related.Add(value);
                    }
                }
                if (related.Count == 0)
                    continue;
                if (below)
                    OnGetLayers(related, relatedModel, relatedGraph!);
                if (!treats)
                    continue;
                var returned = layer!.OnGetIncludingBase(related, relatedGraph);
                if (!ReferenceEquals(returned, related))
                    SetRelation(parents, model, member, relatedModel, returned);
            }
        }
        private bool TreatsBelow(Graph graph, ModelDetail model)
        {
            foreach (var member in model.RelatedMembers)
            {
                if (!graph.HasMemberExplicitly(member.Name))
                    continue;
                if (GetRelatedProvider(member.ActualType) is IProviderRelation { ReadsInStore: false, TreatsReadModels: true })
                    return true;
                var child = graph.GetChildGraph(member.Name);
                if (child is not null && TreatsBelow(child, ModelAnalyzer.GetModel(member.ActualType)))
                    return true;
            }
            return false;
        }
        //the models a layer gave in place of the ones read, matched to their parents by key as a relation is loaded
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Only interface members reach MakeArrayType, models are reference types so the array code is shared.")]
        private static void SetRelation(IEnumerable parents, ModelDetail model, ModelMemberDetail member, ModelDetail relatedModel, IEnumerable returned)
        {
            var keyed = new List<(object Model, object? Key)>();
            foreach (var relatedModelValue in returned)
                keyed.Add((relatedModelValue, member.IsEnumerable ? ModelAnalyzer.GetForeignIdentity(relatedModel.Type, member.ForeignIdentityNames!, relatedModelValue) : ModelAnalyzer.GetIdentity(relatedModel.Type, relatedModelValue)));

            if (!member.IsEnumerable)
            {
                foreach (var parent in parents)
                {
                    var foreignIdentity = ModelAnalyzer.GetForeignIdentity(model.Type, member.ForeignIdentityNames!, parent);
                    object? match = null;
                    foreach (var (relatedModelValue, key) in keyed)
                    {
                        if (ModelAnalyzer.CompareIdentities(foreignIdentity, key))
                        {
                            match = relatedModelValue;
                            break;
                        }
                    }
                    member.SetterBoxed(parent, match);
                }
                return;
            }

            var memberTypeDetail = member.MemberDetail.TypeDetail;
            var arrayType = memberTypeDetail.HasCreator ? null : member.Type.IsArray ? member.Type : relatedModel.Type.MakeArrayType();
            var matches = new List<object>();
            foreach (var parent in parents)
            {
                var identity = ModelAnalyzer.GetIdentity(model.Type, parent);
                matches.Clear();
                foreach (var (relatedModelValue, key) in keyed)
                {
                    if (ModelAnalyzer.CompareIdentities(identity, key))
                        matches.Add(relatedModelValue);
                }
                if (arrayType is null)
                {
                    var relatedForModel = (IList)memberTypeDetail.CreatorBoxed!();
                    foreach (var match in matches)
                        _ = relatedForModel.Add(match);
                    member.SetterBoxed(parent, relatedForModel);
                }
                else
                {
                    var relatedForModel = Array.CreateInstanceFromArrayType(arrayType, matches.Count);
                    for (var i = 0; i < matches.Count; i++)
                        relatedForModel.SetValue(matches[i], i);
                    member.SetterBoxed(parent, relatedForModel);
                }
            }
        }
        private IEnumerable<TModel> OnGetLayersStream(IEnumerable<TModel> models, Graph graph)
        {
            var size = new LinqStreamSize(ModelTypeDetail);
            var chunk = new List<TModel>();
            var bytes = 0L;
            foreach (var model in models)
            {
                chunk.Add(model);
                bytes += size.WithRelations(model);
                if (bytes < StreamChunkBytes)
                    continue;
                OnGetLayers(chunk, ModelTypeDetail, graph);
                foreach (var treated in chunk)
                    yield return treated;
                chunk = new List<TModel>(chunk.Count);
                bytes = 0;
            }
            if (chunk.Count > 0)
            {
                OnGetLayers(chunk, ModelTypeDetail, graph);
                foreach (var treated in chunk)
                    yield return treated;
            }
        }
        private async IAsyncEnumerable<TModel> OnGetLayersStreamAsync(IAsyncEnumerable<TModel> models, Graph graph, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var size = new LinqStreamSize(ModelTypeDetail);
            var chunk = new List<TModel>();
            var bytes = 0L;
            await foreach (var model in models.WithCancellation(cancellationToken))
            {
                chunk.Add(model);
                bytes += size.WithRelations(model);
                if (bytes < StreamChunkBytes)
                    continue;
                OnGetLayers(chunk, ModelTypeDetail, graph);
                foreach (var treated in chunk)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return treated;
                }
                chunk = new List<TModel>(chunk.Count);
                bytes = 0;
            }
            if (chunk.Count > 0)
            {
                OnGetLayers(chunk, ModelTypeDetail, graph);
                foreach (var treated in chunk)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return treated;
                }
            }
        }
        //a projected member, a key, a value, or an order through a relation on another engine can't join, so the models load with what the query reads, the relations through their own providers, and the query runs over them in memory
        private Graph? CrossEngineLinqGraph(LinqQuery query)
        {
            if (query.Select is null && query.GroupBy is null && query.Value is null && query.Order is null && !query.GraphRelations)
                return null;
            var finder = new GraphFinder(this, query.ReturnsModels ? (query.GraphRelations ? new Graph(query.Graph) : new Graph(true)) : new Graph());
            if (query.GraphRelations && query.ReturnsModels)
                finder.ReadGraph();
            finder.Read(query.Select);
            finder.Read(query.GroupBy);
            finder.Read(query.Value);
            if (query.Order is not null)
            {
                foreach (var order in query.Order)
                    finder.Read(order.Expression);
            }
            if (!finder.Crosses)
                return null;
            //the condition runs again over the models, so they load with what it reads too
            finder.Read(query.Where);
            return finder.Graph;
        }
        /// <summary>Loads the relations a LINQ query reads onto models loaded without them, through their own providers, for a provider that runs the query over the models.</summary>
        /// <param name="query">The query.</param>
        /// <param name="models">The models.</param>
        /// <returns>The models with the relations.</returns>
        protected IReadOnlyCollection<TModel> LoadLinqRelations(LinqQuery query, IReadOnlyCollection<TModel> models)
        {
            var fromGraph = query.GraphRelations && query.ReturnsModels;
            var finder = new GraphFinder(this, fromGraph ? new Graph(query.Graph) : new Graph(true));
            if (fromGraph)
                finder.ReadGraph();
            finder.Read(query.Where);
            finder.Read(query.Select);
            finder.Read(query.GroupBy);
            finder.Read(query.Value);
            if (query.Order is not null)
            {
                foreach (var order in query.Order)
                    finder.Read(order.Expression);
            }
            return OnGetWithRelations(models, finder.Graph);
        }
        /// <summary>Asynchronously loads the relations a LINQ query reads onto models loaded without them, through their own providers, for a provider that runs the query over the models.</summary>
        /// <param name="query">The query.</param>
        /// <param name="models">The models.</param>
        /// <param name="cancellationToken">A token that cancels the query.</param>
        /// <returns>A task containing the models with the relations.</returns>
        protected Task<IReadOnlyCollection<TModel>> LoadLinqRelationsAsync(LinqQuery query, IReadOnlyCollection<TModel> models, CancellationToken cancellationToken)
        {
            var fromGraph = query.GraphRelations && query.ReturnsModels;
            var finder = new GraphFinder(this, fromGraph ? new Graph(query.Graph) : new Graph(true));
            if (fromGraph)
                finder.ReadGraph();
            finder.Read(query.Where);
            finder.Read(query.Select);
            finder.Read(query.GroupBy);
            finder.Read(query.Value);
            if (query.Order is not null)
            {
                foreach (var order in query.Order)
                    finder.Read(order.Expression);
            }
            return OnGetWithRelationsAsync(models, finder.Graph, cancellationToken);
        }

        private IReadOnlyCollection<TModel> QueryMany(LinqModelQuery query)
        {
            AddRelationKeys(query.Graph);

            var models = Many(query);

            if (models.Count > 0)
            {
                var returnModels = OnGetWithRelations(models, query.Graph);
                return returnModels;
            }

            return models;
        }
        private TModel? QueryFirst(LinqModelQuery query)
        {
            AddRelationKeys(query.Graph);

            var model = First(query);

            var returnModel = model;
            if (model is not null)
            {
                returnModel = OnGetWithRelations([model], query.Graph).FirstOrDefault();
            }

            return returnModel;
        }
        private TModel? QuerySingle(LinqModelQuery query)
        {
            AddRelationKeys(query.Graph);

            var model = Single(query);

            var returnModel = model;
            if (model is not null)
            {
                returnModel = (OnGetWithRelations([model], query.Graph)).FirstOrDefault();
            }

            return returnModel;
        }
        private long QueryCount(LinqModelQuery query)
        {
            return Count(query);
        }
        private bool QueryAny(LinqModelQuery query)
        {
            return Any(query);
        }

        private async Task<IReadOnlyCollection<TModel>> QueryManyAsync(LinqModelQuery query, CancellationToken cancellationToken)
        {
            AddRelationKeys(query.Graph);

            var models = await ManyAsync(query, cancellationToken);

            if (models.Count > 0)
            {
                var returnModels = await OnGetWithRelationsAsync(models, query.Graph, cancellationToken);
                return returnModels;
            }

            return models;
        }
        private async Task<TModel?> QueryFirstAsync(LinqModelQuery query, CancellationToken cancellationToken)
        {
            AddRelationKeys(query.Graph);

            var model = await FirstAsync(query, cancellationToken);

            var returnModel = model;
            if (model is not null)
            {
                returnModel = (await OnGetWithRelationsAsync([model], query.Graph, cancellationToken)).FirstOrDefault();
            }

            return returnModel;
        }
        private async Task<TModel?> QuerySingleAsync(LinqModelQuery query, CancellationToken cancellationToken)
        {
            AddRelationKeys(query.Graph);

            var model = await SingleAsync(query, cancellationToken);

            var returnModel = model;
            if (model is not null)
            {
                returnModel = (await OnGetWithRelationsAsync([model], query.Graph, cancellationToken)).FirstOrDefault();
            }

            return returnModel;
        }
        private Task<long> QueryCountAsync(LinqModelQuery query, CancellationToken cancellationToken)
        {
            return CountAsync(query, cancellationToken);
        }
        private Task<bool> QueryAnyAsync(LinqModelQuery query, CancellationToken cancellationToken)
        {
            return AnyAsync(query, cancellationToken);
        }
        /// <summary>Queries and returns multiple models matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A collection of matching models.</returns>
        protected abstract IReadOnlyCollection<TModel> Many(LinqModelQuery query);
        /// <summary>Queries and returns the first model matching the given query, or <see langword="null"/>.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>The first matching model, or <see langword="null"/>.</returns>
        protected abstract TModel? First(LinqModelQuery query);
        /// <summary>Queries and returns the single model matching the given query, or <see langword="null"/>.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>The single matching model, or <see langword="null"/>.</returns>
        protected abstract TModel? Single(LinqModelQuery query);
        /// <summary>Returns the count of models matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>The count of matching models.</returns>
        protected abstract long Count(LinqModelQuery query);
        /// <summary>Runs a LINQ query whose conditions are on this engine: on the engine in one pass with <see cref="ITransactStoreEngine.ExecuteLinq{TModel, TResult}"/>, or over the models loaded with <see cref="LoadLinqRelations"/> and <see cref="LinqQuery.Execute(IEnumerable{object}, bool)"/>.</summary>
        /// <param name="query">The query.</param>
        /// <returns>The elements or the value.</returns>
        protected abstract object? ExecuteLinq(LinqQuery query);
        /// <summary>Returns whether any model exists matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns><see langword="true"/> if any matching model exists; otherwise <see langword="false"/>.</returns>
        protected abstract bool Any(LinqModelQuery query);

        /// <summary>Asynchronously queries and returns multiple models matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <param name="cancellationToken">A token that cancels the query.</param>
        /// <returns>A task containing a collection of matching models.</returns>
        protected abstract Task<IReadOnlyCollection<TModel>> ManyAsync(LinqModelQuery query, CancellationToken cancellationToken);
        /// <summary>Asynchronously queries and returns the first model matching the given query, or <see langword="null"/>.</summary>
        /// <param name="query">The query parameters.</param>
        /// <param name="cancellationToken">A token that cancels the query.</param>
        /// <returns>A task containing the first matching model, or <see langword="null"/>.</returns>
        protected abstract Task<TModel?> FirstAsync(LinqModelQuery query, CancellationToken cancellationToken);
        /// <summary>Asynchronously queries and returns the single model matching the given query, or <see langword="null"/>.</summary>
        /// <param name="query">The query parameters.</param>
        /// <param name="cancellationToken">A token that cancels the query.</param>
        /// <returns>A task containing the single matching model, or <see langword="null"/>.</returns>
        protected abstract Task<TModel?> SingleAsync(LinqModelQuery query, CancellationToken cancellationToken);
        /// <summary>Asynchronously returns the count of models matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <param name="cancellationToken">A token that cancels the query.</param>
        /// <returns>A task containing the count of matching models.</returns>
        protected abstract Task<long> CountAsync(LinqModelQuery query, CancellationToken cancellationToken);
        /// <summary>Asynchronously runs a LINQ query whose conditions are on this engine, as <see cref="ExecuteLinq"/> does.</summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">A token that cancels the query.</param>
        /// <returns>A task containing the elements or the value.</returns>
        protected abstract Task<object?> ExecuteLinqAsync(LinqQuery query, CancellationToken cancellationToken);
        /// <summary>Runs a LINQ query whose conditions are on this engine and gives its elements as they're read, as <see cref="ITransactStoreEngine.ExecuteLinqStream{TModel, TResult}"/> does, or as <see cref="ExecuteLinq"/> gives them when the query runs over the models in memory.</summary>
        /// <param name="query">The query.</param>
        /// <returns>The elements, an <see cref="IEnumerable{T}"/> of the query's result type.</returns>
        protected abstract object ExecuteLinqStream(LinqQuery query);
        /// <summary>Runs a LINQ query whose conditions are on this engine and gives its elements as they're read, as <see cref="ITransactStoreEngine.ExecuteLinqStreamAsync{TModel, TResult}"/> does, or as <see cref="ExecuteLinqAsync"/> gives them when the query runs over the models in memory.</summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">A token that cancels the query.</param>
        /// <returns>The elements, an <see cref="IAsyncEnumerable{T}"/> of the query's result type.</returns>
        protected abstract object ExecuteLinqStreamAsync(LinqQuery query, CancellationToken cancellationToken);
        /// <summary>Asynchronously returns whether any model exists matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <param name="cancellationToken">A token that cancels the query.</param>
        /// <returns>A task containing <see langword="true"/> if any matching model exists; otherwise <see langword="false"/>.</returns>
        protected abstract Task<bool> AnyAsync(LinqModelQuery query, CancellationToken cancellationToken);
        /// <summary>The <see cref="Type"/> of <see cref="PersistEvent"/>, used for persist event metadata.</summary>
        protected static readonly Type EventInfoType = typeof(PersistEvent);

        /// <summary>Executes a synchronous persist operation (create, update, or delete).</summary>
        /// <param name="persist">The persist operation to execute.</param>
        public void Persist(Persist persist)
        {
            if (RepoTransaction.Current is not null && !SupportsTransactions)
                throw new NotSupportedException($"{Engine?.GetType().Name ?? GetType().Name} can't take part in a transaction");
            if (persist.Where is not null)
            {
                var where = LinqSubquery.Rewrite(persist.Where, out var found);
                if (found)
                {
                    //a persist can be reused, so it keeps its own condition and only this run reads what the subqueries gave
                    var original = persist.Where;
                    persist.Where = ReadForeignSubqueries(where);
                    try
                    {
                        Persist(persist);
                    }
                    finally
                    {
                        persist.Where = original;
                    }
                    return;
                }
            }
            if (persist.Where is not null)
                persist.Where = SplitCrossEngineWhere(persist.Where);

            switch (persist.Operation)
            {
                case PersistOperation.Create:
                    Create(persist);
                    return;
                case PersistOperation.Update:
                    Update(persist);
                    return;
                case PersistOperation.Delete:
                    Delete(persist);
                    return;
                default:
                    throw new NotImplementedException();
            }
        }
        /// <summary>Executes an asynchronous persist operation (create, update, or delete).</summary>
        /// <param name="persist">The persist operation to execute.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task PersistAsync(Persist persist, CancellationToken cancellationToken)
        {
            if (RepoTransaction.Current is not null && !SupportsTransactions)
                throw new NotSupportedException($"{Engine?.GetType().Name ?? GetType().Name} can't take part in a transaction");
            if (persist.Where is not null)
            {
                var where = LinqSubquery.Rewrite(persist.Where, out var found);
                if (found)
                    return PersistAsyncSubquery(persist, where, cancellationToken);
            }
            if (persist.Where is not null && AnyCrossEngine)
            {
                var lookups = FindCrossEngineLookups(persist.Where);
                if (lookups is not null)
                    return PersistAsyncSplit(persist, lookups, cancellationToken);
            }
            return PersistAsyncCore(persist, cancellationToken);
        }
        //a persist can be reused, so it keeps its own condition and only this run reads what the subqueries gave
        private async Task PersistAsyncSubquery(Persist persist, LambdaExpression where, CancellationToken cancellationToken)
        {
            var original = persist.Where;
            persist.Where = await ReadForeignSubqueriesAsync(where, cancellationToken);
            try
            {
                await PersistAsync(persist, cancellationToken);
            }
            finally
            {
                persist.Where = original;
            }
        }
        private async Task PersistAsyncSplit(Persist persist, List<CrossEngineLookup> lookups, CancellationToken cancellationToken)
        {
            foreach (var lookup in lookups)
                lookup.Matches = (IEnumerable)(await Repo.QueryAsync(lookup.Query, cancellationToken))!;
            persist.Where = ReplaceCrossEngineLookups(persist.Where!, lookups);
            await PersistAsyncCore(persist, cancellationToken);
        }
        private Task PersistAsyncCore(Persist persist, CancellationToken cancellationToken)
        {
            return persist.Operation switch
            {
                PersistOperation.Create => CreateAsync(persist, cancellationToken),
                PersistOperation.Update => UpdateAsync(persist, cancellationToken),
                PersistOperation.Delete => DeleteAsync(persist, cancellationToken),
                _ => throw new NotImplementedException(),
            };
        }

        private void Create(Persist persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var graph = persist.Graph is null ? null : new Graph<TModel>(persist.Graph);

            //the related models each model refers to are saved first for their identities, then the models together, then the related models referring to each
            if (graph is not null && !graph.IsEmpty && ModelTypeDetail.RelatedNonEnumerableMembers.Count > 0)
            {
                foreach (var model in persist.Models)
                    PersistSingleRelations(persist.Event, model, graph, true);
            }

            if (persist.Models.Length == 1)
                PersistModel(persist.Event, persist.Models[0], graph, true);
            else
                PersistModels(persist.Event, persist.Models, graph, true);

            if (graph is not null && !graph.IsEmpty && ModelTypeDetail.RelatedEnumerableMembers.Count > 0)
            {
                foreach (var model in persist.Models)
                    PersistManyRelations(persist.Event, model, graph, true);
            }
        }
        private void Update(Persist persist)
        {
            if (persist.Set is not null)
            {
                if (persist.Where is null)
                    throw new ArgumentException($"An update of {ModelTypeDetail.Name} to computed values needs a condition");
                if (persist.Set.Body is not MemberInitExpression set)
                    throw new ArgumentException($"An update of {ModelTypeDetail.Name} by condition computes its values with x => new {ModelTypeDetail.Name} {{ Member = ..., ... }}");
                if (set.Bindings.Count == 0)
                    throw new ArgumentException($"An update of {ModelTypeDetail.Name} by condition needs a member to set");
                foreach (var binding in set.Bindings)
                {
                    var member = ModelTypeDetail.GetMember(binding.Member.Name);
                    if (member.IsIdentity)
                        throw new ArgumentException($"An update of {ModelTypeDetail.Name} by condition cannot set the identity {member.Name}");
                    if (member.IsRelated)
                        throw new ArgumentException($"An update of {ModelTypeDetail.Name} by condition cannot set the relation {member.Name}");
                }
                foreach (var related in ModelTypeDetail.RelatedMembers)
                {
                    if (LinqMemberFinder.ReadsMember(persist.Set, [related.MemberDetail]))
                        throw new NotSupportedException($"An update of {ModelTypeDetail.Name} by condition computes its values from the model's own members, not from {related.Name}");
                }

                persist.RowsAffected = UpdateModelsWhere(persist.Event, persist.Where, persist.Set);
                return;
            }
            if (persist.Where is not null)
            {
                if (persist.Models is null || persist.Models.Length != 1)
                    throw new ArgumentException($"An update of {ModelTypeDetail.Name} by condition needs one model holding the values to set");
                if (persist.Graph is null || persist.Graph.IsEmpty)
                    throw new ArgumentException($"An update of {ModelTypeDetail.Name} by condition needs a graph naming the members to set");
                foreach (var identity in ModelTypeDetail.IdentityMembers)
                {
                    if (persist.Graph.HasMember(identity.Name))
                        throw new ArgumentException($"An update of {ModelTypeDetail.Name} by condition cannot set the identity {identity.Name}");
                }
                foreach (var related in ModelTypeDetail.RelatedMembers)
                {
                    if (persist.Graph.HasMemberExplicitly(related.Name))
                        throw new ArgumentException($"An update of {ModelTypeDetail.Name} by condition cannot set the relation {related.Name}");
                }

                persist.RowsAffected = UpdateModelsWhere(persist.Event, persist.Where, persist.Models[0], new Graph(persist.Graph));
                return;
            }

            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var graph = persist.Graph is null ? null : new Graph(persist.Graph);

            //the related models each model refers to are saved first for their identities, then the models together, then the related models referring to each
            if (graph is not null && !graph.IsEmpty && ModelTypeDetail.RelatedNonEnumerableMembers.Count > 0)
            {
                foreach (var model in persist.Models)
                    PersistSingleRelations(persist.Event, model, graph, false);
            }

            if (persist.Models.Length == 1)
                PersistModel(persist.Event, persist.Models[0], graph, false);
            else
                PersistModels(persist.Event, persist.Models, graph, false);

            if (graph is not null && !graph.IsEmpty && ModelTypeDetail.RelatedEnumerableMembers.Count > 0)
            {
                foreach (var model in persist.Models)
                    PersistManyRelations(persist.Event, model, graph, false);
            }
        }
        private void Delete(Persist persist)
        {
            if (persist.Where is not null)
            {
                persist.RowsAffected = DeleteModelsWhere(persist.Event, persist.Where);
                return;
            }

            object[] ids;
            if (persist.IDs is not null)
            {
                ids = persist.IDs.Cast<object>().ToArray();
            }
            else if (persist.Models is not null)
            {
                ids = new object[persist.Models.Length];
                var i = 0;
                foreach (var model in persist.Models)
                {
                    var id = ModelAnalyzer.GetIdentity(persist.ModelType, model);
                    ids[i++] = id;
                }
            }
            else
            {
                return;
            }

            if (ids.Length == 0)
                return;

            var graph = persist.Graph is null ? null : new Graph<TModel>(persist.Graph);

            if (graph is not null && !graph.IsEmpty && ModelTypeDetail.RelatedEnumerableMembers.Count > 0)
            {
                foreach (var id in ids)
                {
                    DeleteManyRelations(persist.Event, id, graph);
                }
            }

            DeleteModel(persist.Event, ids);
        }

        private async Task CreateAsync(Persist persist, CancellationToken cancellationToken)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var graph = persist.Graph is null ? null : new Graph<TModel>(persist.Graph);

            //the related models each model refers to are saved first for their identities, then the models together, then the related models referring to each
            if (graph is not null && !graph.IsEmpty && ModelTypeDetail.RelatedNonEnumerableMembers.Count > 0)
            {
                foreach (var model in persist.Models)
                    await PersistSingleRelationsAsync(persist.Event, model, graph, true, cancellationToken);
            }

            if (persist.Models.Length == 1)
                await PersistModelAsync(persist.Event, persist.Models[0], graph, true, cancellationToken);
            else
                await PersistModelsAsync(persist.Event, persist.Models, graph, true, cancellationToken);

            if (graph is not null && !graph.IsEmpty && ModelTypeDetail.RelatedEnumerableMembers.Count > 0)
            {
                foreach (var model in persist.Models)
                    await PersistManyRelationsAsync(persist.Event, model, graph, true, cancellationToken);
            }
        }
        private async Task UpdateAsync(Persist persist, CancellationToken cancellationToken)
        {
            if (persist.Set is not null)
            {
                if (persist.Where is null)
                    throw new ArgumentException($"An update of {ModelTypeDetail.Name} to computed values needs a condition");
                if (persist.Set.Body is not MemberInitExpression set)
                    throw new ArgumentException($"An update of {ModelTypeDetail.Name} by condition computes its values with x => new {ModelTypeDetail.Name} {{ Member = ..., ... }}");
                if (set.Bindings.Count == 0)
                    throw new ArgumentException($"An update of {ModelTypeDetail.Name} by condition needs a member to set");
                foreach (var binding in set.Bindings)
                {
                    var member = ModelTypeDetail.GetMember(binding.Member.Name);
                    if (member.IsIdentity)
                        throw new ArgumentException($"An update of {ModelTypeDetail.Name} by condition cannot set the identity {member.Name}");
                    if (member.IsRelated)
                        throw new ArgumentException($"An update of {ModelTypeDetail.Name} by condition cannot set the relation {member.Name}");
                }
                foreach (var related in ModelTypeDetail.RelatedMembers)
                {
                    if (LinqMemberFinder.ReadsMember(persist.Set, [related.MemberDetail]))
                        throw new NotSupportedException($"An update of {ModelTypeDetail.Name} by condition computes its values from the model's own members, not from {related.Name}");
                }

                persist.RowsAffected = await UpdateModelsWhereAsync(persist.Event, persist.Where, persist.Set, cancellationToken);
                return;
            }
            if (persist.Where is not null)
            {
                if (persist.Models is null || persist.Models.Length != 1)
                    throw new ArgumentException($"An update of {ModelTypeDetail.Name} by condition needs one model holding the values to set");
                if (persist.Graph is null || persist.Graph.IsEmpty)
                    throw new ArgumentException($"An update of {ModelTypeDetail.Name} by condition needs a graph naming the members to set");
                foreach (var identity in ModelTypeDetail.IdentityMembers)
                {
                    if (persist.Graph.HasMember(identity.Name))
                        throw new ArgumentException($"An update of {ModelTypeDetail.Name} by condition cannot set the identity {identity.Name}");
                }
                foreach (var related in ModelTypeDetail.RelatedMembers)
                {
                    if (persist.Graph.HasMemberExplicitly(related.Name))
                        throw new ArgumentException($"An update of {ModelTypeDetail.Name} by condition cannot set the relation {related.Name}");
                }

                persist.RowsAffected = await UpdateModelsWhereAsync(persist.Event, persist.Where, persist.Models[0], new Graph(persist.Graph), cancellationToken);
                return;
            }

            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var graph = persist.Graph is null ? null : new Graph(persist.Graph);

            //the related models each model refers to are saved first for their identities, then the models together, then the related models referring to each
            if (graph is not null && !graph.IsEmpty && ModelTypeDetail.RelatedNonEnumerableMembers.Count > 0)
            {
                foreach (var model in persist.Models)
                    await PersistSingleRelationsAsync(persist.Event, model, graph, false, cancellationToken);
            }

            if (persist.Models.Length == 1)
                await PersistModelAsync(persist.Event, persist.Models[0], graph, false, cancellationToken);
            else
                await PersistModelsAsync(persist.Event, persist.Models, graph, false, cancellationToken);

            if (graph is not null && !graph.IsEmpty && ModelTypeDetail.RelatedEnumerableMembers.Count > 0)
            {
                foreach (var model in persist.Models)
                    await PersistManyRelationsAsync(persist.Event, model, graph, false, cancellationToken);
            }
        }
        private async Task DeleteAsync(Persist persist, CancellationToken cancellationToken)
        {
            if (persist.Where is not null)
            {
                persist.RowsAffected = await DeleteModelsWhereAsync(persist.Event, persist.Where, cancellationToken);
                return;
            }

            object[] ids;
            if (persist.IDs is not null)
            {
                ids = persist.IDs.Cast<object>().ToArray();
            }
            else if (persist.Models is not null)
            {
                ids = new object[persist.Models.Length];
                var i = 0;
                foreach (var model in persist.Models)
                {
                    var id = ModelAnalyzer.GetIdentity(persist.ModelType, model);
                    ids[i++] = id;
                }
            }
            else
            {
                return;
            }

            if (ids.Length == 0)
                return;

            var graph = persist.Graph is null ? null : new Graph<TModel>(persist.Graph);

            if (graph is not null && !graph.IsEmpty && ModelTypeDetail.RelatedEnumerableMembers.Count > 0)
            {
                foreach (var id in ids)
                {
                    await DeleteManyRelationsAsync(persist.Event, id, graph, cancellationToken);
                }
            }

            await DeleteModelAsync(persist.Event, ids, cancellationToken);
        }

        /// <summary>Persists non-enumerable (single) related models and sets their foreign identity on the parent model.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="model">The parent model whose single relations should be persisted.</param>
        /// <param name="graph">The graph specifying which relations to persist.</param>
        /// <param name="create"><see langword="true"/> to create related models; <see langword="false"/> to update.</param>
        protected void PersistSingleRelations(PersistEvent @event, object model, Graph graph, bool create)
        {
            if (ModelTypeDetail.RelatedNonEnumerableMembers.Count == 0)
                return;

            //var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelTypeDetail.RelatedNonEnumerableMembers)
            {
                if (graph.HasMemberExplicitly(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(() =>
                    //{
                    var relatedType = modelPropertyInfo.ActualType;

                    if (modelPropertyInfo.ForeignIdentities is null)
                        throw new Exception($"Model {modelPropertyInfo.Type.Name} missing Foreign Identity");
                    graph.AddMembers(modelPropertyInfo.ForeignIdentities);

                    var relatedModel = modelPropertyInfo.GetterBoxed(model);

                    if (relatedModel is not null)
                    {
                        var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                        var persist = new Persist(create ? PersistOperation.Create : PersistOperation.Update, @event, relatedType, [relatedModel], null, relatedGraph);
                        Repo.Persist(persist);

                        var relatedIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedModel);
                        ModelAnalyzer.SetForeignIdentity(modelType, modelPropertyInfo.ForeignIdentityNames!, model, relatedIdentity);
                    }
                    else
                    {
                        ModelAnalyzer.SetForeignIdentity(modelType, modelPropertyInfo.ForeignIdentityNames!, model, null);
                    }
                    //});
                    //tasks.Add(task);
                }
            }

            //if (tasks.Count == 0)
            //    return;

            //Task.WaitAll(tasks.ToArray());
        }
        //the members of the holder holding a relation's key, one for each identity of the model referred to and in its order
        private static ModelMemberDetail[] ForeignKeyMembers(ModelDetail holder, ModelMemberDetail relation, ModelDetail referred)
        {
            var names = relation.ForeignIdentities ?? throw new Exception($"Model {relation.Type.Name} missing Foreign Identity");
            if (names.Length != referred.IdentityMembers.Count)
                throw new Exception($"{relation.Name} names {names.Length} foreign identities where {referred.Name} has {referred.IdentityMembers.Count} identities");
            var members = new ModelMemberDetail[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                if (!holder.TryGetProperty(names[i], out var member))
                    throw new Exception($"Missing ForeignIdentity {names[i]} for {referred.Name} defined in {holder.Name}");
                members[i] = member;
            }
            return members;
        }
        //the models whose members hold one of the keys; with several members each is looked for among its own values, which can find more models than the keys, so the models found are matched to the keys after
        private static LambdaExpression KeysCondition(ModelDetail model, IReadOnlyList<ModelMemberDetail> members, ICollection<object> keys)
        {
            var parameter = Expression.Parameter(model.Type, "x");
            Expression? condition = null;
            for (var i = 0; i < members.Count; i++)
            {
                var values = new HashSet<object>();
                foreach (var key in keys)
                {
                    var value = members.Count == 1 ? key : ((object?[])key)[i];
                    if (value is not null)
                        _ = values.Add(value);
                }
                var contains = Expression.Call(Expression.Constant(values, hashSetObjectType), hashSetContainsMethod, Expression.Convert(Expression.MakeMemberAccess(parameter, members[i].MemberInfo), objectType));
                condition = condition is null ? contains : Expression.AndAlso(condition, contains);
            }
            return Expression.Lambda(model.LambdaDelegateType, condition!, parameter);
        }
        //the models whose members hold the key
        private static LambdaExpression KeyCondition(ModelDetail model, IReadOnlyList<ModelMemberDetail> members, object? key)
        {
            var parameter = Expression.Parameter(model.Type, "x");
            Expression? condition = null;
            for (var i = 0; i < members.Count; i++)
            {
                var value = members.Count == 1 ? key : ((object?[]?)key)?[i];
                var equal = Expression.Equal(Expression.MakeMemberAccess(parameter, members[i].MemberInfo), Expression.Constant(value, members[i].Type));
                condition = condition is null ? equal : Expression.AndAlso(condition, equal);
            }
            return Expression.Lambda(model.LambdaDelegateType, condition!, parameter);
        }
        //a key with a part that's null refers to nothing
        private static object? KeyOrNull(object? key)
        {
            if (key is object?[] parts)
            {
                foreach (var part in parts)
                {
                    if (part is null)
                        return null;
                }
            }
            return key;
        }

        /// <summary>Persists enumerable (many) related models, determining which should be created, updated, or deleted.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="model">The parent model whose many relations should be persisted.</param>
        /// <param name="graph">The graph specifying which relations to persist.</param>
        /// <param name="create"><see langword="true"/> if the parent model is being created; <see langword="false"/> if updated.</param>
        protected void PersistManyRelations(PersistEvent @event, object model, Graph graph, bool create)
        {
            //var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelTypeDetail.RelatedEnumerableMembers)
            {
                if (graph.HasMemberExplicitly(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(() =>
                    //{
                    var relatedType = modelPropertyInfo.ActualType;

                    var relatedModels = (IEnumerable)modelPropertyInfo.GetterBoxed(model)!;

                    var identity = ModelAnalyzer.GetIdentity(modelType, model);

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    var relatedForeignIdentityMembers = ForeignKeyMembers(relatedModelInfo, modelPropertyInfo, ModelTypeDetail);

                    var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                    var relatedModelsExisting = new List<object>();
                    var relatedModelsDelete = new List<object>();
                    var relatedModelsCreate = new List<object>();
                    var relatedModelsUpdate = new List<object>();

                    if (!create)
                    {
                        var queryExpression = KeyCondition(relatedModelInfo, relatedForeignIdentityMembers, identity);
                        var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                        var allNames = relatedIdentityPropertyNames.Concat(modelPropertyInfo.ForeignIdentities!);
                        var queryGraph = new Graph(allNames);

                        var query = new LinqModelQuery(relatedType, LinqTerminal.Sequence, queryExpression, null, null, null, queryGraph);

                        var relatedExistings = (IEnumerable)Repo.Query(query)!;

                        foreach (var relatedExisting in relatedExistings)
                            relatedModelsExisting.Add(relatedExisting);
                    }

                    var relatedModelIdentities = new Dictionary<object, object>();

                    if (relatedModels is not null)
                    {
                        foreach (var relatedModel in relatedModels)
                        {
                            ModelAnalyzer.SetForeignIdentity(relatedType, modelPropertyInfo.ForeignIdentityNames!, relatedModel, identity);
                        }

                        foreach (var relatedModel in relatedModels)
                        {
                            var relatedIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedModel);
                            relatedModelIdentities.Add(relatedModel, relatedIdentity);
                        }
                    }

                    var relatedModelsExistingIdentities = new Dictionary<object, object>();
                    foreach (var relatedExisting in relatedModelsExisting)
                    {
                        var relatedExistingIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedExisting);
                        relatedModelsExistingIdentities.Add(relatedExisting, relatedExistingIdentity);
                    }

                    var relatedModelsUpdateIdentities = new Dictionary<object, object>();

                    foreach (var relatedModel in relatedModelIdentities)
                    {
                        var foundExisting = false;
                        foreach (var relatedExisting in relatedModelsExistingIdentities)
                        {
                            var relatedExistingIdentity = relatedExisting.Value;
                            if (ModelAnalyzer.CompareIdentities(relatedExistingIdentity, relatedModel.Value))
                            {
                                relatedModelsUpdate.Add(relatedModel.Key);
                                relatedModelsUpdateIdentities.Add(relatedModel.Key, relatedModel.Value);
                                foundExisting = true;
                                break;
                            }
                        }
                        if (!foundExisting)
                        {
                            relatedModelsCreate.Add(relatedModel.Key);
                        }
                    }

                    foreach (var relatedExisting in relatedModelsExistingIdentities)
                    {
                        var foundUpdating = false;
                        foreach (var relatedModelUpdate in relatedModelsUpdateIdentities)
                        {
                            var relatedIdentity = relatedModelUpdate.Value;
                            if (ModelAnalyzer.CompareIdentities(relatedExisting.Value, relatedIdentity))
                            {
                                foundUpdating = true;
                                break;
                            }
                        }
                        if (!foundUpdating)
                        {
                            relatedModelsDelete.Add(relatedExisting.Key);
                        }
                    }

                    if (relatedModelsDelete.Count > 0)
                    {
                        var persist = new Persist(PersistOperation.Delete, @event, relatedType, relatedModelsDelete.ToArray(), null, relatedGraph);
                        Repo.Persist(persist);
                    }
                    if (relatedModelsUpdate.Count > 0)
                    {
                        var persist = new Persist(PersistOperation.Update, @event, relatedType, relatedModelsUpdate.ToArray(), null, relatedGraph);
                        Repo.Persist(persist);
                    }
                    if (relatedModelsCreate.Count > 0)
                    {
                        var persist = new Persist(PersistOperation.Create, @event, relatedType, relatedModelsCreate.ToArray(), null, relatedGraph);
                        Repo.Persist(persist);
                    }
                    //});
                    //tasks.Add(task);
                }
            }

            //if (tasks.Count == 0)
            //    return;

            //Task.WaitAll(tasks.ToArray());
        }
        /// <summary>Deletes all enumerable related models associated with the given parent identity.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="id">The identity of the parent model whose many relations should be deleted.</param>
        /// <param name="graph">The graph specifying which relations to delete.</param>
        protected void DeleteManyRelations(PersistEvent @event, object id, Graph graph)
        {
            //var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelTypeDetail.RelatedEnumerableMembers)
            {
                if (graph.HasMemberExplicitly(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(() =>
                    //{
                    var relatedType = modelPropertyInfo.ActualType;

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    var relatedForeignIdentityMembers = ForeignKeyMembers(relatedModelInfo, modelPropertyInfo, ModelTypeDetail);

                    var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                    var queryExpression = KeyCondition(relatedModelInfo, relatedForeignIdentityMembers, id);
                    var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                    var allNames = relatedIdentityPropertyNames.Concat(modelPropertyInfo.ForeignIdentities!);
                    var queryGraph = new Graph(allNames);

                    var query = new LinqModelQuery(relatedType, LinqTerminal.Sequence, queryExpression, null, null, null, queryGraph);

                    var relatedExistings = (IEnumerable)Repo.Query(query)!;

                    if (relatedExistings.Cast<object>().Any())
                    {
                        var persist = new Persist(PersistOperation.Delete, @event, relatedType, relatedExistings.Cast<object>().ToArray(), null, relatedGraph);
                        Repo.Persist(persist);
                    }
                    //});
                    //tasks.Add(task);
                }
            }

            //if (tasks.Count == 0)
            //    return;

            //Task.WaitAll(tasks.ToArray());
        }

        /// <summary>Asynchronously persists non-enumerable (single) related models and sets their foreign identity on the parent model.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="model">The parent model whose single relations should be persisted.</param>
        /// <param name="graph">The graph specifying which relations to persist.</param>
        /// <param name="create"><see langword="true"/> to create related models; <see langword="false"/> to update.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected async Task PersistSingleRelationsAsync(PersistEvent @event, object model, Graph graph, bool create, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelTypeDetail.RelatedNonEnumerableMembers)
            {
                if (graph.HasMemberExplicitly(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(async () =>
                    //{
                    var relatedType = modelPropertyInfo.ActualType;

                    if (modelPropertyInfo.ForeignIdentities is null)
                        throw new Exception($"Model {modelPropertyInfo.Type.Name} missing Foreign Identity");
                    graph.AddMembers(modelPropertyInfo.ForeignIdentities);

                    var relatedModel = modelPropertyInfo.GetterBoxed(model);

                    if (relatedModel is not null)
                    {
                        var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                        var persist = new Persist(create ? PersistOperation.Create : PersistOperation.Update, @event, relatedType, [relatedModel], null, relatedGraph);
                        await Repo.PersistAsync(persist, cancellationToken);

                        var relatedIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedModel);
                        ModelAnalyzer.SetForeignIdentity(modelType, modelPropertyInfo.ForeignIdentityNames!, model, relatedIdentity);
                    }
                    else
                    {
                        ModelAnalyzer.SetForeignIdentity(modelType, modelPropertyInfo.ForeignIdentityNames!, model, null);
                    }
                    //});
                    //tasks.Add(task);
                }
            }

            //if (tasks.Count == 0)
            //    return Task.CompletedTask;

            //return Task.WhenAll(tasks.ToArray());
        }
        /// <summary>Asynchronously persists enumerable (many) related models, determining which should be created, updated, or deleted.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="model">The parent model whose many relations should be persisted.</param>
        /// <param name="graph">The graph specifying which relations to persist.</param>
        /// <param name="create"><see langword="true"/> if the parent model is being created; <see langword="false"/> if updated.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected async Task PersistManyRelationsAsync(PersistEvent @event, object model, Graph graph, bool create, CancellationToken cancellationToken)
        {
            //var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelTypeDetail.RelatedEnumerableMembers)
            {
                if (graph.HasMemberExplicitly(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(async () =>
                    //{
                    var relatedType = modelPropertyInfo.ActualType;

                    var relatedModels = (IEnumerable)modelPropertyInfo.GetterBoxed(model)!;

                    var identity = ModelAnalyzer.GetIdentity(modelType, model);

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    var relatedForeignIdentityMembers = ForeignKeyMembers(relatedModelInfo, modelPropertyInfo, ModelTypeDetail);

                    var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                    var relatedModelsExisting = new List<object>();
                    var relatedModelsDelete = new List<object>();
                    var relatedModelsCreate = new List<object>();
                    var relatedModelsUpdate = new List<object>();

                    if (!create)
                    {
                        var queryExpression = KeyCondition(relatedModelInfo, relatedForeignIdentityMembers, identity);
                        var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                        var allNames = relatedIdentityPropertyNames.Concat(modelPropertyInfo.ForeignIdentities!);
                        var queryGraph = new Graph(allNames);

                        var query = new LinqModelQuery(relatedType, LinqTerminal.Sequence, queryExpression, null, null, null, queryGraph);

                        var relatedExistings = (IEnumerable)(await Repo.QueryAsync(query, cancellationToken))!;

                        foreach (var relatedExisting in relatedExistings)
                            relatedModelsExisting.Add(relatedExisting);
                    }

                    var relatedModelIdentities = new Dictionary<object, object>();

                    if (relatedModels is not null)
                    {
                        foreach (var relatedModel in relatedModels)
                        {
                            ModelAnalyzer.SetForeignIdentity(relatedType, modelPropertyInfo.ForeignIdentityNames!, relatedModel, identity);
                        }

                        foreach (var relatedModel in relatedModels)
                        {
                            var relatedIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedModel);
                            relatedModelIdentities.Add(relatedModel, relatedIdentity);
                        }
                    }

                    var relatedModelsExistingIdentities = new Dictionary<object, object>();
                    foreach (var relatedExisting in relatedModelsExisting)
                    {
                        var relatedExistingIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedExisting);
                        relatedModelsExistingIdentities.Add(relatedExisting, relatedExistingIdentity);
                    }

                    var relatedModelsUpdateIdentities = new Dictionary<object, object>();

                    foreach (var relatedModel in relatedModelIdentities)
                    {
                        var foundExisting = false;
                        foreach (var relatedExisting in relatedModelsExistingIdentities)
                        {
                            var relatedExistingIdentity = relatedExisting.Value;
                            if (ModelAnalyzer.CompareIdentities(relatedExistingIdentity, relatedModel.Value))
                            {
                                relatedModelsUpdate.Add(relatedModel.Key);
                                relatedModelsUpdateIdentities.Add(relatedModel.Key, relatedModel.Value);
                                foundExisting = true;
                                break;
                            }
                        }
                        if (!foundExisting)
                        {
                            relatedModelsCreate.Add(relatedModel.Key);
                        }
                    }

                    foreach (var relatedExisting in relatedModelsExistingIdentities)
                    {
                        var foundUpdating = false;
                        foreach (var relatedModelUpdate in relatedModelsUpdateIdentities)
                        {
                            var relatedIdentity = relatedModelUpdate.Value;
                            if (ModelAnalyzer.CompareIdentities(relatedExisting.Value, relatedIdentity))
                            {
                                foundUpdating = true;
                                break;
                            }
                        }
                        if (!foundUpdating)
                        {
                            relatedModelsDelete.Add(relatedExisting.Key);
                        }
                    }

                    if (relatedModelsDelete.Count > 0)
                    {
                        var persist = new Persist(PersistOperation.Delete, @event, relatedType, relatedModelsDelete.ToArray(), null, relatedGraph);
                        await Repo.PersistAsync(persist, cancellationToken);
                    }
                    if (relatedModelsUpdate.Count > 0)
                    {
                        var persist = new Persist(PersistOperation.Update, @event, relatedType, relatedModelsUpdate.ToArray(), null, relatedGraph);
                        await Repo.PersistAsync(persist, cancellationToken);
                    }
                    if (relatedModelsCreate.Count > 0)
                    {
                        var persist = new Persist(PersistOperation.Create, @event, relatedType, relatedModelsCreate.ToArray(), null, relatedGraph);
                        await Repo.PersistAsync(persist, cancellationToken);
                    }
                    //});
                    //tasks.Add(task);
                }
            }

            //if (tasks.Count == 0)
            //    return Task.CompletedTask;

            //return Task.WhenAll(tasks.ToArray());
        }
        /// <summary>Asynchronously deletes all enumerable related models associated with the given parent identity.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="id">The identity of the parent model whose many relations should be deleted.</param>
        /// <param name="graph">The graph specifying which relations to delete.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected async Task DeleteManyRelationsAsync(PersistEvent @event, object id, Graph graph, CancellationToken cancellationToken)
        {
            //var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelTypeDetail.RelatedEnumerableMembers)
            {
                if (graph.HasMemberExplicitly(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(async () =>
                    //{
                    var relatedType = modelPropertyInfo.ActualType;

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    var relatedForeignIdentityMembers = ForeignKeyMembers(relatedModelInfo, modelPropertyInfo, ModelTypeDetail);

                    var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                    var queryExpression = KeyCondition(relatedModelInfo, relatedForeignIdentityMembers, id);
                    var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                    var allNames = relatedIdentityPropertyNames.Concat(modelPropertyInfo.ForeignIdentities!);
                    var queryGraph = new Graph(allNames);

                    var query = new LinqModelQuery(relatedType, LinqTerminal.Sequence, queryExpression, null, null, null, queryGraph);

                    var relatedExistings = (IEnumerable)(await Repo.QueryAsync(query, cancellationToken))!;

                    if (relatedExistings.Cast<object>().Any())
                    {
                        var persist = new Persist(PersistOperation.Delete, @event, relatedType, relatedExistings.Cast<object>().ToArray(), null, relatedGraph);
                        await Repo.PersistAsync(persist, cancellationToken);
                    }
                    //});
                    //tasks.Add(task);
                }
            }

            //if (tasks.Count == 0)
            //    return Task.CompletedTask;

            //return Task.WhenAll(tasks.ToArray());
        }

        /// <summary>Persists a single model to the underlying store.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="model">The model to persist.</param>
        /// <param name="graph">The graph specifying which members to persist.</param>
        /// <param name="create"><see langword="true"/> to create the model; <see langword="false"/> to update.</param>
        protected abstract void PersistModel(PersistEvent @event, object model, Graph? graph, bool create);
        /// <summary>Persists many models without their relations, by default one at a time with <see cref="PersistModel"/>.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="models">The models to persist.</param>
        /// <param name="graph">The graph specifying which members to persist.</param>
        /// <param name="create"><see langword="true"/> to create; <see langword="false"/> to update.</param>
        protected virtual void PersistModels(PersistEvent @event, object[] models, Graph? graph, bool create)
        {
            foreach (var model in models)
                PersistModel(@event, model, graph, create);
        }
        /// <summary>Deletes models with the specified identities from the underlying store.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="ids">The identities of the models to delete.</param>
        protected abstract void DeleteModel(PersistEvent @event, object[] ids);
        /// <summary>Sets the graph's members to the values on every model matching the condition, without loading them.</summary>
        /// <param name="event">The persist event.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="model">The model holding the values to set.</param>
        /// <param name="graph">The members to set, never empty and never an identity or relation.</param>
        /// <returns>How many models were updated.</returns>
        protected abstract int UpdateModelsWhere(PersistEvent @event, LambdaExpression where, object model, Graph graph);
        /// <summary>Sets members on every model matching the condition to values computed from each one, without loading them.</summary>
        /// <param name="event">The persist event.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="set">The members to set and their values in terms of the model, as a projection of the model to itself; never an identity or relation, and reading only the model's own members.</param>
        /// <returns>How many models were updated.</returns>
        protected abstract int UpdateModelsWhere(PersistEvent @event, LambdaExpression where, LambdaExpression set);
        /// <summary>Deletes every model matching the condition, without loading them.</summary>
        /// <param name="event">The persist event.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <returns>How many models were deleted.</returns>
        protected abstract int DeleteModelsWhere(PersistEvent @event, LambdaExpression where);

        /// <summary>Asynchronously persists a single model to the underlying store.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="model">The model to persist.</param>
        /// <param name="graph">The graph specifying which members to persist.</param>
        /// <param name="create"><see langword="true"/> to create the model; <see langword="false"/> to update.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected abstract Task PersistModelAsync(PersistEvent @event, object model, Graph? graph, bool create, CancellationToken cancellationToken);
        /// <summary>Asynchronously persists many models without their relations, by default one at a time with <see cref="PersistModelAsync"/>.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="models">The models to persist.</param>
        /// <param name="graph">The graph specifying which members to persist.</param>
        /// <param name="create"><see langword="true"/> to create; <see langword="false"/> to update.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        protected virtual async Task PersistModelsAsync(PersistEvent @event, object[] models, Graph? graph, bool create, CancellationToken cancellationToken)
        {
            foreach (var model in models)
                await PersistModelAsync(@event, model, graph, create, cancellationToken);
        }
        /// <summary>Asynchronously deletes models with the specified identities from the underlying store.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="ids">The identities of the models to delete.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected abstract Task DeleteModelAsync(PersistEvent @event, object[] ids, CancellationToken cancellationToken);
        /// <summary>Asynchronously sets the graph's members to the values on every model matching the condition, without loading them.</summary>
        /// <param name="event">The persist event.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="model">The model holding the values to set.</param>
        /// <param name="graph">The members to set, never empty and never an identity or relation.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task containing how many models were updated.</returns>
        protected abstract Task<int> UpdateModelsWhereAsync(PersistEvent @event, LambdaExpression where, object model, Graph graph, CancellationToken cancellationToken);
        /// <summary>Asynchronously sets members on every model matching the condition to values computed from each one, without loading them.</summary>
        /// <param name="event">The persist event.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="set">The members to set and their values in terms of the model, as a projection of the model to itself; never an identity or relation, and reading only the model's own members.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task containing how many models were updated.</returns>
        protected abstract Task<int> UpdateModelsWhereAsync(PersistEvent @event, LambdaExpression where, LambdaExpression set, CancellationToken cancellationToken);
        /// <summary>Asynchronously deletes every model matching the condition, without loading them.</summary>
        /// <param name="event">The persist event.</param>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task containing how many models were deleted.</returns>
        protected abstract Task<int> DeleteModelsWhereAsync(PersistEvent @event, LambdaExpression where, CancellationToken cancellationToken);
    }
}