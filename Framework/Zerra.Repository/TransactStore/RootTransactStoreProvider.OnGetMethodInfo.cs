// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository
{
    public abstract partial class RootTransactStoreProvider<TModel> where TModel : class, new()
    {
        private sealed class OnGetMethodInfo
        {
            public Type PropertyType { get; private set; }
            public bool Collection { get; private set; }
            public Type RelatedProviderType { get; private set; }
            public OnGetMethodInfo(Type propertyType, bool collection, Type relatedProviderType)
            {
                this.PropertyType = propertyType;
                this.Collection = collection;
                this.RelatedProviderType = relatedProviderType;
            }
        }
    }
}