// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Map.Converters.Collections.Collections
{
    internal sealed class MapConverterICollectionTOfT<TSource, TTarget, TSourceInner, TTargetInner> : MapConverter<TSource, TTarget>
    {
        private MapConverter converter = null!;

        private static TSourceInner? SourceGetter(object parent) => ((IEnumerator<TSourceInner>)parent).Current;
        private static void TargetSetter(object parent, TTargetInner value) => ((ICollection<TTargetInner>)parent).Add(value);

        protected override sealed void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<TSourceInner>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTargetInner>.GetTypeDetail();
            converter = MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterICollectionTOfT<TSource, TTarget, TSourceInner, TTargetInner>), SourceGetter, null, TargetSetter);
        }

        public override TTarget? Map(TSource? source, TTarget? target, Graph? graph)
        {
            if (source is null)
                return default;

            var sourceEnumerable = (IEnumerable<TSourceInner>)source;

            var targetSet = (ICollection<TTargetInner>?)target;

            int sourceCount;
            if (sourceEnumerable is ICollection<TSourceInner> sourceCollection)
                sourceCount = sourceCollection.Count;
            else
                sourceCount = sourceEnumerable.Count();

            if (targetSet == null || sourceCount != targetSet.Count)
            {
                if (!targetTypeDetail.HasCreator)
                    throw new NotSupportedException($"Target type {targetTypeDetail.Type.FullName} has no public parameterless constructor.");
                target = targetTypeDetail.Creator!()!;
                targetSet = (ICollection<TTargetInner>)target;
            }
            else
            {
                targetSet.Clear();
            }

            var sourceEnumerator = sourceEnumerable.GetEnumerator();
            while (sourceEnumerator.MoveNext())
            {
                converter.MapFromParent(sourceEnumerator, targetSet, graph);
            }

            return target;
        }
    }
}