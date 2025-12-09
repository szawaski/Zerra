// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Reflection;

namespace Zerra.Map.Converters
{
    public abstract class MapConverter<TSource, TTarget> : MapConverter
    {
        protected TypeDetail<TSource> sourceTypeDetail { get; private set; } = null!;
        protected TypeDetail<TTarget> targetTypeDetail { get; private set; } = null!;
        private Func<object, TSource?>? sourceGetter;
        private Func<object, TTarget?>? targetGetter;
        private Action<object, TTarget?>? targetSetter;

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

        protected virtual void Setup() { }

        public override sealed object? Map(object? source, object? target, Graph? graph)
        {
            return Map((TSource?)source, (TTarget?)target, graph);
        }

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

        public abstract TTarget? Map(TSource? source, TTarget? target, Graph? graph);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed void CollectedValuesSetter(object parent, in object? value)
        {
            if (targetSetter == null)
                throw new InvalidOperationException($"{this.GetType().Name} Converter not setup correctly");
            targetSetter(parent, (TTarget?)value);
        }
    }
}