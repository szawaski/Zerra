// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;

namespace Zerra.Map.Converters
{
    /// <summary>
    /// Provides an abstract base class for map converters that transform objects between different types.
    /// </summary>
    public abstract class MapConverter
    {
        /// <summary>
        /// Sets up the converter with optional delegate functions for accessing source and target properties.
        /// </summary>
        /// <param name="sourceGetterDelegate">An optional delegate to get the source value from a parent object.</param>
        /// <param name="targetGetterDelegate">An optional delegate to get the target value from a parent object.</param>
        /// <param name="targetSetterDelegate">An optional delegate to set the target value on a parent object.</param>
        public abstract void Setup(Delegate? sourceGetterDelegate, Delegate? targetGetterDelegate, Delegate? targetSetterDelegate);

        /// <summary>
        /// Maps the source object to the target type.
        /// </summary>
        /// <param name="source">The source object to map. Can be null.</param>
        /// <param name="target">The target object to populate, or null if a new instance should be created.</param>
        /// <param name="graph">The conversion graph for tracking circular references and dependencies, or null if not applicable.</param>
        /// <returns>The mapped target object.</returns>
        public abstract object? Map(object? source, object? target, Graph? graph);

        /// <summary>
        /// Maps a source value from a parent object to a target value on another parent object.
        /// </summary>
        /// <param name="sourceParent">The parent object containing the source value.</param>
        /// <param name="targetParent">The parent object to which the mapped target value will be set.</param>
        /// <param name="graph">The conversion graph for tracking circular references and dependencies, or null if not applicable.</param>
        public abstract void MapFromParent(object sourceParent, object targetParent, Graph? graph);

        /// <summary>
        /// Sets a collected value on a parent object.
        /// </summary>
        /// <param name="parent">The parent object on which to set the value.</param>
        /// <param name="value">The value to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract void CollectedValuesSetter(object parent, in object? value);
    }
}