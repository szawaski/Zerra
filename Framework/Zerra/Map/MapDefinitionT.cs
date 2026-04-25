// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Map
{
    /// <summary>
    /// Implement this class for a map definition from one object type to another.
    /// </summary>
    /// <typeparam name="TSource">The source type to map from.</typeparam>
    /// <typeparam name="TTarget">The target type that the source will be mapped into.</typeparam>
    public class MapDefinition<TSource, TTarget> : IMapDefinition<TSource, TTarget>
    {
        /// <summary>
        /// Override for customizations to the mapping between source and target.
        /// Use the <paramref name="map"/> parameter to define how properties from the source should map to the target.
        /// </summary>
        /// <param name="map">The map setup used to define custom mappings. Allows overriding automatic property mapping and defining two-way mappings.</param>
        public virtual void Define(IMapSetup<TSource, TTarget> map) { }
    }
}
