// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;
using Zerra.Map.Converters;

namespace Zerra.Map
{
    /// <summary>
    /// Provides object mapping functionality with support for type conversion, graph-based mapping, and custom converters.
    /// </summary>
    public static class Mapper
    {
        /// <summary>
        /// Maps the source object to an instance of <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TTarget">The target type to map to.</typeparam>
        /// <param name="source">The source object to map from. Cannot be null.</param>
        /// <param name="graph">Optional graph specifying which members to include or exclude in the mapping.</param>
        /// <returns>A new instance of <typeparamref name="TTarget"/> populated with mapped values from the source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static TTarget Map<TTarget>(this object source, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var converter = MapConverterFactory.GetRoot(source.GetType(), typeof(TTarget));
            var result = (TTarget?)converter.Map(source, default, graph);
            return result!;
        }

        /// <summary>
        /// Maps the source object to an instance of <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TTarget">The target type to map to.</typeparam>
        /// <param name="source">The source object to map from. Cannot be null.</param>
        /// <param name="sourceType">The type of the object to map. Cannot be null.</param>
        /// <param name="graph">Optional graph specifying which members to include or exclude in the mapping.</param>
        /// <returns>A new instance of <typeparamref name="TTarget"/> populated with mapped values from the source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static TTarget Map<TTarget>(this object source, Type sourceType, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var converter = MapConverterFactory.GetRoot(sourceType, typeof(TTarget));
            var result = (TTarget?)converter.Map(source, default, graph);
            return result!;
        }

        /// <summary>
        /// Maps the source object to an instance of <paramref name="targetType"/>.
        /// </summary>
        /// <param name="source">The source object to map from. Cannot be null.</param>
        /// <param name="sourceType">The type of the object to map. Cannot be null.</param>
        /// <param name="targetType">The type of the target object to map to. Cannot be null.</param>
        /// <param name="graph">Optional graph specifying which members to include or exclude in the mapping.</param>
        /// <returns>A new instance of <paramref name="targetType"/> populated with mapped values from the source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static object Map(this object source, Type sourceType, Type targetType, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var converter = MapConverterFactory.GetRoot(sourceType, targetType);
            var result = converter.Map(source, default, graph);
            return result!;
        }

        /// <summary>
        /// Maps the source object of type <typeparamref name="TSource"/> to an instance of <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TSource">The source type to map from.</typeparam>
        /// <typeparam name="TTarget">The target type to map to.</typeparam>
        /// <param name="source">The source object to map from. Cannot be null.</param>
        /// <param name="graph">Optional graph specifying which members to include or exclude in the mapping.</param>
        /// <returns>A new instance of <typeparamref name="TTarget"/> populated with mapped values from the source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static TTarget Map<TSource, TTarget>(this TSource source, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var converter = MapConverterFactory.RootConverter<TSource, TTarget>.Converter;
            var result = converter.Map(source, default, graph);
            return result!;
        }

        /// <summary>
        /// Maps the source object of type <typeparamref name="TSource"/> to an existing instance of <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TSource">The source type to map from.</typeparam>
        /// <typeparam name="TTarget">The target type to map to.</typeparam>
        /// <param name="source">The source object to map from. Cannot be null.</param>
        /// <param name="target">The target object to map to. Cannot be null.</param>
        /// <param name="graph">Optional graph specifying which members to include or exclude in the mapping.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="target"/> is null.</exception>
        public static void MapTo<TSource, TTarget>(this TSource source, TTarget target, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (target is null)
                throw new ArgumentNullException(nameof(target));

            var converter = MapConverterFactory.RootConverter<TSource, TTarget>.Converter;
            _ = converter.Map(source, target, graph);
        }

        /// <summary>
        /// Maps the source object of type <paramref name="sourceType"/> to an existing instance of <paramref name="targetType"/>.
        /// </summary>
        /// <param name="source">The source object to map from. Cannot be null.</param>
        /// <param name="sourceType">The type of the object to map. Cannot be null.</param>
        /// <param name="target">The target object to map to. Cannot be null.</param>
        /// <param name="targetType">The type of the target object to map to. Cannot be null.</param>
        /// <param name="graph">Optional graph specifying which members to include or exclude in the mapping.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="target"/> is null.</exception>
        public static void MapTo(this object source, Type sourceType, object target, Type targetType, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (target is null)
                throw new ArgumentNullException(nameof(target));

            var converter = MapConverterFactory.GetRoot(sourceType, targetType);
            _ = converter.Map(source, target, graph);
        }

        /// <summary>
        /// Creates a deep copy of the source object of type <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of object to copy.</typeparam>
        /// <param name="source">The source object to copy. Cannot be null.</param>
        /// <param name="graph">Optional graph specifying which members to include or exclude in the copy.</param>
        /// <returns>A new instance of <typeparamref name="TTarget"/> that is a copy of the source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static TTarget Copy<TTarget>(this TTarget source, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var converter = MapConverterFactory.RootConverter<TTarget, TTarget>.Converter;
            var result = converter.Map(source, default, graph);
            return result!;
        }

        /// <summary>
        /// Creates a deep copy of the source object of type <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of object to copy.</typeparam>
        /// <param name="source">The source object to copy. Cannot be null.</param>
        /// <param name="graph">Optional graph specifying which members to include or exclude in the copy.</param>
        /// <returns>A new instance of <typeparamref name="TTarget"/> that is a copy of the source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static TTarget Copy<TTarget>(this object source, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var converter = MapConverterFactory.GetRoot(typeof(TTarget), typeof(TTarget));
            var result = (TTarget?)converter.Map(source, default, graph);
            return result!;
        }

        /// <summary>
        /// Creates a deep copy of the source object using its runtime type.
        /// </summary>
        /// <param name="source">The source object to copy. Cannot be null.</param>
        /// <param name="sourceType">The type of the object to copy. Cannot be null.</param>
        /// <param name="graph">Optional graph specifying which members to include or exclude in the copy.</param>
        /// <returns>A new instance of the same type as <paramref name="source"/> that is a copy of the source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public static object Copy(this object source, Type sourceType, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var converter = MapConverterFactory.GetRoot(sourceType, sourceType);
            var result = converter.Map(source, default, graph);
            return result!;
        }

        /// <summary>
        /// Registers a custom converter for mapping between the specified source and target types.
        /// </summary>
        /// <param name="sourceType">The source type for the conversion. Cannot be null.</param>
        /// <param name="targetType">The target type for the conversion. Cannot be null.</param>
        /// <param name="converter">A factory function that creates instances of the converter. Cannot be null.</param>
        public static void AddConverter(Type sourceType, Type targetType, Func<MapConverter> converter) => MapConverterFactory.AddConverter(sourceType, targetType, converter);
    }
}
