// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.Collections;
using Zerra.Linq;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract partial class RootTransactStoreProvider<TModel> : BaseStore, ITransactStoreProvider<TModel>, IProviderRelation<TModel>
        where TModel : class, new()
    {
        protected static readonly Type modelType = typeof(TModel);
        private static readonly Type objectType = typeof(object);
        private static readonly Type listType = typeof(List<>);
        private static readonly Type listObjectType = typeof(List<object>);
        private static readonly MethodInfo containsMethod = typeof(List<object>).GetMethods().First(m => m.Name == nameof(List<object>.Contains));

        protected virtual bool EventLinking { get { return false; } }
        protected virtual bool QueryLinking { get { return true; } }
        protected virtual bool PersistLinking { get { return true; } }

        protected static readonly ModelDetail ModelDetail = ModelAnalyzer.GetModel(typeof(TModel));

        public Type ModelType => modelType;

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

        public Expression<Func<TModel, bool>>? GetWhereExpression(Graph? graph)
        {
            Expression<Func<TModel, bool>>? whereExpression = null;

            foreach (var property in ModelDetail.RelatedProperties)
            {
                if (graph is not null && graph.HasMember(property.Name))
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
        public LambdaExpression? GetWhereExpressionIncludingBase(Graph? graph)
        {
            return GetWhereExpression(graph);
        }

        public void OnQuery(Graph? graph)
        {
            if (!EventLinking || graph is null)
                return;

            foreach (var property in ModelDetail.RelatedProperties)
            {
                if (graph.HasMember(property.Name))
                {
                    var relatedProvider = GetRelatedProvider(property.Type);

                    if (relatedProvider is not null)
                    {
                        var relatedGraph = graph.GetChildGraph(property.Name);

                        if (relatedProvider is IProviderRelation relatedProviderGeneric)
                        {
                            relatedProviderGeneric.OnQueryIncludingBase(relatedGraph);
                            if (relatedGraph is not null)
                                graph.AddOrReplaceChildGraph(property.Name, relatedGraph);
                        }
                    }
                }
            }
        }
        public void OnQueryIncludingBase(Graph? graph)
        {
            OnQueryWithRelations(graph);
        }

        public void OnQueryWithRelations(Graph? graph)
        {
            if (EventLinking)
            {
                OnQuery(graph);
            }

            if (QueryLinking)
            {
                foreach (var modelPropertyInfo in ModelDetail.RelatedProperties)
                {
                    if (graph is not null && graph.HasMember(modelPropertyInfo.Name))
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
                foreach (var modelPropertyInfo in ModelDetail.RelatedProperties)
                {
                    if (graph is not null && graph.HasMember(modelPropertyInfo.Name))
                    {
                        //var task = Task.Run(() =>
                        //{
                        if (!modelPropertyInfo.IsEnumerable)
                        {
                            //related single
                            var relatedType = modelPropertyInfo.InnerType;
                            var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            if (modelPropertyInfo.ForeignIdentity is null || !ModelDetail.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var foreignIdentityPropertyInfo))
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

                            if (relatedModelInfo.IdentityProperties.Count == 0)
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");
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
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

                            if (relatedGraph is not null)
                            {
                                relatedGraph.AddMembers(modelPropertyInfo.ForeignIdentity);
                            }

                            var foreignIdentities = new List<object>();
                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(modelPropertyInfo.Type, model);
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
                                var identity = ModelAnalyzer.GetIdentity(modelPropertyInfo.Type, model);
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
                                    var relatedForModel = (IList)listTypeDetails.CreatorBoxed();
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
                foreach (var modelPropertyInfo in ModelDetail.RelatedProperties)
                {
                    if (graph.HasMember(modelPropertyInfo.Name))
                    {
                        //var task = Task.Run(async () =>
                        //{
                        if (!modelPropertyInfo.IsEnumerable)
                        {
                            //related single
                            var relatedType = modelPropertyInfo.InnerType;
                            var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            if (modelPropertyInfo.ForeignIdentity is null || !ModelDetail.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var foreignIdentityPropertyInfo))
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

                            if (relatedModelInfo.IdentityProperties.Count == 0)
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");
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
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

                            if (relatedGraph is not null)
                            {
                                relatedGraph.AddMembers(modelPropertyInfo.ForeignIdentity);
                            }

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
                                    var relatedForModel = (IList)listTypeDetails.CreatorBoxed();
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

        public IReadOnlyCollection<TModel> OnGet(IReadOnlyCollection<TModel> models, Graph? graph)
        {
            if (!EventLinking || graph is null)
                return models;

            foreach (var property in ModelDetail.RelatedProperties)
            {
                if (graph.HasMember(property.Name))
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
        public async Task<IReadOnlyCollection<TModel>> OnGetAsync(IReadOnlyCollection<TModel> models, Graph? graph)
        {
            if (!EventLinking || graph is null)
                return models;

            foreach (var property in ModelDetail.RelatedProperties)
            {
                if (graph.HasMember(property.Name))
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

        public IEnumerable OnGetIncludingBase(IEnumerable models, Graph? graph)
        {
            return OnGetIncludingBase((IReadOnlyCollection<TModel>)models, (Graph<TModel>?)graph);
        }
        public IReadOnlyCollection<TModel> OnGetIncludingBase(IReadOnlyCollection<TModel> models, Graph? graph)
        {
            return OnGetWithRelations(models, graph);
        }

        public async Task<IEnumerable> OnGetIncludingBaseAsync(IEnumerable models, Graph? graph)
        {
            return await OnGetIncludingBaseAsync((IReadOnlyCollection<TModel>)(object)models, (Graph<TModel>?)graph);
        }
        public Task<IReadOnlyCollection<TModel>> OnGetIncludingBaseAsync(IReadOnlyCollection<TModel> models, Graph? graph)
        {
            return OnGetWithRelationsAsync(models, graph);
        }

        public object? Query(Query query)
        {
            return query.Operation switch
            {
                QueryOperation.Many => Many(query),
                QueryOperation.First => First(query),
                QueryOperation.Single => Single(query),
                QueryOperation.Count => Count(query),
                QueryOperation.Any => Any(query),
                QueryOperation.EventMany => EventMany(query),
                QueryOperation.EventFirst => EventFirst(query),
                QueryOperation.EventSingle => EventSingle(query),
                QueryOperation.EventCount => EventCount(query),
                QueryOperation.EventAny => EventAny(query),
                _ => throw new NotImplementedException(),
            };
        }
        public Task<object?> QueryAsync(Query query)
        {
            return query.Operation switch
            {
                QueryOperation.Many => ManyAsync(query),
                QueryOperation.First => FirstAsync(query),
                QueryOperation.Single => SingleAsync(query),
                QueryOperation.Count => CountAsync(query),
                QueryOperation.Any => AnyAsync(query),
                QueryOperation.EventMany => EventManyAsync(query),
                QueryOperation.EventFirst => EventFirstAsync(query),
                QueryOperation.EventSingle => EventSingleAsync(query),
                QueryOperation.EventCount => EventCountAsync(query),
                QueryOperation.EventAny => EventAnyAsync(query),
                _ => throw new NotImplementedException(),
            };
        }

        private object Many(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var models = QueryMany(query);

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
        private object? First(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = QueryFirst(query);

            var returnModel = model;
            if (model is not null)
            {
                returnModel = OnGetWithRelations(new TModel[] { model }, query.Graph).FirstOrDefault();
            }

            return returnModel;
        }
        private object? Single(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = QuerySingle(query);

            var returnModel = model;
            if (model is not null)
            {
                returnModel = (OnGetWithRelations(new TModel[] { model }, query.Graph)).FirstOrDefault();
            }

            return returnModel;
        }
        private object Count(Query query)
        {
            return QueryCount(query);
        }
        private object Any(Query query)
        {
            return QueryAny(query);
        }
        private object EventMany(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var models = QueryEventMany(query);

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
        private object? EventFirst(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = QueryEventFirst(query);

            if (model is not null)
            {
                var returnModel = OnGetWithRelations(new TModel[] { model.Model }, query.Graph).FirstOrDefault();
                if (returnModel is null)
                    model = null;
            }

            return model;
        }
        private object? EventSingle(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = QueryEventSingle(query);

            if (model is not null)
            {
                var returnModel = OnGetWithRelations(new TModel[] { model.Model }, query.Graph).FirstOrDefault();
                if (returnModel is null)
                    model = null;
            }

            return model;
        }
        private object EventCount(Query query)
        {
            return QueryEventCount(query);
        }
        private object EventAny(Query query)
        {
            return QueryEventAny(query);
        }

        private async Task<object?> ManyAsync(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var models = await QueryManyAsync(query);

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
        private async Task<object?> FirstAsync(Query query)
        {
            Graph? graph = null;
            if (query.Graph is not null)
            {
                graph = new Graph(query.Graph);
                OnQueryWithRelations(graph);

                query = new Query(query);
            }

            var model = await QueryFirstAsync(query);

            var returnModel = model;
            if (model is not null)
            {
                returnModel = (await OnGetWithRelationsAsync(new TModel[] { model }, graph)).FirstOrDefault();
            }

            return returnModel;
        }
        private async Task<object?> SingleAsync(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = await QuerySingleAsync(query);

            var returnModel = model;
            if (model is not null)
            {
                returnModel = (await OnGetWithRelationsAsync(new TModel[] { model }, query.Graph)).FirstOrDefault();
            }

            return returnModel;
        }
        private async Task<object?> CountAsync(Query query)
        {
            return await QueryCountAsync(query);
        }
        private async Task<object?> AnyAsync(Query query)
        {
            return await QueryAnyAsync(query);
        }
        private async Task<object?> EventManyAsync(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var models = await QueryEventManyAsync(query);

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
        private async Task<object?> EventFirstAsync(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = await QueryEventFirstAsync(query);

            if (model is not null)
            {
                var returnModel = OnGetWithRelations(new TModel[] { model.Model }, query.Graph).FirstOrDefault();
                if (returnModel is null)
                    model = null;
            }

            return model;
        }
        private async Task<object?> EventSingleAsync(Query query)
        {
            if (query.Graph is not null)
            {
                query = new Query(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = await QueryEventSingleAsync(query);

            if (model is not null)
            {
                var returnModel = OnGetWithRelations(new TModel[] { model.Model }, query.Graph).FirstOrDefault();
                if (returnModel is null)
                    model = null;
            }

            return model;
        }
        private async Task<object?> EventCountAsync(Query query)
        {
            return await QueryEventCountAsync(query);
        }
        private async Task<object?> EventAnyAsync(Query query)
        {
            return await QueryEventAnyAsync(query);
        }

        protected abstract IReadOnlyCollection<TModel> QueryMany(Query query);
        protected abstract TModel? QueryFirst(Query query);
        protected abstract TModel? QuerySingle(Query query);
        protected abstract long QueryCount(Query query);
        protected abstract bool QueryAny(Query query);
        protected abstract IReadOnlyCollection<EventModel<TModel>> QueryEventMany(Query query);
        protected abstract EventModel<TModel>? QueryEventFirst(Query query);
        protected abstract EventModel<TModel>? QueryEventSingle(Query query);
        protected abstract long QueryEventCount(Query query);
        protected abstract bool QueryEventAny(Query query);

        protected abstract Task<IReadOnlyCollection<TModel>> QueryManyAsync(Query query);
        protected abstract Task<TModel?> QueryFirstAsync(Query query);
        protected abstract Task<TModel?> QuerySingleAsync(Query query);
        protected abstract Task<long> QueryCountAsync(Query query);
        protected abstract Task<bool> QueryAnyAsync(Query query);
        protected abstract Task<IReadOnlyCollection<EventModel<TModel>>> QueryEventManyAsync(Query query);
        protected abstract Task<EventModel<TModel>?> QueryEventFirstAsync(Query query);
        protected abstract Task<EventModel<TModel>?> QueryEventSingleAsync(Query query);
        protected abstract Task<long> QueryEventCountAsync(Query query);
        protected abstract Task<bool> QueryEventAnyAsync(Query query);

        protected static readonly Type EventInfoType = typeof(PersistEvent);

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

        protected void PersistSingleRelations(PersistEvent @event, object model, Graph graph, bool create)
        {
            //var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelDetail.RelatedNonEnumerableProperties)
            {
                if (graph.HasMember(modelPropertyInfo.Name))
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

                        if (create)
                        {
                            var persist = new Create(@event, relatedModel, relatedGraph);
                            Repo.Persist(persist);
                        }
                        else
                        {
                            var persist = new Update(@event, relatedModel, relatedGraph);
                            Repo.Persist(persist);
                        }

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
        protected void PersistManyRelations(PersistEvent @event, object model, Graph graph, bool create)
        {
            //var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelDetail.RelatedEnumerableProperties)
            {
                if (graph.HasMember(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(() =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;

                    var relatedModels = (IEnumerable)modelPropertyInfo.GetterBoxed(model)!;

                    var identity = ModelAnalyzer.GetIdentity(relatedType, model);

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    if (modelPropertyInfo.ForeignIdentity is null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                        throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

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
                        var persist = new Delete(@event, relatedModelsDelete, relatedGraph);
                        Repo.Persist(persist);
                    }
                    if (relatedModelsUpdate.Count > 0)
                    {
                        var persist = new Update(@event, relatedModelsUpdate, relatedGraph);
                        Repo.Persist(persist);
                    }
                    if (relatedModelsCreate.Count > 0)
                    {
                        var persist = new Create(@event, relatedModelsCreate, relatedGraph);
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
        protected void DeleteManyRelations(PersistEvent @event, object id, Graph graph)
        {
            //var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelDetail.RelatedEnumerableProperties)
            {
                if (graph.HasMember(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(() =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    if (modelPropertyInfo.ForeignIdentity is null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                        throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

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
                        var persist = new Delete(@event, relatedExistings, relatedGraph);
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

        protected async Task PersistSingleRelationsAsync(PersistEvent @event, object model, Graph graph, bool create)
        {
            var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelDetail.RelatedNonEnumerableProperties)
            {
                if (graph.HasMember(modelPropertyInfo.Name))
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

                        if (create)
                        {
                            var persist = new Create(@event, relatedModel, relatedGraph);
                            await Repo.PersistAsync(persist);
                        }
                        else
                        {
                            var persist = new Update(@event, relatedModel, relatedGraph);
                            await Repo.PersistAsync(persist);
                        }

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
        protected async Task PersistManyRelationsAsync(PersistEvent @event, object model, Graph graph, bool create)
        {
            //var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelDetail.RelatedEnumerableProperties)
            {
                if (graph.HasMember(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(async () =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;

                    var relatedModels = (IEnumerable)modelPropertyInfo.GetterBoxed(model)!;

                    var identity = ModelAnalyzer.GetIdentity(modelPropertyInfo.Type, model);

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    if (modelPropertyInfo.ForeignIdentity is null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                        throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

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
                        var persist = new Delete(@event, relatedModelsDelete, relatedGraph);
                        await Repo.PersistAsync(persist);
                    }
                    if (relatedModelsUpdate.Count > 0)
                    {
                        var persist = new Update(@event, relatedModelsUpdate, relatedGraph);
                        await Repo.PersistAsync(persist);
                    }
                    if (relatedModelsCreate.Count > 0)
                    {
                        var persist = new Create(@event, relatedModelsCreate, relatedGraph);
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
        protected async Task DeleteManyRelationsAsync(PersistEvent @event, object id, Graph graph)
        {
            //var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelDetail.RelatedEnumerableProperties)
            {
                if (graph.HasMember(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(async () =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    if (modelPropertyInfo.ForeignIdentity is null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                        throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

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
                        var persist = new Delete(@event, relatedExistings, relatedGraph);
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

        protected abstract void PersistModel(PersistEvent @event, object model, Graph? graph, bool create);
        protected abstract void DeleteModel(PersistEvent @event, object[] ids);

        protected abstract Task PersistModelAsync(PersistEvent @event, object model, Graph? graph, bool create);
        protected abstract Task DeleteModelAsync(PersistEvent @event, object[] ids);
    }
}