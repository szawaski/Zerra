// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Map.Converters.Collections.Enumerables
{
    internal sealed class MapConverterIEnumerableT<TSource, TSourceInner, TTargetInner> : MapConverter<TSource, IEnumerable<TTargetInner>>
    {
        private MapConverter<TSourceInner, TTargetInner> converter = null!;

        protected override void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<TSourceInner>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTargetInner>.GetTypeDetail();
            converter = (MapConverter<TSourceInner, TTargetInner>)MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterIEnumerableT<TSource, TSourceInner, TTargetInner>), null, null, null);
        }
        public override IEnumerable<TTargetInner>? Map(TSource? source, IEnumerable<TTargetInner>? target, Graph? graph)
        {
            if (source is null)
                return null;

            var sourceEnumerable = (IEnumerable<TSourceInner>)source;

            int sourceCount;
            if (sourceEnumerable is ICollection<TSourceInner> sourceCollection)
                sourceCount = sourceCollection.Count;
            else
                sourceCount = sourceEnumerable.Count();

            var targetArray = new TTargetInner[sourceCount];
            target = targetArray;

            var index = 0;
            var sourceEnumerator = sourceEnumerable.GetEnumerator();
            while (sourceEnumerator.MoveNext())
            {
                targetArray[index] = converter.Map(sourceEnumerator.Current, default, graph)!;
                index++;
            }

            return target;
        }
    }
}