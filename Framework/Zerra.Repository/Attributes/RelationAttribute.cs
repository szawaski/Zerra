// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;

namespace Zerra.Repository
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RelationAttribute : Attribute
    {
        public string ForeignIdentity { get; private set; }
        public RelationAttribute(string foreignIdentity)
        {
            if (!foreignIdentity.All(x => Char.IsLetterOrDigit(x) || x == '_' || x == '`'))
                throw new ArgumentException(String.Format("{0}.{1}={2}", nameof(RelationAttribute), nameof(ForeignIdentity), foreignIdentity));
            this.ForeignIdentity = foreignIdentity;
        }
    }
}
