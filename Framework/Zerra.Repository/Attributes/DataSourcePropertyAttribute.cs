// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;

namespace Zerra.Repository
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DataSourcePropertyAttribute : Attribute
    {
        public string SourceName { get; private set; }
        public DataSourcePropertyAttribute()
        {
        }
        public DataSourcePropertyAttribute(string sourceName)
        {
            if (!sourceName.All(x => char.IsLetterOrDigit(x) || x == '_' || x == '`'))
                throw new ArgumentException(String.Format("{0}.{1}={2}", nameof(DataSourcePropertyAttribute), nameof(SourceName), sourceName));
            this.SourceName = sourceName;
        }
    }
}
