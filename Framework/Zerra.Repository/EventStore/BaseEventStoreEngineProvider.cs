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
        private static IEventStoreEngine engine;
        private static readonly object providerLock = new object();

        public IEventStoreEngine GetEngine()
        {
            if (engine == null)
            {
                lock (providerLock)
                {
                    if (engine == null)
                    {
                        var context = Instantiator.CreateInstance<TContext>();
                        engine = context.InitializeEngine<IEventStoreEngine>();
                    }
                }
            }
            return engine;
        }
    }
}