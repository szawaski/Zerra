// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;

namespace Zerra.SourceGeneration
{
    public static partial class TypeFinder
    {
        /// <summary>
        /// Represents a method discovered during type analysis.
        /// </summary>
        public sealed class FoundMethod
        {
            /// <summary>
            /// The Roslyn method symbol for this method.
            /// </summary>
            public readonly IMethodSymbol MethodSymbol;

            /// <summary>
            /// The name of the method.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// Indicates whether this method is an explicit interface implementation.
            /// </summary>
            public readonly bool IsExplicitFromInterface;

            /// <summary>
            /// Initializes a new instance of <see cref="FoundMethod"/>.
            /// </summary>
            /// <param name="methodSymbol">The Roslyn method symbol.</param>
            /// <param name="name">The name of the method.</param>
            /// <param name="isExplicitFromInterface">Whether the method is an explicit interface implementation.</param>
            public FoundMethod(IMethodSymbol methodSymbol, string name, bool isExplicitFromInterface)
            {
                this.MethodSymbol = methodSymbol;
                this.Name = name;
                this.IsExplicitFromInterface = isExplicitFromInterface;
            }
        }
    }
}

