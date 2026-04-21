// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Zerra.Linq;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Abstract layer provider that applies filtering rules and lifecycle hooks (OnQuery, OnGet, OnCreate, OnUpdate, OnDelete) around the next provider in the chain.
    /// </summary>
    /// <typeparam name="TNextProviderInterface">The type of the next provider in the chain.</typeparam>
    /// <typeparam name="TModel">The model type managed by this provider.</typeparam>
    public abstract class BaseTransactStoreRuleProvider<TNextProviderInterface, TModel> : BaseTransactStoreLayerProvider<TNextProviderInterface, TModel>
        where TNextProviderInterface : ITransactStoreProvider<TModel>
        where TModel : class, new()
    {
        /// <summary>Initializes a new instance of <see cref="BaseTransactStoreRuleProvider{TNextProviderInterface, TModel}"/> with the next provider in the chain.</summary>
        /// <param name="nextProvider">The next provider to delegate operations to.</param>
        public BaseTransactStoreRuleProvider(TNextProviderInterface nextProvider)
            : base(nextProvider) { }

        private LambdaExpression? AppendWhereExpression(LambdaExpression? whereExpression, Graph? graph)
        {
            var appendWhereExpression = WhereExpression(graph);
            if (whereExpression is null && appendWhereExpression is null)
            {
                return null;
            }
            else if (whereExpression is null)
            {
                return appendWhereExpression;
            }
            else if (appendWhereExpression is null)
            {
                return whereExpression;
            }
            else
            {
                return LinqAppender.AppendAnd<TModel>(whereExpression, appendWhereExpression);
            }
        }

        /// <summary>Returns an additional where expression to append to all queries. Override to apply custom filtering rules.</summary>
        /// <param name="graph">The graph for the current query.</param>
        /// <returns>A lambda expression to AND with the query where clause, or <see langword="null"/> for no additional filter.</returns>
        public virtual LambdaExpression? WhereExpression(Graph? graph)
        {
            return null;
        }
        /// <summary>Returns the where expression for this provider layer.</summary>
        /// <param name="graph">The graph for the current query.</param>
        /// <returns>A lambda where expression, or <see langword="null"/> if none applies.</returns>
        public LambdaExpression? GetWhereExpression(Graph? graph)
        {
            return this.WhereExpression(graph);
        }
        /// <summary>Returns the combined where expression for this provider and all base providers in the chain.</summary>
        /// <param name="graph">The graph for the current query.</param>
        /// <returns>A combined lambda where expression, or <see langword="null"/> if none applies.</returns>
        /// <inheritdoc/>
        public override sealed LambdaExpression? GetWhereExpressionIncludingBase(Graph? graph)
        {
            var expression1 = this.GetWhereExpression(graph);
            var expression2 = ProviderRelation?.GetWhereExpressionIncludingBase(graph);
            if (expression1 is null && expression2 is null)
            {
                return null;
            }
            else if (expression1 is null)
            {
                return expression2;
            }
            else if (expression2 is null)
            {
                return expression1;
            }
            else
            {
                return LinqAppender.AppendAnd<TModel>(expression1, expression2);
            }
        }

        /// <summary>Called before a query is executed. Override to modify the graph or apply side effects.</summary>
        /// <param name="graph">The graph for the current query.</param>
        public virtual void OnQuery(Graph? graph) { }
        /// <summary>Invokes <see cref="OnQuery"/> on this provider then propagates to the next provider in the chain.</summary>
        /// <param name="graph">The graph for the current query.</param>
        /// <inheritdoc/>
        public override sealed void OnQueryIncludingBase(Graph? graph)
        {
            this.OnQuery(graph);
            ProviderRelation?.OnQueryIncludingBase(graph);
        }

        /// <summary>Called after models are retrieved. Override to filter or transform the result set.</summary>
        /// <param name="models">The retrieved models.</param>
        /// <param name="graph">The graph used during the query.</param>
        /// <returns>The filtered or transformed models.</returns>
        public virtual IEnumerable OnGet(IEnumerable models, Graph? graph) { return models; }
        /// <inheritdoc/>
        public override sealed async Task<IEnumerable> OnGetIncludingBaseAsync(IEnumerable models, Graph? graph)
        {
            if (ProviderRelation is not null)
            {
                var returnModels1 = await ProviderRelation.OnGetIncludingBaseAsync(models, graph);
                var returnModels2 = this.OnGet(returnModels1, graph);
                return returnModels2;
            }
            else
            {
                var returnModels1 = this.OnGet(models, graph);
                return returnModels1;
            }
        }

        /// <inheritdoc/>
        public override sealed async Task<object?> ManyAsync(Query query)
        {
            var appenedQuery = new Query(query);

            OnQuery(appenedQuery.Graph);
            var where = AppendWhereExpression(appenedQuery.Where, appenedQuery.Graph);

            appenedQuery.Where = where;

            var models = (IReadOnlyCollection<TModel>)(await NextProvider.QueryAsync(appenedQuery))!;

            IEnumerable returnModels;
            if (models.Count > 0)
            {
                returnModels = OnGet(models, appenedQuery.Graph);
            }
            else
            {
                returnModels = Array.Empty<TModel>();
            }

            return returnModels;
        }

        /// <inheritdoc/>
        public override sealed async Task<object?> FirstAsync(Query query)
        {
            var appenedQuery = new Query(query);

            OnQuery(appenedQuery.Graph);
            var where = AppendWhereExpression(appenedQuery.Where, appenedQuery.Graph);

            appenedQuery.Where = where;

            var model = (TModel?)await NextProvider.QueryAsync(appenedQuery);

            TModel? returnModel = null;

            if (model is not null)
            {
                returnModel = OnGet(new TModel[] { model }, appenedQuery.Graph).Cast<TModel>().SingleOrDefault();
            }

            return returnModel;
        }

        /// <inheritdoc/>
        public override sealed async Task<object?> SingleAsync(Query query)
        {
            var appenedQuery = new Query(query);

            OnQuery(appenedQuery.Graph);

            var where = AppendWhereExpression(appenedQuery.Where, appenedQuery.Graph);
            appenedQuery.Where = where;

            var model = (TModel?)await NextProvider.QueryAsync(appenedQuery);

            TModel? returnModel = null;

            if (model is not null)
            {
                returnModel = OnGet(new TModel[] { model }, appenedQuery.Graph).Cast<TModel>().SingleOrDefault();
            }

            return returnModel;
        }

        /// <inheritdoc/>
        public override sealed Task<object?> CountAsync(Query query)
        {
            var appenedQuery = new Query(query);

            OnQuery(appenedQuery.Graph);

            var where = AppendWhereExpression(appenedQuery.Where, appenedQuery.Graph);
            appenedQuery.Where = where;

            var count = NextProvider.QueryAsync(appenedQuery);

            return count;
        }

        /// <inheritdoc/>
        public override sealed Task<object?> AnyAsync(Query query)
        {
            var appenedQuery = new Query(query);

            OnQuery(appenedQuery.Graph);

            var where = AppendWhereExpression(appenedQuery.Where, appenedQuery.Graph);
            appenedQuery.Where = where;

            var any = NextProvider.QueryAsync(appenedQuery);

            return any;
        }

        /// <inheritdoc/>
        public override sealed async Task<object?> EventManyAsync(Query query)
        {
            var appenedQuery = new Query(query);

            OnQuery(appenedQuery.Graph);

            var where = AppendWhereExpression(appenedQuery.Where, appenedQuery.Graph);
            appenedQuery.Where = where;

            var eventModels = (ICollection<EventModel<TModel>>)(await NextProvider.QueryAsync(appenedQuery))!;

            var models = eventModels.Select(x => x.Model).ToArray();

            ICollection<EventModel<TModel>>? returnEventModels = null;
            if (models.Length > 0)
            {
                var returnModels = OnGet(models, appenedQuery.Graph).Cast<TModel>();
                returnEventModels = eventModels.Where(x => returnModels.Contains(x.Model)).ToArray();
            }
            else
            {
                returnEventModels = Array.Empty<EventModel<TModel>>();
            }

            return returnEventModels;
        }

        /// <summary>Called before models are created. Override to validate or transform the models prior to persistence.</summary>
        /// <param name="models">The models to be created.</param>
        /// <param name="graph">The graph specifying which members are being persisted.</param>
        /// <returns>The models to pass to the next provider.</returns>
        protected virtual IEnumerable OnCreate(IEnumerable models, Graph? graph) { return models; }
        /// <summary>Called before models are updated. Override to validate or transform the models prior to persistence.</summary>
        /// <param name="models">The models to be updated.</param>
        /// <param name="graph">The graph specifying which members are being persisted.</param>
        /// <returns>The models to pass to the next provider.</returns>
        protected virtual IEnumerable OnUpdate(IEnumerable models, Graph? graph) { return models; }
        /// <summary>Called before models are deleted. Override to validate or filter the identities prior to deletion.</summary>
        /// <param name="identities">The identities of the models to be deleted.</param>
        /// <returns>The identities to pass to the next provider.</returns>
        protected virtual ICollection OnDelete(ICollection identities) { return identities; }

        /// <summary>Called after models have been successfully created. Override to perform post-create side effects.</summary>
        /// <param name="models">The models that were created.</param>
        /// <param name="graph">The graph used during persistence.</param>
        protected virtual void OnCreateComplete(IEnumerable models, Graph? graph) { }
        /// <summary>Called after models have been successfully updated. Override to perform post-update side effects.</summary>
        /// <param name="models">The models that were updated.</param>
        /// <param name="graph">The graph used during persistence.</param>
        protected virtual void OnUpdateComplete(IEnumerable models, Graph? graph) { }
        /// <summary>Called after models have been successfully deleted. Override to perform post-delete side effects.</summary>
        /// <param name="identities">The identities of the models that were deleted.</param>
        protected virtual void OnDeleteComplete(ICollection identities) { }

        /// <inheritdoc/>
        public override sealed async Task CreateAsync(Persist persist)
        {
            if (persist.Models is null)
                throw new Exception($"Invalid {nameof(Persist)} for {nameof(CreateAsync)}");

            var appenedPersist = new Persist(persist);
            var returnModels = OnCreate(appenedPersist.Models!, appenedPersist.Graph);
            await NextProvider.PersistAsync(new Create(appenedPersist.Event, returnModels, appenedPersist.Graph));
            OnCreateComplete(returnModels, appenedPersist.Graph);
        }

        /// <inheritdoc/>
        public override sealed async Task UpdateAsync(Persist persist)
        {
            if (persist.Models is null)
                throw new Exception($"Invalid {nameof(Persist)} for {nameof(UpdateAsync)}");

            var appenedPersist = new Persist(persist);
            var returnModels = OnUpdate(appenedPersist.Models!, appenedPersist.Graph);
            await NextProvider.PersistAsync(new Update(appenedPersist.Event, returnModels, appenedPersist.Graph));
            OnUpdateComplete(returnModels, appenedPersist.Graph);
        }

        /// <inheritdoc/>
        public override sealed async Task DeleteAsync(Persist persist)
        {
            ICollection returnIds;
            if (persist.IDs is not null)
            {
                returnIds = OnDelete(persist.IDs);
            }
            else if (persist.Models is not null)
            {
                var ids = new List<object>();
                foreach (var model in persist.Models)
                {
                    var id = ModelAnalyzer.GetIdentity(modelType, model);
                    if (id is null)
                        throw new Exception($"Model {typeof(TModel).Name} missing Identity");
                    ids.Add(id);
                }

                returnIds = OnDelete(ids);
            }
            else
            {
                throw new Exception($"Invalid {nameof(Persist)} for {nameof(DeleteAsync)}");
            }

            await NextProvider.PersistAsync(new DeleteByID<TModel>(persist.Event, returnIds));
            OnDeleteComplete(returnIds);
        }
    }
}
