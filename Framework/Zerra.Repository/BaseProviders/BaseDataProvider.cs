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
using Zerra.Providers;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract class BaseDataProvider<TModel> : IBaseProvider, IDataProvider<TModel>, IProviderRelation<TModel>
        where TModel : class, new()
    {
        protected static readonly Type modelType = typeof(TModel);
        protected static readonly Type graphType = typeof(Graph<>);
        protected static readonly Type listType = typeof(List<>);
        protected static readonly Type queryManyType = typeof(QueryMany<>);
        protected static readonly Type enumerableType = typeof(ICollection<>);
        protected static readonly Type dataQueryProviderType = typeof(IDataProvider<>);
        protected static readonly Type funcType = typeof(Func<,>);
        protected static readonly Type expressionType = typeof(Expression<>);
        protected static readonly Type boolType = typeof(bool);

        protected virtual bool DisableEventLinking { get { return false; } }
        protected virtual bool DisableQueryLinking { get { return false; } }

        protected static readonly ModelDetail ModelDetail = ModelAnalyzer.GetModel<TModel>();

        private static readonly ConcurrentFactoryDictionary<Type, Type[]> queryManyParameterTypes = new ConcurrentFactoryDictionary<Type, Type[]>();
        private static Type[] GetQueryManyParameterTypes(Type type)
        {
            var types = queryManyParameterTypes.GetOrAdd(type, (relatedType) =>
            {
                var funcGeneric = TypeAnalyzer.GetGenericType(funcType, relatedType, boolType);
                var expressionGeneric = TypeAnalyzer.GetGenericType(expressionType, funcGeneric);
                var graphGeneric = TypeAnalyzer.GetGenericType(graphType, relatedType);
                var queryGenericTypes = new Type[] { expressionGeneric, graphGeneric };
                return queryGenericTypes;
            });
            return types;
        }

        private class GetWhereExpressionMethodInfo
        {
            public Type PropertyType { get; set; }
            public bool Enumerable { get; set; }
            public Type RelatedProviderType { get; set; }
        }
        private static readonly ConcurrentFactoryDictionary<Type, GetWhereExpressionMethodInfo> relatedPropertyGetWhereExpressionMethods = new ConcurrentFactoryDictionary<Type, GetWhereExpressionMethodInfo>();
        private static GetWhereExpressionMethodInfo GetRelatedPropertyGetWhereExpressionMethod(Type type)
        {
            var relatedPropertyGetWhereExpressionMethod = relatedPropertyGetWhereExpressionMethods.GetOrAdd(type, (t) =>
            {
                return GenerateRelatedPropertyGetWhereExpressionMethod(t);
            });
            return relatedPropertyGetWhereExpressionMethod;
        }
        private static GetWhereExpressionMethodInfo GenerateRelatedPropertyGetWhereExpressionMethod(Type type)
        {
            Type propertyType = type;
            var propertyTypeDetails = TypeAnalyzer.GetType(propertyType);
            bool enumerable = false;
            if (propertyTypeDetails.Interfaces.Select(x => x.Name).Contains(TypeAnalyzer.GetGenericType(enumerableType, propertyType).Name))
            {
                propertyType = propertyTypeDetails.InnerTypes[0];
                enumerable = true;
            }

            if (!propertyType.IsClass)
                return null;

            Type relatedProviderType = TypeAnalyzer.GetGenericType(dataQueryProviderType, propertyType);

            if (Resolver.TryGet(relatedProviderType, out object relatedProvider))
            {
                return new GetWhereExpressionMethodInfo
                {
                    PropertyType = propertyType,
                    Enumerable = enumerable,
                    RelatedProviderType = relatedProviderType
                };
            }

            return null;
        }

        public Expression<Func<TModel, bool>> GetWhereExpression(Graph<TModel> graph)
        {
            Expression<Func<TModel, bool>> whereExpression = null;

            foreach (var property in ModelDetail.RelatedProperties)
            {
                if (graph.HasChildGraph(property.Name))
                {
                    GetWhereExpressionMethodInfo appendWhereExpressionMethodInfo = GetRelatedPropertyGetWhereExpressionMethod(property.Type);

                    if (appendWhereExpressionMethodInfo != null)
                    {
                        object relatedProvider = Resolver.Get(appendWhereExpressionMethodInfo.RelatedProviderType);
                        Graph relatedGraph = graph.GetChildGraphNotNull(property.Name, appendWhereExpressionMethodInfo.PropertyType);

                        Expression returnWhereExpression = null;
                        if (relatedProvider is IProviderRelation relatedProviderGeneric)
                        {
                            returnWhereExpression = relatedProviderGeneric.GetWhereExpressionIncludingBase(relatedGraph);
                        }

                        if (returnWhereExpression != null)
                        {
                            if (whereExpression == null)
                                whereExpression = x => true;
                            whereExpression = whereExpression.AppendExpressionOnMember(property.MemberInfo, returnWhereExpression);
                        }
                    }
                }
            }

            return whereExpression;
        }
        public Expression GetWhereExpressionIncludingBase(Graph graph)
        {
            return GetWhereExpressionIncludingBase((Graph<TModel>)graph);
        }
        public Expression<Func<TModel, bool>> GetWhereExpressionIncludingBase(Graph<TModel> graph)
        {
            return GetWhereExpression(graph);
        }

        private class OnQueryMethodInfo
        {
            public Type PropertyType { get; set; }
            public Type RelatedProviderType { get; set; }
        }
        private static readonly ConcurrentFactoryDictionary<Type, OnQueryMethodInfo> relatedPropertyOnQueryMethods = new ConcurrentFactoryDictionary<Type, OnQueryMethodInfo>();
        private static OnQueryMethodInfo GetRelatedPropertyOnQueryMethod(Type type)
        {
            var relatedPropertyOnQueryMethod = relatedPropertyOnQueryMethods.GetOrAdd(type, (t) =>
            {
                return GenerateRelatedPropertyOnQueryMethod(t);
            });
            return relatedPropertyOnQueryMethod;
        }
        private static OnQueryMethodInfo GenerateRelatedPropertyOnQueryMethod(Type type)
        {
            var propertyType = type;
            var propertyTypeDetails = TypeAnalyzer.GetType(propertyType);
            if (propertyTypeDetails.Interfaces.Select(x => x.Name).Contains(TypeAnalyzer.GetGenericType(enumerableType, propertyType).Name))
            {
                propertyType = propertyTypeDetails.InnerTypes[0];
            }

            if (!propertyType.IsClass)
                return null;

            var relatedProviderType = TypeAnalyzer.GetGenericType(dataQueryProviderType, propertyType);

            if (Resolver.TryGet(relatedProviderType, out object relatedProvider))
            {
                return new OnQueryMethodInfo
                {
                    PropertyType = propertyType,
                    RelatedProviderType = relatedProviderType
                };
            }

            return null;
        }
        public void OnQuery(Graph<TModel> graph)
        {
            if (DisableEventLinking)
                return;

            foreach (var property in ModelDetail.RelatedProperties)
            {
                if (graph.HasChildGraph(property.Name))
                {
                    var onQueryMethodInfo = GetRelatedPropertyOnQueryMethod(property.Type);

                    if (onQueryMethodInfo != null)
                    {
                        var relatedProvider = Resolver.Get(onQueryMethodInfo.RelatedProviderType);
                        var relatedGraph = graph.GetChildGraphNotNull(property.Name, onQueryMethodInfo.PropertyType);

                        if (relatedProvider is IProviderRelation relatedProviderGeneric)
                        {
                            relatedProviderGeneric.OnQueryIncludingBase(relatedGraph);
                            graph.ReplaceChildGraphs(relatedGraph);
                        }
                    }
                }
            }
        }
        public void OnQueryIncludingBase(Graph graph)
        {
            OnQueryIncludingBase((Graph<TModel>)graph);
        }
        public void OnQueryIncludingBase(Graph<TModel> graph)
        {
            OnQueryWithRelations(graph);
        }

        private class OnGetMethodInfo
        {
            public Type PropertyType { get; set; }
            public bool Enumerable { get; set; }
            public Type RelatedProviderType { get; set; }
        }
        private static readonly ConcurrentFactoryDictionary<Type, OnGetMethodInfo> relatedPropertyOnGetMethods = new ConcurrentFactoryDictionary<Type, OnGetMethodInfo>();
        private static OnGetMethodInfo GetRelatedPropertyOnGetMethod(Type type)
        {
            var relatedPropertyOnGetMethod = relatedPropertyOnGetMethods.GetOrAdd(type, (t) =>
            {
                return GenerateRelatedPropertyOnGetMethod(t);
            });
            return relatedPropertyOnGetMethod;
        }
        private static OnGetMethodInfo GenerateRelatedPropertyOnGetMethod(Type type)
        {
            var propertyType = type;
            var propertyTypeDetails = TypeAnalyzer.GetType(propertyType);
            bool enumerable = false;
            if (propertyTypeDetails.Interfaces.Select(x => x.Name).Contains(TypeAnalyzer.GetGenericType(enumerableType, propertyType).Name))
            {
                propertyType = propertyTypeDetails.InnerTypes[0];
                enumerable = true;
            }

            if (!propertyType.IsClass)
                return null;

            var relatedProviderType = TypeAnalyzer.GetGenericType(dataQueryProviderType, propertyType);

            if (Resolver.TryGet(relatedProviderType, out object relatedProvider))
            {
                return new OnGetMethodInfo
                {
                    PropertyType = propertyType,
                    Enumerable = enumerable,
                    RelatedProviderType = relatedProviderType
                };
            }

            return null;
        }

        public void OnQueryWithRelations(Graph<TModel> graph)
        {
            if (!DisableEventLinking)
            {
                OnQuery(graph);
            }

            if (!DisableQueryLinking)
            {
                foreach (var modelPropertyInfo in ModelDetail.RelatedProperties)
                {
                    if (graph.HasChildGraph(modelPropertyInfo.Name))
                    {
                        if (!modelPropertyInfo.IsEnumerable)
                        {
                            //related single
                            graph.AddProperties(modelPropertyInfo.ForeignIdentity);
                        }
                        else
                        {
                            //related many
                            var identityNames = ModelAnalyzer.GetIdentityPropertyNames(modelType);
                            graph.AddProperties(identityNames);
                        }
                    }
                }
            }
        }

        private static readonly MethodInfo repoQueryManyGeneric = typeof(Repo).GetMethods().First(m => m.Name == nameof(Repo.Query) && m.GetParameters().First().ParameterType.Name == typeof(QueryMany<>).Name);
        private static readonly MethodInfo repoQueryAsyncManyGeneric = typeof(Repo).GetMethods().First(m => m.Name == nameof(Repo.QueryAsync) && m.GetParameters().First().ParameterType.Name == typeof(QueryMany<>).Name);
        private static readonly MethodInfo containsMethod = typeof(Enumerable).GetMethods().First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);
        public ICollection<TModel> OnGetWithRelations(ICollection<TModel> models, Graph<TModel> graph)
        {
            ICollection<TModel> returnModels = models;
            if (!DisableEventLinking)
            {
                returnModels = OnGet(models, graph);
            }

            if (!DisableQueryLinking)
            {
                //Get related
                //var tasks = new List<Task>();
                foreach (var modelPropertyInfo in ModelDetail.RelatedProperties)
                {
                    if (graph.HasChildGraph(modelPropertyInfo.Name))
                    {
                        //var task = Task.Run(() =>
                        //{
                        if (!modelPropertyInfo.IsEnumerable)
                        {
                            //related single
                            var relatedType = modelPropertyInfo.InnerType;
                            var relatedGraph = graph.GetChildGraphNotNull(modelPropertyInfo.Name, relatedType);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            if (!ModelDetail.TryGetProperty(modelPropertyInfo.ForeignIdentity, out ModelPropertyDetail foreignIdentityPropertyInfo))
                                throw new Exception(String.Format("Missing ForeignIdentity {0} for {1} defined in {2}", modelPropertyInfo.ForeignIdentity, ModelDetail.Name, ModelDetail.Name));

                            if (relatedModelInfo.IdentityProperties.Count == 0)
                                throw new Exception(String.Format("Missing ForeignIdentity {0} for {1} defined in {2}", modelPropertyInfo.ForeignIdentity, relatedModelInfo.Name, ModelDetail.Name));
                            var relatedIdentityPropertyInfo = relatedModelInfo.IdentityProperties[0];

                            var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                            var foreignIdentityListTypeDetail = TypeAnalyzer.GetGenericTypeDetail(listType, foreignIdentityPropertyInfo.InnerType);
                            var foreignIdentities = (IList)foreignIdentityListTypeDetail.Creator();
                            foreach (var model in returnModels)
                            {
                                var foreignIdentity = ModelAnalyzer.GetForeignIdentity(modelPropertyInfo.ForeignIdentity, model);
                                if (foreignIdentity != null)
                                    foreignIdentities.Add(foreignIdentity);
                            }

                            var containsMethodGeneric = TypeAnalyzer.GetGenericMethod(containsMethod, foreignIdentityPropertyInfo.InnerType).MethodInfo;
                            var condition = Expression.Call(containsMethodGeneric, Expression.Constant(foreignIdentities, foreignIdentityListTypeDetail.Type), Expression.MakeMemberAccess(queryExpressionParameter, relatedIdentityPropertyInfo.MemberInfo));
                            var queryExpression = Expression.Lambda(condition, queryExpressionParameter);

                            var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                            var allNames = relatedIdentityPropertyNames.Concat(new string[] { foreignIdentityPropertyInfo.Name }).ToArray();
                            relatedGraph.AddProperties(relatedIdentityPropertyNames);

                            var queryGeneric = TypeAnalyzer.GetGenericTypeDetail(queryManyType, relatedType);
                            var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                            var query = queryGeneric.GetConstructor(queryParameterTypes).Creator(new object[] { queryExpression, relatedGraph });

                            var repoQueryMany = TypeAnalyzer.GetGenericMethod(repoQueryManyGeneric, relatedType);
                            var relatedModels = (ICollection)repoQueryMany.Caller(repoQueryMany, new object[] { query });

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (object relatedModel in relatedModels)
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
                            var relatedGraph = graph.GetChildGraphNotNull(modelPropertyInfo.Name, relatedType);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            if (!relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out ModelPropertyDetail relatedForeignIdentityPropertyInfo))
                                throw new Exception(String.Format("Missing ForeignIdentity {0} for {1} defined in {2}", modelPropertyInfo.ForeignIdentity, relatedModelInfo.Name, ModelDetail.Name));

                            relatedGraph.AddProperties(modelPropertyInfo.ForeignIdentity);

                            var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                            var foreignIdentityListTypeDetail = TypeAnalyzer.GetGenericTypeDetail(listType, relatedForeignIdentityPropertyInfo.Type);
                            var foreignIdentities = (IList)foreignIdentityListTypeDetail.Creator();
                            foreach (var model in returnModels)
                            {
                                object identity = ModelAnalyzer.GetIdentity(model);
                                foreignIdentities.Add(identity);
                            }

                            var containsMethodGeneric = TypeAnalyzer.GetGenericMethod(containsMethod, relatedForeignIdentityPropertyInfo.Type).MethodInfo;
                            var condition = Expression.Call(containsMethodGeneric, Expression.Constant(foreignIdentities, foreignIdentityListTypeDetail.Type), Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo));
                            var queryExpression = Expression.Lambda(condition, queryExpressionParameter);

                            var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                            var allNames = relatedIdentityPropertyNames.Concat(new string[] { modelPropertyInfo.ForeignIdentity }).ToArray();
                            relatedGraph.AddProperties(allNames);

                            var queryGeneric = TypeAnalyzer.GetGenericTypeDetail(queryManyType, relatedType);
                            var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                            var query = queryGeneric.GetConstructor(queryParameterTypes).Creator(new object[] { queryExpression, relatedGraph });

                            var repoQueryMany = TypeAnalyzer.GetGenericMethod(repoQueryManyGeneric, relatedType);
                            var relatedModels = (ICollection)repoQueryMany.Caller(repoQueryMany, new object[] { query });

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (object relatedModel in relatedModels)
                            {
                                object relatedIdentity = ModelAnalyzer.GetForeignIdentity(relatedType, modelPropertyInfo.ForeignIdentity, relatedModel);
                                if (relatedIdentity != null)
                                    relatedModelIdentities.Add(relatedModel, relatedIdentity);
                            }

                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(model);
                                var relatedForModel = (IList)TypeAnalyzer.GetGenericTypeDetail(listType, relatedType).Creator();
                                foreach (var relatedModel in relatedModelIdentities)
                                {
                                    if (ModelAnalyzer.CompareIdentities(identity, relatedModel.Value))
                                    {
                                        relatedForModel.Add(relatedModel.Key);
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
        public async Task<ICollection<TModel>> OnGetWithRelationsAsync(ICollection<TModel> models, Graph<TModel> graph)
        {
            ICollection<TModel> returnModels = models;
            if (!DisableEventLinking)
            {
                returnModels = await OnGetAsync(models, graph);
            }

            if (!DisableQueryLinking)
            {
                //Get related
                //var tasks = new List<Task>();
                foreach (var modelPropertyInfo in ModelDetail.RelatedProperties)
                {
                    if (graph.HasChildGraph(modelPropertyInfo.Name))
                    {
                        //var task = Task.Run(async () =>
                        //{
                        if (!modelPropertyInfo.IsEnumerable)
                        {
                            //related single
                            var relatedType = modelPropertyInfo.InnerType;
                            var relatedGraph = graph.GetChildGraphNotNull(modelPropertyInfo.Name, relatedType);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            if (!ModelDetail.TryGetProperty(modelPropertyInfo.ForeignIdentity, out ModelPropertyDetail foreignIdentityPropertyInfo))
                                throw new Exception(String.Format("Missing ForeignIdentity {0} for {1} defined in {2}", modelPropertyInfo.ForeignIdentity, ModelDetail.Name, ModelDetail.Name));

                            if (relatedModelInfo.IdentityProperties.Count == 0)
                                throw new Exception(String.Format("Missing ForeignIdentity {0} for {1} defined in {2}", modelPropertyInfo.ForeignIdentity, relatedModelInfo.Name, ModelDetail.Name));
                            var relatedIdentityPropertyInfo = relatedModelInfo.IdentityProperties[0];

                            var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                            var foreignIdentityListTypeDetail = TypeAnalyzer.GetGenericTypeDetail(listType, foreignIdentityPropertyInfo.InnerType);
                            var foreignIdentities = (IList)foreignIdentityListTypeDetail.Creator();
                            foreach (var model in returnModels)
                            {
                                var foreignIdentity = ModelAnalyzer.GetForeignIdentity(modelPropertyInfo.ForeignIdentity, model);
                                if (foreignIdentity != null)
                                    foreignIdentities.Add(foreignIdentity);
                            }

                            var containsMethodGeneric = TypeAnalyzer.GetGenericMethod(containsMethod, foreignIdentityPropertyInfo.InnerType).MethodInfo;
                            var condition = Expression.Call(containsMethodGeneric, Expression.Constant(foreignIdentities, foreignIdentityListTypeDetail.Type), Expression.MakeMemberAccess(queryExpressionParameter, relatedIdentityPropertyInfo.MemberInfo));
                            var queryExpression = Expression.Lambda(condition, queryExpressionParameter);

                            var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                            var allNames = relatedIdentityPropertyNames.Concat(new string[] { foreignIdentityPropertyInfo.Name }).ToArray();
                            relatedGraph.AddProperties(relatedIdentityPropertyNames);

                            var queryGeneric = TypeAnalyzer.GetGenericTypeDetail(queryManyType, relatedType);
                            var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                            var query = queryGeneric.GetConstructor(queryParameterTypes).Creator(new object[] { queryExpression, relatedGraph });

                            var repoQueryMany = TypeAnalyzer.GetGenericMethod(repoQueryAsyncManyGeneric, relatedType);
                            var relatedModels = (ICollection)(await repoQueryMany.CallerAsync(repoQueryMany, new object[] { query }));

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (object relatedModel in relatedModels)
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
                            var relatedGraph = graph.GetChildGraphNotNull(modelPropertyInfo.Name, relatedType);

                            var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                            if (!relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out ModelPropertyDetail relatedForeignIdentityPropertyInfo))
                                throw new Exception(String.Format("Missing ForeignIdentity {0} for {1} defined in {2}", modelPropertyInfo.ForeignIdentity, relatedModelInfo.Name, ModelDetail.Name));

                            relatedGraph.AddProperties(modelPropertyInfo.ForeignIdentity);

                            var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                            var foreignIdentityListTypeDetail = TypeAnalyzer.GetGenericTypeDetail(listType, relatedForeignIdentityPropertyInfo.Type);
                            var foreignIdentities = (IList)foreignIdentityListTypeDetail.Creator();
                            foreach (var model in returnModels)
                            {
                                object identity = ModelAnalyzer.GetIdentity(model);
                                foreignIdentities.Add(identity);
                            }

                            var containsMethodGeneric = TypeAnalyzer.GetGenericMethod(containsMethod, relatedForeignIdentityPropertyInfo.Type).MethodInfo;
                            var condition = Expression.Call(containsMethodGeneric, Expression.Constant(foreignIdentities, foreignIdentityListTypeDetail.Type), Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo));
                            var queryExpression = Expression.Lambda(condition, queryExpressionParameter);

                            var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);
                            var allNames = relatedIdentityPropertyNames.Concat(new string[] { modelPropertyInfo.ForeignIdentity }).ToArray();
                            relatedGraph.AddProperties(allNames);

                            var queryGeneric = TypeAnalyzer.GetGenericTypeDetail(queryManyType, relatedType);
                            var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                            var query = queryGeneric.GetConstructor(queryParameterTypes).Creator(new object[] { queryExpression, relatedGraph });

                            var repoQueryMany = TypeAnalyzer.GetGenericMethod(repoQueryAsyncManyGeneric, relatedType);
                            var relatedModels = (ICollection)(await repoQueryMany.CallerAsync(repoQueryMany, new object[] { query }));

                            var relatedModelIdentities = new Dictionary<object, object>();
                            foreach (object relatedModel in relatedModels)
                            {
                                object relatedIdentity = ModelAnalyzer.GetForeignIdentity(relatedType, modelPropertyInfo.ForeignIdentity, relatedModel);
                                if (relatedIdentity != null)
                                    relatedModelIdentities.Add(relatedModel, relatedIdentity);
                            }

                            foreach (var model in returnModels)
                            {
                                var identity = ModelAnalyzer.GetIdentity(model);
                                var relatedForModel = (IList)TypeAnalyzer.GetGenericTypeDetail(listType, relatedType).Creator();
                                foreach (var relatedModel in relatedModelIdentities)
                                {
                                    if (ModelAnalyzer.CompareIdentities(identity, relatedModel.Value))
                                    {
                                        relatedForModel.Add(relatedModel.Key);
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

        public ICollection<TModel> OnGet(ICollection<TModel> models, Graph<TModel> graph)
        {
            if (DisableEventLinking)
                return models;

            foreach (var property in ModelDetail.RelatedProperties)
            {
                if (graph.HasChildGraph(property.Name))
                {
                    var onGetMethodInfo = GetRelatedPropertyOnGetMethod(property.Type);

                    if (onGetMethodInfo != null)
                    {
                        var relatedProvider = Resolver.Get(onGetMethodInfo.RelatedProviderType);
                        if (relatedProvider is IProviderRelation relatedProviderGeneric)
                        {
                            var relatedGraph = graph.GetChildGraphNotNull(property.Name, onGetMethodInfo.PropertyType);
                            if (onGetMethodInfo.Enumerable)
                            {
                                foreach (var model in models)
                                {
                                    var related = (ICollection)property.Getter(model);
                                    if (related != null)
                                    {
                                        var returnModels = relatedProviderGeneric.OnGetIncludingBase(related, relatedGraph);
                                        property.Setter(model, returnModels);
                                    }
                                }
                            }
                            else
                            {
                                var relatedModels = (IList)TypeAnalyzer.GetGenericTypeDetail(listType, property.Type).Creator();
                                var relatedModelsDictionary = new Dictionary<object, List<TModel>>();
                                foreach (var model in models)
                                {
                                    var related = property.Getter(model);
                                    if (related != null)
                                    {
                                        relatedModels.Add(related);
                                        if (!relatedModelsDictionary.TryGetValue(model, out List<TModel> relatedModelList))
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
                                        relatedModelsDictionary.Remove(returnModel);
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
        public async Task<ICollection<TModel>> OnGetAsync(ICollection<TModel> models, Graph<TModel> graph)
        {
            if (DisableEventLinking)
                return models;

            foreach (var property in ModelDetail.RelatedProperties)
            {
                if (graph.HasChildGraph(property.Name))
                {
                    var onGetMethodInfo = GetRelatedPropertyOnGetMethod(property.Type);

                    if (onGetMethodInfo != null)
                    {
                        var relatedProvider = Resolver.Get(onGetMethodInfo.RelatedProviderType);
                        if (relatedProvider is IProviderRelation relatedProviderGeneric)
                        {
                            var relatedGraph = graph.GetChildGraphNotNull(property.Name, onGetMethodInfo.PropertyType);
                            if (onGetMethodInfo.Enumerable)
                            {
                                foreach (var model in models)
                                {
                                    var related = (ICollection)property.Getter(model);
                                    if (related != null)
                                    {
                                        var returnModels = relatedProviderGeneric.OnGetIncludingBaseAsync(related, relatedGraph);
                                        property.Setter(model, returnModels);
                                    }
                                }
                            }
                            else
                            {
                                var relatedModels = (IList)TypeAnalyzer.GetGenericTypeDetail(listType, property.Type).Creator();
                                var relatedModelsDictionary = new Dictionary<object, List<TModel>>();
                                foreach (var model in models)
                                {
                                    var related = property.Getter(model);
                                    if (related != null)
                                    {
                                        relatedModels.Add(related);
                                        if (!relatedModelsDictionary.TryGetValue(model, out List<TModel> relatedModelList))
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
                                        relatedModelsDictionary.Remove(returnModel);
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

        public ICollection OnGetIncludingBase(ICollection models, Graph graph)
        {
            return (ICollection)OnGetIncludingBase((ICollection<TModel>)models, (Graph<TModel>)graph);
        }
        public ICollection<TModel> OnGetIncludingBase(ICollection<TModel> models, Graph<TModel> graph)
        {
            return OnGetWithRelations(models, graph);
        }

        public async Task<ICollection> OnGetIncludingBaseAsync(ICollection models, Graph graph)
        {
            return (ICollection)(await OnGetIncludingBaseAsync((ICollection<TModel>)(object)models, (Graph<TModel>)graph));
        }
        public Task<ICollection<TModel>> OnGetIncludingBaseAsync(ICollection<TModel> models, Graph<TModel> graph)
        {
            return OnGetWithRelationsAsync(models, graph);
        }

        public object Query(Query<TModel> query)
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
        public Task<object> QueryAsync(Query<TModel> query)
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
            var graph = new Graph<TModel>(query.Graph);

            OnQueryWithRelations(graph);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Graph = graph;

            var models = QueryMany(appenedQuery);

            ICollection<TModel> returnModels;
            if (models.Count > 0)
            {
                returnModels = OnGetWithRelations(models, graph);
            }
            else
            {
                returnModels = Array.Empty<TModel>();
            }

            return returnModels;
        }
        private object First(Query<TModel> query)
        {
            var graph = new Graph<TModel>(query.Graph);

            OnQueryWithRelations(graph);

            var appenedQuery = new QueryFirst<TModel>(query.Where, query.Order, graph);

            var model = QueryFirst(appenedQuery);

            TModel returnModel = model;
            if (model != null)
            {
                returnModel = (OnGetWithRelations(new TModel[] { model }, graph)).FirstOrDefault();
            }

            return returnModel;
        }
        private object Single(Query<TModel> query)
        {
            var graph = new Graph<TModel>(query.Graph);

            OnQueryWithRelations(graph);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Graph = graph;

            var model = QuerySingle(appenedQuery);

            TModel returnModel = model;
            if (model != null)
            {
                returnModel = (OnGetWithRelations(new TModel[] { model }, graph)).FirstOrDefault();
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
            return QueryEventMany(query);
        }
        private object EventFirst(Query<TModel> query)
        {
            return QueryEventMany(query);
        }
        private object EventSingle(Query<TModel> query)
        {
            return QueryEventSingle(query);
        }
        private object EventCount(Query<TModel> query)
        {
            return QueryEventCount(query);
        }
        private object EventAny(Query<TModel> query)
        {
            return QueryEventAny(query);
        }

        private async Task<object> ManyAsync(Query<TModel> query)
        {
            var graph = new Graph<TModel>(query.Graph);

            OnQueryWithRelations(graph);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Graph = graph;

            var models = await QueryManyAsync(appenedQuery);

            ICollection<TModel> returnModels;
            if (models.Count > 0)
            {
                returnModels = await OnGetWithRelationsAsync(models, graph);
            }
            else
            {
                returnModels = Array.Empty<TModel>();
            }

            return returnModels;
        }
        private async Task<object> FirstAsync(Query<TModel> query)
        {
            var graph = new Graph<TModel>(query.Graph);

            OnQueryWithRelations(graph);

            var appenedQuery = new QueryFirst<TModel>(query.Where, query.Order, graph);

            var model = await QueryFirstAsync(appenedQuery);

            TModel returnModel = model;
            if (model != null)
            {
                returnModel = (await OnGetWithRelationsAsync(new TModel[] { model }, graph)).FirstOrDefault();
            }

            return returnModel;
        }
        private async Task<object> SingleAsync(Query<TModel> query)
        {
            var graph = new Graph<TModel>(query.Graph);

            OnQueryWithRelations(graph);

            var appenedQuery = new Query<TModel>(query);
            appenedQuery.Graph = graph;

            var model = await QuerySingleAsync(appenedQuery);

            TModel returnModel = model;
            if (model != null)
            {
                returnModel = (await OnGetWithRelationsAsync(new TModel[] { model }, graph)).FirstOrDefault();
            }

            return returnModel;
        }
        private async Task<object> CountAsync(Query<TModel> query)
        {
            return await QueryCountAsync(query);
        }
        private async Task<object> AnyAsync(Query<TModel> query)
        {
            return await QueryAnyAsync(query);
        }
        private async Task<object> EventManyAsync(Query<TModel> query)
        {
            return await QueryEventManyAsync(query);
        }
        private async Task<object> EventFirstAsync(Query<TModel> query)
        {
            return await QueryEventManyAsync(query);
        }
        private async Task<object> EventSingleAsync(Query<TModel> query)
        {
            return await QueryEventSingleAsync(query);
        }
        private async Task<object> EventCountAsync(Query<TModel> query)
        {
            return await QueryEventCountAsync(query);
        }
        private async Task<object> EventAnyAsync(Query<TModel> query)
        {
            return await QueryEventAnyAsync(query);
        }

        protected abstract ICollection<TModel> QueryMany(Query<TModel> query);
        protected abstract TModel QueryFirst(Query<TModel> query);
        protected abstract TModel QuerySingle(Query<TModel> query);
        protected abstract long QueryCount(Query<TModel> query);
        protected abstract bool QueryAny(Query<TModel> query);
        protected abstract ICollection<EventModel<TModel>> QueryEventMany(Query<TModel> query);
        protected abstract EventModel<TModel> QueryEventFirst(Query<TModel> query);
        protected abstract EventModel<TModel> QueryEventSingle(Query<TModel> query);
        protected abstract long QueryEventCount(Query<TModel> query);
        protected abstract bool QueryEventAny(Query<TModel> query);

        protected abstract Task<ICollection<TModel>> QueryManyAsync(Query<TModel> query);
        protected abstract Task<TModel> QueryFirstAsync(Query<TModel> query);
        protected abstract Task<TModel> QuerySingleAsync(Query<TModel> query);
        protected abstract Task<long> QueryCountAsync(Query<TModel> query);
        protected abstract Task<bool> QueryAnyAsync(Query<TModel> query);
        protected abstract Task<ICollection<EventModel<TModel>>> QueryEventManyAsync(Query<TModel> query);
        protected abstract Task<EventModel<TModel>> QueryEventFirstAsync(Query<TModel> query);
        protected abstract Task<EventModel<TModel>> QueryEventSingleAsync(Query<TModel> query);
        protected abstract Task<long> QueryEventCountAsync(Query<TModel> query);
        protected abstract Task<bool> QueryEventAnyAsync(Query<TModel> query);

        protected static readonly Type EventInfoType = typeof(PersistEvent);
        protected static readonly Type CreateType = typeof(Create<>);
        protected static readonly Type UpdateType = typeof(Update<>);
        protected static readonly Type DeleteType = typeof(Delete<>);

        private static readonly Type[] GraphParameterTypes = new Type[] { typeof(string[]) };

        protected virtual bool DisablePersistLinking { get { return false; } }

        private static readonly ConcurrentFactoryDictionary<Type, Type[]> persistParameterTypes = new ConcurrentFactoryDictionary<Type, Type[]>();
        private static Type[] GetPersistParameterTypes(Type type)
        {
            var types = persistParameterTypes.GetOrAdd(type, (relatedType) =>
            {
                var graphGeneric = TypeAnalyzer.GetGenericType(graphType, relatedType);
                var queryGenericTypes = new Type[] { EventInfoType, relatedType, graphGeneric };
                return queryGenericTypes;
            });
            return types;
        }

        private static readonly ConcurrentFactoryDictionary<Type, Type[]> persistEnumerableParameterTypes = new ConcurrentFactoryDictionary<Type, Type[]>();
        private static Type[] GetPersistEnumerableParameterTypes(Type type)
        {
            var types = persistEnumerableParameterTypes.GetOrAdd(type, (relatedType) =>
            {
                var enumerableType = TypeAnalyzer.GetGenericType(BaseDataProvider<TModel>.enumerableType, relatedType);
                var graphGeneric = TypeAnalyzer.GetGenericType(graphType, relatedType);
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
            if (persist.Models.Length == 0)
                return;

            var graph = new Graph<TModel>(persist.Graph);

            foreach (var model in persist.Models)
            {
                if (!DisablePersistLinking && !graph.IsEmpty)
                {
                    PersistSingleRelations(persist.Event, model, graph, true);
                }

                PersistModel(persist.Event, model, graph, true);

                if (!DisablePersistLinking && !graph.IsEmpty)
                {
                    PersistManyRelations(persist.Event, model, graph, true);
                }
            }
        }
        private void Update(Persist<TModel> persist)
        {
            if (persist.Models.Length == 0)
                return;

            var graph = new Graph<TModel>(persist.Graph);

            foreach (var model in persist.Models)
            {
                if (!DisablePersistLinking && !graph.IsEmpty)
                {
                    PersistSingleRelations(persist.Event, model, graph, false);
                }

                PersistModel(persist.Event, model, graph, false);

                if (!DisablePersistLinking && !graph.IsEmpty)
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
            else
            {
                var idList = new List<object>();
                foreach (var model in persist.Models)
                {
                    object id = ModelAnalyzer.GetIdentity<TModel>(model);
                    idList.Add(id);
                }
                ids = idList.ToArray();
            }

            if (ids.Length == 0)
                return;

            var graph = new Graph<TModel>(persist.Graph);

            if (!DisablePersistLinking && !graph.IsEmpty)
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
            if (persist.Models.Length == 0)
                return;

            var graph = new Graph<TModel>(persist.Graph);

            foreach (var model in persist.Models)
            {
                if (!DisablePersistLinking && !graph.IsEmpty)
                {
                    await PersistSingleRelationsAsync(persist.Event, model, graph, true);
                }

                await PersistModelAsync(persist.Event, model, graph, true);

                if (!DisablePersistLinking && !graph.IsEmpty)
                {
                    await PersistManyRelationsAsync(persist.Event, model, graph, true);
                }
            }
        }
        private async Task UpdateAsync(Persist<TModel> persist)
        {
            if (persist.Models.Length == 0)
                return;

            var graph = new Graph<TModel>(persist.Graph);

            foreach (var model in persist.Models)
            {
                if (!DisablePersistLinking && !graph.IsEmpty)
                {
                    await PersistSingleRelationsAsync(persist.Event, model, graph, false);
                }

                await PersistModelAsync(persist.Event, model, graph, false);

                if (!DisablePersistLinking && !graph.IsEmpty)
                {
                    await PersistManyRelationsAsync(persist.Event, model, graph, false);
                }
            }
        }
        private async Task DeleteAsync(Persist<TModel> persist)
        {
            object[] ids;
            if (persist.IDs != null)
            {
                ids = persist.IDs.Cast<object>().ToArray();
            }
            else
            {
                var idList = new List<object>();
                foreach (var model in persist.Models)
                {
                    object id = ModelAnalyzer.GetIdentity<TModel>(model);
                    idList.Add(id);
                }
                ids = idList.ToArray();
            }

            if (ids.Length == 0)
                return;

            var graph = new Graph<TModel>(persist.Graph);

            if (!DisablePersistLinking && !graph.IsEmpty)
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
                if (graph.HasChildGraph(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(() =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;
                    graph.AddProperties(modelPropertyInfo.ForeignIdentity);

                    var relatedModel = modelPropertyInfo.Getter(model);

                    if (relatedModel != null)
                    {
                        var relatedGraph = graph.GetChildGraphNotNull(modelPropertyInfo.Name, relatedType);

                        if (create)
                        {
                            var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(CreateType, relatedType);
                            var persistParameterTypes = GetPersistParameterTypes(relatedType);
                            var persist = persistGeneric.GetConstructor(persistParameterTypes).Creator(new object[] { @event, relatedModel, relatedGraph });
                            var repoCreate = TypeAnalyzer.GetGenericMethod(repoPersistGeneric, relatedType);
                            repoCreate.Caller(repoCreate, new object[] { persist });
                        }
                        else
                        {
                            var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(UpdateType, relatedType);
                            var persistParameterTypes = GetPersistParameterTypes(relatedType);
                            var persist = persistGeneric.GetConstructor(persistParameterTypes).Creator(new object[] { @event, relatedModel, relatedGraph });
                            var repoUpdate = TypeAnalyzer.GetGenericMethod(repoPersistGeneric, relatedType);
                            repoUpdate.Caller(repoUpdate, new object[] { persist });
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
                if (graph.HasChildGraph(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(() =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;

                    var relatedModels = (ICollection)modelPropertyInfo.Getter(model);

                    var identity = ModelAnalyzer.GetIdentity(model);

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    if (!relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out ModelPropertyDetail relatedForeignIdentityPropertyInfo))
                        throw new Exception(String.Format("Missing ForeignIdentity {0} for {1} defined in {2}", modelPropertyInfo.ForeignIdentity, relatedModelInfo.Name, ModelDetail.Name));

                    var relatedGraph = graph.GetChildGraphNotNull(modelPropertyInfo.Name, relatedType);
                    var relatedGraphType = TypeAnalyzer.GetGenericTypeDetail(graphType, relatedType);

                    var listTypeDetail = TypeAnalyzer.GetGenericTypeDetail(listType, relatedType);
                    var relatedModelsExisting = (IList)listTypeDetail.Creator();
                    var relatedModelsDelete = (IList)listTypeDetail.Creator();
                    var relatedModelsCreate = (IList)listTypeDetail.Creator();
                    var relatedModelsUpdate = (IList)listTypeDetail.Creator();

                    if (!create)
                    {
                        var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                        var queryExpression = Expression.Lambda(Expression.Equal(Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo), Expression.Constant(identity, relatedForeignIdentityPropertyInfo.Type)), queryExpressionParameter);
                        var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                        var allNames = relatedIdentityPropertyNames.Concat(new string[] { modelPropertyInfo.ForeignIdentity }).ToArray();
                        var queryGraph = relatedGraphType.GetConstructor(GraphParameterTypes).Creator(new object[] { allNames });

                        var queryGeneric = TypeAnalyzer.GetGenericTypeDetail(queryManyType, relatedType);
                        var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                        var query = queryGeneric.GetConstructor(queryParameterTypes).Creator(new object[] { queryExpression, queryGraph });

                        var repoQueryMany = TypeAnalyzer.GetGenericMethod(repoQueryManyGeneric, relatedType);
                        var relatedExistings = (ICollection)repoQueryMany.Caller(repoQueryMany, new object[] { query });

                        foreach (var relatedExisting in relatedExistings)
                            relatedModelsExisting.Add(relatedExisting);
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
                    foreach (object relatedExisting in relatedModelsExisting)
                    {
                        var relatedExistingIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedExisting);
                        relatedModelsExistingIdentities.Add(relatedExisting, relatedExistingIdentity);
                    }

                    var relatedModelsUpdateIdentities = new Dictionary<object, object>();

                    foreach (var relatedModel in relatedModelIdentities)
                    {
                        bool foundExisting = false;
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
                        bool foundUpdating = false;
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
                        var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(DeleteType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructor(persistParameterTypes).Creator(new object[] { @event, relatedModelsDelete, relatedGraph });
                        var repoDelete = TypeAnalyzer.GetGenericMethod(repoPersistGeneric, relatedType);
                        repoDelete.Caller(repoDelete, new object[] { persist });
                    }
                    if (relatedModelsUpdate.Count > 0)
                    {
                        var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(UpdateType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructor(persistParameterTypes).Creator(new object[] { @event, relatedModelsUpdate, relatedGraph });
                        var repoUpdate = TypeAnalyzer.GetGenericMethod(repoPersistGeneric, relatedType);
                        repoUpdate.Caller(repoUpdate, new object[] { persist });
                    }
                    if (relatedModelsCreate.Count > 0)
                    {
                        var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(CreateType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructor(persistParameterTypes).Creator(new object[] { @event, relatedModelsCreate, relatedGraph });
                        var repoCreate = TypeAnalyzer.GetGenericMethod(repoPersistGeneric, relatedType);
                        repoCreate.Caller(repoCreate, new object[] { persist });
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
                if (graph.HasChildGraph(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(() =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    if (!relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out ModelPropertyDetail relatedForeignIdentityPropertyInfo))
                        throw new Exception(String.Format("Missing ForeignIdentity {0} for {1} defined in {2}", modelPropertyInfo.ForeignIdentity, relatedModelInfo.Name, ModelDetail.Name));

                    var relatedGraph = graph.GetChildGraphNotNull(modelPropertyInfo.Name, relatedType);
                    var relatedGraphType = TypeAnalyzer.GetGenericTypeDetail(graphType, relatedType);

                    var relatedModelsExisting = (IList)TypeAnalyzer.GetGenericTypeDetail(listType, relatedType).Creator();

                    var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                    var queryExpression = Expression.Lambda(Expression.Equal(Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo), Expression.Constant(id, relatedForeignIdentityPropertyInfo.Type)), queryExpressionParameter);
                    var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                    var allNames = relatedIdentityPropertyNames.Concat(new string[] { modelPropertyInfo.ForeignIdentity }).ToArray();
                    var queryGraph = relatedGraphType.GetConstructor(GraphParameterTypes).Creator(new object[] { allNames });

                    var queryGeneric = TypeAnalyzer.GetGenericTypeDetail(queryManyType, relatedType);
                    var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                    var query = queryGeneric.GetConstructor(queryParameterTypes).Creator(new object[] { queryExpression, queryGraph });

                    var repoQueryMany = TypeAnalyzer.GetGenericMethod(repoQueryManyGeneric, relatedType);
                    var relatedExistings = (ICollection)repoQueryMany.Caller(repoQueryMany, new object[] { query });

                    foreach (var relatedExisting in relatedExistings)
                        relatedModelsExisting.Add(relatedExisting);

                    if (relatedModelsExisting.Count > 0)
                    {
                        var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(DeleteType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructor(persistParameterTypes).Creator(new object[] { @event, relatedModelsExisting, relatedGraph });
                        var repoDelete = TypeAnalyzer.GetGenericMethod(repoPersistGeneric, relatedType);
                        repoDelete.Caller(repoDelete, new object[] { persist });
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
                if (graph.HasChildGraph(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(async () =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;
                    graph.AddProperties(modelPropertyInfo.ForeignIdentity);

                    var relatedModel = modelPropertyInfo.Getter(model);

                    if (relatedModel != null)
                    {
                        var relatedGraph = graph.GetChildGraphNotNull(modelPropertyInfo.Name, relatedType);

                        if (create)
                        {
                            var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(CreateType, relatedType);
                            var persistParameterTypes = GetPersistParameterTypes(relatedType);
                            var persist = persistGeneric.GetConstructor(persistParameterTypes).Creator(new object[] { @event, relatedModel, relatedGraph });
                            var repoCreate = TypeAnalyzer.GetGenericMethod(repoPersistAsyncGeneric, relatedType);
                            await repoCreate.CallerAsync(repoCreate, new object[] { persist });
                        }
                        else
                        {
                            var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(UpdateType, relatedType);
                            var persistParameterTypes = GetPersistParameterTypes(relatedType);
                            var persist = persistGeneric.GetConstructor(persistParameterTypes).Creator(new object[] { @event, relatedModel, relatedGraph });
                            var repoUpdate = TypeAnalyzer.GetGenericMethod(repoPersistAsyncGeneric, relatedType);
                            await repoUpdate.CallerAsync(repoUpdate, new object[] { persist });
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
                if (graph.HasChildGraph(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(async () =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;

                    var relatedModels = (ICollection)modelPropertyInfo.Getter(model);

                    var identity = ModelAnalyzer.GetIdentity(model);

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    if (!relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out ModelPropertyDetail relatedForeignIdentityPropertyInfo))
                        throw new Exception(String.Format("Missing ForeignIdentity {0} for {1} defined in {2}", modelPropertyInfo.ForeignIdentity, relatedModelInfo.Name, ModelDetail.Name));

                    var relatedGraph = graph.GetChildGraphNotNull(modelPropertyInfo.Name, relatedType);
                    var relatedGraphType = TypeAnalyzer.GetGenericTypeDetail(graphType, relatedType);

                    var listTypeDetail = TypeAnalyzer.GetGenericTypeDetail(listType, relatedType);
                    var relatedModelsExisting = (IList)listTypeDetail.Creator();
                    var relatedModelsDelete = (IList)listTypeDetail.Creator();
                    var relatedModelsCreate = (IList)listTypeDetail.Creator();
                    var relatedModelsUpdate = (IList)listTypeDetail.Creator();

                    if (!create)
                    {
                        var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                        var queryExpression = Expression.Lambda(Expression.Equal(Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo), Expression.Constant(identity, relatedForeignIdentityPropertyInfo.Type)), queryExpressionParameter);
                        var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                        var allNames = relatedIdentityPropertyNames.Concat(new string[] { modelPropertyInfo.ForeignIdentity }).ToArray();
                        var queryGraph = relatedGraphType.GetConstructor(GraphParameterTypes).Creator(new object[] { allNames });

                        var queryGeneric = TypeAnalyzer.GetGenericTypeDetail(queryManyType, relatedType);
                        var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                        var query = queryGeneric.GetConstructor(queryParameterTypes).Creator(new object[] { queryExpression, queryGraph });

                        var repoQueryMany = TypeAnalyzer.GetGenericMethod(repoQueryAsyncManyGeneric, relatedType);
                        var relatedExistings = (ICollection)await repoQueryMany.CallerAsync(repoQueryMany, new object[] { query });

                        foreach (var relatedExisting in relatedExistings)
                            relatedModelsExisting.Add(relatedExisting);
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
                    foreach (object relatedExisting in relatedModelsExisting)
                    {
                        var relatedExistingIdentity = ModelAnalyzer.GetIdentity(relatedType, relatedExisting);
                        relatedModelsExistingIdentities.Add(relatedExisting, relatedExistingIdentity);
                    }

                    var relatedModelsUpdateIdentities = new Dictionary<object, object>();

                    foreach (var relatedModel in relatedModelIdentities)
                    {
                        bool foundExisting = false;
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
                        bool foundUpdating = false;
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
                        var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(DeleteType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructor(persistParameterTypes).Creator(new object[] { @event, relatedModelsDelete, relatedGraph });
                        var repoDelete = TypeAnalyzer.GetGenericMethod(repoPersistAsyncGeneric, relatedType);
                        await repoDelete.CallerAsync(repoDelete, new object[] { persist });
                    }
                    if (relatedModelsUpdate.Count > 0)
                    {
                        var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(UpdateType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructor(persistParameterTypes).Creator(new object[] { @event, relatedModelsUpdate, relatedGraph });
                        var repoUpdate = TypeAnalyzer.GetGenericMethod(repoPersistAsyncGeneric, relatedType);
                        await repoUpdate.CallerAsync(repoUpdate, new object[] { persist });
                    }
                    if (relatedModelsCreate.Count > 0)
                    {
                        var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(CreateType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructor(persistParameterTypes).Creator(new object[] { @event, relatedModelsCreate, relatedGraph });
                        var repoCreate = TypeAnalyzer.GetGenericMethod(repoPersistAsyncGeneric, relatedType);
                        await repoCreate.CallerAsync(repoCreate, new object[] { persist });
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
                if (graph.HasChildGraph(modelPropertyInfo.Name))
                {
                    //var task = Task.Run(async () =>
                    //{
                    var relatedType = modelPropertyInfo.InnerType;

                    var relatedModelInfo = ModelAnalyzer.GetModel(relatedType);

                    if (!relatedModelInfo.TryGetProperty(modelPropertyInfo.ForeignIdentity, out ModelPropertyDetail relatedForeignIdentityPropertyInfo))
                        throw new Exception(String.Format("Missing ForeignIdentity {0} for {1} defined in {2}", modelPropertyInfo.ForeignIdentity, relatedModelInfo.Name, ModelDetail.Name));

                    var relatedGraph = graph.GetChildGraphNotNull(modelPropertyInfo.Name, relatedType);
                    var relatedGraphType = TypeAnalyzer.GetGenericTypeDetail(graphType, relatedType);

                    var relatedModelsExisting = (IList)TypeAnalyzer.GetType(TypeAnalyzer.GetGenericType(listType, relatedType)).Creator();

                    var queryExpressionParameter = Expression.Parameter(relatedType, "x");
                    var queryExpression = Expression.Lambda(Expression.Equal(Expression.MakeMemberAccess(queryExpressionParameter, relatedForeignIdentityPropertyInfo.MemberInfo), Expression.Constant(id, relatedForeignIdentityPropertyInfo.Type)), queryExpressionParameter);
                    var relatedIdentityPropertyNames = ModelAnalyzer.GetIdentityPropertyNames(relatedType);

                    var allNames = relatedIdentityPropertyNames.Concat(new string[] { modelPropertyInfo.ForeignIdentity }).ToArray();
                    var queryGraph = relatedGraphType.GetConstructor(GraphParameterTypes).Creator(new object[] { allNames });

                    var queryGeneric = TypeAnalyzer.GetGenericTypeDetail(queryManyType, relatedType);
                    var queryParameterTypes = GetQueryManyParameterTypes(relatedType);
                    var query = queryGeneric.GetConstructor(queryParameterTypes).Creator(new object[] { queryExpression, queryGraph });

                    var repoQueryMany = TypeAnalyzer.GetGenericMethod(repoQueryAsyncManyGeneric, relatedType);
                    var relatedExistings = (ICollection)await repoQueryMany.CallerAsync(repoQueryMany, new object[] { query });

                    foreach (var relatedExisting in relatedExistings)
                        relatedModelsExisting.Add(relatedExisting);

                    if (relatedModelsExisting.Count > 0)
                    {
                        var persistGeneric = TypeAnalyzer.GetGenericTypeDetail(DeleteType, relatedType);
                        var persistParameterTypes = GetPersistEnumerableParameterTypes(relatedType);
                        var persist = persistGeneric.GetConstructor(persistParameterTypes).Creator(new object[] { @event, relatedModelsExisting, relatedGraph });
                        var repoDelete = TypeAnalyzer.GetGenericMethod(repoPersistAsyncGeneric, relatedType);
                        await repoDelete.CallerAsync(repoDelete, new object[] { persist });
                    }
                    //});
                    //tasks.Add(task);
                }
            }

            //if (tasks.Count == 0)
            //    return Task.CompletedTask;

            //return Task.WhenAll(tasks.ToArray());
        }

        protected abstract void PersistModel(PersistEvent @event, TModel model, Graph<TModel> graph, bool create);
        protected abstract void DeleteModel(PersistEvent @event, object[] ids);

        protected abstract Task PersistModelAsync(PersistEvent @event, TModel model, Graph<TModel> graph, bool create);
        protected abstract Task DeleteModelAsync(PersistEvent @event, object[] ids);
    }
}