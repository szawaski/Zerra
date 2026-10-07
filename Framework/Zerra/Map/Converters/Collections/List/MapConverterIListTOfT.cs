// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Map.Converters.Collections.List
{
    internal sealed class MapConverterIListTOfT<TSource, TTarget, TSourceInner, TTargetInner> : MapConverter<TSource, TTarget>
    {
        private MapConverter<TSourceInner, TTargetInner> converter = null!;

        protected override sealed void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<TSourceInner>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTargetInner>.GetTypeDetail();
            converter = (MapConverter<TSourceInner, TTargetInner>)MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterIListTOfT<TSource, TTarget, TSourceInner, TTargetInner>), null, null, null);
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

            var hasExistingValues = targetList.Count > 0;
            var index = 0;
            var sourceEnumerator = sourceEnumerable.GetEnumerator();
            while (sourceEnumerator.MoveNext())
            {
                if (hasExistingValues)
                    targetList[index] = converter.Map(sourceEnumerator.Current, targetList[index], graph)!;
                else
                    targetList.Add(converter.Map(sourceEnumerator.Current, default, graph)!);
                index++;
            }

            return target;
        }
    }
}