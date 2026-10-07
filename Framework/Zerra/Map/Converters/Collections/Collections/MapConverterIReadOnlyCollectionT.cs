// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Map.Converters.Collections.Collections
{
    internal sealed class MapConverterIReadOnlyCollectionT<TSource, TSourceInner, TTargetInner> : MapConverter<TSource, IReadOnlyCollection<TTargetInner>>
    {
        private MapConverter<TSourceInner, TTargetInner> converter = null!;

        protected override sealed void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<TSourceInner>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTargetInner>.GetTypeDetail();
            converter = (MapConverter<TSourceInner, TTargetInner>)MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterIReadOnlyCollectionT<TSource, TSourceInner, TTargetInner>), null, null, null);
        }

        public override IReadOnlyCollection<TTargetInner>? Map(TSource? source, IReadOnlyCollection<TTargetInner>? target, Graph? graph)
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
                var item = converter.Map(sourceEnumerator.Current, default, graph);
                targetList.Add(item!);
            }

            return target;
        }
    }
}