// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Threading.Tasks;
using Zerra.Providers;
using Zerra.Reflection;

namespace Zerra.Repository
{
    public abstract class BaseDualTransactStoreProvider<TThisProviderInterface, TNextProviderInterface, TModel> : BaseLayerProvider<TNextProviderInterface>, IDualBaseProvider, ITransactStoreProvider<TModel>
        where TThisProviderInterface : ITransactStoreProvider<TModel>
        where TNextProviderInterface : ITransactStoreProvider<TModel>
        where TModel : class, new()
    {
        protected ITransactStoreProvider<TModel> ThisProvider
        {
            get
            {
                var context = Instantiator.CreateInstance<TThisProviderInterface>();
                return context;
            }
        }

        public object Query(Query<TModel> query)
        {
            if (query.IsTemporal)
            {
                return ThisProvider.Query(query);
            }
            else
            {
                return NextProvider.Query(query);
            }
        }

        public Task<object> QueryAsync(Query<TModel> query)
        {
            if (query.IsTemporal)
            {
                return ThisProvider.QueryAsync(query);
            }
            else
            {
                return NextProvider.QueryAsync(query);
            }
        }

        public void Persist(Persist<TModel> persist)
        {
            ThisProvider.Persist(persist);
            NextProvider.Persist(persist);
        }

        public async Task PersistAsync(Persist<TModel> persist)
        {
            await ThisProvider.PersistAsync(persist);
            await NextProvider.PersistAsync(persist);
        }
    }
}