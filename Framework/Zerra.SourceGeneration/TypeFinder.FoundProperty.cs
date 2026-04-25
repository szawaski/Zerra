// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;

namespace Zerra.SourceGeneration
{
    public static partial class TypeFinder
    {
        /// <summary>
        /// Represents a property discovered during type analysis.
        /// </summary>
        public sealed class FoundProperty
        {
            /// <summary>
            /// The Roslyn property symbol for this property.
            /// </summary>
            public readonly IPropertySymbol PropertySymbol;

            /// <summary>
            /// The name of the property.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// Indicates whether this property is an explicit interface implementation.
            /// </summary>
            public readonly bool IsExplicitFromInterface;

            /// <summary>
            /// Initializes a new instance of <see cref="FoundProperty"/>.
            /// </summary>
            /// <param name="propertySymbol">The Roslyn property symbol.</param>
            /// <param name="name">The name of the property.</param>
            /// <param name="isExplicitFromInterface">Whether the property is an explicit interface implementation.</param>
            public FoundProperty(IPropertySymbol propertySymbol, string name, bool isExplicitFromInterface)
            {
                this.PropertySymbol = propertySymbol;
                this.Name = name;
                this.IsExplicitFromInterface = isExplicitFromInterface;
            }
        }
    }
}

