// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;

namespace Zerra.Repository
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DataSourceRelationAttribute : Attribute
    {
        public string ForeignIdentity { get; private set; }
        public DataSourceRelationAttribute(string foreignIdentity)
        {
            if (!foreignIdentity.All(x => char.IsLetterOrDigit(x) || x == '_' || x == '`'))
                throw new ArgumentException(String.Format("{0}.{1}={2}", nameof(DataSourceRelationAttribute), nameof(ForeignIdentity), foreignIdentity));
            this.ForeignIdentity = foreignIdentity;
        }
    }
}
