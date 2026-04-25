// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;

namespace Zerra.SourceGeneration
{
    public static partial class TypeFinder
    {
        /// <summary>
        /// Represents a required member discovered during type analysis.
        /// </summary>
        public sealed class RequiredMembers
        {
            /// <summary>
            /// The Roslyn type symbol of the required member.
            /// </summary>
            public readonly ITypeSymbol Type;

            /// <summary>
            /// The name of the required member.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// Initializes a new instance of <see cref="RequiredMembers"/>.
            /// </summary>
            /// <param name="type">The Roslyn type symbol of the required member.</param>
            /// <param name="name">The name of the required member.</param>
            public RequiredMembers(ITypeSymbol type, string name)
            {
                this.Type = type;
                this.Name = name;
            }
        }
    }
}

