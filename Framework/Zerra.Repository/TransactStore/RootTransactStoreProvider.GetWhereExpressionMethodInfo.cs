// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository
{
    public abstract partial class RootTransactStoreProvider<TModel> where TModel : class, new()
    {
        private sealed class GetWhereExpressionMethodInfo
        {
            public Type PropertyType { get; set; }
            public bool Enumerable { get; set; }
            public Type RelatedProviderType { get; set; }
        }
    }
}