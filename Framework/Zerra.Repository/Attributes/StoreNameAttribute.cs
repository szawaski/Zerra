// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;

namespace Zerra.Repository
{
    [AttributeUsage(AttributeTargets.Property)]
    public class StoreNameAttribute : Attribute
    {
        public string StoreName { get; private set; }
        public StoreNameAttribute(string storeName)
        {
            if (!storeName.All(x => char.IsLetterOrDigit(x) || x == '_' || x == '`'))
                throw new ArgumentException(String.Format("{0}.{1}={2}", nameof(StoreNameAttribute), nameof(StoreName), storeName));
            this.StoreName = storeName;
        }
    }
}
