// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra.Map
{
    /// <summary>
    /// Used within <see cref="IMapDefinition{TSource, TTarget}"/> this contains the methods to define a custom map from one object to another.
    /// Properties are initially automatically mapped by name but this allows overriding and customizations.
    /// </summary>
    /// <typeparam name="TSource">The source map type.</typeparam>
    /// <typeparam name="TTarget">The target type that the source will be map into.</typeparam>
    public interface IMapSetup<TSource, TTarget>
    {
        /// <summary>
        /// Define how a target type property will recieve a value from the source during mapping.
        /// This will override any properties that were able to be automatically mapped by name.
        /// This only works one direction.
        /// </summary>
        /// <param name="property">An expression pointing to the target property.</param>
        /// <param name="value">An expression retrieving the value from the source type.</param>
        void Define(Expression<Func<TTarget, object?>> property, Expression<Func<TSource, object?>> value);
        /// <summary>
        /// Define a one-to-one mapping that works both directions when mapping the source to the target.
        /// This will override any properties that were able to be automatically mapped by name.
        /// </summary>
        /// <param name="property">An expression pointing to the target property.</param>
        /// <param name="sourceProperty">An expression pointing to the source property.</param>
        void DefineTwoWay(Expression<Func<TTarget, object?>> property, Expression<Func<TSource, object?>> sourceProperty);
    }
}
