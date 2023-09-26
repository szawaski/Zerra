// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Repository
{
    public abstract class BaseEventStoreContextProvider<TContext, TModel> : IAggregateRootContextProvider<TModel>
        where TContext : DataContext
        where TModel : AggregateRoot
    {
        public DataContext GetContext()
        {
            var context = Instantiator.Create<TContext>();
            return context;
        }
    }
}