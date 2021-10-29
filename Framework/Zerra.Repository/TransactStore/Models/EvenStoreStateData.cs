// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public class EvenStoreStateData<TModel> where TModel : class, new()
    {
        public ulong? Number { get; set; }
        public bool Deleted { get; set; }
        public TModel Model { get; set; }
    }
}
