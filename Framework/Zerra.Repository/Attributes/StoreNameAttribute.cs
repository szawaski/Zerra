// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;

namespace Zerra.Repository
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class StoreNameAttribute : Attribute
    {
        public string StoreName { get; }
        public StoreNameAttribute(string storeName)
        {
            if (!storeName.All(x => Char.IsLetterOrDigit(x) || x == '_' || x == '`'))
                throw new ArgumentException($"{nameof(StoreNameAttribute)}.{nameof(StoreName)}={storeName}");
            this.StoreName = storeName;
        }
    }
}
