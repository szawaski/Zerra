// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Zerra.Linq;
using Zerra.Providers;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract class BaseTransactStoreRuleProvider<TNextProviderInterface, TModel> : BaseTransactStoreLayerProvider<TNextProviderInterface, TModel>, IRuleProvider
        where TNextProviderInterface : ITransactStoreProvider<TModel>
        where TModel : class, new()
    {
        private Expression? AppendWhereExpression(Expression? whereExpression, Graph? graph)
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

        public virtual Expression? WhereExpression(Graph? graph)
        {
            return null;
        }
        public Expression? GetWhereExpression(Graph? graph)
        {
            return this.WhereExpression(graph);
        }
        public override sealed Expression? GetWhereExpressionIncludingBase(Graph? graph)
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

        public virtual void OnQuery(Graph? graph) { }
        public override sealed void OnQueryIncludingBase(Graph? graph)
        {
            this.OnQuery(graph);
            ProviderRelation?.OnQueryIncludingBase(graph);
        }

        public virtual IEnumerable OnGet(IEnumerable models, Graph? graph) { return models; }
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
                returnModel = OnGet(new TModel[] { model }, appenedQuery.Graph).SingleOrDefault();
            }

            return returnModel;
        }

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
                returnModel = OnGet(new TModel[] { model }, appenedQuery.Graph).SingleOrDefault();
            }

            return returnModel;
        }

        public override sealed Task<object?> CountAsync(Query query)
        {
            var appenedQuery = new Query(query);

            OnQuery(appenedQuery.Graph);

            var where = AppendWhereExpression(appenedQuery.Where, appenedQuery.Graph);
            appenedQuery.Where = where;

            var count = NextProvider.QueryAsync(appenedQuery);

            return count;
        }

        public override sealed Task<object?> AnyAsync(Query query)
        {
            var appenedQuery = new Query(query);

            OnQuery(appenedQuery.Graph);

            var where = AppendWhereExpression(appenedQuery.Where, appenedQuery.Graph);
            appenedQuery.Where = where;

            var any = NextProvider.QueryAsync(appenedQuery);

            return any;
        }

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
                var returnModels = OnGet(models, appenedQuery.Graph);
                returnEventModels = eventModels.Where(x => returnModels.Contains(x.Model)).ToArray();
            }
            else
            {
                returnEventModels = Array.Empty<EventModel<TModel>>();
            }

            return returnEventModels;
        }

        protected virtual IEnumerable OnCreate(IEnumerable models, Graph? graph) { return models; }
        protected virtual IEnumerable OnUpdate(IEnumerable models, Graph? graph) { return models; }
        protected virtual ICollection OnDelete(ICollection identities) { return identities; }

        protected virtual void OnCreateComplete(IEnumerable models, Graph? graph) { }
        protected virtual void OnUpdateComplete(IEnumerable models, Graph? graph) { }
        protected virtual void OnDeleteComplete(ICollection identities) { }

        public override sealed async Task CreateAsync(Persist persist)
        {
            if (persist.Models is null)
                throw new Exception($"Invalid {nameof(Persist)} for {nameof(CreateAsync)}");

            var appenedPersist = new Persist(persist);
            var returnModels = OnCreate(appenedPersist.Models!, appenedPersist.Graph);
            await NextProvider.PersistAsync(new Create(appenedPersist.Event, returnModels, appenedPersist.Graph));
            OnCreateComplete(returnModels, appenedPersist.Graph);
        }

        public override sealed async Task UpdateAsync(Persist persist)
        {
            if (persist.Models is null)
                throw new Exception($"Invalid {nameof(Persist)} for {nameof(UpdateAsync)}");

            var appenedPersist = new Persist(persist);
            var returnModels = OnUpdate(appenedPersist.Models!, appenedPersist.Graph);
            await NextProvider.PersistAsync(new Update(appenedPersist.Event, returnModels, appenedPersist.Graph));
            OnUpdateComplete(returnModels, appenedPersist.Graph);
        }

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
