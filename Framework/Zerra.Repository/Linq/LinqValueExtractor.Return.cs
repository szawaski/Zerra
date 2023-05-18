// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository
{
    internal static partial class LinqValueExtractor
    {
        private sealed class Return
        {
            public Type PropertyModelType { get; private set; }
            public string PropertyName { get; private set; }
            public object[] Values { get; private set; }
            public bool HasValues { get; private set; }

            public Return(Type propertyModelType, string propertyName)
            {
                this.PropertyModelType = propertyModelType;
                this.PropertyName = propertyName;
            }

            public Return(object value)
            {
                this.HasValues = true;
                this.Values = new object[] { value };
            }
            public Return(object[] values)
            {
                this.HasValues = true;
                this.Values = values;
            }
        }
    }
}