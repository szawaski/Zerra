// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Zerra.Collections;
using Zerra.Linq;
using Zerra.Reflection;
using Zerra.Reflection.Dynamic;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract partial class RootTransactStoreProvider<TModel> : ITransactStoreProvider<TModel>, IProviderRelation<TModel>
        where TModel : class, new()
    {
        protected static readonly Type modelType = typeof(TModel);
        protected static readonly Type graphType = typeof(Graph<>);
        protected static readonly Type listType = typeof(List<>);
        protected static readonly Type queryManyType = typeof(QueryMany<>);
        protected static readonly Type collectionType = typeof(IReadOnlyCollection<>);
        protected static readonly Type dataQueryProviderType = typeof(ITransactStoreProvider<>);
        protected static readonly Type funcType = typeof(Func<,>);
        protected static readonly Type expressionType = typeof(Expression<>);
        protected static readonly Type boolType = typeof(bool);

        protected virtual bool EventLinking { get { return false; } }
        protected virtual bool QueryLinking { get { return true; } }
        protected virtual bool PersistLinking { get { return true; } }

        protected static readonly ModelDetail ModelDetail = ModelAnalyzer.GetModel<TModel>();

        private static readonly ConcurrentFactoryDictionary<Type, Type[]> queryManyParameterTypes = new();
        private static Type[] GetQueryManyParameterTypes(Type type)
        {
            var types = queryManyParameterTypes.GetOrAdd(type, static (type) =>
            {
                var funcGeneric = GenericTypeCache.GetGenericType(funcType, type, boolType);
                var expressionGeneric = GenericTypeCache.GetGenericType(expressionType, funcGeneric);
                var graphGeneric = GenericTypeCache.GetGenericType(graphType, type);
                var queryGenericTypes = new Type[] { expressionGeneric, graphGeneric };
                return queryGenericTypes;
            });
            return types;
        }
        private static readonly ConcurrentFactoryDictionary<Type, GetWhereExpressionMethodInfo?> relatedPropertyGetWhereExpressionMethods = new();
        private static GetWhereExpressionMethodInfo? GetRelatedPropertyGetWhereExpressionMethod(Type type)
        {
            var relatedPropertyGetWhereExpressionMethod = relatedPropertyGetWhereExpressionMethods.GetOrAdd(type, GenerateRelatedPropertyGetWhereExpressionMethod);
            return relatedPropertyGetWhereExpressionMethod;
        }
        private static GetWhereExpressionMethodInfo? GenerateRelatedPropertyGetWhereExpressionMethod(Type type)
        {
            var propertyType = type;
            var propertyTypeDetails = TypeAnalyzer.GetTypeDetail(propertyType);
            var enumerable = false;
            if (propertyTypeDetails.Interfaces.Select(x => x.Name).Contains(GenericTypeCache.GetGenericType(collectionType, propertyType).Name))
            {
                propertyType = propertyTypeDetails.InnerType;
                enumerable = true;
            }

            if (!propertyType.IsClass)
                return null;

            var relatedProviderType = GenericTypeCache.GetGenericType(dataQueryProviderType, propertyType);

            if (Resolver.TryGetSingle(relatedProviderType, out var relatedProvider))
            {
                return new GetWhereExpressionMethodInfo(propertyType, enumerable, relatedProviderType);
            }

            return null;
        }

        public Expression<Func<TModel, bool>>? GetWhereExpression(Graph<TModel>? graph)
        {
            Expression<Func<TModel, bool>>? whereExpression = null;

            foreach (var property in ModelDetail.RelatedProperties)
            {
                if (graph is null || graph.HasMember(property.Name))
                {
                    var appendWhereExpressionMethodInfo = GetRelatedPropertyGetWhereExpressionMethod(property.Type);

                    if (appendWhereExpressionMethodInfo is not null)
                    {
                        var relatedProvider = Resolver.GetSingle(appendWhereExpressionMethodInfo.RelatedProviderType);
                        var relatedGraph = graph?.GetChildGraph(property.Name);

                        Expression? returnWhereExpression = null;
                        if (relatedProvider is IProviderRelation relatedProviderGeneric)
                        {
                            returnWhereExpression = relatedProviderGeneric.GetWhereExpressionIncludingBase(relatedGraph);
                        }

                        if (returnWhereExpression is not null)
                        {
                            whereExpression ??= x => true;
                            whereExpression = whereExpression.AppendExpressionOnMember(property.MemberInfo, returnWhereExpression);
                        }
                    }
                }
            }

            return whereExpression;
        }
        public Expression? GetWhereExpressionIncludingBase(Graph? graph)
        {
            return GetWhereExpressionIncludingBase((Graph<TModel>?)graph);
        }
        public Expression<Func<TModel, bool>>? GetWhereExpressionIncludingBase(Graph<TModel>? graph)
        {
            return GetWhereExpression(graph);
        }
        private static readonly ConcurrentFactoryDictionary<Type, OnQueryMethodInfo?> relatedPropertyOnQueryMethods = new();
        private static OnQueryMethodInfo? GetRelatedPropertyOnQueryMethod(Type type)
        {
            var relatedPropertyOnQueryMethod = relatedPropertyOnQueryMethods.GetOrAdd(type, GenerateRelatedPropertyOnQueryMethod);
            return relatedPropertyOnQueryMethod;
        }
        private static OnQueryMethodInfo? GenerateRelatedPropertyOnQueryMethod(Type type)
        {
            var propertyType = type;
            var propertyTypeDetails = TypeAnalyzer.GetTypeDetail(propertyType);
            if (propertyTypeDetails.Interfaces.Select(x => x.Name).Contains(GenericTypeCache.GetGenericType(collectionType, propertyType).Name))
            {
                propertyType = propertyTypeDetails.InnerType;
            }

            if (!propertyType.IsClass)
                return null;

            var relatedProviderType = GenericTypeCache.GetGenericType(dataQueryProviderType, propertyType);

            if (Resolver.TryGetSingle(relatedProviderType, out var relatedProvider))
            {
                return new OnQueryMethodInfo(propertyType, relatedProviderType);
            }

            return null;
        }
        public void OnQuery(Graph<TModel>? graph)
        {
            if (!EventLinking || graph is null)
                return;

            foreach (var property in ModelDetail.RelatedProperties)
            {
                if (graph.HasMember(property.Name))
                {
                    var onQueryMethodInfo = GetRelatedPropertyOnQueryMethod(property.Type);

                    if (onQueryMethodInfo is not null)
                    {
                        var relatedProvider = Resolver.GetSingle(onQueryMethodInfo.RelatedProviderType);
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
            OnQueryIncludingBase((Graph<TModel>?)graph);
        }
        public void OnQueryIncludingBase(Graph<TModel>? graph)
        {
            OnQueryWithRelations(graph);
        }
        private static readonly ConcurrentFactoryDictionary<Type, OnGetMethodInfo?> relatedPropertyOnGetMethods = new();
        private static OnGetMethodInfo? GetRelatedPropertyOnGetMethod(Type type)
        {
            var relatedPropertyOnGetMethod = relatedPropertyOnGetMethods.GetOrAdd(type, GenerateRelatedPropertyOnGetMethod);
            return relatedPropertyOnGetMethod;
        }
        private static OnGetMethodInfo? GenerateRelatedPropertyOnGetMethod(Type type)
        {
            var propertyType = type;
            var propertyTypeDetails = TypeAnalyzer.GetTypeDetail(propertyType);
            var collection = false;
            if (propertyTypeDetails.Interfaces.Select(x => x.Name).Contains(GenericTypeCache.GetGenericType(collectionType, propertyType).Name))
            {
                propertyType = propertyTypeDetails.InnerType;
                collection = true;
            }

            if (!propertyType.IsClass)
                return null;

            var relatedProviderType = GenericTypeCache.GetGenericType(dataQueryProviderType, propertyType);

            if (Resolver.TryGetSingle(relatedProviderType, out var relatedProvider))
            {
                return new OnGetMethodInfo(propertyType, collection, relatedProviderType);
            }

            return null;
        }

        public void OnQueryWithRelations(Graph<TModel>? graph)
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

        private static readonly MethodInfo repoQueryManyGeneric = typeof(Repo).GetMethods().First(m => m.Name == nameof(Repo.Query) && m.GetParameters().First().ParameterType.Name == typeof(QueryMany<>).Name);
        private static readonly MethodInfo repoQueryAsyncManyGeneric = typeof(Repo).GetMethods().First(m => m.Name == nameof(Repo.QueryAsync) && m.GetParameters().First().ParameterType.Name == typeof(QueryMany<>).Name);
        private static readonly MethodInfo containsMethod = typeof(Enumerable).GetMethods().First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);
        public IReadOnlyCollection<TModel> OnGetWithRelations(IReadOnlyCollection<TModel> models, Graph<TModel>? graph)
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

                            var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                            var foreignIdentityListTypeDetail = GenericTypeCache.GetGenericTypeDetail(listType, foreignIdentityPropertyInfo.InnerType);
                            var foreignIdentities = (IList)foreignIdentityListTypeDetail.CreatorBoxed();
                            foreach (var model in returnModels)
                            {
                                var foreignIdentity = ModelAnalyzer.GetForeignIdentity(modelPropertyInfo.ForeignIdentity, model);
                                if (foreignIdentity is not null)
                                    _ = foreignIdentities.Add(foreignIdentity);
                            }

                            var containsMethodGeneric = GenericTypeCache.GetGenericMethodDetail(containsMethod, foreignIdentityPropertyInfo.InnerType).MethodInfo;
                            var condition = Expression.Call(containsMethodGeneric, Expression.Constant(foreignIdentities, foreignIdentityListTypeDetail.Type), Expression.MakeMemberAccess(queryExpressionParameter, relatedIdentityPropertyInfo.MemberInfo));
                            var queryExpression = Expression.Lambda(condition, queryExpressionParameter);

                            if (relatedGraph is not null)
                            {
                                var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                                var allNames = relatedIdentityPropertyNames.Concat(new string[] { foreignIdentityPropertyInfo.Name }).ToArray();
                                relatedGraph.AddMembers(relatedIdentityPropertyNames);
                            }

                            var queryGeneric = GenericTypeCache.GetGenericTypeDetail(queryManyType, relatedType);
                            var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                            var query = queryGeneric.GetConstructor(queryParameterTypes).CreatorBoxed([queryExpression, relatedGraph]);

                            var repoQueryMany = GenericTypeCache.GetGenericMethodDetail(repoQueryManyGeneric, relatedType);
                            var relatedModels = (IEnumerable)repoQueryMany.CallerBoxed(repoQueryMany, [query])!;

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (var relatedModel in relatedModels)
                            {
                                var relatedIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedModel);
                                if (relatedIdentity is not null)
                                    relatedModelIdentities.Add(relatedModel, relatedIdentity);
                            }

                            foreach (var model in returnModels)
                            {
                                var foreignIdentity = ModelAnalyzer.GetForeignIdentity(modelPropertyInfo.ForeignIdentity, model);
                                foreach (var relatedModel in relatedModelIdentities)
                                {
                                    if (ModelAnalyzer.CompareIdentities(foreignIdentity, relatedModel.Value))
                                    {
                                        modelPropertyInfo.Setter(model, relatedModel.Key);
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

                            var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                            var foreignIdentityListTypeDetail = GenericTypeCache.GetGenericTypeDetail(listType, relatedForeignIdentityPropertyInfo.Type);
                            var foreignIdentities = (IList)foreignIdentityListTypeDetail.CreatorBoxed();
                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(model);
                                _ = foreignIdentities.Add(identity);
                            }

                            var containsMethodGeneric = GenericTypeCache.GetGenericMethodDetail(containsMethod, relatedForeignIdentityPropertyInfo.Type).MethodInfo;
                            var condition = Expression.Call(containsMethodGeneric, Expression.Constant(foreignIdentities, foreignIdentityListTypeDetail.Type), Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo));
                            var queryExpression = Expression.Lambda(condition, queryExpressionParameter);

                            if (relatedGraph is not null)
                            {
                                var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                                var allNames = relatedIdentityPropertyNames.Concat(new string[] { modelPropertyInfo.ForeignIdentity }).ToArray();
                                relatedGraph.AddMembers(allNames);
                            }

                            var queryGeneric = GenericTypeCache.GetGenericTypeDetail(queryManyType, relatedType);
                            var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                            var query = queryGeneric.GetConstructor(queryParameterTypes).CreatorBoxed([queryExpression, relatedGraph]);

                            var repoQueryMany = GenericTypeCache.GetGenericMethodDetail(repoQueryManyGeneric, relatedType);
                            var relatedModels = (IEnumerable)repoQueryMany.CallerBoxed(repoQueryMany, [query])!;

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (var relatedModel in relatedModels)
                            {
                                var relatedIdentity = ModelAnalyzer.GetForeignIdentity(relatedType, modelPropertyInfo.ForeignIdentity, relatedModel);
                                if (relatedIdentity is not null)
                                    relatedModelIdentities.Add(relatedModel, relatedIdentity);
                            }

                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(model);
                                var relatedForModel = (IList)GenericTypeCache.GetGenericTypeDetail(listType, relatedType).CreatorBoxed();
                                foreach (var relatedModel in relatedModelIdentities)
                                {
                                    if (ModelAnalyzer.CompareIdentities(identity, relatedModel.Value))
                                    {
                                        _ = relatedForModel.Add(relatedModel.Key);
                                    }
                                }

                                modelPropertyInfo.Setter(model, relatedForModel);
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
        public async Task<IReadOnlyCollection<TModel>> OnGetWithRelationsAsync(IReadOnlyCollection<TModel> models, Graph<TModel>? graph)
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

                            var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                            var foreignIdentityListTypeDetail = GenericTypeCache.GetGenericTypeDetail(listType, foreignIdentityPropertyInfo.InnerType);
                            var foreignIdentities = (IList)foreignIdentityListTypeDetail.CreatorBoxed();
                            foreach (var model in returnModels)
                            {
                                var foreignIdentity = ModelAnalyzer.GetForeignIdentity(modelPropertyInfo.ForeignIdentity, model);
                                if (foreignIdentity is not null)
                                    _ = foreignIdentities.Add(foreignIdentity);
                            }

                            var containsMethodGeneric = GenericTypeCache.GetGenericMethodDetail(containsMethod, foreignIdentityPropertyInfo.InnerType).MethodInfo;
                            var condition = Expression.Call(containsMethodGeneric, Expression.Constant(foreignIdentities, foreignIdentityListTypeDetail.Type), Expression.MakeMemberAccess(queryExpressionParameter, relatedIdentityPropertyInfo.MemberInfo));
                            var queryExpression = Expression.Lambda(condition, queryExpressionParameter);

                            if (relatedGraph is not null)
                            {
                                var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                                var allNames = relatedIdentityPropertyNames.Concat(new string[] { foreignIdentityPropertyInfo.Name }).ToArray();
                                relatedGraph.AddMembers(relatedIdentityPropertyNames);
                            }

                            var queryGeneric = GenericTypeCache.GetGenericTypeDetail(queryManyType, relatedType);
                            var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                            var query = queryGeneric.GetConstructor(queryParameterTypes).CreatorBoxed([queryExpression, relatedGraph]);

                            var repoQueryMany = GenericTypeCache.GetGenericMethodDetail(repoQueryAsyncManyGeneric, relatedType);
                            var task = (Task)repoQueryMany.CallerBoxed(repoQueryMany, [query])!;
                            await task;
                            var relatedModels = (IEnumerable)repoQueryMany.ReturnTypeDetail.GetMember("Result").GetterBoxed!(task)!;

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (var relatedModel in relatedModels)
                            {
                                var relatedIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedModel);
                                if (relatedIdentity is not null)
                                    relatedModelIdentities.Add(relatedModel, relatedIdentity);
                            }

                            foreach (var model in returnModels)
                            {
                                var foreignIdentity = ModelAnalyzer.GetForeignIdentity(modelPropertyInfo.ForeignIdentity, model);
                                foreach (var relatedModel in relatedModelIdentities)
                                {
                                    if (ModelAnalyzer.CompareIdentities(foreignIdentity, relatedModel.Value))
                                    {
                                        modelPropertyInfo.Setter(model, relatedModel.Key);
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

                            var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                            var foreignIdentityListTypeDetail = GenericTypeCache.GetGenericTypeDetail(listType, relatedForeignIdentityPropertyInfo.Type);
                            var foreignIdentities = (IList)foreignIdentityListTypeDetail.CreatorBoxed();
                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(model);
                                _ = foreignIdentities.Add(identity);
                            }

                            var containsMethodGeneric = GenericTypeCache.GetGenericMethodDetail(containsMethod, relatedForeignIdentityPropertyInfo.Type).MethodInfo;
                            var condition = Expression.Call(containsMethodGeneric, Expression.Constant(foreignIdentities, foreignIdentityListTypeDetail.Type), Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo));
                            var queryExpression = Expression.Lambda(condition, queryExpressionParameter);

                            if (relatedGraph is not null)
                            {
                                var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                                var allNames = relatedIdentityPropertyNames.Concat(new string[] { modelPropertyInfo.ForeignIdentity }).ToArray();
                                relatedGraph.AddMembers(allNames);
                            }

                            var queryGeneric = GenericTypeCache.GetGenericTypeDetail(queryManyType, relatedType);
                            var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                            var query = queryGeneric.GetConstructor(queryParameterTypes).CreatorBoxed([queryExpression, relatedGraph]);

                            var repoQueryMany = GenericTypeCache.GetGenericMethodDetail(repoQueryAsyncManyGeneric, relatedType);
                            var task = (Task)repoQueryMany.CallerBoxed(repoQueryMany, [query])!;
                            await task;
                            var relatedModels = (IEnumerable)repoQueryMany.ReturnTypeDetail.GetMember("Result").GetterBoxed!(task)!;

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (var relatedModel in relatedModels)
                            {
                                var relatedIdentity = ModelAnalyzer.GetForeignIdentity(relatedType, modelPropertyInfo.ForeignIdentity, relatedModel);
                                if (relatedIdentity is not null)
                                    relatedModelIdentities.Add(relatedModel, relatedIdentity);
                            }

                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(model);
                                var relatedForModel = (IList)GenericTypeCache.GetGenericTypeDetail(listType, relatedType).CreatorBoxed();
                                foreach (var relatedModel in relatedModelIdentities)
                                {
                                    if (ModelAnalyzer.CompareIdentities(identity, relatedModel.Value))
                                    {
                                        _ = relatedForModel.Add(relatedModel.Key);
                                    }
                                }

                                modelPropertyInfo.Setter(model, relatedForModel);
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

        public IReadOnlyCollection<TModel> OnGet(IReadOnlyCollection<TModel> models, Graph<TModel>? graph)
        {
            if (!EventLinking || graph is null)
                return models;

            foreach (var property in ModelDetail.RelatedProperties)
            {
                if (graph.HasMember(property.Name))
                {
                    var onGetMethodInfo = GetRelatedPropertyOnGetMethod(property.Type);

                    if (onGetMethodInfo is not null)
                    {
                        var relatedProvider = Resolver.GetSingle(onGetMethodInfo.RelatedProviderType);
                        if (relatedProvider is IProviderRelation relatedProviderGeneric)
                        {
                            var relatedGraph = graph.GetChildGraph(property.Name);
                            if (onGetMethodInfo.Collection)
                            {
                                foreach (var model in models)
                                {
                                    var related = (IEnumerable)property.Getter(model)!;
                                    if (related is not null)
                                    {
                                        var returnModels = relatedProviderGeneric.OnGetIncludingBase(related, relatedGraph);
                                        property.Setter(model, returnModels);
                                    }
                                }
                            }
                            else
                            {
                                var relatedModels = (IList)GenericTypeCache.GetGenericTypeDetail(listType, property.Type).CreatorBoxed();
                                var relatedModelsDictionary = new Dictionary<object, List<TModel>>();
                                foreach (var model in models)
                                {
                                    var related = property.Getter(model);
                                    if (related is not null)
                                    {
                                        _ = relatedModels.Add(related);
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
                                            property.Setter(model, null);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return models;
        }
        public async Task<IReadOnlyCollection<TModel>> OnGetAsync(IReadOnlyCollection<TModel> models, Graph<TModel>? graph)
        {
            if (!EventLinking || graph is null)
                return models;

            foreach (var property in ModelDetail.RelatedProperties)
            {
                if (graph.HasMember(property.Name))
                {
                    var onGetMethodInfo = GetRelatedPropertyOnGetMethod(property.Type);

                    if (onGetMethodInfo is not null)
                    {
                        var relatedProvider = Resolver.GetSingle(onGetMethodInfo.RelatedProviderType);
                        if (relatedProvider is IProviderRelation relatedProviderGeneric)
                        {
                            var relatedGraph = graph.GetChildGraph(property.Name);
                            if (onGetMethodInfo.Collection)
                            {
                                foreach (var model in models)
                                {
                                    var related = (IEnumerable)property.Getter(model)!;
                                    if (related is not null)
                                    {
                                        var returnModels = relatedProviderGeneric.OnGetIncludingBaseAsync(related, relatedGraph);
                                        property.Setter(model, returnModels);
                                    }
                                }
                            }
                            else
                            {
                                var relatedModels = (IList)GenericTypeCache.GetGenericTypeDetail(listType, property.Type).CreatorBoxed();
                                var relatedModelsDictionary = new Dictionary<object, List<TModel>>();
                                foreach (var model in models)
                                {
                                    var related = property.Getter(model);
                                    if (related is not null)
                                    {
                                        _ = relatedModels.Add(related);
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
                                            property.Setter(model, null);
                                        }
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
            return (IEnumerable)OnGetIncludingBase((IReadOnlyCollection<TModel>)models, (Graph<TModel>?)graph);
        }
        public IReadOnlyCollection<TModel> OnGetIncludingBase(IReadOnlyCollection<TModel> models, Graph<TModel>? graph)
        {
            return OnGetWithRelations(models, graph);
        }

        public async Task<IEnumerable> OnGetIncludingBaseAsync(IEnumerable models, Graph? graph)
        {
            return (IEnumerable)(await OnGetIncludingBaseAsync((IReadOnlyCollection<TModel>)(object)models, (Graph<TModel>?)graph));
        }
        public Task<IReadOnlyCollection<TModel>> OnGetIncludingBaseAsync(IReadOnlyCollection<TModel> models, Graph<TModel>? graph)
        {
            return OnGetWithRelationsAsync(models, graph);
        }

        public object? Query(Query<TModel> query)
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
            ;
        }
        public Task<object?> QueryAsync(Query<TModel> query)
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
            ;
        }

        private object Many(Query<TModel> query)
        {
            if (query.Graph is not null)
            {
                query = new Query<TModel>(query); //copies graph internally
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
        private object? First(Query<TModel> query)
        {
            if (query.Graph is not null)
            {
                query = new Query<TModel>(query); //copies graph internally
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
        private object? Single(Query<TModel> query)
        {
            if (query.Graph is not null)
            {
                query = new Query<TModel>(query); //copies graph internally
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
        private object Count(Query<TModel> query)
        {
            return QueryCount(query);
        }
        private object Any(Query<TModel> query)
        {
            return QueryAny(query);
        }
        private object EventMany(Query<TModel> query)
        {
            if (query.Graph is not null)
            {
                query = new Query<TModel>(query); //copies graph internally
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
        private object? EventFirst(Query<TModel> query)
        {
            if (query.Graph is not null)
            {
                query = new Query<TModel>(query); //copies graph internally
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
        private object? EventSingle(Query<TModel> query)
        {
            if (query.Graph is not null)
            {
                query = new Query<TModel>(query); //copies graph internally
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
        private object EventCount(Query<TModel> query)
        {
            return QueryEventCount(query);
        }
        private object EventAny(Query<TModel> query)
        {
            return QueryEventAny(query);
        }

        private async Task<object?> ManyAsync(Query<TModel> query)
        {
            if (query.Graph is not null)
            {
                query = new Query<TModel>(query); //copies graph internally
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
        private async Task<object?> FirstAsync(Query<TModel> query)
        {
            Graph<TModel>? graph = null;
            if (query.Graph is not null)
            {
                graph = new Graph<TModel>(query.Graph);
                OnQueryWithRelations(graph);

                query = new QueryFirst<TModel>(query.Where, query.Order, graph);
            }

            var model = await QueryFirstAsync(query);

            var returnModel = model;
            if (model is not null)
            {
                returnModel = (await OnGetWithRelationsAsync(new TModel[] { model }, graph)).FirstOrDefault();
            }

            return returnModel;
        }
        private async Task<object?> SingleAsync(Query<TModel> query)
        {
            if (query.Graph is not null)
            {
                query = new Query<TModel>(query); //copies graph internally
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
        private async Task<object?> CountAsync(Query<TModel> query)
        {
            return await QueryCountAsync(query);
        }
        private async Task<object?> AnyAsync(Query<TModel> query)
        {
            return await QueryAnyAsync(query);
        }
        private async Task<object?> EventManyAsync(Query<TModel> query)
        {
            if (query.Graph is not null)
            {
                query = new Query<TModel>(query); //copies graph internally
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
        private async Task<object?> EventFirstAsync(Query<TModel> query)
        {
            if (query.Graph is not null)
            {
                query = new Query<TModel>(query); //copies graph internally
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
        private async Task<object?> EventSingleAsync(Query<TModel> query)
        {
            if (query.Graph is not null)
            {
                query = new Query<TModel>(query); //copies graph internally
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
        private async Task<object?> EventCountAsync(Query<TModel> query)
        {
            return await QueryEventCountAsync(query);
        }
        private async Task<object?> EventAnyAsync(Query<TModel> query)
        {
            return await QueryEventAnyAsync(query);
        }

        protected abstract IReadOnlyCollection<TModel> QueryMany(Query<TModel> query);
        protected abstract TModel? QueryFirst(Query<TModel> query);
        protected abstract TModel? QuerySingle(Query<TModel> query);
        protected abstract long QueryCount(Query<TModel> query);
        protected abstract bool QueryAny(Query<TModel> query);
        protected abstract IReadOnlyCollection<EventModel<TModel>> QueryEventMany(Query<TModel> query);
        protected abstract EventModel<TModel>? QueryEventFirst(Query<TModel> query);
        protected abstract EventModel<TModel>? QueryEventSingle(Query<TModel> query);
        protected abstract long QueryEventCount(Query<TModel> query);
        protected abstract bool QueryEventAny(Query<TModel> query);

        protected abstract Task<IReadOnlyCollection<TModel>> QueryManyAsync(Query<TModel> query);
        protected abstract Task<TModel?> QueryFirstAsync(Query<TModel> query);
        protected abstract Task<TModel?> QuerySingleAsync(Query<TModel> query);
        protected abstract Task<long> QueryCountAsync(Query<TModel> query);
        protected abstract Task<bool> QueryAnyAsync(Query<TModel> query);
        protected abstract Task<IReadOnlyCollection<EventModel<TModel>>> QueryEventManyAsync(Query<TModel> query);
        protected abstract Task<EventModel<TModel>?> QueryEventFirstAsync(Query<TModel> query);
        protected abstract Task<EventModel<TModel>?> QueryEventSingleAsync(Query<TModel> query);
        protected abstract Task<long> QueryEventCountAsync(Query<TModel> query);
        protected abstract Task<bool> QueryEventAnyAsync(Query<TModel> query);

        protected static readonly Type EventInfoType = typeof(PersistEvent);
        protected static readonly Type CreateType = typeof(Create<>);
        protected static readonly Type UpdateType = typeof(Update<>);
        protected static readonly Type DeleteType = typeof(Delete<>);

        private static readonly Type[] GraphParameterTypes = [typeof(string[])];

        private static readonly ConcurrentFactoryDictionary<Type, Type[]> persistParameterTypes = new();
        private static Type[] GetPersistParameterTypes(Type type)
        {
            var types = persistParameterTypes.GetOrAdd(type, (type) =>
            {
                var graphGeneric = GenericTypeCache.GetGenericType(graphType, type);
                var queryGenericTypes = new Type[] { EventInfoType, type, graphGeneric };
                return queryGenericTypes;
            });
            return types;
        }

        private static readonly ConcurrentFactoryDictionary<Type, Type[]> persistEnumerableParameterTypes = new();
        private static Type[] GetPersistEnumerableParameterTypes(Type type)
        {
            var types = persistEnumerableParameterTypes.GetOrAdd(type, static (type) =>
            {
                var enumerableType = GenericTypeCache.GetGenericType(RootTransactStoreProvider<TModel>.collectionType, type);
                var graphGeneric = GenericTypeCache.GetGenericType(graphType, type);
                var queryGenericTypes = new Type[] { EventInfoType, enumerableType, graphGeneric };
                return queryGenericTypes;
            });
            return types;
        }

        public void Persist(Persist<TModel> persist)
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
        public Task PersistAsync(Persist<TModel> persist)
        {
            return persist.Operation switch
            {
                PersistOperation.Create => CreateAsync(persist),
                PersistOperation.Update => UpdateAsync(persist),
                PersistOperation.Delete => DeleteAsync(persist),
                _ => throw new NotImplementedException(),
            };
        }

        private void Create(Persist<TModel> persist)
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
        private void Update(Persist<TModel> persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var graph = persist.Graph is null ? null : new Graph<TModel>(persist.Graph);

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
        private void Delete(Persist<TModel> persist)
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
                    var id = ModelAnalyzer.GetIdentity(model);
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

        private async Task CreateAsync(Persist<TModel> persist)
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
        private async Task UpdateAsync(Persist<TModel> persist)
        {
            if (persist.Models is null || persist.Models.Length == 0)
                return;

            var graph = persist.Graph is null ? null : new Graph<TModel>(persist.Graph);

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
        private async Task DeleteAsync(Persist<TModel> persist)
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
                    var id = ModelAnalyzer.GetIdentity(model);
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

        private static readonly MethodInfo repoPersistGeneric = typeof(Repo).GetMethods().First(m => m.Name == nameof(Repo.Persist) && m.GetParameters().First().ParameterType.Name == typeof(Persist<>).Name);
        protected void PersistSingleRelations(PersistEvent @event, TModel model, Graph<TModel> graph, bool create)
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

                    var relatedModel = modelPropertyInfo.Getter(model);

                    if (relatedModel is not null)
                    {
                        var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                        if (create)
                        {
                            var persistGeneric = GenericTypeCache.GetGenericTypeDetail(CreateType, relatedType);
                            var persistParameterTypes = GetPersistParameterTypes(relatedType);
                            var persist = persistGeneric.GetConstructor(persistParameterTypes).CreatorBoxed([@event, relatedModel, relatedGraph]);
                            var repoCreate = GenericTypeCache.GetGenericMethodDetail(repoPersistGeneric, relatedType);
                            _ = repoCreate.CallerBoxed(repoCreate, [persist]);
                        }
                        else
                        {
                            var persistGeneric = GenericTypeCache.GetGenericTypeDetail(UpdateType, relatedType);
                            var persistParameterTypes = GetPersistParameterTypes(relatedType);
                            var persist = persistGeneric.GetConstructor(persistParameterTypes).CreatorBoxed([@event, relatedModel, relatedGraph]);
                            var repoUpdate = GenericTypeCache.GetGenericMethodDetail(repoPersistGeneric, relatedType);
                            _ = repoUpdate.CallerBoxed(repoUpdate, [persist]);
                        }

                        var relatedIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedModel);
                        ModelAnalyzer.SetForeignIdentity(modelPropertyInfo.ForeignIdentity, model, relatedIdentity);
                    }
                    else
                    {
                        ModelAnalyzer.SetForeignIdentity(modelPropertyInfo.ForeignIdentity, model, null);
                    }
                    //});
                    //tasks.Add(task);
                }
            }

            //if (tasks.Count == 0)
            //    return;

            //Task.WaitAll(tasks.ToArray());
        }
        protected void PersistManyRelations(PersistEvent @event, TModel model, Graph<TModel> graph, bool create)
        {
            //var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelDetail.RelatedEnumerableProperties)
            {
                if (graph.HasMember(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(() =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;

                    var relatedModels = (IEnumerable)modelPropertyInfo.Getter(model)!;

                    var identity = ModelAnalyzer.GetIdentity(model);

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    if (modelPropertyInfo.ForeignIdentity is null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                        throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

                    var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);
                    var relatedGraphType = GenericTypeCache.GetGenericTypeDetail(graphType, relatedType);

                    var listTypeDetail = GenericTypeCache.GetGenericTypeDetail(listType, relatedType);
                    var relatedModelsExisting = (IList)listTypeDetail.CreatorBoxed();
                    var relatedModelsDelete = (IList)listTypeDetail.CreatorBoxed();
                    var relatedModelsCreate = (IList)listTypeDetail.CreatorBoxed();
                    var relatedModelsUpdate = (IList)listTypeDetail.CreatorBoxed();

                    if (!create)
                    {
                        var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                        var queryExpression = Expression.Lambda(Expression.Equal(Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo), Expression.Constant(identity, relatedForeignIdentityPropertyInfo.Type)), queryExpressionParameter);
                        var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                        var allNames = relatedIdentityPropertyNames.Concat(new string[] { modelPropertyInfo.ForeignIdentity }).ToArray();
                        var queryGraph = relatedGraphType.GetConstructor(GraphParameterTypes).CreatorBoxed([allNames]);

                        var queryGeneric = GenericTypeCache.GetGenericTypeDetail(queryManyType, relatedType);
                        var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                        var query = queryGeneric.GetConstructor(queryParameterTypes).CreatorBoxed([queryExpression, queryGraph]);

                        var repoQueryMany = GenericTypeCache.GetGenericMethodDetail(repoQueryManyGeneric, relatedType);
                        var relatedExistings = (IEnumerable)repoQueryMany.CallerBoxed(repoQueryMany, [query])!;

                        foreach (var relatedExisting in relatedExistings)
                            _ = relatedModelsExisting.Add(relatedExisting);
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
                                _ = relatedModelsUpdate.Add(relatedModel.Key);
                                relatedModelsUpdateIdentities.Add(relatedModel.Key, relatedModel.Value);
                                foundExisting = true;
                                break;
                            }
                        }
                        if (!foundExisting)
                        {
                            _ = relatedModelsCreate.Add(relatedModel.Key);
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
                            _ = relatedModelsDelete.Add(relatedExisting.Key);
                        }
                    }

                    if (relatedModelsDelete.Count > 0)
                    {
                        var persistGeneric = GenericTypeCache.GetGenericTypeDetail(DeleteType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructor(persistParameterTypes).CreatorBoxed([@event, relatedModelsDelete, relatedGraph]);
                        var repoDelete = GenericTypeCache.GetGenericMethodDetail(repoPersistGeneric, relatedType);
                        _ = repoDelete.CallerBoxed(repoDelete, [persist]);
                    }
                    if (relatedModelsUpdate.Count > 0)
                    {
                        var persistGeneric = GenericTypeCache.GetGenericTypeDetail(UpdateType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructor(persistParameterTypes).CreatorBoxed([@event, relatedModelsUpdate, relatedGraph]);
                        var repoUpdate = GenericTypeCache.GetGenericMethodDetail(repoPersistGeneric, relatedType);
                        _ = repoUpdate.CallerBoxed(repoUpdate, [persist]);
                    }
                    if (relatedModelsCreate.Count > 0)
                    {
                        var persistGeneric = GenericTypeCache.GetGenericTypeDetail(CreateType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructor(persistParameterTypes).CreatorBoxed([@event, relatedModelsCreate, relatedGraph]);
                        var repoCreate = GenericTypeCache.GetGenericMethodDetail(repoPersistGeneric, relatedType);
                        _ = repoCreate.CallerBoxed(repoCreate, [persist]);
                    }
                    //});
                    //tasks.Add(task);
                }
            }

            //if (tasks.Count == 0)
            //    return;

            //Task.WaitAll(tasks.ToArray());
        }
        protected void DeleteManyRelations(PersistEvent @event, object id, Graph<TModel> graph)
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
                    var relatedGraphType = GenericTypeCache.GetGenericTypeDetail(graphType, relatedType);

                    var relatedModelsExisting = (IList)GenericTypeCache.GetGenericTypeDetail(listType, relatedType).CreatorBoxed();

                    var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                    var queryExpression = Expression.Lambda(Expression.Equal(Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo), Expression.Constant(id, relatedForeignIdentityPropertyInfo.Type)), queryExpressionParameter);
                    var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                    var allNames = relatedIdentityPropertyNames.Concat(new string[] { modelPropertyInfo.ForeignIdentity }).ToArray();
                    var queryGraph = relatedGraphType.GetConstructor(GraphParameterTypes).CreatorBoxed([allNames]);

                    var queryGeneric = GenericTypeCache.GetGenericTypeDetail(queryManyType, relatedType);
                    var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                    var query = queryGeneric.GetConstructor(queryParameterTypes).CreatorBoxed([queryExpression, queryGraph]);

                    var repoQueryMany = GenericTypeCache.GetGenericMethodDetail(repoQueryManyGeneric, relatedType);
                    var relatedExistings = (IEnumerable)repoQueryMany.CallerBoxed(repoQueryMany, [query])!;

                    foreach (var relatedExisting in relatedExistings)
                        _ = relatedModelsExisting.Add(relatedExisting);

                    if (relatedModelsExisting.Count > 0)
                    {
                        var persistGeneric = GenericTypeCache.GetGenericTypeDetail(DeleteType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructor(persistParameterTypes).CreatorBoxed([@event, relatedModelsExisting, relatedGraph]);
                        var repoDelete = GenericTypeCache.GetGenericMethodDetail(repoPersistGeneric, relatedType);
                        _ = repoDelete.CallerBoxed(repoDelete, [persist]);
                    }
                    //});
                    //tasks.Add(task);
                }
            }

            //if (tasks.Count == 0)
            //    return;

            //Task.WaitAll(tasks.ToArray());
        }

        private static readonly MethodInfo repoPersistAsyncGeneric = typeof(Repo).GetMethods().First(m => m.Name == nameof(Repo.PersistAsync) && m.GetParameters().First().ParameterType.Name == typeof(Persist<>).Name);
        protected async Task PersistSingleRelationsAsync(PersistEvent @event, TModel model, Graph<TModel> graph, bool create)
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

                    var relatedModel = modelPropertyInfo.Getter(model);

                    if (relatedModel is not null)
                    {
                        var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);

                        if (create)
                        {
                            var persistGeneric = GenericTypeCache.GetGenericTypeDetail(CreateType, relatedType);
                            var persistParameterTypes = GetPersistParameterTypes(relatedType);
                            var persist = persistGeneric.GetConstructor(persistParameterTypes).CreatorBoxed([@event, relatedModel, relatedGraph]);
                            var repoCreate = GenericTypeCache.GetGenericMethodDetail(repoPersistAsyncGeneric, relatedType);
                            await (Task)repoCreate.CallerBoxed(repoCreate, [persist])!;
                        }
                        else
                        {
                            var persistGeneric = GenericTypeCache.GetGenericTypeDetail(UpdateType, relatedType);
                            var persistParameterTypes = GetPersistParameterTypes(relatedType);
                            var persist = persistGeneric.GetConstructor(persistParameterTypes).CreatorBoxed([@event, relatedModel, relatedGraph]);
                            var repoUpdate = GenericTypeCache.GetGenericMethodDetail(repoPersistAsyncGeneric, relatedType);
                            await (Task)repoUpdate.CallerBoxed(repoUpdate, [persist])!;
                        }

                        var relatedIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedModel);
                        ModelAnalyzer.SetForeignIdentity(modelPropertyInfo.ForeignIdentity, model, relatedIdentity);
                    }
                    else
                    {
                        ModelAnalyzer.SetForeignIdentity(modelPropertyInfo.ForeignIdentity, model, null);
                    }
                    //});
                    //tasks.Add(task);
                }
            }

            //if (tasks.Count == 0)
            //    return Task.CompletedTask;

            //return Task.WhenAll(tasks.ToArray());
        }
        protected async Task PersistManyRelationsAsync(PersistEvent @event, TModel model, Graph<TModel> graph, bool create)
        {
            //var tasks = new List<Task>();

            foreach (var modelPropertyInfo in ModelDetail.RelatedEnumerableProperties)
            {
                if (graph.HasMember(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(async () =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;

                    var relatedModels = (IEnumerable)modelPropertyInfo.Getter(model)!;

                    var identity = ModelAnalyzer.GetIdentity(model);

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    if (modelPropertyInfo.ForeignIdentity is null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                        throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

                    var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name);
                    var relatedGraphType = GenericTypeCache.GetGenericTypeDetail(graphType, relatedType);

                    var listTypeDetail = GenericTypeCache.GetGenericTypeDetail(listType, relatedType);
                    var relatedModelsExisting = (IList)listTypeDetail.CreatorBoxed();
                    var relatedModelsDelete = (IList)listTypeDetail.CreatorBoxed();
                    var relatedModelsCreate = (IList)listTypeDetail.CreatorBoxed();
                    var relatedModelsUpdate = (IList)listTypeDetail.CreatorBoxed();

                    if (!create)
                    {
                        var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                        var queryExpression = Expression.Lambda(Expression.Equal(Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo), Expression.Constant(identity, relatedForeignIdentityPropertyInfo.Type)), queryExpressionParameter);
                        var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                        var allNames = relatedIdentityPropertyNames.Concat(new string[] { modelPropertyInfo.ForeignIdentity }).ToArray();
                        var queryGraph = relatedGraphType.GetConstructor(GraphParameterTypes).CreatorBoxed([allNames]);

                        var queryGeneric = GenericTypeCache.GetGenericTypeDetail(queryManyType, relatedType);
                        var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                        var query = queryGeneric.GetConstructor(queryParameterTypes).CreatorBoxed([queryExpression, queryGraph]);

                        var repoQueryMany = GenericTypeCache.GetGenericMethodDetail(repoQueryAsyncManyGeneric, relatedType);
                        var task = (Task)repoQueryMany.CallerBoxed(repoQueryMany, [query])!;
                        await task;
                        var relatedExistings = (IEnumerable)repoQueryMany.ReturnTypeDetail.GetMember("Result").GetterBoxed!(task)!;

                        foreach (var relatedExisting in relatedExistings)
                            _ = relatedModelsExisting.Add(relatedExisting);
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
                                _ = relatedModelsUpdate.Add(relatedModel.Key);
                                relatedModelsUpdateIdentities.Add(relatedModel.Key, relatedModel.Value);
                                foundExisting = true;
                                break;
                            }
                        }
                        if (!foundExisting)
                        {
                            _ = relatedModelsCreate.Add(relatedModel.Key);
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
                            _ = relatedModelsDelete.Add(relatedExisting.Key);
                        }
                    }

                    if (relatedModelsDelete.Count > 0)
                    {
                        var persistGeneric = GenericTypeCache.GetGenericTypeDetail(DeleteType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructor(persistParameterTypes).CreatorBoxed([@event, relatedModelsDelete, relatedGraph]);
                        var repoDelete = GenericTypeCache.GetGenericMethodDetail(repoPersistAsyncGeneric, relatedType);
                        await (Task)repoDelete.CallerBoxed(repoDelete, [persist])!;
                    }
                    if (relatedModelsUpdate.Count > 0)
                    {
                        var persistGeneric = GenericTypeCache.GetGenericTypeDetail(UpdateType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructor(persistParameterTypes).CreatorBoxed([@event, relatedModelsUpdate, relatedGraph]);
                        var repoUpdate = GenericTypeCache.GetGenericMethodDetail(repoPersistAsyncGeneric, relatedType);
                        await (Task)repoUpdate.CallerBoxed(repoUpdate, [persist])!;
                    }
                    if (relatedModelsCreate.Count > 0)
                    {
                        var persistGeneric = GenericTypeCache.GetGenericTypeDetail(CreateType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructor(persistParameterTypes).CreatorBoxed([@event, relatedModelsCreate, relatedGraph]);
                        var repoCreate = GenericTypeCache.GetGenericMethodDetail(repoPersistAsyncGeneric, relatedType);
                        await (Task)repoCreate.CallerBoxed(repoCreate, [persist])!;
                    }
                    //});
                    //tasks.Add(task);
                }
            }

            //if (tasks.Count == 0)
            //    return Task.CompletedTask;

            //return Task.WhenAll(tasks.ToArray());
        }
        protected async Task DeleteManyRelationsAsync(PersistEvent @event, object id, Graph<TModel> graph)
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
                    var relatedGraphType = GenericTypeCache.GetGenericTypeDetail(graphType, relatedType);

                    var relatedModelsExisting = (IList)TypeAnalyzer.GetTypeDetail(GenericTypeCache.GetGenericType(listType, relatedType)).CreatorBoxed();

                    var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                    var queryExpression = Expression.Lambda(Expression.Equal(Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo), Expression.Constant(id, relatedForeignIdentityPropertyInfo.Type)), queryExpressionParameter);
                    var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                    var allNames = relatedIdentityPropertyNames.Concat(new string[] { modelPropertyInfo.ForeignIdentity }).ToArray();
                    var queryGraph = relatedGraphType.GetConstructor(GraphParameterTypes).CreatorBoxed([allNames]);

                    var queryGeneric = GenericTypeCache.GetGenericTypeDetail(queryManyType, relatedType);
                    var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                    var query = queryGeneric.GetConstructor(queryParameterTypes).CreatorBoxed([queryExpression, queryGraph]);

                    var repoQueryMany = GenericTypeCache.GetGenericMethodDetail(repoQueryAsyncManyGeneric, relatedType);
                    var task = (Task)repoQueryMany.CallerBoxed(repoQueryMany, [query])!;
                    await task;
                    var relatedExistings = (IEnumerable)repoQueryMany.ReturnTypeDetail.GetMember("Result").GetterBoxed!(task)!;

                    foreach (var relatedExisting in relatedExistings)
                        _ = relatedModelsExisting.Add(relatedExisting);

                    if (relatedModelsExisting.Count > 0)
                    {
                        var persistGeneric = GenericTypeCache.GetGenericTypeDetail(DeleteType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructor(persistParameterTypes).CreatorBoxed([@event, relatedModelsExisting, relatedGraph]);
                        var repoDelete = GenericTypeCache.GetGenericMethodDetail(repoPersistAsyncGeneric, relatedType);
                        await (Task)repoDelete.CallerBoxed(repoDelete, [persist])!;
                    }
                    //});
                    //tasks.Add(task);
                }
            }

            //if (tasks.Count == 0)
            //    return Task.CompletedTask;

            //return Task.WhenAll(tasks.ToArray());
        }

        protected abstract void PersistModel(PersistEvent @event, TModel model, Graph<TModel>? graph, bool create);
        protected abstract void DeleteModel(PersistEvent @event, object[] ids);

        protected abstract Task PersistModelAsync(PersistEvent @event, TModel model, Graph<TModel>? graph, bool create);
        protected abstract Task DeleteModelAsync(PersistEvent @event, object[] ids);
    }
}