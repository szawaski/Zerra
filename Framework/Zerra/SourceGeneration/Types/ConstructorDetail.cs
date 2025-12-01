// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.VisualBasic.FileIO;

namespace Zerra.SourceGeneration.Types
{
    /// <summary>
    /// Metadata for a type constructor including its parameters and creation delegate.
    /// Provides a boxed delegate for instantiating types with specific parameter values.
    /// Used by the source generator and runtime reflection to enable efficient object creation.
    /// </summary>
    public abstract class ConstructorDetail
    {
        /// <summary>Collection of parameters required by this constructor.</summary>
        public readonly IReadOnlyList<ParameterDetail> Parameters;
        /// <summary>The typed delegate for creating instances using this constructor.</summary>
        public readonly Delegate Creator;
        /// <summary>Boxed delegate for creating instances using this constructor; accepts parameter values as object array and returns object.</summary>
        public readonly Func<object?[], object> CreatorBoxed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructorDetail"/> class with constructor metadata.
        /// </summary>
        /// <param name="parameters">The parameters required by this constructor.</param>
        /// <param name="creator">The typed delegate for creating instances using this constructor.</param>
        /// <param name="creatorBoxed">Boxed delegate for creating instances with the specified parameters.</param>
        public ConstructorDetail(IReadOnlyList<ParameterDetail> parameters, Delegate creator, Func<object?[], object> creatorBoxed)
        {
            this.Parameters = parameters;
            this.Creator = creator;
            this.CreatorBoxed = creatorBoxed;
        }
    }
}
