// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Repository
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class EntityAttribute : GenerateTypeDetailAttribute
    {
        public string? StoreName { get; }
        public EntityAttribute(string? storeName = null)
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
