// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Zerra.Repository.Linq;
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
            var appendWhereExpression = GetWhereExpressionIncludingBase(graph);
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
        /// <summary>Returns the combined where expression for this provider and all base providers in the chain.</summary>
        /// <param name="graph">The graph for the current query.</param>
        /// <returns>A combined lambda where expression, or <see langword="null"/> if none applies.</returns>
        /// <inheritdoc/>
        public override sealed LambdaExpression? GetWhereExpressionIncludingBase(Graph? graph)
        {
            var expression1 = this.WhereExpression(graph);
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
        public override sealed IEnumerable OnGetIncludingBase(IEnumerable models, Graph? graph)
        {
            if (ProviderRelation is not null)
            {
                var returnModels1 = ProviderRelation.OnGetIncludingBase(models, graph);
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
        public override sealed object? Query(LinqQuery query)
        {
            OnQuery(query.Graph);
            //the query carries its result types, so the appended where is swapped onto it for the call and the original put back
            var where = query.Where;
            query.Where = AppendWhereExpression(where, query.Graph);
            try
            {
                return OnResult(query, NextProvider.Query(query));
            }
            finally
            {
                query.Where = where;
            }
        }
        /// <inheritdoc/>
        public override sealed async Task<object?> QueryAsync(LinqQuery query, CancellationToken cancellationToken)
        {
            OnQuery(query.Graph);
            var where = query.Where;
            query.Where = AppendWhereExpression(where, query.Graph);
            try
            {
                return OnResult(query, await NextProvider.QueryAsync(query, cancellationToken));
            }
            finally
            {
                query.Where = where;
            }
        }
        private object? OnResult(LinqQuery query, object? result)
        {
            switch (result)
            {
                case IReadOnlyCollection<TModel> models when query.ReturnsModels:
                    return models.Count == 0 ? models : OnGet(models, query.Graph).Cast<TModel>().ToArray();
                case TModel model:
                    return OnGet(new TModel[] { model }, query.Graph).Cast<TModel>().FirstOrDefault();
                case ICollection<EventModel<TModel>> eventModels:
                    {
                        if (eventModels.Count == 0)
                            return eventModels;
                        var returnModels = OnGet(eventModels.Select(x => x.Model).ToArray(), query.Graph).Cast<TModel>();
                        return eventModels.Where(x => returnModels.Contains(x.Model)).ToArray();
                    }
                case EventModel<TModel> eventModel:
                    return OnGet(new TModel[] { eventModel.Model }, query.Graph).Cast<TModel>().FirstOrDefault() is null ? null : eventModel;
                default:
                    return result;
            }
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

        /// <summary>Called before models are updated by a condition. The models aren't loaded, so <see cref="OnUpdate"/> isn't called; override to change the condition, or throw to refuse.</summary>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="model">The model holding the values to set.</param>
        /// <param name="graph">The members being set.</param>
        /// <returns>The condition to pass to the next provider.</returns>
        protected virtual LambdaExpression OnUpdateWhere(LambdaExpression where, object model, Graph? graph) { return where; }
        /// <summary>Called before models are updated by a condition to values computed from each one. The models aren't loaded, so <see cref="OnUpdate"/> isn't called; override to change the condition, or throw to refuse.</summary>
        /// <param name="where">The condition selecting the models.</param>
        /// <param name="set">The members being set and their values, as a projection of the model to itself.</param>
        /// <returns>The condition to pass to the next provider.</returns>
        protected virtual LambdaExpression OnUpdateWhere(LambdaExpression where, LambdaExpression set) { return where; }
        /// <summary>Called before models are deleted by a condition. The models aren't loaded, so <see cref="OnDelete"/> isn't called; override to change the condition, or throw to refuse.</summary>
        /// <param name="where">The condition selecting the models.</param>
        /// <returns>The condition to pass to the next provider.</returns>
        protected virtual LambdaExpression OnDeleteWhere(LambdaExpression where) { return where; }
        /// <summary>Called after models have been updated by a condition.</summary>
        /// <param name="where">The condition that selected the models, with this provider's <see cref="WhereExpression"/> added.</param>
        /// <param name="model">The model holding the values set.</param>
        /// <param name="graph">The members set.</param>
        /// <param name="rowsAffected">How many models were updated.</param>
        protected virtual void OnUpdateWhereComplete(LambdaExpression where, object model, Graph? graph, int rowsAffected) { }
        /// <summary>Called after models have been updated by a condition to values computed from each one.</summary>
        /// <param name="where">The condition that selected the models, with this provider's <see cref="WhereExpression"/> added.</param>
        /// <param name="set">The members set and their values, as a projection of the model to itself.</param>
        /// <param name="rowsAffected">How many models were updated.</param>
        protected virtual void OnUpdateWhereComplete(LambdaExpression where, LambdaExpression set, int rowsAffected) { }
        /// <summary>Called after models have been deleted by a condition.</summary>
        /// <param name="where">The condition that selected the models, with this provider's <see cref="WhereExpression"/> added.</param>
        /// <param name="rowsAffected">How many models were deleted.</param>
        protected virtual void OnDeleteWhereComplete(LambdaExpression where, int rowsAffected) { }

        /// <inheritdoc/>
        public override sealed void Create(Persist persist)
        {
            if (persist.Models is null)
                throw new Exception($"Invalid {nameof(Persist)} for {nameof(CreateAsync)}");

            var appenedPersist = new Persist(persist);
            var returnModels = OnCreate(appenedPersist.Models!, appenedPersist.Graph);
            NextProvider.Persist(new Persist(appenedPersist.Operation, appenedPersist.Event, appenedPersist.ModelType, returnModels.Cast<object>().ToArray(), null, appenedPersist.Graph));
            OnCreateComplete(returnModels, appenedPersist.Graph);
        }

        /// <inheritdoc/>
        public override sealed void Update(Persist persist)
        {
            if (persist.Set is not null)
            {
                var where = AppendWhereExpression(OnUpdateWhere(persist.Where!, persist.Set), null)!;
                var forwarded = new Persist(persist.Operation, persist.Event, persist.ModelType, where, persist.Set);
                NextProvider.Persist(forwarded);
                persist.RowsAffected = forwarded.RowsAffected;
                OnUpdateWhereComplete(where, persist.Set, forwarded.RowsAffected);
                return;
            }
            if (persist.Where is not null)
            {
                var model = persist.Models is { Length: 1 } ? persist.Models[0] : throw new Exception($"Invalid {nameof(Persist)} for {nameof(Update)}");
                var where = AppendWhereExpression(OnUpdateWhere(persist.Where, model, persist.Graph), null)!;
                var forwarded = new Persist(persist.Operation, persist.Event, persist.ModelType, where, model, persist.Graph);
                NextProvider.Persist(forwarded);
                persist.RowsAffected = forwarded.RowsAffected;
                OnUpdateWhereComplete(where, model, persist.Graph, forwarded.RowsAffected);
                return;
            }

            if (persist.Models is null)
                throw new Exception($"Invalid {nameof(Persist)} for {nameof(UpdateAsync)}");

            var appenedPersist = new Persist(persist);
            var returnModels = OnUpdate(appenedPersist.Models!, appenedPersist.Graph);
            NextProvider.Persist(new Persist(appenedPersist.Operation, appenedPersist.Event, appenedPersist.ModelType, returnModels.Cast<object>().ToArray(), null, appenedPersist.Graph));
            OnUpdateComplete(returnModels, appenedPersist.Graph);
        }

        /// <inheritdoc/>
        public override sealed void Delete(Persist persist)
        {
            if (persist.Where is not null)
            {
                var where = AppendWhereExpression(OnDeleteWhere(persist.Where), null)!;
                var forwarded = new Persist(persist.Operation, persist.Event, persist.ModelType, where, null, null);
                NextProvider.Persist(forwarded);
                persist.RowsAffected = forwarded.RowsAffected;
                OnDeleteWhereComplete(where, forwarded.RowsAffected);
                return;
            }

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

            NextProvider.Persist(new Persist<TModel>(PersistOperation.Delete, persist.Event, null, returnIds.Cast<object>().ToArray(), persist.Graph));
            OnDeleteComplete(returnIds);
        }

        /// <inheritdoc/>
        public override sealed async Task CreateAsync(Persist persist, CancellationToken cancellationToken)
        {
            if (persist.Models is null)
                throw new Exception($"Invalid {nameof(Persist)} for {nameof(CreateAsync)}");

            var appenedPersist = new Persist(persist);
            var returnModels = OnCreate(appenedPersist.Models!, appenedPersist.Graph);
            await NextProvider.PersistAsync(new Persist(appenedPersist.Operation, appenedPersist.Event, appenedPersist.ModelType, returnModels.Cast<object>().ToArray(), null, appenedPersist.Graph), cancellationToken);
            OnCreateComplete(returnModels, appenedPersist.Graph);
        }

        /// <inheritdoc/>
        public override sealed async Task UpdateAsync(Persist persist, CancellationToken cancellationToken)
        {
            if (persist.Set is not null)
            {
                var where = AppendWhereExpression(OnUpdateWhere(persist.Where!, persist.Set), null)!;
                var forwarded = new Persist(persist.Operation, persist.Event, persist.ModelType, where, persist.Set);
                await NextProvider.PersistAsync(forwarded, cancellationToken);
                persist.RowsAffected = forwarded.RowsAffected;
                OnUpdateWhereComplete(where, persist.Set, forwarded.RowsAffected);
                return;
            }
            if (persist.Where is not null)
            {
                var model = persist.Models is { Length: 1 } ? persist.Models[0] : throw new Exception($"Invalid {nameof(Persist)} for {nameof(UpdateAsync)}");
                var where = AppendWhereExpression(OnUpdateWhere(persist.Where, model, persist.Graph), null)!;
                var forwarded = new Persist(persist.Operation, persist.Event, persist.ModelType, where, model, persist.Graph);
                await NextProvider.PersistAsync(forwarded, cancellationToken);
                persist.RowsAffected = forwarded.RowsAffected;
                OnUpdateWhereComplete(where, model, persist.Graph, forwarded.RowsAffected);
                return;
            }

            if (persist.Models is null)
                throw new Exception($"Invalid {nameof(Persist)} for {nameof(UpdateAsync)}");

            var appenedPersist = new Persist(persist);
            var returnModels = OnUpdate(appenedPersist.Models!, appenedPersist.Graph);
            await NextProvider.PersistAsync(new Persist(appenedPersist.Operation, appenedPersist.Event, appenedPersist.ModelType, returnModels.Cast<object>().ToArray(), null, appenedPersist.Graph), cancellationToken);
            OnUpdateComplete(returnModels, appenedPersist.Graph);
        }

        /// <inheritdoc/>
        public override sealed async Task DeleteAsync(Persist persist, CancellationToken cancellationToken)
        {
            if (persist.Where is not null)
            {
                var where = AppendWhereExpression(OnDeleteWhere(persist.Where), null)!;
                var forwarded = new Persist(persist.Operation, persist.Event, persist.ModelType, where, null, null);
                await NextProvider.PersistAsync(forwarded, cancellationToken);
                persist.RowsAffected = forwarded.RowsAffected;
                OnDeleteWhereComplete(where, forwarded.RowsAffected);
                return;
            }

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

            await NextProvider.PersistAsync(new Persist<TModel>(PersistOperation.Delete, persist.Event, null, returnIds.Cast<object>().ToArray(), persist.Graph), cancellationToken);
            OnDeleteComplete(returnIds);
        }
    }
}
