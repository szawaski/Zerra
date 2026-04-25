// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Map
{
    /// <summary>
    /// Used within <see cref="MapDefinition{TSource, TTarget}"/> this contains the methods to define a custom map from one object to another.
    /// Properties are initially automatically mapped by name but this allows overriding and customizations.
    /// </summary>
    /// <typeparam name="TSource">The source map type.</typeparam>
    /// <typeparam name="TTarget">The target type that the source will be mapped into.</typeparam>
    public interface IMapSetup<TSource, TTarget>
    {
        /// <summary>
        /// Define how a target type property will receive a value from the source during mapping.
        /// This will override any properties that were able to be automatically mapped by name.
        /// This only works one direction.
        /// </summary>
        /// <param name="property">An expression pointing to the target property.</param>
        /// <param name="value">An expression retrieving the value from the source type.</param>
        void Define<TPropertyValue, TValue>(Expression<Func<TTarget, TPropertyValue?>> property, Expression<Func<TSource, TValue?>> value);
        /// <summary>
        /// Define a one-to-one mapping that works both directions when mapping the source to the target.
        /// This will override any properties that were able to be automatically mapped by name.
        /// </summary>
        /// <param name="property1">An expression pointing to the target property.</param>
        /// <param name="property2">An expression pointing to the source property.</param>
        void DefineTwoWay<TPropertyValue1, TPropertyValue2>(Expression<Func<TTarget, TPropertyValue1?>> property1, Expression<Func<TSource, TPropertyValue2?>> property2);

        /// <summary>
        /// Define how a source type property will receive a value from the target during reverse mapping.
        /// This will override any properties that were able to be automatically mapped by name.
        /// This only works in the reverse direction (target to source).
        /// </summary>
        /// <param name="property">An expression pointing to the source property.</param>
        /// <param name="value">An expression retrieving the value from the target type.</param>
        void DefineReverse<TPropertyValue, TValue>(Expression<Func<TSource, TPropertyValue?>> property, Expression<Func<TTarget, TValue?>> value);
    }
}
