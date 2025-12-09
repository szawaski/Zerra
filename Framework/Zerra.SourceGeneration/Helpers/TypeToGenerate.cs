// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;

namespace Zerra.SourceGeneration
{
    public sealed class TypeToGenerate
    {
        public readonly ITypeSymbol TypeSymbol;
        public readonly string TypeName;
        public readonly string Source;
        public TypeToGenerate(ITypeSymbol typeSymbol, string typeName, string source)
        {
            this.TypeSymbol = typeSymbol;
            this.TypeName = typeName;
            this.Source = source;
        }
    }
}

