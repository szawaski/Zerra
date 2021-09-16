// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public class EventStoreEventModelData<TModel> where TModel : class, new()
    {
        public object Source { get; set; }
        public string SourceType { get; set; }
        public TModel Model { get; set; }
        public Graph<TModel> Graph { get; set; }
    }
}
