// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.Collections;
using Zerra.Reflection;
using Zerra.Repository.Linq;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Abstract base provider for transactional store operations, supporting query, persist, and relation linking for <typeparamref name="TModel"/>.
    /// </summary>
    /// <typeparam name="TModel">The model type managed by this provider.</typeparam>
    public abstract partial class RootTransactStoreProvider<TModel> : BaseStore, ITransactStoreProvider<TModel>, IProviderRelation<TModel>
        where TModel : class, new()
    {
        /// <summary>The <see cref="Type"/> of <typeparamref name="TModel"/>.</summary>
        protected static readonly Type modelType = typeof(TModel);
        /// <summary>The analyzed model metadata for <typeparamref name="TModel"/>.</summary>
        protected static readonly ModelDetail modelTypeDetail = ModelAnalyzer.GetModel(typeof(TModel));
        private static readonly Type objectType = typeof(object);
        private static readonly Type listType = typeof(List<>);
        private static readonly Type listObjectType = typeof(List<object>);
        private static readonly MethodInfo containsMethod = typeof(List<object>).GetMethods().First(m => m.Name == nameof(List<>.Contains));

        /// <summary>Gets a value indicating whether event-based relation linking is enabled. Defaults to <see langword="true"/>.</summary>
        protected virtual bool EventLinking { get { return true; } }
        /// <summary>Gets a value indicating whether query-based relation linking is enabled. Defaults to <see langword="true"/>.</summary>
        protected virtual bool QueryLinking { get { return true; } }
        /// <summary>Gets a value indicating whether persist-based relation linking is enabled. Defaults to <see langword="true"/>.</summary>
        protected virtual bool PersistLinking { get { return true; } }

        /// <summary>The analyzed model metadata for <typeparamref name="TModel"/>.</summary>
        protected ModelDetail ModelTypeDetail => modelTypeDetail;

        /// <summary>Gets the <see cref="Type"/> of the model managed by this provider.</summary>
        protected Type ModelType => modelType;

        private readonly ConcurrentFactoryDictionary<Type, ITransactStoreProvider?> relatedProviders = new();
        private ITransactStoreProvider? GetRelatedProvider(Type type)
        {
            var relatedProvider = relatedProviders.GetOrAdd(type, () =>
            {
                var propertyType = type;
                var propertyTypeDetails = TypeAnalyzer.GetTypeDetail(propertyType);
                if (propertyTypeDetails.HasIEnumerableGeneric)
                    propertyType = propertyTypeDetails.InnerType!;

                if (!propertyType.IsClass)
                    return null;

                if (Repo.TryGetProvider(propertyType, out var provider))
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

            foreach (var property in ModelTypeDetail.RelatedProperties)
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

        /// <summary>Invoked during a query to propagate query events to related providers via event linking.</summary>
        /// <param name="graph">The graph specifying which members are being queried.</param>
        public void OnQuery(Graph? graph)
        {
            if (!EventLinking || graph is null)
                return;

            foreach (var property in ModelTypeDetail.RelatedProperties)
            {
                if (graph.HasMemberExplicitly(property.Name))
                {
                    var relatedProvider = GetRelatedProvider(property.Type);

                    if (relatedProvider is not null)
                    {
                        if (relatedProvider is IProviderRelation relatedProviderGeneric)
                        {
                            var relatedGraph = graph.GetChildGraph(property.Name);
                            relatedProviderGeneric.OnQueryIncludingBase(relatedGraph);
                            if (relatedGraph is not null)
                                graph.AddOrReplaceChildGraph(property.Name, relatedGraph);
                        }
                    }
                }
            }
        }
        /// <summary>Invoked during a query including base type handling, delegating to <see cref="OnQueryWithRelations"/>.</summary>
        /// <param name="graph">The graph specifying which members are being queried.</param>
        public void OnQueryIncludingBase(Graph? graph)
        {
            OnQueryWithRelations(graph);
        }

        /// <summary>Prepares the query graph by adding required identity and foreign-key members for all related properties.</summary>
        /// <param name="graph">The graph to augment with relation members.</param>
        public void OnQueryWithRelations(Graph? graph)
        {
            if (EventLinking)
            {
                OnQuery(graph);
            }

            if (QueryLinking)
            {
                foreach (var modelPropertyInfo in ModelTypeDetail.RelatedProperties)
                {
                    if (graph is not null && graph.HasMemberExplicitly(modelPropertyInfo.Name))
                    {
                        if (!modelPropertyInfo.IsEnumerable)
                        {
                            //related single
                            if (modelPropertyInfo.ForeignIdentity is null)
                                throw new Exception($"Model {modelPropertyInfo.Type.Name} missing Foreign Identity");
                            graph.AddMembers(modelPropertyInfo.ForeignIdentity);
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
        public IReadOnlyCollection<TModel> OnGetWithRelations(IReadOnlyCollection<TModel> models, Graph? graph)
        {
            var returnModels = models;
            if (EventLinking)
            {
                returnModels = OnGet(models, graph);
            }

            if (QueryLinking)
            {
                //Get related
                //var tasks = new HashSet<Task>();
                foreach (var modelPropertyInfo in ModelTypeDetail.RelatedProperties)
                {
                    if (graph is not null && graph.HasMemberExplicitly(modelPropertyInfo.Name))
                    {
                        //var task = Task.Run(() =>
                        //{
                        if (!modelPropertyInfo.IsEnumerable)
                        {
                            //related single
                            var relatedType = modelPropertyInfo.InnerType;
                            var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            if (modelPropertyInfo.ForeignIdentity is null || !ModelTypeDetail.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var foreignIdentityPropertyInfo))
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelTypeDetail.Name}");

                            if (relatedModelInfo.IdentityProperties.Count == 0)
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelTypeDetail.Name}");
                            var relatedIdentityPropertyInfo = relatedModelInfo.IdentityProperties[0];

                            var foreignIdentities = new List<object>();
                            foreach (var model in returnModels)
                            {
                                var foreignIdentity = ModelAnalyzer.GetForeignIdentity(modelType, modelPropertyInfo.ForeignIdentity, model);
                                if (foreignIdentity is not null)
                                    foreignIdentities.Add(foreignIdentity);
                            }

                            var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                            var condition = Expression.Call(Expression.Constant(foreignIdentities, listObjectType), containsMethod, Expression.Convert(Expression.MakeMemberAccess(queryExpressionParameter, relatedIdentityPropertyInfo.MemberInfo), objectType));
                            var queryExpression = Expression.Lambda(relatedModelInfo.LambdaDelegateType, condition, queryExpressionParameter);

                            if (relatedGraph is not null)
                            {
                                var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                                var allNames = relatedIdentityPropertyNames.Append(foreignIdentityPropertyInfo.Name);
                                relatedGraph.AddMembers(relatedIdentityPropertyNames);
                            }

                            var query = new Query(QueryOperation.Many, relatedType, queryExpression, null, null, null, relatedGraph);

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
                                var foreignIdentity = ModelAnalyzer.GetForeignIdentity(modelType, modelPropertyInfo.ForeignIdentity, model);
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
                            var relatedType = modelPropertyInfo.InnerType;
                            var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            if (modelPropertyInfo.ForeignIdentity is null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelTypeDetail.Name}");

                            relatedGraph?.AddMembers(modelPropertyInfo.ForeignIdentity);

                            var foreignIdentities = new List<object>();
                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(modelType, model);
                                foreignIdentities.Add(identity);
                            }

                            var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                            var condition = Expression.Call(Expression.Constant(foreignIdentities, listObjectType), containsMethod, Expression.Convert(Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo), objectType));
                            var queryExpression = Expression.Lambda(relatedModelInfo.LambdaDelegateType, condition, queryExpressionParameter);

                            if (relatedGraph is not null)
                            {
                                var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                                var allNames = relatedIdentityPropertyNames.Append(modelPropertyInfo.ForeignIdentity);
                                relatedGraph.AddMembers(allNames);
                            }

                            var query = new Query(QueryOperation.Many, relatedType, queryExpression, null, null, null, relatedGraph);

                            var relatedModels = (IEnumerable)Repo.Query(query)!;

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (var relatedModel in relatedModels)
                            {
                                var relatedIdentity = ModelAnalyzer.GetForeignIdentity(relatedType, modelPropertyInfo.ForeignIdentity, relatedModel);
                                if (relatedIdentity is not null)
                                    relatedModelIdentities.Add(relatedModel, relatedIdentity);
                            }

                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(modelType, model);
                                var modelTypeDetail = modelPropertyInfo.Type.GetTypeDetail();
                                var listTypeDetails = relatedModels.GetType().GetTypeDetail();
                                if (listTypeDetails.Type.IsArray)
                                {
                                    var relatedForModel = new List<object>();
                                    foreach (var relatedModel in relatedModelIdentities)
                                    {
                                        if (ModelAnalyzer.CompareIdentities(identity, relatedModel.Value))
                                            relatedForModel.Add(relatedModel.Key);
                                    }
                                    var array = (Array)listTypeDetails.Constructors[0].CreatorBoxed([relatedForModel.Count]);
                                    for (var i = 0; i < relatedForModel.Count; i++)
                                        array.SetValue(relatedForModel[i], i);
                                }
                                else
                                {
                                    var relatedForModel = (IList)listTypeDetails.CreatorBoxed!();
                                    foreach (var relatedModel in relatedModelIdentities)
                                    {
                                        if (ModelAnalyzer.CompareIdentities(identity, relatedModel.Value))
                                            _ = relatedForModel.Add(relatedModel.Key);
                                    }
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
        /// <returns>A task representing the asynchronous operation, containing the models with related properties populated.</returns>
        public async Task<IReadOnlyCollection<TModel>> OnGetWithRelationsAsync(IReadOnlyCollection<TModel> models, Graph? graph)
        {
            var returnModels = models;
            if (EventLinking && graph is not null)
            {
                returnModels = await OnGetAsync(models, graph);
            }

            if (QueryLinking && graph is not null)
            {
                //Get related
                //var tasks = new List<Task>();
                foreach (var modelPropertyInfo in ModelTypeDetail.RelatedProperties)
                {
                    if (graph.HasMemberExplicitly(modelPropertyInfo.Name))
                    {
                        //var task = Task.Run(async () =>
                        //{
                        if (!modelPropertyInfo.IsEnumerable)
                        {
                            //related single
                            var relatedType = modelPropertyInfo.InnerType;
                            var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            if (modelPropertyInfo.ForeignIdentity is null || !ModelTypeDetail.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var foreignIdentityPropertyInfo))
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelTypeDetail.Name}");

                            if (relatedModelInfo.IdentityProperties.Count == 0)
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelTypeDetail.Name}");
                            var relatedIdentityPropertyInfo = relatedModelInfo.IdentityProperties[0];

                            var foreignIdentities = new List<object>();
                            foreach (var model in returnModels)
                            {
                                var foreignIdentity = ModelAnalyzer.GetForeignIdentity(modelType, modelPropertyInfo.ForeignIdentity, model);
                                if (foreignIdentity is not null)
                                    foreignIdentities.Add(foreignIdentity);
                            }

                            var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                            var condition = Expression.Call(Expression.Constant(foreignIdentities, listObjectType), containsMethod, Expression.Convert(Expression.MakeMemberAccess(queryExpressionParameter, relatedIdentityPropertyInfo.MemberInfo), objectType));
                            var queryExpression = Expression.Lambda(relatedModelInfo.LambdaDelegateType, condition, queryExpressionParameter);

                            if (relatedGraph is not null)
                            {
                                var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                                var allNames = relatedIdentityPropertyNames.Append(foreignIdentityPropertyInfo.ForeignIdentity);
                                relatedGraph.AddMembers(relatedIdentityPropertyNames);
                            }

                            var query = new Query(QueryOperation.Many, relatedType, queryExpression, null, null, null, relatedGraph);

                            var relatedModels = (IEnumerable)(await Repo.QueryAsync(query))!;

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (var relatedModel in relatedModels)
                            {
                                var relatedIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedModel);
                                if (relatedIdentity is not null)
                                    relatedModelIdentities.Add(relatedModel, relatedIdentity);
                            }

                            foreach (var model in returnModels)
                            {
                                var foreignIdentity = ModelAnalyzer.GetForeignIdentity(modelType, modelPropertyInfo.ForeignIdentity, model);
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
                            var relatedType = modelPropertyInfo.InnerType;
                            var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            if (modelPropertyInfo.ForeignIdentity is null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelTypeDetail.Name}");

                            relatedGraph?.AddMembers(modelPropertyInfo.ForeignIdentity);

                            var foreignIdentities = new List<object>();
                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(modelType, model);
                                foreignIdentities.Add(identity);
                            }

                            var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                            var condition = Expression.Call(Expression.Constant(foreignIdentities, listObjectType), containsMethod, Expression.Convert(Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo), objectType));
                            var queryExpression = Expression.Lambda(relatedModelInfo.LambdaDelegateType, condition, queryExpressionParameter);

                            if (relatedGraph is not null)
                            {
                                var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                                var allNames = relatedIdentityPropertyNames.Append(modelPropertyInfo.ForeignIdentity);
                                relatedGraph.AddMembers(allNames);
                            }

                            var query = new Query(QueryOperation.Many, relatedType, queryExpression, null, null, null, relatedGraph);

                            var relatedModels = (IEnumerable)(await Repo.QueryAsync(query))!;

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (var relatedModel in relatedModels)
                            {
                                var relatedIdentity = ModelAnalyzer.GetForeignIdentity(relatedType, modelPropertyInfo.ForeignIdentity, relatedModel);
                                if (relatedIdentity is not null)
                                    relatedModelIdentities.Add(relatedModel, relatedIdentity);
                            }

                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(modelType, model);
                                var modelTypeDetail = modelPropertyInfo.Type.GetTypeDetail();
                                var listTypeDetails = relatedModels.GetType().GetTypeDetail();
                                if (listTypeDetails.Type.IsArray)
                                {
                                    var relatedForModel = new List<object>();
                                    foreach (var relatedModel in relatedModelIdentities)
                                    {
                                        if (ModelAnalyzer.CompareIdentities(identity, relatedModel.Value))
                                            relatedForModel.Add(relatedModel.Key);
                                    }
                                    var array = (Array)listTypeDetails.Constructors[0].CreatorBoxed([relatedForModel.Count]);
                                    for (var i = 0; i < relatedForModel.Count; i++)
                                        array.SetValue(relatedForModel[i], i);
                                }
                                else
                                {
                                    var relatedForModel = (IList)listTypeDetails.CreatorBoxed!();
                                    foreach (var relatedModel in relatedModelIdentities)
                                    {
                                        if (ModelAnalyzer.CompareIdentities(identity, relatedModel.Value))
                                            _ = relatedForModel.Add(relatedModel.Key);
                                    }
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

        /// <summary>Invokes event-linked related providers to post-process retrieved models.</summary>
        /// <param name="models">The models to post-process.</param>
        /// <param name="graph">The graph specifying which members to process.</param>
        /// <returns>The post-processed models.</returns>
        public IReadOnlyCollection<TModel> OnGet(IReadOnlyCollection<TModel> models, Graph? graph)
        {
            if (!EventLinking || graph is null)
                return models;

            foreach (var property in ModelTypeDetail.RelatedProperties)
            {
                if (graph.HasMemberExplicitly(property.Name))
                {
                    var relatedProvider = GetRelatedProvider(property.Type);

                    if (relatedProvider is not null && relatedProvider is IProviderRelation relatedProviderGeneric)
                    {
                        var relatedGraph = graph.GetChildGraph(property.Name);

                        if (property.Type.GetTypeDetail().IsIEnumerableGeneric)
                        {
                            foreach (var model in models)
                            {
                                var related = (IEnumerable)property.GetterBoxed(model)!;
                                if (related is not null)
                                {
                                    var returnModels = relatedProviderGeneric.OnGetIncludingBase(related, relatedGraph);
                                    property.SetterBoxed(model, returnModels);
                                }
                            }
                        }
                        else
                        {
                            var relatedModels = new List<object>();
                            var relatedModelsDictionary = new Dictionary<object, List<TModel>>();
                            foreach (var model in models)
                            {
                                var related = property.GetterBoxed(model);
                                if (related is not null)
                                {
                                    relatedModels.Add(related);
                                    if (!relatedModelsDictionary.TryGetValue(model, out var relatedModelList))
                                    {
                                        relatedModelList = new List<TModel>();
                                        relatedModelsDictionary.Add(related, relatedModelList);
                                    }
                                    relatedModelList.Add(model);
                                }
                            }

                            if (relatedModels.Count > 0)
                            {
                                var returnModels = relatedProviderGeneric.OnGetIncludingBase(relatedModels, relatedGraph);
                                foreach (var returnModel in returnModels)
                                {
                                    _ = relatedModelsDictionary.Remove(returnModel);
                                }

                                foreach (var relatedModelSet in relatedModelsDictionary)
                                {
                                    foreach (var model in relatedModelSet.Value)
                                    {
                                        property.SetterBoxed(model, null);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return models;
        }
        /// <summary>Asynchronously invokes event-linked related providers to post-process retrieved models.</summary>
        /// <param name="models">The models to post-process.</param>
        /// <param name="graph">The graph specifying which members to process.</param>
        /// <returns>A task representing the asynchronous operation, containing the post-processed models.</returns>
        public async Task<IReadOnlyCollection<TModel>> OnGetAsync(IReadOnlyCollection<TModel> models, Graph? graph)
        {
            if (!EventLinking || graph is null)
                return models;

            foreach (var property in ModelTypeDetail.RelatedProperties)
            {
                if (graph.HasMemberExplicitly(property.Name))
                {
                    var relatedProvider = GetRelatedProvider(property.Type);

                    if (relatedProvider is not null && relatedProvider is IProviderRelation relatedProviderGeneric)
                    {
                        var relatedGraph = graph.GetChildGraph(property.Name);

                        if (property.Type.GetTypeDetail().IsIEnumerableGeneric)
                        {
                            foreach (var model in models)
                            {
                                var related = (IEnumerable)property.GetterBoxed(model)!;
                                if (related is not null)
                                {
                                    var returnModels = relatedProviderGeneric.OnGetIncludingBaseAsync(related, relatedGraph);
                                    property.SetterBoxed(model, returnModels);
                                }
                            }
                        }
                        else
                        {
                            var relatedModels = new List<object>();
                            var relatedModelsDictionary = new Dictionary<object, List<TModel>>();
                            foreach (var model in models)
                            {
                                var related = property.GetterBoxed(model);
                                if (related is not null)
                                {
                                    relatedModels.Add(related);
                                    if (!relatedModelsDictionary.TryGetValue(model, out var relatedModelList))
                                    {
                                        relatedModelList = new List<TModel>();
                                        relatedModelsDictionary.Add(related, relatedModelList);
                                    }
                                    relatedModelList.Add(model);
                                }
                            }

                            if (relatedModels.Count > 0)
                            {
                                var returnModels = await relatedProviderGeneric.OnGetIncludingBaseAsync(relatedModels, relatedGraph);
                                foreach (var returnModel in returnModels)
                                {
                                    _ = relatedModelsDictionary.Remove(returnModel);
                                }

                                foreach (var relatedModelSet in relatedModelsDictionary)
                                {
                                    foreach (var model in relatedModelSet.Value)
                                    {
                                        property.SetterBoxed(model, null);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return models;
        }

        /// <summary>Populates related properties on models, including base type handling.</summary>
        /// <param name="models">The models to process.</param>
        /// <param name="graph">The graph specifying which members to populate.</param>
        /// <returns>The models with related properties populated.</returns>
        public IEnumerable OnGetIncludingBase(IEnumerable models, Graph? graph)
        {
            return OnGetIncludingBase((IReadOnlyCollection<TModel>)models, (Graph<TModel>?)graph);
        }
        /// <summary>Populates related properties on models, including base type handling.</summary>
        /// <param name="models">The models to process.</param>
        /// <param name="graph">The graph specifying which members to populate.</param>
        /// <returns>The models with related properties populated.</returns>
        public IReadOnlyCollection<TModel> OnGetIncludingBase(IReadOnlyCollection<TModel> models, Graph? graph)
        {
            return OnGetWithRelations(models, graph);
        }

        /// <summary>Asynchronously populates related properties on models, including base type handling.</summary>
        /// <param name="models">The models to process.</param>
        /// <param name="graph">The graph specifying which members to populate.</param>
        /// <returns>A task representing the asynchronous operation, containing the models with related properties populated.</returns>
        public async Task<IEnumerable> OnGetIncludingBaseAsync(IEnumerable models, Graph? graph)
        {
            return await OnGetIncludingBaseAsync((IReadOnlyCollection<TModel>)(object)models, (Graph<TModel>?)graph);
        }
        /// <summary>Asynchronously populates related properties on models, including base type handling.</summary>
        /// <param name="models">The models to process.</param>
        /// <param name="graph">The graph specifying which members to populate.</param>
        /// <returns>A task representing the asynchronous operation, containing the models with related properties populated.</returns>
        public Task<IReadOnlyCollection<TModel>> OnGetIncludingBaseAsync(IReadOnlyCollection<TModel> models, Graph? graph)
        {
            return OnGetWithRelationsAsync(models, graph);
        }

        /// <summary>Executes a synchronous query operation and returns the result.</summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>The query result, or <see langword="null"/> if no result is found.</returns>
        public object? Query(Query query)
        {
            return query.Operation switch
            {
                QueryOperation.Many => QueryMany(query),
                QueryOperation.First => QueryFirst(query),
                QueryOperation.Single => QuerySingle(query),
                QueryOperation.Count => QueryCount(query),
                QueryOperation.Any => QueryAny(query),
                QueryOperation.EventMany => QueryEventMany(query),
                QueryOperation.EventFirst => QueryEventFirst(query),
                QueryOperation.EventSingle => QueryEventSingle(query),
                QueryOperation.EventCount => QueryEventCount(query),
                QueryOperation.EventAny => QueryEventAny(query),
                _ => throw new NotImplementedException(),
            };
        }
        /// <summary>Executes an asynchronous query operation and returns the result.</summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A task representing the asynchronous operation, containing the query result or <see langword="null"/>.</returns>
        public Task<object?> QueryAsync(Query query)
        {
            return query.Operation switch
            {
                QueryOperation.Many => QueryManyAsync(query),
                QueryOperation.First => QueryFirstAsync(query),
                QueryOperation.Single => QuerySingleAsync(query),
                QueryOperation.Count => QueryCountAsync(query),
                QueryOperation.Any => QueryAnyAsync(query),
                QueryOperation.EventMany => QueryEventManyAsync(query),
                QueryOperation.EventFirst => QueryEventFirstAsync(query),
                QueryOperation.EventSingle => QueryEventSingleAsync(query),
                QueryOperation.EventCount => QueryEventCountAsync(query),
                QueryOperation.EventAny => QueryEventAnyAsync(query),
                _ => throw new NotImplementedException(),
            };
        }

        private object QueryMany(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var models = Many(query);

            IReadOnlyCollection<TModel> returnModels;
            if (models.Count > 0)
            {
                returnModels = OnGetWithRelations(models, query.Graph);
            }
            else
            {
                returnModels = Array.Empty<TModel>();
            }

            return returnModels;
        }
        private object? QueryFirst(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = First(query);

            var returnModel = model;
            if (model is not null)
            {
                returnModel = OnGetWithRelations([model], query.Graph).FirstOrDefault();
            }

            return returnModel;
        }
        private object? QuerySingle(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = Single(query);

            var returnModel = model;
            if (model is not null)
            {
                returnModel = (OnGetWithRelations([model], query.Graph)).FirstOrDefault();
            }

            return returnModel;
        }
        private object QueryCount(Query query)
        {
            return Count(query);
        }
        private object QueryAny(Query query)
        {
            return Any(query);
        }
        private object QueryEventMany(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var models = EventMany(query);

            IReadOnlyCollection<TModel> returnModels;
            if (models.Count > 0)
            {
                returnModels = OnGetWithRelations(models.Select(x => x.Model).ToArray(), query.Graph);
            }
            else
            {
                returnModels = Array.Empty<TModel>();
            }

            return models.Where(x => returnModels.Contains(x.Model)).ToArray();
        }
        private object? QueryEventFirst(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = EventFirst(query);

            if (model is not null)
            {
                var returnModel = OnGetWithRelations([model.Model], query.Graph).FirstOrDefault();
                if (returnModel is null)
                    model = null;
            }

            return model;
        }
        private object? QueryEventSingle(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = EventSingle(query);

            if (model is not null)
            {
                var returnModel = OnGetWithRelations([model.Model], query.Graph).FirstOrDefault();
                if (returnModel is null)
                    model = null;
            }

            return model;
        }
        private object QueryEventCount(Query query)
        {
            return EventCount(query);
        }
        private object QueryEventAny(Query query)
        {
            return EventAny(query);
        }

        private async Task<object?> QueryManyAsync(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var models = await ManyAsync(query);

            IReadOnlyCollection<TModel> returnModels;
            if (models.Count > 0)
            {
                returnModels = await OnGetWithRelationsAsync(models, query.Graph);
            }
            else
            {
                returnModels = Array.Empty<TModel>();
            }

            return returnModels;
        }
        private async Task<object?> QueryFirstAsync(Query query)
        {
            Graph? graph = null;
            if (query.Graph is not null)
            {
                graph = new Graph(query.Graph);
                OnQueryWithRelations(graph);

                query = new Query(query);
            }

            var model = await FirstAsync(query);

            var returnModel = model;
            if (model is not null)
            {
                returnModel = (await OnGetWithRelationsAsync([model], graph)).FirstOrDefault();
            }

            return returnModel;
        }
        private async Task<object?> QuerySingleAsync(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = await SingleAsync(query);

            var returnModel = model;
            if (model is not null)
            {
                returnModel = (await OnGetWithRelationsAsync([model], query.Graph)).FirstOrDefault();
            }

            return returnModel;
        }
        private async Task<object?> QueryCountAsync(Query query)
        {
            return await CountAsync(query);
        }
        private async Task<object?> QueryAnyAsync(Query query)
        {
            return await AnyAsync(query);
        }
        private async Task<object?> QueryEventManyAsync(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var models = await EventManyAsync(query);

            IReadOnlyCollection<TModel> returnModels;
            if (models.Count > 0)
            {
                returnModels = OnGetWithRelations(models.Select(x => x.Model).ToArray(), query.Graph);
            }
            else
            {
                returnModels = Array.Empty<TModel>();
            }

            return models.Where(x => returnModels.Contains(x.Model)).ToArray();
        }
        private async Task<object?> QueryEventFirstAsync(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = await EventFirstAsync(query);

            if (model is not null)
            {
                var returnModel = OnGetWithRelations([model.Model], query.Graph).FirstOrDefault();
                if (returnModel is null)
                    model = null;
            }

            return model;
        }
        private async Task<object?> QueryEventSingleAsync(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = await EventSingleAsync(query);

            if (model is not null)
            {
                var returnModel = OnGetWithRelations([model.Model], query.Graph).FirstOrDefault();
                if (returnModel is null)
                    model = null;
            }

            return model;
        }
        private async Task<object?> QueryEventCountAsync(Query query)
        {
            return await EventCountAsync(query);
        }
        private async Task<object?> QueryEventAnyAsync(Query query)
        {
            return await EventAnyAsync(query);
        }

        /// <summary>Queries and returns multiple models matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A collection of matching models.</returns>
        protected abstract IReadOnlyCollection<TModel> Many(Query query);
        /// <summary>Queries and returns the first model matching the given query, or <see langword="null"/>.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>The first matching model, or <see langword="null"/>.</returns>
        protected abstract TModel? First(Query query);
        /// <summary>Queries and returns the single model matching the given query, or <see langword="null"/>.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>The single matching model, or <see langword="null"/>.</returns>
        protected abstract TModel? Single(Query query);
        /// <summary>Returns the count of models matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>The count of matching models.</returns>
        protected abstract long Count(Query query);
        /// <summary>Returns whether any model exists matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns><see langword="true"/> if any matching model exists; otherwise <see langword="false"/>.</returns>
        protected abstract bool Any(Query query);
        /// <summary>Queries and returns multiple event models matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A collection of matching event models.</returns>
        protected abstract IReadOnlyCollection<EventModel<TModel>> EventMany(Query query);
        /// <summary>Queries and returns the first event model matching the given query, or <see langword="null"/>.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>The first matching event model, or <see langword="null"/>.</returns>
        protected abstract EventModel<TModel>? EventFirst(Query query);
        /// <summary>Queries and returns the single event model matching the given query, or <see langword="null"/>.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>The single matching event model, or <see langword="null"/>.</returns>
        protected abstract EventModel<TModel>? EventSingle(Query query);
        /// <summary>Returns the count of event models matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>The count of matching event models.</returns>
        protected abstract long EventCount(Query query);
        /// <summary>Returns whether any event model exists matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns><see langword="true"/> if any matching event model exists; otherwise <see langword="false"/>.</returns>
        protected abstract bool EventAny(Query query);

        /// <summary>Asynchronously queries and returns multiple models matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A task containing a collection of matching models.</returns>
        protected abstract Task<IReadOnlyCollection<TModel>> ManyAsync(Query query);
        /// <summary>Asynchronously queries and returns the first model matching the given query, or <see langword="null"/>.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A task containing the first matching model, or <see langword="null"/>.</returns>
        protected abstract Task<TModel?> FirstAsync(Query query);
        /// <summary>Asynchronously queries and returns the single model matching the given query, or <see langword="null"/>.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A task containing the single matching model, or <see langword="null"/>.</returns>
        protected abstract Task<TModel?> SingleAsync(Query query);
        /// <summary>Asynchronously returns the count of models matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A task containing the count of matching models.</returns>
        protected abstract Task<long> CountAsync(Query query);
        /// <summary>Asynchronously returns whether any model exists matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A task containing <see langword="true"/> if any matching model exists; otherwise <see langword="false"/>.</returns>
        protected abstract Task<bool> AnyAsync(Query query);
        /// <summary>Asynchronously queries and returns multiple event models matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A task containing a collection of matching event models.</returns>
        protected abstract Task<IReadOnlyCollection<EventModel<TModel>>> EventManyAsync(Query query);
        /// <summary>Asynchronously queries and returns the first event model matching the given query, or <see langword="null"/>.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A task containing the first matching event model, or <see langword="null"/>.</returns>
        protected abstract Task<EventModel<TModel>?> EventFirstAsync(Query query);
        /// <summary>Asynchronously queries and returns the single event model matching the given query, or <see langword="null"/>.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A task containing the single matching event model, or <see langword="null"/>.</returns>
        protected abstract Task<EventModel<TModel>?> EventSingleAsync(Query query);
        /// <summary>Asynchronously returns the count of event models matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A task containing the count of matching event models.</returns>
        protected abstract Task<long> EventCountAsync(Query query);
        /// <summary>Asynchronously returns whether any event model exists matching the given query.</summary>
        /// <param name="query">The query parameters.</param>
        /// <returns>A task containing <see langword="true"/> if any matching event model exists; otherwise <see langword="false"/>.</returns>
        protected abstract Task<bool> EventAnyAsync(Query query);

        /// <summary>The <see cref="Type"/> of <see cref="PersistEvent"/>, used for persist event metadata.</summary>
        protected static readonly Type EventInfoType = typeof(PersistEvent);

        /// <summary>Executes a synchronous persist operation (create, update, or delete).</summary>
        /// <param name="persist">The persist operation to execute.</param>
        public void Persist(Persist persist)
        {
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
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task PersistAsync(Persist persist)
        {
            return persist.Operation switch
            {
                PersistOperation.Create => CreateAsync(persist),
                PersistOperation.Update => UpdateAsync(persist),
                PersistOperation.Delete => DeleteAsync(persist),
                _ => throw new NotImplementedException(),
            };
        }

        private void Create(Persist persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var graph = persist.Graph is null ? null : new Graph<TModel>(persist.Graph);

            foreach (var model in persist.Models)
            {
                if (PersistLinking && graph is not null && !graph.IsEmpty)
                {
                    PersistSingleRelations(persist.Event, model, graph, true);
                }

                PersistModel(persist.Event, model, graph, true);

                if (PersistLinking && graph is not null && !graph.IsEmpty)
                {
                    PersistManyRelations(persist.Event, model, graph, true);
                }
            }
        }
        private void Update(Persist persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var graph = persist.Graph is null ? null : new Graph(persist.Graph);

            foreach (var model in persist.Models)
            {
                if (!PersistLinking && graph is not null && !graph.IsEmpty)
                {
                    PersistSingleRelations(persist.Event, model, graph, false);
                }

                PersistModel(persist.Event, model, graph, false);

                if (PersistLinking && graph is not null && !graph.IsEmpty)
                {
                    PersistManyRelations(persist.Event, model, graph, false);
                }
            }
        }
        private void Delete(Persist persist)
        {
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

            if (PersistLinking && graph is not null && !graph.IsEmpty)
            {
                foreach (var id in ids)
                {
                    DeleteManyRelations(persist.Event, id, graph);
                }
            }

            DeleteModel(persist.Event, ids);
        }

        private async Task CreateAsync(Persist persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var graph = persist.Graph is null ? null : new Graph<TModel>(persist.Graph);

            foreach (var model in persist.Models)
            {
                if (PersistLinking && graph is not null && !graph.IsEmpty)
                {
                    await PersistSingleRelationsAsync(persist.Event, model, graph, true);
                }

                await PersistModelAsync(persist.Event, model, graph, true);

                if (PersistLinking && graph is not null && !graph.IsEmpty)
                {
                    await PersistManyRelationsAsync(persist.Event, model, graph, true);
                }
            }
        }
        private async Task UpdateAsync(Persist persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var graph = persist.Graph is null ? null : new Graph(persist.Graph);

            foreach (var model in persist.Models)
            {
                if (PersistLinking && graph is not null && !graph.IsEmpty)
                {
                    await PersistSingleRelationsAsync(persist.Event, model, graph, false);
                }

                await PersistModelAsync(persist.Event, model, graph, false);

                if (PersistLinking && graph is not null && !graph.IsEmpty)
                {
                    await PersistManyRelationsAsync(persist.Event, model, graph, false);
                }
            }
        }
        private async Task DeleteAsync(Persist persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

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

            if (PersistLinking && graph is not null && !graph.IsEmpty)
            {
                foreach (var id in ids)
                {
                    await DeleteManyRelationsAsync(persist.Event, id, graph);
                }
            }

            await DeleteModelAsync(persist.Event, ids);
        }

        /// <summary>Persists non-enumerable (single) related models and sets their foreign identity on the parent model.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="model">The parent model whose single relations should be persisted.</param>
        /// <param name="graph">The graph specifying which relations to persist.</param>
        /// <param name="create"><see langword="true"/> to create related models; <see langword="false"/> to update.</param>
        protected void PersistSingleRelations(PersistEvent @event, object model, Graph graph, bool create)
        {
            //var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelTypeDetail.RelatedNonEnumerableProperties)
            {
                if (graph.HasMemberExplicitly(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(() =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;

                    if (modelPropertyInfo.ForeignIdentity is null)
                        throw new Exception($"Model {modelPropertyInfo.Type.Name} missing Foreign Identity");
                    graph.AddMembers(modelPropertyInfo.ForeignIdentity);

                    var relatedModel = modelPropertyInfo.GetterBoxed(model);

                    if (relatedModel is not null)
                    {
                        var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                        var persist = new Persist(create ? PersistOperation.Create : PersistOperation.Update, @event, relatedType, [relatedModel], null, relatedGraph);
                        Repo.Persist(persist);

                        var relatedIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedModel);
                        ModelAnalyzer.SetForeignIdentity(modelPropertyInfo.Type, modelPropertyInfo.ForeignIdentity, model, relatedIdentity);
                    }
                    else
                    {
                        ModelAnalyzer.SetForeignIdentity(modelPropertyInfo.Type, modelPropertyInfo.ForeignIdentity, model, null);
                    }
                    //});
                    //tasks.Add(task);
                }
            }

            //if (tasks.Count == 0)
            //    return;

            //Task.WaitAll(tasks.ToArray());
        }
        /// <summary>Persists enumerable (many) related models, determining which should be created, updated, or deleted.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="model">The parent model whose many relations should be persisted.</param>
        /// <param name="graph">The graph specifying which relations to persist.</param>
        /// <param name="create"><see langword="true"/> if the parent model is being created; <see langword="false"/> if updated.</param>
        protected void PersistManyRelations(PersistEvent @event, object model, Graph graph, bool create)
        {
            //var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelTypeDetail.RelatedEnumerableProperties)
            {
                if (graph.HasMemberExplicitly(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(() =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;

                    var relatedModels = (IEnumerable)modelPropertyInfo.GetterBoxed(model)!;

                    var identity = ModelAnalyzer.GetIdentity(relatedType, model);

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    if (modelPropertyInfo.ForeignIdentity is null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                        throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelTypeDetail.Name}");

                    var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                    var relatedModelsExisting = new List<object>();
                    var relatedModelsDelete = new List<object>();
                    var relatedModelsCreate = new List<object>();
                    var relatedModelsUpdate = new List<object>();

                    if (!create)
                    {
                        var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                        var queryExpression = Expression.Lambda(relatedModelInfo.LambdaDelegateType, Expression.Equal(Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo), Expression.Constant(identity, relatedForeignIdentityPropertyInfo.Type)), queryExpressionParameter);
                        var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                        var allNames = relatedIdentityPropertyNames.Append(modelPropertyInfo.ForeignIdentity);
                        var queryGraph = new Graph(allNames);

                        var query = new Query(QueryOperation.Many, modelType, queryExpression, null, null, null, queryGraph);

                        var relatedExistings = (IEnumerable)Repo.Query(query)!;

                        foreach (var relatedExisting in relatedExistings)
                            relatedModelsExisting.Add(relatedExisting);
                    }

                    var relatedModelIdentities = new Dictionary<object, object>();

                    if (relatedModels is not null)
                    {
                        foreach (var relatedModel in relatedModels)
                        {
                            ModelAnalyzer.SetForeignIdentity(relatedType, modelPropertyInfo.ForeignIdentity, relatedModel, identity);
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

            foreach (var modelPropertyInfo in ModelTypeDetail.RelatedEnumerableProperties)
            {
                if (graph.HasMemberExplicitly(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(() =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    if (modelPropertyInfo.ForeignIdentity is null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                        throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelTypeDetail.Name}");

                    var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                    var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                    var queryExpression = Expression.Lambda(relatedModelInfo.LambdaDelegateType, Expression.Equal(Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo), Expression.Constant(id, relatedForeignIdentityPropertyInfo.Type)), queryExpressionParameter);
                    var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                    var allNames = relatedIdentityPropertyNames.Append(modelPropertyInfo.ForeignIdentity);
                    var queryGraph = new Graph(allNames);

                    var query = new Query(QueryOperation.Many, modelType, queryExpression, null, null, null, queryGraph);

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
        /// <returns>A task representing the asynchronous operation.</returns>
        protected async Task PersistSingleRelationsAsync(PersistEvent @event, object model, Graph graph, bool create)
        {
            var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelTypeDetail.RelatedNonEnumerableProperties)
            {
                if (graph.HasMemberExplicitly(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(async () =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;

                    if (modelPropertyInfo.ForeignIdentity is null)
                        throw new Exception($"Model {modelPropertyInfo.Type.Name} missing Foreign Identity");
                    graph.AddMembers(modelPropertyInfo.ForeignIdentity);

                    var relatedModel = modelPropertyInfo.GetterBoxed(model);

                    if (relatedModel is not null)
                    {
                        var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                        var persist = new Persist(create ? PersistOperation.Create : PersistOperation.Update, @event, relatedType, [relatedModel], null, relatedGraph);
                        await Repo.PersistAsync(persist);

                        var relatedIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedModel);
                        ModelAnalyzer.SetForeignIdentity(modelPropertyInfo.Type, modelPropertyInfo.ForeignIdentity, model, relatedIdentity);
                    }
                    else
                    {
                        ModelAnalyzer.SetForeignIdentity(modelPropertyInfo.Type, modelPropertyInfo.ForeignIdentity, model, null);
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
        /// <returns>A task representing the asynchronous operation.</returns>
        protected async Task PersistManyRelationsAsync(PersistEvent @event, object model, Graph graph, bool create)
        {
            //var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelTypeDetail.RelatedEnumerableProperties)
            {
                if (graph.HasMemberExplicitly(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(async () =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;

                    var relatedModels = (IEnumerable)modelPropertyInfo.GetterBoxed(model)!;

                    var identity = ModelAnalyzer.GetIdentity(modelPropertyInfo.Type, model);

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    if (modelPropertyInfo.ForeignIdentity is null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                        throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelTypeDetail.Name}");

                    var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                    var relatedModelsExisting = new List<object>();
                    var relatedModelsDelete = new List<object>();
                    var relatedModelsCreate = new List<object>();
                    var relatedModelsUpdate = new List<object>();

                    if (!create)
                    {
                        var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                        var queryExpression = Expression.Lambda(relatedModelInfo.LambdaDelegateType, Expression.Equal(Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo), Expression.Constant(identity, relatedForeignIdentityPropertyInfo.Type)), queryExpressionParameter);
                        var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                        var allNames = relatedIdentityPropertyNames.Append(modelPropertyInfo.ForeignIdentity);
                        var queryGraph = new Graph(allNames);

                        var query = new Query(QueryOperation.Many, modelType, queryExpression, null, null, null, queryGraph);

                        var relatedExistings = (IEnumerable)(await Repo.QueryAsync(query))!;

                        foreach (var relatedExisting in relatedExistings)
                            relatedModelsExisting.Add(relatedExisting);
                    }

                    var relatedModelIdentities = new Dictionary<object, object>();

                    if (relatedModels is not null)
                    {
                        foreach (var relatedModel in relatedModels)
                        {
                            ModelAnalyzer.SetForeignIdentity(relatedType, modelPropertyInfo.ForeignIdentity, relatedModel, identity);
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
                        await Repo.PersistAsync(persist);
                    }
                    if (relatedModelsUpdate.Count > 0)
                    {
                        var persist = new Persist(PersistOperation.Update, @event, relatedType, relatedModelsUpdate.ToArray(), null, relatedGraph);
                        await Repo.PersistAsync(persist);
                    }
                    if (relatedModelsCreate.Count > 0)
                    {
                        var persist = new Persist(PersistOperation.Create, @event, relatedType, relatedModelsCreate.ToArray(), null, relatedGraph);
                        await Repo.PersistAsync(persist);
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
        /// <returns>A task representing the asynchronous operation.</returns>
        protected async Task DeleteManyRelationsAsync(PersistEvent @event, object id, Graph graph)
        {
            //var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelTypeDetail.RelatedEnumerableProperties)
            {
                if (graph.HasMemberExplicitly(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(async () =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    if (modelPropertyInfo.ForeignIdentity is null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                        throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelTypeDetail.Name}");

                    var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                    var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                    var queryExpression = Expression.Lambda(relatedModelInfo.LambdaDelegateType, Expression.Equal(Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo), Expression.Constant(id, relatedForeignIdentityPropertyInfo.Type)), queryExpressionParameter);
                    var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                    var allNames = relatedIdentityPropertyNames.Append(modelPropertyInfo.ForeignIdentity);
                    var queryGraph = new Graph(allNames);

                    var query = new Query(QueryOperation.Many, modelType, queryExpression, null, null, null, queryGraph);

                    var relatedExistings = (IEnumerable)(await Repo.QueryAsync(query))!;

                    if (relatedExistings.Cast<object>().Any())
                    {
                        var persist = new Persist(PersistOperation.Delete, @event, relatedType, relatedExistings.Cast<object>().ToArray(), null, relatedGraph);
                        await Repo.PersistAsync(persist);
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
        /// <summary>Deletes models with the specified identities from the underlying store.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="ids">The identities of the models to delete.</param>
        protected abstract void DeleteModel(PersistEvent @event, object[] ids);

        /// <summary>Asynchronously persists a single model to the underlying store.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="model">The model to persist.</param>
        /// <param name="graph">The graph specifying which members to persist.</param>
        /// <param name="create"><see langword="true"/> to create the model; <see langword="false"/> to update.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected abstract Task PersistModelAsync(PersistEvent @event, object model, Graph? graph, bool create);
        /// <summary>Asynchronously deletes models with the specified identities from the underlying store.</summary>
        /// <param name="event">The persist event context.</param>
        /// <param name="ids">The identities of the models to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected abstract Task DeleteModelAsync(PersistEvent @event, object[] ids);
    }
}