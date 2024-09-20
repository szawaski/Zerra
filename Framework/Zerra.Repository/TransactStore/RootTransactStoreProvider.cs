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
        protected static readonly Type collectionType = typeof(ICollection<>);
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
            var types = queryManyParameterTypes.GetOrAdd(type, (type) =>
            {
                var funcGeneric = TypeAnalyzer.GetGenericType(funcType, type, boolType);
                var expressionGeneric = TypeAnalyzer.GetGenericType(expressionType, funcGeneric);
                var graphGeneric = TypeAnalyzer.GetGenericType(graphType, type);
                var queryGenericTypes = new Type[] { expressionGeneric, graphGeneric };
                return queryGenericTypes;
            });
            return types;
        }
        private static readonly ConcurrentFactoryDictionary<Type, GetWhereExpressionMethodInfo?> relatedPropertyGetWhereExpressionMethods = new();
        private static GetWhereExpressionMethodInfo? GetRelatedPropertyGetWhereExpressionMethod(Type type)
        {
            var relatedPropertyGetWhereExpressionMethod = relatedPropertyGetWhereExpressionMethods.GetOrAdd(type, (type) =>
            {
                return GenerateRelatedPropertyGetWhereExpressionMethod(type);
            });
            return relatedPropertyGetWhereExpressionMethod;
        }
        private static GetWhereExpressionMethodInfo? GenerateRelatedPropertyGetWhereExpressionMethod(Type type)
        {
            var propertyType = type;
            var propertyTypeDetails = TypeAnalyzer.GetTypeDetail(propertyType);
            var enumerable = false;
            if (propertyTypeDetails.Interfaces.Select(x => x.Name).Contains(TypeAnalyzer.GetGenericType(collectionType, propertyType).Name))
            {
                propertyType = propertyTypeDetails.InnerType;
                enumerable = true;
            }

            if (!propertyType.IsClass)
                return null;

            var relatedProviderType = TypeAnalyzer.GetGenericType(dataQueryProviderType, propertyType);

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
                if (graph == null || graph.HasMember(property.Name))
                {
                    var appendWhereExpressionMethodInfo = GetRelatedPropertyGetWhereExpressionMethod(property.Type);

                    if (appendWhereExpressionMethodInfo != null)
                    {
                        var relatedProvider = Resolver.GetSingle(appendWhereExpressionMethodInfo.RelatedProviderType);
                        var relatedGraph = graph?.GetChildGraph(property.Name, appendWhereExpressionMethodInfo.PropertyType);

                        Expression? returnWhereExpression = null;
                        if (relatedProvider is IProviderRelation relatedProviderGeneric)
                        {
                            returnWhereExpression = relatedProviderGeneric.GetWhereExpressionIncludingBase(relatedGraph);
                        }

                        if (returnWhereExpression != null)
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
            var relatedPropertyOnQueryMethod = relatedPropertyOnQueryMethods.GetOrAdd(type, (type) =>
            {
                return GenerateRelatedPropertyOnQueryMethod(type);
            });
            return relatedPropertyOnQueryMethod;
        }
        private static OnQueryMethodInfo? GenerateRelatedPropertyOnQueryMethod(Type type)
        {
            var propertyType = type;
            var propertyTypeDetails = TypeAnalyzer.GetTypeDetail(propertyType);
            if (propertyTypeDetails.Interfaces.Select(x => x.Name).Contains(TypeAnalyzer.GetGenericType(collectionType, propertyType).Name))
            {
                propertyType = propertyTypeDetails.InnerType;
            }

            if (!propertyType.IsClass)
                return null;

            var relatedProviderType = TypeAnalyzer.GetGenericType(dataQueryProviderType, propertyType);

            if (Resolver.TryGetSingle(relatedProviderType, out var relatedProvider))
            {
                return new OnQueryMethodInfo(propertyType, relatedProviderType);
            }

            return null;
        }
        public void OnQuery(Graph<TModel>? graph)
        {
            if (!EventLinking || graph == null)
                return;

            foreach (var property in ModelDetail.RelatedProperties)
            {
                if (graph.HasMember(property.Name))
                {
                    var onQueryMethodInfo = GetRelatedPropertyOnQueryMethod(property.Type);

                    if (onQueryMethodInfo != null)
                    {
                        var relatedProvider = Resolver.GetSingle(onQueryMethodInfo.RelatedProviderType);
                        var relatedGraph = graph.GetChildGraph(property.Name, onQueryMethodInfo.PropertyType);

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
            var relatedPropertyOnGetMethod = relatedPropertyOnGetMethods.GetOrAdd(type, (type) =>
            {
                return GenerateRelatedPropertyOnGetMethod(type);
            });
            return relatedPropertyOnGetMethod;
        }
        private static OnGetMethodInfo? GenerateRelatedPropertyOnGetMethod(Type type)
        {
            var propertyType = type;
            var propertyTypeDetails = TypeAnalyzer.GetTypeDetail(propertyType);
            var collection = false;
            if (propertyTypeDetails.Interfaces.Select(x => x.Name).Contains(TypeAnalyzer.GetGenericType(collectionType, propertyType).Name))
            {
                propertyType = propertyTypeDetails.InnerType;
                collection = true;
            }

            if (!propertyType.IsClass)
                return null;

            var relatedProviderType = TypeAnalyzer.GetGenericType(dataQueryProviderType, propertyType);

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
                    if (graph != null && graph.HasMember(modelPropertyInfo.Name))
                    {
                        if (!modelPropertyInfo.IsEnumerable)
                        {
                            //related single
                            if (modelPropertyInfo.ForeignIdentity == null)
                                throw new Exception($"Model {modelPropertyInfo.Type.GetNiceName()} missing Foreign Identity");
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
        public ICollection<TModel> OnGetWithRelations(ICollection<TModel> models, Graph<TModel>? graph)
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
                    if (graph != null && graph.HasMember(modelPropertyInfo.Name))
                    {
                        //var task = Task.Run(() =>
                        //{
                        if (!modelPropertyInfo.IsEnumerable)
                        {
                            //related single
                            var relatedType = modelPropertyInfo.InnerType;
                            var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name, relatedType);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            if (modelPropertyInfo.ForeignIdentity == null || !ModelDetail.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var foreignIdentityPropertyInfo))
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

                            if (relatedModelInfo.IdentityProperties.Count == 0)
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");
                            var relatedIdentityPropertyInfo = relatedModelInfo.IdentityProperties[0];

                            var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                            var foreignIdentityListTypeDetail = TypeAnalyzer.GetGenericTypeDetail(listType, foreignIdentityPropertyInfo.InnerType);
                            var foreignIdentities = (IList)foreignIdentityListTypeDetail.CreatorBoxed();
                            foreach (var model in returnModels)
                            {
                                var foreignIdentity = ModelAnalyzer.GetForeignIdentity(modelPropertyInfo.ForeignIdentity, model);
                                if (foreignIdentity != null)
                                    _ = foreignIdentities.Add(foreignIdentity);
                            }

                            var containsMethodGeneric = TypeAnalyzer.GetGenericMethodDetail(containsMethod, foreignIdentityPropertyInfo.InnerType).MethodInfo;
                            var condition = Expression.Call(containsMethodGeneric, Expression.Constant(foreignIdentities, foreignIdentityListTypeDetail.Type), Expression.MakeMemberAccess(queryExpressionParameter, relatedIdentityPropertyInfo.MemberInfo));
                            var queryExpression = Expression.Lambda(condition, queryExpressionParameter);

                            if (relatedGraph != null)
                            {
                                var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                                var allNames = relatedIdentityPropertyNames.Concat(new string[] { foreignIdentityPropertyInfo.Name }).ToArray();
                                relatedGraph.AddMembers(relatedIdentityPropertyNames);
                            }

                            var queryGeneric = TypeAnalyzer.GetGenericTypeDetail(queryManyType, relatedType);
                            var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                            var query = queryGeneric.GetConstructorBoxed(queryParameterTypes).CreatorWithArgsBoxed(new object?[] { queryExpression, relatedGraph });

                            var repoQueryMany = TypeAnalyzer.GetGenericMethodDetail(repoQueryManyGeneric, relatedType);
                            var relatedModels = (ICollection)repoQueryMany.CallerBoxed(repoQueryMany, new object[] { query })!;

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (var relatedModel in relatedModels)
                            {
                                var relatedIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedModel);
                                if (relatedIdentity != null)
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
                            var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name, relatedType);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            if (modelPropertyInfo.ForeignIdentity == null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

                            if (relatedGraph != null)
                            {
                                relatedGraph.AddMembers(modelPropertyInfo.ForeignIdentity);
                            }

                            var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                            var foreignIdentityListTypeDetail = TypeAnalyzer.GetGenericTypeDetail(listType, relatedForeignIdentityPropertyInfo.Type);
                            var foreignIdentities = (IList)foreignIdentityListTypeDetail.CreatorBoxed();
                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(model);
                                _ = foreignIdentities.Add(identity);
                            }

                            var containsMethodGeneric = TypeAnalyzer.GetGenericMethodDetail(containsMethod, relatedForeignIdentityPropertyInfo.Type).MethodInfo;
                            var condition = Expression.Call(containsMethodGeneric, Expression.Constant(foreignIdentities, foreignIdentityListTypeDetail.Type), Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo));
                            var queryExpression = Expression.Lambda(condition, queryExpressionParameter);

                            if (relatedGraph != null)
                            {
                                var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                                var allNames = relatedIdentityPropertyNames.Concat(new string[] { modelPropertyInfo.ForeignIdentity }).ToArray();
                                relatedGraph.AddMembers(allNames);
                            }

                            var queryGeneric = TypeAnalyzer.GetGenericTypeDetail(queryManyType, relatedType);
                            var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                            var query = queryGeneric.GetConstructorBoxed(queryParameterTypes).CreatorWithArgsBoxed(new object?[] { queryExpression, relatedGraph });

                            var repoQueryMany = TypeAnalyzer.GetGenericMethodDetail(repoQueryManyGeneric, relatedType);
                            var relatedModels = (ICollection)repoQueryMany.CallerBoxed(repoQueryMany, new object[] { query })!;

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (var relatedModel in relatedModels)
                            {
                                var relatedIdentity = ModelAnalyzer.GetForeignIdentity(relatedType, modelPropertyInfo.ForeignIdentity, relatedModel);
                                if (relatedIdentity != null)
                                    relatedModelIdentities.Add(relatedModel, relatedIdentity);
                            }

                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(model);
                                var relatedForModel = (IList)TypeAnalyzer.GetGenericTypeDetail(listType, relatedType).CreatorBoxed();
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
        public async Task<ICollection<TModel>> OnGetWithRelationsAsync(ICollection<TModel> models, Graph<TModel>? graph)
        {
            var returnModels = models;
            if (EventLinking && graph != null)
            {
                returnModels = await OnGetAsync(models, graph);
            }

            if (QueryLinking && graph != null)
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
                            var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name, relatedType);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            if (modelPropertyInfo.ForeignIdentity == null || !ModelDetail.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var foreignIdentityPropertyInfo))
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

                            if (relatedModelInfo.IdentityProperties.Count == 0)
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");
                            var relatedIdentityPropertyInfo = relatedModelInfo.IdentityProperties[0];

                            var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                            var foreignIdentityListTypeDetail = TypeAnalyzer.GetGenericTypeDetail(listType, foreignIdentityPropertyInfo.InnerType);
                            var foreignIdentities = (IList)foreignIdentityListTypeDetail.CreatorBoxed();
                            foreach (var model in returnModels)
                            {
                                var foreignIdentity = ModelAnalyzer.GetForeignIdentity(modelPropertyInfo.ForeignIdentity, model);
                                if (foreignIdentity != null)
                                    _ = foreignIdentities.Add(foreignIdentity);
                            }

                            var containsMethodGeneric = TypeAnalyzer.GetGenericMethodDetail(containsMethod, foreignIdentityPropertyInfo.InnerType).MethodInfo;
                            var condition = Expression.Call(containsMethodGeneric, Expression.Constant(foreignIdentities, foreignIdentityListTypeDetail.Type), Expression.MakeMemberAccess(queryExpressionParameter, relatedIdentityPropertyInfo.MemberInfo));
                            var queryExpression = Expression.Lambda(condition, queryExpressionParameter);

                            if (relatedGraph != null)
                            {
                                var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                                var allNames = relatedIdentityPropertyNames.Concat(new string[] { foreignIdentityPropertyInfo.Name }).ToArray();
                                relatedGraph.AddMembers(relatedIdentityPropertyNames);
                            }

                            var queryGeneric = TypeAnalyzer.GetGenericTypeDetail(queryManyType, relatedType);
                            var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                            var query = queryGeneric.GetConstructorBoxed(queryParameterTypes).CreatorWithArgsBoxed(new object?[] { queryExpression, relatedGraph });

                            var repoQueryMany = TypeAnalyzer.GetGenericMethodDetail(repoQueryAsyncManyGeneric, relatedType);
                            var relatedModels = (ICollection)(await repoQueryMany.CallerBoxedAsync(repoQueryMany, new object[] { query }))!;

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (var relatedModel in relatedModels)
                            {
                                var relatedIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedModel);
                                if (relatedIdentity != null)
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
                            var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name, relatedType);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            if (modelPropertyInfo.ForeignIdentity == null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                                throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

                            if (relatedGraph != null)
                            {
                                relatedGraph.AddMembers(modelPropertyInfo.ForeignIdentity);
                            }

                            var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                            var foreignIdentityListTypeDetail = TypeAnalyzer.GetGenericTypeDetail(listType, relatedForeignIdentityPropertyInfo.Type);
                            var foreignIdentities = (IList)foreignIdentityListTypeDetail.CreatorBoxed();
                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(model);
                                _ = foreignIdentities.Add(identity);
                            }

                            var containsMethodGeneric = TypeAnalyzer.GetGenericMethodDetail(containsMethod, relatedForeignIdentityPropertyInfo.Type).MethodInfo;
                            var condition = Expression.Call(containsMethodGeneric, Expression.Constant(foreignIdentities, foreignIdentityListTypeDetail.Type), Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo));
                            var queryExpression = Expression.Lambda(condition, queryExpressionParameter);

                            if (relatedGraph != null)
                            {
                                var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                                var allNames = relatedIdentityPropertyNames.Concat(new string[] { modelPropertyInfo.ForeignIdentity }).ToArray();
                                relatedGraph.AddMembers(allNames);
                            }

                            var queryGeneric = TypeAnalyzer.GetGenericTypeDetail(queryManyType, relatedType);
                            var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                            var query = queryGeneric.GetConstructorBoxed(queryParameterTypes).CreatorWithArgsBoxed(new object?[] { queryExpression, relatedGraph });

                            var repoQueryMany = TypeAnalyzer.GetGenericMethodDetail(repoQueryAsyncManyGeneric, relatedType);
                            var relatedModels = (ICollection)(await repoQueryMany.CallerBoxedAsync(repoQueryMany, new object[] { query }))!;

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (var relatedModel in relatedModels)
                            {
                                var relatedIdentity = ModelAnalyzer.GetForeignIdentity(relatedType, modelPropertyInfo.ForeignIdentity, relatedModel);
                                if (relatedIdentity != null)
                                    relatedModelIdentities.Add(relatedModel, relatedIdentity);
                            }

                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(model);
                                var relatedForModel = (IList)TypeAnalyzer.GetGenericTypeDetail(listType, relatedType).CreatorBoxed();
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

        public ICollection<TModel> OnGet(ICollection<TModel> models, Graph<TModel>? graph)
        {
            if (!EventLinking || graph == null)
                return models;

            foreach (var property in ModelDetail.RelatedProperties)
            {
                if (graph.HasMember(property.Name))
                {
                    var onGetMethodInfo = GetRelatedPropertyOnGetMethod(property.Type);

                    if (onGetMethodInfo != null)
                    {
                        var relatedProvider = Resolver.GetSingle(onGetMethodInfo.RelatedProviderType);
                        if (relatedProvider is IProviderRelation relatedProviderGeneric)
                        {
                            var relatedGraph = graph.GetChildGraph(property.Name, onGetMethodInfo.PropertyType);
                            if (onGetMethodInfo.Collection)
                            {
                                foreach (var model in models)
                                {
                                    var related = (ICollection)property.Getter(model)!;
                                    if (related != null)
                                    {
                                        var returnModels = relatedProviderGeneric.OnGetIncludingBase(related, relatedGraph);
                                        property.Setter(model, returnModels);
                                    }
                                }
                            }
                            else
                            {
                                var relatedModels = (IList)TypeAnalyzer.GetGenericTypeDetail(listType, property.Type).CreatorBoxed();
                                var relatedModelsDictionary = new Dictionary<object, List<TModel>>();
                                foreach (var model in models)
                                {
                                    var related = property.Getter(model);
                                    if (related != null)
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
        public async Task<ICollection<TModel>> OnGetAsync(ICollection<TModel> models, Graph<TModel>? graph)
        {
            if (!EventLinking || graph == null)
                return models;

            foreach (var property in ModelDetail.RelatedProperties)
            {
                if (graph.HasMember(property.Name))
                {
                    var onGetMethodInfo = GetRelatedPropertyOnGetMethod(property.Type);

                    if (onGetMethodInfo != null)
                    {
                        var relatedProvider = Resolver.GetSingle(onGetMethodInfo.RelatedProviderType);
                        if (relatedProvider is IProviderRelation relatedProviderGeneric)
                        {
                            var relatedGraph = graph.GetChildGraph(property.Name, onGetMethodInfo.PropertyType);
                            if (onGetMethodInfo.Collection)
                            {
                                foreach (var model in models)
                                {
                                    var related = (ICollection)property.Getter(model)!;
                                    if (related != null)
                                    {
                                        var returnModels = relatedProviderGeneric.OnGetIncludingBaseAsync(related, relatedGraph);
                                        property.Setter(model, returnModels);
                                    }
                                }
                            }
                            else
                            {
                                var relatedModels = (IList)TypeAnalyzer.GetGenericTypeDetail(listType, property.Type).CreatorBoxed();
                                var relatedModelsDictionary = new Dictionary<object, List<TModel>>();
                                foreach (var model in models)
                                {
                                    var related = property.Getter(model);
                                    if (related != null)
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

        public ICollection OnGetIncludingBase(ICollection models, Graph? graph)
        {
            return (ICollection)OnGetIncludingBase((ICollection<TModel>)models, (Graph<TModel>?)graph);
        }
        public ICollection<TModel> OnGetIncludingBase(ICollection<TModel> models, Graph<TModel>? graph)
        {
            return OnGetWithRelations(models, graph);
        }

        public async Task<ICollection> OnGetIncludingBaseAsync(ICollection models, Graph? graph)
        {
            return (ICollection)(await OnGetIncludingBaseAsync((ICollection<TModel>)(object)models, (Graph<TModel>?)graph));
        }
        public Task<ICollection<TModel>> OnGetIncludingBaseAsync(ICollection<TModel> models, Graph<TModel>? graph)
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
            if (query.Graph != null)
            {
                query = new Query<TModel>(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var models = QueryMany(query);

            ICollection<TModel> returnModels;
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
            if (query.Graph != null)
            {
                query = new Query<TModel>(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = QueryFirst(query);

            var returnModel = model;
            if (model != null)
            {
                returnModel = OnGetWithRelations(new TModel[] { model }, query.Graph).FirstOrDefault();
            }

            return returnModel;
        }
        private object? Single(Query<TModel> query)
        {
            if (query.Graph != null)
            {
                query = new Query<TModel>(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = QuerySingle(query);

            var returnModel = model;
            if (model != null)
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
            if (query.Graph != null)
            {
                query = new Query<TModel>(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var models = QueryEventMany(query);

            ICollection<TModel> returnModels;
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
            if (query.Graph != null)
            {
                query = new Query<TModel>(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = QueryEventFirst(query);

            if (model != null)
            {
                var returnModel = OnGetWithRelations(new TModel[] { model.Model }, query.Graph).FirstOrDefault();
                if (returnModel == null)
                    model = null;
            }

            return model;
        }
        private object? EventSingle(Query<TModel> query)
        {
            if (query.Graph != null)
            {
                query = new Query<TModel>(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = QueryEventSingle(query);

            if (model != null)
            {
                var returnModel = OnGetWithRelations(new TModel[] { model.Model }, query.Graph).FirstOrDefault();
                if (returnModel == null)
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
            if (query.Graph != null)
            {
                query = new Query<TModel>(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var models = await QueryManyAsync(query);

            ICollection<TModel> returnModels;
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
            if (query.Graph != null)
            {
                graph = new Graph<TModel>(query.Graph);
                OnQueryWithRelations(graph);

                query = new QueryFirst<TModel>(query.Where, query.Order, graph);
            }

            var model = await QueryFirstAsync(query);

            var returnModel = model;
            if (model != null)
            {
                returnModel = (await OnGetWithRelationsAsync(new TModel[] { model }, graph)).FirstOrDefault();
            }

            return returnModel;
        }
        private async Task<object?> SingleAsync(Query<TModel> query)
        {
            if (query.Graph != null)
            {
                query = new Query<TModel>(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = await QuerySingleAsync(query);

            var returnModel = model;
            if (model != null)
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
            if (query.Graph != null)
            {
                query = new Query<TModel>(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var models = await QueryEventManyAsync(query);

            ICollection<TModel> returnModels;
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
            if (query.Graph != null)
            {
                query = new Query<TModel>(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = await QueryEventFirstAsync(query);

            if (model != null)
            {
                var returnModel = OnGetWithRelations(new TModel[] { model.Model }, query.Graph).FirstOrDefault();
                if (returnModel == null)
                    model = null;
            }

            return model;
        }
        private async Task<object?> EventSingleAsync(Query<TModel> query)
        {
            if (query.Graph != null)
            {
                query = new Query<TModel>(query); //copies graph internally
                OnQueryWithRelations(query.Graph!);
            }

            var model = await QueryEventSingleAsync(query);

            if (model != null)
            {
                var returnModel = OnGetWithRelations(new TModel[] { model.Model }, query.Graph).FirstOrDefault();
                if (returnModel == null)
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

        protected abstract ICollection<TModel> QueryMany(Query<TModel> query);
        protected abstract TModel? QueryFirst(Query<TModel> query);
        protected abstract TModel? QuerySingle(Query<TModel> query);
        protected abstract long QueryCount(Query<TModel> query);
        protected abstract bool QueryAny(Query<TModel> query);
        protected abstract ICollection<EventModel<TModel>> QueryEventMany(Query<TModel> query);
        protected abstract EventModel<TModel>? QueryEventFirst(Query<TModel> query);
        protected abstract EventModel<TModel>? QueryEventSingle(Query<TModel> query);
        protected abstract long QueryEventCount(Query<TModel> query);
        protected abstract bool QueryEventAny(Query<TModel> query);

        protected abstract Task<ICollection<TModel>> QueryManyAsync(Query<TModel> query);
        protected abstract Task<TModel?> QueryFirstAsync(Query<TModel> query);
        protected abstract Task<TModel?> QuerySingleAsync(Query<TModel> query);
        protected abstract Task<long> QueryCountAsync(Query<TModel> query);
        protected abstract Task<bool> QueryAnyAsync(Query<TModel> query);
        protected abstract Task<ICollection<EventModel<TModel>>> QueryEventManyAsync(Query<TModel> query);
        protected abstract Task<EventModel<TModel>?> QueryEventFirstAsync(Query<TModel> query);
        protected abstract Task<EventModel<TModel>?> QueryEventSingleAsync(Query<TModel> query);
        protected abstract Task<long> QueryEventCountAsync(Query<TModel> query);
        protected abstract Task<bool> QueryEventAnyAsync(Query<TModel> query);

        protected static readonly Type EventInfoType = typeof(PersistEvent);
        protected static readonly Type CreateType = typeof(Create<>);
        protected static readonly Type UpdateType = typeof(Update<>);
        protected static readonly Type DeleteType = typeof(Delete<>);

        private static readonly Type[] GraphParameterTypes = new Type[] { typeof(string[]) };

        private static readonly ConcurrentFactoryDictionary<Type, Type[]> persistParameterTypes = new();
        private static Type[] GetPersistParameterTypes(Type type)
        {
            var types = persistParameterTypes.GetOrAdd(type, (type) =>
            {
                var graphGeneric = TypeAnalyzer.GetGenericType(graphType, type);
                var queryGenericTypes = new Type[] { EventInfoType, type, graphGeneric };
                return queryGenericTypes;
            });
            return types;
        }

        private static readonly ConcurrentFactoryDictionary<Type, Type[]> persistEnumerableParameterTypes = new();
        private static Type[] GetPersistEnumerableParameterTypes(Type type)
        {
            var types = persistEnumerableParameterTypes.GetOrAdd(type, (type) =>
            {
                var enumerableType = TypeAnalyzer.GetGenericType(RootTransactStoreProvider<TModel>.collectionType, type);
                var graphGeneric = TypeAnalyzer.GetGenericType(graphType, type);
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
            if (persist.Models == null || persist.Models.Length == 0)
                return;

            var graph = persist.Graph == null ? null : new Graph<TModel>(persist.Graph);

            foreach (var model in persist.Models)
            {
                if (PersistLinking && graph != null && !graph.IsEmpty)
                {
                    PersistSingleRelations(persist.Event, model, graph, true);
                }

                PersistModel(persist.Event, model, graph, true);

                if (PersistLinking && graph != null && !graph.IsEmpty)
                {
                    PersistManyRelations(persist.Event, model, graph, true);
                }
            }
        }
        private void Update(Persist<TModel> persist)
        {
            if (persist.Models == null || persist.Models.Length == 0)
                return;

            var graph = persist.Graph == null ? null : new Graph<TModel>(persist.Graph);

            foreach (var model in persist.Models)
            {
                if (!PersistLinking && graph != null && !graph.IsEmpty)
                {
                    PersistSingleRelations(persist.Event, model, graph, false);
                }

                PersistModel(persist.Event, model, graph, false);

                if (PersistLinking && graph != null && !graph.IsEmpty)
                {
                    PersistManyRelations(persist.Event, model, graph, false);
                }
            }
        }
        private void Delete(Persist<TModel> persist)
        {
            object[] ids;
            if (persist.IDs != null)
            {
                ids = persist.IDs.Cast<object>().ToArray();
            }
            else if (persist.Models != null)
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

            var graph = persist.Graph == null ? null : new Graph<TModel>(persist.Graph);

            if (PersistLinking && graph != null && !graph.IsEmpty)
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
            if (persist.Models == null || persist.Models.Length == 0)
                return;

            var graph = persist.Graph == null ? null : new Graph<TModel>(persist.Graph);

            foreach (var model in persist.Models)
            {
                if (PersistLinking && graph != null && !graph.IsEmpty)
                {
                    await PersistSingleRelationsAsync(persist.Event, model, graph, true);
                }

                await PersistModelAsync(persist.Event, model, graph, true);

                if (PersistLinking && graph != null && !graph.IsEmpty)
                {
                    await PersistManyRelationsAsync(persist.Event, model, graph, true);
                }
            }
        }
        private async Task UpdateAsync(Persist<TModel> persist)
        {
            if (persist.Models == null || persist.Models.Length == 0)
                return;

            var graph = persist.Graph == null ? null : new Graph<TModel>(persist.Graph);

            foreach (var model in persist.Models)
            {
                if (PersistLinking && graph != null && !graph.IsEmpty)
                {
                    await PersistSingleRelationsAsync(persist.Event, model, graph, false);
                }

                await PersistModelAsync(persist.Event, model, graph, false);

                if (PersistLinking && graph != null && !graph.IsEmpty)
                {
                    await PersistManyRelationsAsync(persist.Event, model, graph, false);
                }
            }
        }
        private async Task DeleteAsync(Persist<TModel> persist)
        {
            if (persist.Models == null || persist.Models.Length == 0)
                return;

            object[] ids;
            if (persist.IDs != null)
            {
                ids = persist.IDs.Cast<object>().ToArray();
            }
            else if (persist.Models != null)
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

            var graph = persist.Graph == null ? null : new Graph<TModel>(persist.Graph);

            if (PersistLinking && graph != null && !graph.IsEmpty)
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

                    if (modelPropertyInfo.ForeignIdentity == null)
                        throw new Exception($"Model {modelPropertyInfo.Type.GetNiceName()} missing Foreign Identity");
                    graph.AddMembers(modelPropertyInfo.ForeignIdentity);

                    var relatedModel = modelPropertyInfo.Getter(model);

                    if (relatedModel != null)
                    {
                        var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name, relatedType);

                        if (create)
                        {
                            var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(CreateType, relatedType);
                            var persistParameterTypes = GetPersistParameterTypes(relatedType);
                            var persist = persistGeneric.GetConstructorBoxed(persistParameterTypes).CreatorWithArgsBoxed(new object?[] { @event, relatedModel, relatedGraph });
                            var repoCreate = TypeAnalyzer.GetGenericMethodDetail(repoPersistGeneric, relatedType);
                            _ = repoCreate.CallerBoxed(repoCreate, new object[] { persist });
                        }
                        else
                        {
                            var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(UpdateType, relatedType);
                            var persistParameterTypes = GetPersistParameterTypes(relatedType);
                            var persist = persistGeneric.GetConstructorBoxed(persistParameterTypes).CreatorWithArgsBoxed(new object?[] { @event, relatedModel, relatedGraph });
                            var repoUpdate = TypeAnalyzer.GetGenericMethodDetail(repoPersistGeneric, relatedType);
                            _ = repoUpdate.CallerBoxed(repoUpdate, new object[] { persist });
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

                    if (modelPropertyInfo.ForeignIdentity == null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                        throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

                    var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name, relatedType);
                    var relatedGraphType = TypeAnalyzer.GetGenericTypeDetail(graphType, relatedType);

                    var listTypeDetail = TypeAnalyzer.GetGenericTypeDetail(listType, relatedType);
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
                        var queryGraph = relatedGraphType.GetConstructorBoxed(GraphParameterTypes).CreatorWithArgsBoxed(new object[] { allNames });

                        var queryGeneric = TypeAnalyzer.GetGenericTypeDetail(queryManyType, relatedType);
                        var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                        var query = queryGeneric.GetConstructorBoxed(queryParameterTypes).CreatorWithArgsBoxed(new object[] { queryExpression, queryGraph });

                        var repoQueryMany = TypeAnalyzer.GetGenericMethodDetail(repoQueryManyGeneric, relatedType);
                        var relatedExistings = (ICollection)repoQueryMany.CallerBoxed(repoQueryMany, new object[] { query })!;

                        foreach (var relatedExisting in relatedExistings)
                            _ = relatedModelsExisting.Add(relatedExisting);
                    }

                    var relatedModelIdentities = new Dictionary<object, object>();

                    if (relatedModels != null)
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
                        var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(DeleteType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructorBoxed(persistParameterTypes).CreatorWithArgsBoxed(new object?[] { @event, relatedModelsDelete, relatedGraph });
                        var repoDelete = TypeAnalyzer.GetGenericMethodDetail(repoPersistGeneric, relatedType);
                        _ = repoDelete.CallerBoxed(repoDelete, new object[] { persist });
                    }
                    if (relatedModelsUpdate.Count > 0)
                    {
                        var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(UpdateType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructorBoxed(persistParameterTypes).CreatorWithArgsBoxed(new object?[] { @event, relatedModelsUpdate, relatedGraph });
                        var repoUpdate = TypeAnalyzer.GetGenericMethodDetail(repoPersistGeneric, relatedType);
                        _ = repoUpdate.CallerBoxed(repoUpdate, new object[] { persist });
                    }
                    if (relatedModelsCreate.Count > 0)
                    {
                        var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(CreateType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructorBoxed(persistParameterTypes).CreatorWithArgsBoxed(new object?[] { @event, relatedModelsCreate, relatedGraph });
                        var repoCreate = TypeAnalyzer.GetGenericMethodDetail(repoPersistGeneric, relatedType);
                        _ = repoCreate.CallerBoxed(repoCreate, new object[] { persist });
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

                    if (modelPropertyInfo.ForeignIdentity == null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                        throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

                    var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name, relatedType);
                    var relatedGraphType = TypeAnalyzer.GetGenericTypeDetail(graphType, relatedType);

                    var relatedModelsExisting = (IList)TypeAnalyzer.GetGenericTypeDetail(listType, relatedType).CreatorBoxed();

                    var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                    var queryExpression = Expression.Lambda(Expression.Equal(Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo), Expression.Constant(id, relatedForeignIdentityPropertyInfo.Type)), queryExpressionParameter);
                    var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                    var allNames = relatedIdentityPropertyNames.Concat(new string[] { modelPropertyInfo.ForeignIdentity }).ToArray();
                    var queryGraph = relatedGraphType.GetConstructorBoxed(GraphParameterTypes).CreatorWithArgsBoxed(new object[] { allNames });

                    var queryGeneric = TypeAnalyzer.GetGenericTypeDetail(queryManyType, relatedType);
                    var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                    var query = queryGeneric.GetConstructorBoxed(queryParameterTypes).CreatorWithArgsBoxed(new object[] { queryExpression, queryGraph });

                    var repoQueryMany = TypeAnalyzer.GetGenericMethodDetail(repoQueryManyGeneric, relatedType);
                    var relatedExistings = (ICollection)repoQueryMany.CallerBoxed(repoQueryMany, new object[] { query })!;

                    foreach (var relatedExisting in relatedExistings)
                        _ = relatedModelsExisting.Add(relatedExisting);

                    if (relatedModelsExisting.Count > 0)
                    {
                        var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(DeleteType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructorBoxed(persistParameterTypes).CreatorWithArgsBoxed(new object?[] { @event, relatedModelsExisting, relatedGraph });
                        var repoDelete = TypeAnalyzer.GetGenericMethodDetail(repoPersistGeneric, relatedType);
                        _ = repoDelete.CallerBoxed(repoDelete, new object[] { persist });
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

                    if (modelPropertyInfo.ForeignIdentity == null)
                        throw new Exception($"Model {modelPropertyInfo.Type.GetNiceName()} missing Foreign Identity");
                    graph.AddMembers(modelPropertyInfo.ForeignIdentity);

                    var relatedModel = modelPropertyInfo.Getter(model);

                    if (relatedModel != null)
                    {
                        var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name, relatedType);

                        if (create)
                        {
                            var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(CreateType, relatedType);
                            var persistParameterTypes = GetPersistParameterTypes(relatedType);
                            var persist = persistGeneric.GetConstructorBoxed(persistParameterTypes).CreatorWithArgsBoxed(new object?[] { @event, relatedModel, relatedGraph });
                            var repoCreate = TypeAnalyzer.GetGenericMethodDetail(repoPersistAsyncGeneric, relatedType);
                            _ = await repoCreate.CallerBoxedAsync(repoCreate, new object[] { persist });
                        }
                        else
                        {
                            var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(UpdateType, relatedType);
                            var persistParameterTypes = GetPersistParameterTypes(relatedType);
                            var persist = persistGeneric.GetConstructorBoxed(persistParameterTypes).CreatorWithArgsBoxed(new object?[] { @event, relatedModel, relatedGraph });
                            var repoUpdate = TypeAnalyzer.GetGenericMethodDetail(repoPersistAsyncGeneric, relatedType);
                            _ = await repoUpdate.CallerBoxedAsync(repoUpdate, new object[] { persist });
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

                    var relatedModels = (ICollection)modelPropertyInfo.Getter(model)!;

                    var identity = ModelAnalyzer.GetIdentity(model);

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    if (modelPropertyInfo.ForeignIdentity == null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                        throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

                    var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name, relatedType);
                    var relatedGraphType = TypeAnalyzer.GetGenericTypeDetail(graphType, relatedType);

                    var listTypeDetail = TypeAnalyzer.GetGenericTypeDetail(listType, relatedType);
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
                        var queryGraph = relatedGraphType.GetConstructorBoxed(GraphParameterTypes).CreatorWithArgsBoxed(new object[] { allNames });

                        var queryGeneric = TypeAnalyzer.GetGenericTypeDetail(queryManyType, relatedType);
                        var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                        var query = queryGeneric.GetConstructorBoxed(queryParameterTypes).CreatorWithArgsBoxed(new object[] { queryExpression, queryGraph });

                        var repoQueryMany = TypeAnalyzer.GetGenericMethodDetail(repoQueryAsyncManyGeneric, relatedType);
                        var relatedExistings = (ICollection)(await repoQueryMany.CallerBoxedAsync(repoQueryMany, new object[] { query }))!;

                        foreach (var relatedExisting in relatedExistings)
                            _ = relatedModelsExisting.Add(relatedExisting);
                    }

                    var relatedModelIdentities = new Dictionary<object, object>();

                    if (relatedModels != null)
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
                        var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(DeleteType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructorBoxed(persistParameterTypes).CreatorWithArgsBoxed(new object?[] { @event, relatedModelsDelete, relatedGraph });
                        var repoDelete = TypeAnalyzer.GetGenericMethodDetail(repoPersistAsyncGeneric, relatedType);
                        _ = await repoDelete.CallerBoxedAsync(repoDelete, new object[] { persist });
                    }
                    if (relatedModelsUpdate.Count > 0)
                    {
                        var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(UpdateType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructorBoxed(persistParameterTypes).CreatorWithArgsBoxed(new object?[] { @event, relatedModelsUpdate, relatedGraph });
                        var repoUpdate = TypeAnalyzer.GetGenericMethodDetail(repoPersistAsyncGeneric, relatedType);
                        _ = await repoUpdate.CallerBoxedAsync(repoUpdate, new object[] { persist });
                    }
                    if (relatedModelsCreate.Count > 0)
                    {
                        var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(CreateType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructorBoxed(persistParameterTypes).CreatorWithArgsBoxed(new object?[] { @event, relatedModelsCreate, relatedGraph });
                        var repoCreate = TypeAnalyzer.GetGenericMethodDetail(repoPersistAsyncGeneric, relatedType);
                        _ = await repoCreate.CallerBoxedAsync(repoCreate, new object[] { persist });
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

                    if (modelPropertyInfo.ForeignIdentity == null || !relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out var relatedForeignIdentityPropertyInfo))
                        throw new Exception($"Missing ForeignIdentity {modelPropertyInfo.ForeignIdentity} for {relatedModelInfo.Name} defined in {ModelDetail.Name}");

                    var relatedGraph = graph.GetChildGraph(modelPropertyInfo.Name, relatedType);
                    var relatedGraphType = TypeAnalyzer.GetGenericTypeDetail(graphType, relatedType);

                    var relatedModelsExisting = (IList)TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(listType, relatedType)).CreatorBoxed();

                    var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                    var queryExpression = Expression.Lambda(Expression.Equal(Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo), Expression.Constant(id, relatedForeignIdentityPropertyInfo.Type)), queryExpressionParameter);
                    var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                    var allNames = relatedIdentityPropertyNames.Concat(new string[] { modelPropertyInfo.ForeignIdentity }).ToArray();
                    var queryGraph = relatedGraphType.GetConstructorBoxed(GraphParameterTypes).CreatorWithArgsBoxed(new object[] { allNames });

                    var queryGeneric = TypeAnalyzer.GetGenericTypeDetail(queryManyType, relatedType);
                    var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                    var query = queryGeneric.GetConstructorBoxed(queryParameterTypes).CreatorWithArgsBoxed(new object[] { queryExpression, queryGraph });

                    var repoQueryMany = TypeAnalyzer.GetGenericMethodDetail(repoQueryAsyncManyGeneric, relatedType);
                    var relatedExistings = (ICollection)(await repoQueryMany.CallerBoxedAsync(repoQueryMany, new object[] { query }))!;

                    foreach (var relatedExisting in relatedExistings)
                        _ = relatedModelsExisting.Add(relatedExisting);

                    if (relatedModelsExisting.Count > 0)
                    {
                        var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(DeleteType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructorBoxed(persistParameterTypes).CreatorWithArgsBoxed(new object?[] { @event, relatedModelsExisting, relatedGraph });
                        var repoDelete = TypeAnalyzer.GetGenericMethodDetail(repoPersistAsyncGeneric, relatedType);
                        _ = await repoDelete.CallerBoxedAsync(repoDelete, new object[] { persist });
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