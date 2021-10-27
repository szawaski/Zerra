// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Zerra.Providers;

namespace Zerra.Repository
{
    public abstract class BaseDataLayerProvider<TNextProviderInterface, TModel> : BaseLayerProvider<TNextProviderInterface>, IDataProvider<TModel>, IProviderRelation<TModel>
        where TNextProviderInterface : IDataProvider<TModel>
        where TModel : class, new()
    {
        protected IProviderRelation<TModel> ProviderRelation = null;

        public BaseDataLayerProvider()
            : base()
        {
            ProviderRelation = NextProvider as IProviderRelation<TModel>;
        }

        public Expression GetWhereExpressionIncludingBase(Graph graph)
        {
            return GetWhereExpressionIncludingBase((Graph<TModel>)graph);
        }
        public virtual Expression<Func<TModel, bool>> GetWhereExpressionIncludingBase(Graph<TModel> graph)
        {
            Expression<Func<TModel, bool>> expression = ProviderRelation.GetWhereExpressionIncludingBase(graph);
            return expression;
        }

        public void OnQueryIncludingBase(Graph graph)
        {
            OnQueryIncludingBase((Graph<TModel>)graph);
        }
        public virtual void OnQueryIncludingBase(Graph<TModel> graph)
        {
            ProviderRelation.OnQueryIncludingBase(graph);
        }

        public ICollection OnGetIncludingBase(ICollection models, Graph graph)
        {
            return (ICollection)OnGetIncludingBase((ICollection<TModel>)(object)models, (Graph<TModel>)graph);
        }
        public virtual ICollection<TModel> OnGetIncludingBase(ICollection<TModel> models, Graph<TModel> graph)
        {
            return ProviderRelation.OnGetIncludingBase(models, graph);
        }

        public async Task<ICollection> OnGetIncludingBaseAsync(ICollection models, Graph graph)
        {
            return (ICollection)await OnGetIncludingBaseAsync((ICollection<TModel>)models, (Graph<TModel>)graph);
        }
        public virtual Task<ICollection<TModel>> OnGetIncludingBaseAsync(ICollection<TModel> models, Graph<TModel> graph)
        {
            return ProviderRelation.OnGetIncludingBaseAsync(models, graph);
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
                _ => throw new NotImplementedException(),
            };
            ;
        }

        public abstract object Many(Query<TModel> query);
        public abstract object First(Query<TModel> query);
        public abstract object Single(Query<TModel> query);
        public abstract object Count(Query<TModel> query);
        public abstract object Any(Query<TModel> query);
        public abstract object EventMany(Query<TModel> query);

        public abstract Task<object> ManyAsync(Query<TModel> query);
        public abstract Task<object> FirstAsync(Query<TModel> query);
        public abstract Task<object> SingleAsync(Query<TModel> query);
        public abstract Task<object> CountAsync(Query<TModel> query);
        public abstract Task<object> AnyAsync(Query<TModel> query);
        public abstract Task<object> EventManyAsync(Query<TModel> query);

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

        public abstract void Create(Persist<TModel> persist);
        public abstract void Update(Persist<TModel> persist);
        public abstract void Delete(Persist<TModel> persist);

        public abstract Task CreateAsync(Persist<TModel> persist);
        public abstract Task UpdateAsync(Persist<TModel> persist);
        public abstract Task DeleteAsync(Persist<TModel> persist);
    }
}
