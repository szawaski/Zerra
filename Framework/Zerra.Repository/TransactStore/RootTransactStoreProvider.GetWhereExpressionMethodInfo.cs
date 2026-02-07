// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository
{
    public abstract partial class RootTransactStoreProvider<TModel>
    {
        private sealed class GetWhereExpressionMethodInfo
        {
            public Type PropertyType { get; }
            public bool Enumerable { get; }
            public Type RelatedProviderType { get; }
            public GetWhereExpressionMethodInfo(Type propertyType, bool enumerable, Type relatedProviderType)
            {
                this.PropertyType = propertyType;
                this.Enumerable = enumerable;
                this.RelatedProviderType = relatedProviderType;
            }
        }
    }
}