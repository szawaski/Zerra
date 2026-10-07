// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Map.Converters.Collections.List
{
    internal sealed class MapConverterIListT<TSource, TSourceInner, TTargetInner> : MapConverter<TSource, IList<TTargetInner>>
    {
        private MapConverter<TSourceInner, TTargetInner> converter = null!;

        protected override sealed void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<TSourceInner>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTargetInner>.GetTypeDetail();
            converter = (MapConverter<TSourceInner, TTargetInner>)MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterIListT<TSource, TSourceInner, TTargetInner>), null, null, null);
        }

        public override IList<TTargetInner>? Map(TSource? source, IList<TTargetInner>? target, Graph? graph)
        {
            if (source is null)
                return null;

            var sourceEnumerable = (IEnumerable<TSourceInner>)source;

            int sourceCount;
            if (sourceEnumerable is ICollection<TSourceInner> sourceCollection)
                sourceCount = sourceCollection.Count;
            else
                sourceCount = sourceEnumerable.Count();

            if (target == null || sourceCount != target.Count)
                target = new List<TTargetInner>(sourceCount);

            var hasExistingValues = target.Count > 0;
            var index = 0;
            var sourceEnumerator = sourceEnumerable.GetEnumerator();
            while (sourceEnumerator.MoveNext())
            {
                if (hasExistingValues)
                    target[index] = converter.Map(sourceEnumerator.Current, target[index], graph)!;
                else
                    target.Add(converter.Map(sourceEnumerator.Current, default, graph)!);
                index++;
            }

            return target;
        }
    }
}