// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Reflection;

namespace Zerra.Map.Converters
{
    /// <summary>
    /// Provides a generic base class for type-specific map converters that transform instances from <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>.
    /// </summary>
    /// <typeparam name="TSource">The source type to convert from.</typeparam>
    /// <typeparam name="TTarget">The target type to convert to.</typeparam>
    public abstract class MapConverter<TSource, TTarget> : MapConverter
    {
        /// <summary>
        /// Gets the type detail information for the source type.
        /// </summary>
        protected TypeDetail<TSource> sourceTypeDetail { get; private set; } = null!;

        /// <summary>
        /// Gets the type detail information for the target type.
        /// </summary>
        protected TypeDetail<TTarget> targetTypeDetail { get; private set; } = null!;

        private Func<object, TSource?>? sourceGetter;
        private Func<object, TTarget?>? targetGetter;
        private Action<object, TTarget?>? targetSetter;

        /// <inheritdoc/>
        public override sealed void Setup(Delegate? sourceGetterDelegate, Delegate? targetGetterDelegate, Delegate? targetSetterDelegate)
        {
            this.sourceTypeDetail = TypeAnalyzer<TSource>.GetTypeDetail();
            this.targetTypeDetail = TypeAnalyzer<TTarget>.GetTypeDetail();

            if (sourceGetterDelegate is not null)
            {
                sourceGetter = sourceGetterDelegate as Func<object, TSource?>;
                if (sourceGetter == null)
                    throw new InvalidOperationException($"{this.GetType().Name} source getter delegate is not of correct type Action<object, {typeof(TTarget).FullName}>");
            }
            if (targetGetterDelegate is not null)
            {
                targetGetter = targetGetterDelegate as Func<object, TTarget?>;
                if (targetGetter == null)
                    throw new InvalidOperationException($"{this.GetType().Name} target getter delegate is not of correct type Action<object, {typeof(TTarget).FullName}>");
            }
            if (targetSetterDelegate is not null)
            {
                targetSetter = targetSetterDelegate as Action<object, TTarget?>;
                if (targetSetter == null)
                    throw new InvalidOperationException($"{this.GetType().Name} target setter delegate is not of correct type Action<object, {typeof(TTarget).FullName}>");
            }

            Setup();
        }

        /// <summary>
        /// Override this method to provide additional setup logic for the converter.
        /// </summary>
        protected virtual void Setup() { }

        /// <inheritdoc/>
        public override sealed object? Map(object? source, object? target, Graph? graph)
        {
            return Map((TSource?)source, (TTarget?)target, graph);
        }

        /// <inheritdoc/>
        public override sealed void MapFromParent(object sourceParent, object targetParent, Graph? graph)
        {
            if (sourceGetter == null || targetSetter == null)
                throw new InvalidOperationException($"{this.GetType().Name} Converter not setup correctly");

            var source = sourceGetter(sourceParent);

            TTarget? target;
            if (targetGetter != null)
                target = targetGetter(targetParent);
            else
                target = default;

            var targetResult = Map(source, target, graph);

            targetSetter(targetParent, targetResult);
        }

        /// <summary>
        /// Maps the source value to the target type. Derived classes must implement this method to provide the conversion logic.
        /// </summary>
        /// <param name="source">The source value to convert. Can be null.</param>
        /// <param name="target">The target object to populate, or null if a new instance should be created.</param>
        /// <param name="graph">The conversion graph for tracking circular references and dependencies, or null if not applicable.</param>
        /// <returns>The converted target value.</returns>
        public abstract TTarget? Map(TSource? source, TTarget? target, Graph? graph);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed void CollectedValuesSetter(object parent, in object? value)
        {
            if (targetSetter == null)
                throw new InvalidOperationException($"{this.GetType().Name} Converter not setup correctly");
            targetSetter(parent, (TTarget?)value);
        }
    }
}