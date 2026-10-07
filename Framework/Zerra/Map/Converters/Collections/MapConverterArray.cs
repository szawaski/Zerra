// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Map.Converters.Collections
{
    internal sealed class MapConverterArray<TSource, TSourceInner, TTargetInner> : MapConverter<TSource, TTargetInner[]>
    {
        private MapConverter<TSourceInner, TTargetInner> converter = null!;

        protected override void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<TSourceInner>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTargetInner>.GetTypeDetail();
            converter = (MapConverter<TSourceInner, TTargetInner>)MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterArray<TSource, TSourceInner, TTargetInner>), null, null, null);
        }
        public override TTargetInner[]? Map(TSource? source, TTargetInner[]? target, Graph? graph)
        {
            if (source is null)
                return null;

            var sourceEnumerable = (IEnumerable<TSourceInner>)source;

            int sourceCount;
            if (sourceEnumerable is ICollection<TSourceInner> sourceCollection)
                sourceCount = sourceCollection.Count;
            else
                sourceCount = sourceEnumerable.Count();

            if (target == null || sourceCount != target.Length)
                target = new TTargetInner[sourceCount];

            var index = 0;
            var sourceEnumerator = sourceEnumerable.GetEnumerator();
            while (sourceEnumerator.MoveNext())
            {
                target[index] = converter.Map(sourceEnumerator.Current, target[index], graph)!;
                index++;
            }

            return target;
        }
    }
}