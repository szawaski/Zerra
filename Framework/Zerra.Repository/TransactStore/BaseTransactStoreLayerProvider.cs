// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    public abstract class BaseTransactStoreLayerProvider<TNextProviderInterface, TModel> : LayerProvider<TNextProviderInterface>, ITransactStoreProvider<TModel>, IProviderRelation
        where TNextProviderInterface : ITransactStoreProvider<TModel>
        where TModel : class, new()
    {
        protected static readonly Type modelType = typeof(TModel);

        protected IProviderRelation<TModel>? ProviderRelation = null;

        public BaseTransactStoreLayerProvider(TNextProviderInterface nextProvider)
            : base(nextProvider)
        {
            ProviderRelation = NextProvider as IProviderRelation<TModel>;
        }

        public Type ModelType => modelType;

        public virtual LambdaExpression? GetWhereExpressionIncludingBase(Graph? graph)
        {
            var expression = ProviderRelation?.GetWhereExpressionIncludingBase(graph);
            return expression;
        }

        public virtual void OnQueryIncludingBase(Graph? graph)
        {
            ProviderRelation?.OnQueryIncludingBase(graph);
        }

        public virtual IEnumerable OnGetIncludingBase(IEnumerable models, Graph? graph)
        {
            if (ProviderRelation is null)
                return models;
            return ProviderRelation.OnGetIncludingBase(models, graph);
        }

        public virtual Task<IEnumerable> OnGetIncludingBaseAsync(IEnumerable models, Graph? graph)
        {
            if (ProviderRelation is null)
                return Task.FromResult(models);
            return ProviderRelation.OnGetIncludingBaseAsync(models, graph);
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
                _ => throw new NotImplementedException(),
            };
            ;
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
                _ => throw new NotImplementedException(),
            };
            ;
        }

        public abstract object Many(Query query);
        public abstract object? First(Query query);
        public abstract object? Single(Query query);
        public abstract object Count(Query query);
        public abstract object Any(Query query);
        public abstract object EventMany(Query query);

        public abstract Task<object?> ManyAsync(Query query);
        public abstract Task<object?> FirstAsync(Query query);
        public abstract Task<object?> SingleAsync(Query query);
        public abstract Task<object?> CountAsync(Query query);
        public abstract Task<object?> AnyAsync(Query query);
        public abstract Task<object?> EventManyAsync(Query query);

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

        public abstract void Create(Persist persist);
        public abstract void Update(Persist persist);
        public abstract void Delete(Persist persist);

        public abstract Task CreateAsync(Persist persist);
        public abstract Task UpdateAsync(Persist persist);
        public abstract Task DeleteAsync(Persist persist);
    }
}
