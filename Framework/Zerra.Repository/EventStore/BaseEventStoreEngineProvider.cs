// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Providers;
using Zerra.Reflection;

namespace Zerra.Repository.EventStore
{
    public abstract class BaseEventStoreEngineProvider<TContext, TModel> : IBaseProvider, IEventStoreEngineProvider<TModel>
        where TContext : DataContext
        where TModel : AggregateRoot
    {
        public IEventStoreEngine GetEngine()
        {
            var context = Instantiator.CreateInstance<TContext>();
            return context.InitializeEngine<IEventStoreEngine>();
        }
    }
}