// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public interface IAggregateEventStoreEngineProvider<TModel> : IEventStoreEngineProvider where TModel : AggregateRoot
    {

    }
}
