// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;

namespace Zerra.SourceGeneration
{
    public static partial class TypeFinder
    {
        public sealed class FoundMethod
        {
            public readonly IMethodSymbol MethodSymbol;
            public readonly string Name;
            public readonly bool IsExplicitFromInterface;
            public FoundMethod(IMethodSymbol methodSymbol, string name, bool isExplicitFromInterface)
            {
                this.MethodSymbol = methodSymbol;
                this.Name = name;
                this.IsExplicitFromInterface = isExplicitFromInterface;
            }
        }
    }
}

