// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;

namespace Zerra.Repository
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EntityAttribute : Attribute
    {
        public string StoreName { get; private set; }
        public EntityAttribute(string storeName = null)
        {
            if (!String.IsNullOrWhiteSpace(storeName))
            {
                if (!storeName.All(x => Char.IsLetterOrDigit(x) || x == '_' || x == '`'))
                    throw new ArgumentException($"{nameof(EntityAttribute)}.{nameof(StoreName)}={storeName}");
                this.StoreName = storeName;
            }
        }
    }
}
