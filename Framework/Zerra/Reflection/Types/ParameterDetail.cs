// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Reflection
{
    /// <summary>
    /// Metadata for a constructor or method parameter including its type and name.
    /// Provides cached access to detailed type information for the parameter type.
    /// Used by the source generator and runtime reflection to enable parameter inspection and validation.
    /// </summary>
    public sealed class ParameterDetail
    {
        /// <summary>The type of this parameter.</summary>
        public readonly Type Type;
        /// <summary>The name of this parameter.</summary>
        public readonly string Name;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterDetail"/> class with parameter metadata.
        /// </summary>
        /// <param name="type">The type of the parameter.</param>
        /// <param name="name">The name of the parameter.</param>
        public ParameterDetail(Type type, string name)
        {
            this.Type = type;
            this.Name = name;
        }

        /// <summary>
        /// Gets the cached type detail for this parameter's type.
        /// Lazily initializes and caches the type detail information for serialization and reflection.
        /// </summary>
        public TypeDetail TypeDetail
        {
            get
            {
                field ??= TypeAnalyzer.GetTypeDetail(this.Type);
                return field;
            }
        }
    }
}
