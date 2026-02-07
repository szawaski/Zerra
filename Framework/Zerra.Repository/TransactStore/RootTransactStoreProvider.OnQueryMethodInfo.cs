// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository
{
    public abstract partial class RootTransactStoreProvider<TModel>
    {
        private sealed class OnQueryMethodInfo
        {
            public Type PropertyType { get; set; }
            public Type RelatedProviderType { get; set; }
            public OnQueryMethodInfo(Type propertyType, Type relatedProviderType)
            {
                this.PropertyType = propertyType;
                this.RelatedProviderType = relatedProviderType;
            }
        }
    }
}