// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    /// <summary>
    /// Generic specialized version of <see cref="ConstructorDetail"/> for a specific type <typeparamref name="T"/>.
    /// Provides a strongly-typed creator delegate without boxing.
    /// Inherits parameter metadata and boxed creator from <see cref="ConstructorDetail"/>.
    /// </summary>
    /// <typeparam name="T">The type being constructed.</typeparam>
    public sealed class ConstructorDetail<T> : ConstructorDetail
    {
        /// <summary>Strongly-typed delegate for creating instances of type <typeparamref name="T"/> without boxing; accepts parameter values as object array.</summary>
        public readonly Func<object?[], T> Creator;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructorDetail{T}"/> class with generic type specialization.
        /// Provides both strongly-typed and boxed creator delegates for object instantiation.
        /// </summary>
        /// <param name="parameters">The parameters required by this constructor.</param>
        /// <param name="creator">Strongly-typed delegate for creating instances of type <typeparamref name="T"/> without boxing.</param>
        /// <param name="creatorBoxed">Boxed delegate for creating instances with the specified parameters.</param>
        public ConstructorDetail(IReadOnlyList<ParameterDetail> parameters, Func<object?[], T> creator, Func<object?[], object> creatorBoxed)
            : base(parameters, creatorBoxed)
        {
            this.Creator = creator;
        }
    }
}
