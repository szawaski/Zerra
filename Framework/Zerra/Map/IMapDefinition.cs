// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Map
{
    /// <summary>
    /// Implement this class for a custom map definintion from one object type to another
    /// This is utilized by <see cref="Mapper"/> extensions.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type that the source will be map into.</typeparam>
    public interface IMapDefinition<TSource, TTarget>
    {
        /// <summary>
        /// Define the mapping definitions
        /// </summary>
        /// <param name="map">The map setup used to define mappings.</param>
        void Define(IMapSetup<TSource, TTarget> map);
    }
}
