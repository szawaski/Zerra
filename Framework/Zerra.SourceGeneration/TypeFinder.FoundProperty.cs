// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;

namespace Zerra.SourceGeneration
{
    public static partial class TypeFinder
    {
        public sealed class FoundProperty
        {
            public readonly IPropertySymbol PropertySymbol;
            public readonly string Name;
            public readonly bool IsExplicitFromInterface;
            public FoundProperty(IPropertySymbol propertySymbol, string name, bool isExplicitFromInterface)
            {
                this.PropertySymbol = propertySymbol;
                this.Name = name;
                this.IsExplicitFromInterface = isExplicitFromInterface;
            }
        }
    }
}

