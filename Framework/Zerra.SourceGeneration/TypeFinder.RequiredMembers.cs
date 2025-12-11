// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;

namespace Zerra.SourceGeneration
{
    public static partial class TypeFinder
    {
        public sealed class RequiredMembers
        {
            public readonly ITypeSymbol Type;
            public readonly string Name;
            public RequiredMembers(ITypeSymbol type, string name)
            {
                this.Type = type;
                this.Name = name;
            }
        }
    }
}

