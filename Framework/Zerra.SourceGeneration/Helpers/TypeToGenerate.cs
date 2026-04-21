// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;

namespace Zerra.SourceGeneration
{
    /// <summary>
    /// Represents a type that has been identified for source generation.
    /// </summary>
    public sealed class TypeToGenerate
    {
        /// <summary>
        /// The Roslyn type symbol for this type.
        /// </summary>
        public readonly ITypeSymbol TypeSymbol;

        /// <summary>
        /// The fully qualified name of the type.
        /// </summary>
        public readonly string TypeName;

        /// <summary>
        /// The source text associated with this type.
        /// </summary>
        public readonly string Source;

        /// <summary>
        /// Initializes a new instance of <see cref="TypeToGenerate"/>.
        /// </summary>
        /// <param name="typeSymbol">The Roslyn type symbol.</param>
        /// <param name="typeName">The fully qualified name of the type.</param>
        /// <param name="source">The source text associated with this type.</param>
        public TypeToGenerate(ITypeSymbol typeSymbol, string typeName, string source)
        {
            this.TypeSymbol = typeSymbol;
            this.TypeName = typeName;
            this.Source = source;
        }
    }
}

