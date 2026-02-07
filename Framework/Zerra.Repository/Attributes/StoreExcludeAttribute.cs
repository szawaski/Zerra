// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class StoreExcludeAttribute : Attribute
    {
        public StoreExcludeAttribute()
        {
        }
    }
}
