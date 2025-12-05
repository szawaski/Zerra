// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.SourceGeneration;

namespace Zerra.Map.Converters.Collections.List
{
    internal sealed class MapConverterIReadOnlyListT<TSource, TSourceInner, TTargetInner> : MapConverter<TSource, IReadOnlyList<TTargetInner>>
    {
        private MapConverter converter = null!;

        private static TSourceInner? SourceGetter(object parent) => ((IEnumerator<TSourceInner>)parent).Current;
        private static void TargetSetter(object parent, TTargetInner value) => ((List<TTargetInner>)parent).Add(value);

        protected override sealed void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<TSourceInner>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTargetInner>.GetTypeDetail();
            converter = MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterIReadOnlyListT<TSource, TSourceInner, TTargetInner>), SourceGetter, null, TargetSetter);
        }

        public override IReadOnlyList<TTargetInner>? Map(TSource? source, IReadOnlyList<TTargetInner>? target, Graph? graph)
        {
            if (source is null)
                return null;

            var sourceEnumerable = (IEnumerable<TSourceInner>)source;

            int sourceCount;
            if (sourceEnumerable is ICollection<TSourceInner> sourceCollection)
                sourceCount = sourceCollection.Count;
            else
                sourceCount = sourceEnumerable.Count();

            var targetList = new List<TTargetInner>(sourceCount);
            target = targetList;

            var sourceEnumerator = sourceEnumerable.GetEnumerator();
            while (sourceEnumerator.MoveNext())
            {
                converter.MapFromParent(sourceEnumerator, targetList, graph);
            }

            return target;
        }
    }
}