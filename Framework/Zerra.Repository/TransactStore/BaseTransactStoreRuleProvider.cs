// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Zerra.Providers;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract class BaseTransactStoreRuleProvider<TNextProviderInterface, TModel> : BaseTransactStoreLayerProvider<TNextProviderInterface, TModel>, IRuleProvider
        where TNextProviderInterface : ITransactStoreProvider<TModel>
        where TModel : class, new()
    {
        private Expression<Func<TModel, bool>>? AppendWhereExpression(Expression<Func<TModel, bool>>? whereExpression, Graph<TModel>? graph)
        {
            var appendWhereExpression = WhereExpression(graph);
            if (whereExpression == null && appendWhereExpression == null)
            {
                return null;
            }
            else if (whereExpression == null)
            {
                return appendWhereExpression;
            }
            else if (appendWhereExpression == null)
            {
                return whereExpression;
            }
            else
            {
                return whereExpression.AppendAnd(appendWhereExpression);
            }
        }

        public virtual Expression<Func<TModel, bool>>? WhereExpression(Graph<TModel>? graph)
        {
            return null;
        }
        public Expression<Func<TModel, bool>>? GetWhereExpression(Graph<TModel>? graph)
        {
            return this.WhereExpression(graph);
        }
        public override sealed Expression<Func<TModel, bool>>? GetWhereExpressionIncludingBase(Graph<TModel>? graph)
        {
            var expression1 = this.GetWhereExpression(graph);
            var expression2 = ProviderRelation?.GetWhereExpressionIncludingBase(graph);
            if (expression1 == null && expression2 == null)
            {
                return null;
            }
            else if (expression1 == null)
            {
                return expression2;
            }
            else if (expression2 == null)
            {
                return expression1;
            }
            else
            {
                return expression1.AppendAnd(expression2);
            }
        }

        public virtual void OnQuery(Graph<TModel>? graph) { }
        public override sealed void OnQueryIncludingBase(Graph<TModel>? graph)
        {
            this.OnQuery(graph);
            ProviderRelation?.OnQueryIncludingBase(graph);
        }

        public virtual ICollection<TModel> OnGet(ICollection<TModel> models, Graph<TModel>? graph) { return models; }
        public override sealed async Task<ICollection<TModel>> OnGetIncludingBaseAsync(ICollection<TModel> models, Graph<TModel>? graph)
        {
            if (ProviderRelation != null)
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

        public override sealed async Task<object?> ManyAsync(Query<TModel> query)
        {
            var appenedQuery = new Query<TModel>(query);

            OnQuery(appenedQuery.Graph);
            var where = AppendWhereExpression(appenedQuery.Where, appenedQuery.Graph);

            appenedQuery.Where = where;

            var models = (ICollection<TModel>)(await NextProvider.QueryAsync(appenedQuery))!;

            ICollection<TModel> returnModels;
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

        public override sealed async Task<object?> FirstAsync(Query<TModel> query)
        {
            var appenedQuery = new Query<TModel>(query);

            OnQuery(appenedQuery.Graph);
            var where = AppendWhereExpression(appenedQuery.Where, appenedQuery.Graph);

            appenedQuery.Where = where;

            var model = (TModel?)await NextProvider.QueryAsync(appenedQuery);

            TModel? returnModel = null;

            if (model != null)
            {
                returnModel = OnGet(new TModel[] { model }, appenedQuery.Graph).SingleOrDefault();
            }

            return returnModel;
        }

        public override sealed async Task<object?> SingleAsync(Query<TModel> query)
        {
            var appenedQuery = new Query<TModel>(query);

            OnQuery(appenedQuery.Graph);

            var where = AppendWhereExpression(appenedQuery.Where, appenedQuery.Graph);
            appenedQuery.Where = where;

            var model = (TModel?)await NextProvider.QueryAsync(appenedQuery);

            TModel? returnModel = null;

            if (model != null)
            {
                returnModel = OnGet(new TModel[] { model }, appenedQuery.Graph).SingleOrDefault();
            }

            return returnModel;
        }

        public override sealed Task<object?> CountAsync(Query<TModel> query)
        {
            var appenedQuery = new Query<TModel>(query);

            OnQuery(appenedQuery.Graph);

            var where = AppendWhereExpression(appenedQuery.Where, appenedQuery.Graph);
            appenedQuery.Where = where;

            var count = NextProvider.QueryAsync(appenedQuery);

            return count;
        }

        public override sealed Task<object?> AnyAsync(Query<TModel> query)
        {
            var appenedQuery = new Query<TModel>(query);

            OnQuery(appenedQuery.Graph);

            var where = AppendWhereExpression(appenedQuery.Where, appenedQuery.Graph);
            appenedQuery.Where = where;

            var any = NextProvider.QueryAsync(appenedQuery);

            return any;
        }

        public override sealed async Task<object?> EventManyAsync(Query<TModel> query)
        {
            var appenedQuery = new Query<TModel>(query);

            OnQuery(appenedQuery.Graph);

            var where = AppendWhereExpression(appenedQuery.Where, appenedQuery.Graph);
            appenedQuery.Where = where;

            var eventModels = (ICollection<EventModel<TModel>>)(await NextProvider.QueryAsync(appenedQuery))!;

            var models = eventModels.Select(x => x.Model).ToArray();

            ICollection<EventModel<TModel>>? returnEventModels = null;
            if (models.Length > 0)
            {
                var returnModels = OnGet(models, appenedQuery.Graph);
                returnEventModels = eventModels.Where(x => returnModels.Contains(x.Model)).ToArray();
            }
            else
            {
                returnEventModels = Array.Empty<EventModel<TModel>>();
            }

            return returnEventModels;
        }

        protected virtual ICollection<TModel> OnCreate(ICollection<TModel> models, Graph<TModel>? graph) { return models; }
        protected virtual ICollection<TModel> OnUpdate(ICollection<TModel> models, Graph<TModel>? graph) { return models; }
        protected virtual ICollection OnDelete(ICollection identities) { return identities; }

        protected virtual void OnCreateComplete(ICollection<TModel> models, Graph<TModel>? graph) { }
        protected virtual void OnUpdateComplete(ICollection<TModel> models, Graph<TModel>? graph) { }
        protected virtual void OnDeleteComplete(ICollection identities) { }

        public override sealed async Task CreateAsync(Persist<TModel> persist)
        {
            if (persist.Models == null)
                throw new Exception($"Invalid {nameof(Persist<TModel>)} for {nameof(CreateAsync)}");

            var appenedPersist = new Persist<TModel>(persist);
            var returnModels = OnCreate(appenedPersist.Models!, appenedPersist.Graph);
            await NextProvider.PersistAsync(new Create<TModel>(appenedPersist.Event, returnModels, appenedPersist.Graph));
            OnCreateComplete(returnModels, appenedPersist.Graph);
        }

        public override sealed async Task UpdateAsync(Persist<TModel> persist)
        {
            if (persist.Models == null)
                throw new Exception($"Invalid {nameof(Persist<TModel>)} for {nameof(UpdateAsync)}");

            var appenedPersist = new Persist<TModel>(persist);
            var returnModels = OnUpdate(appenedPersist.Models!, appenedPersist.Graph);
            await NextProvider.PersistAsync(new Update<TModel>(appenedPersist.Event, returnModels, appenedPersist.Graph));
            OnUpdateComplete(returnModels, appenedPersist.Graph);
        }

        public override sealed async Task DeleteAsync(Persist<TModel> persist)
        {
            ICollection returnIds;
            if (persist.IDs != null)
            {
                returnIds = OnDelete(persist.IDs);
            }
            else if (persist.Models != null)
            {
                var ids = new List<object>();
                foreach (var model in persist.Models)
                {
                    var id = ModelAnalyzer.GetIdentity(model);
                    if (id == null)
                        throw new Exception($"Model {typeof(TModel).GetNiceName()} is missing an identiy for {nameof(DeleteAsync)}");
                    ids.Add(id);
                }

                returnIds = OnDelete(ids);
            }
            else
            {
                throw new Exception($"Invalid {nameof(Persist<TModel>)} for {nameof(DeleteAsync)}");
            }

            await NextProvider.PersistAsync(new DeleteByID<TModel>(persist.Event, returnIds));
            OnDeleteComplete(returnIds);
        }
    }
}
