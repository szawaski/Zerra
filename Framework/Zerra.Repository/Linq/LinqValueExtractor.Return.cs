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
            public Type? PropertyModelType { get; }
            public string? PropertyName { get; }
            public object?[]? Values { get; }

            public Return(Type propertyModelType, string propertyName)
            {
                this.PropertyModelType = propertyModelType;
                this.PropertyName = propertyName;
            }

            public Return(object? value)
            {
                this.Values = [value];
            }
            public Return(object?[] values)
            {
                this.Values = values;
            }
        }
    }
}