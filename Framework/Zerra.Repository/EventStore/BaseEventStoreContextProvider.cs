// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Providers;
using Zerra.Reflection;

namespace Zerra.Repository
{
    public abstract class BaseEventStoreContextProvider<TContext, TModel> : IBaseProvider, IEventStoreContextProvider<TModel>
        where TContext : DataContext<IEventStoreEngine>
        where TModel : AggregateRoot
    {
        public DataContext<IEventStoreEngine> GetContext()
        {
            var context = Instantiator.CreateInstance<TContext>();
            return context;
        }
    }
}