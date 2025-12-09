// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Map.Converters.Collections.List
{
    internal sealed class MapConverterIListTOfT<TSource, TTarget, TSourceInner, TTargetInner> : MapConverter<TSource, TTarget>
    {
        private MapConverter converter = null!;

        private static TSourceInner? SourceGetter(object parent) => ((IEnumerator<TSourceInner>)parent).Current;
        private static TTargetInner? TargetGetter(object parent) => ((ListAccessor<TTargetInner>)parent).Get();
        private static void TargetSetter(object parent, TTargetInner value) => ((ListAccessor<TTargetInner>)parent).Add(value);

        protected override sealed void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<TSourceInner>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTargetInner>.GetTypeDetail();
            converter = MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterIListTOfT<TSource, TTarget, TSourceInner, TTargetInner>), SourceGetter, TargetGetter, TargetSetter);
        }

        public override TTarget? Map(TSource? source, TTarget? target, Graph? graph)
        {
            if (source is null)
                return default;

            var sourceEnumerable = (IEnumerable<TSourceInner>)source;

            var targetList = (IList<TTargetInner>?)target;

            int sourceCount;
            if (sourceEnumerable is ICollection<TSourceInner> sourceCollection)
                sourceCount = sourceCollection.Count;
            else
                sourceCount = sourceEnumerable.Count();

            if (targetList == null || sourceCount != targetList.Count)
            {
                if (!targetTypeDetail.HasCreator)
                    throw new NotSupportedException($"Target type {targetTypeDetail.Type.FullName} has no public parameterless constructor.");
                target = targetTypeDetail.Creator!()!;
                targetList = (IList<TTargetInner>)target;
            }

            var targetAccessor = new ListAccessor<TTargetInner>(targetList);

            var sourceEnumerator = sourceEnumerable.GetEnumerator();
            while (sourceEnumerator.MoveNext())
            {
                converter.MapFromParent(sourceEnumerator, targetAccessor, graph);
                targetAccessor.Index++;
            }

            return target;
        }
    }
}