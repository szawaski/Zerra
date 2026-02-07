// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository
{
    public abstract partial class RootTransactStoreProvider<TModel>
    {
        private sealed class OnGetMethodInfo
        {
            public Type PropertyType { get; }
            public bool Collection { get; }
            public Type RelatedProviderType { get; }
            public OnGetMethodInfo(Type propertyType, bool collection, Type relatedProviderType)
            {
                this.PropertyType = propertyType;
                this.Collection = collection;
                this.RelatedProviderType = relatedProviderType;
            }
        }
    }
}