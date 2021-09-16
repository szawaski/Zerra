// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;

namespace Zerra.Repository
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DataSourceEntityAttribute : Attribute
    {
        public string SourceName { get; private set; }
        public DataSourceEntityAttribute(string sourceName = null)
        {
            if (!String.IsNullOrWhiteSpace(sourceName))
            {
                if (!sourceName.All(x => char.IsLetterOrDigit(x) || x == '_' || x == '`'))
                    throw new ArgumentException($"{nameof(DataSourceEntityAttribute)}.{nameof(SourceName)}={sourceName}");
                this.SourceName = sourceName;
            }
        }
    }
}
