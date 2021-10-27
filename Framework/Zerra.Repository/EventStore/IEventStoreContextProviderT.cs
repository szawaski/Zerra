// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.EventStore
{
    public interface IEventStoreContextProvider<TModel> : IEventStoreContextProvider where TModel : AggregateRoot
    {
        
    }
}
