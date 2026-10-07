// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if !NETSTANDARD2_0

using Zerra.Reflection;

namespace Zerra.Map.Converters.Collections.Sets
{
    internal sealed class MapConverterIReadOnlySetT<TSource, TSourceInner, TTargetInner> : MapConverter<TSource, IReadOnlySet<TTargetInner>>
    {
        private MapConverter<TSourceInner, TTargetInner> converter = null!;

        protected override sealed void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<TSourceInner>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTargetInner>.GetTypeDetail();
            converter = (MapConverter<TSourceInner, TTargetInner>)MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterIReadOnlySetT<TSource, TSourceInner, TTargetInner>), null, null, null);
        }

        public override IReadOnlySet<TTargetInner>? Map(TSource? source, IReadOnlySet<TTargetInner>? target, Graph? graph)
        {
            if (source is null)
                return null;

            var sourceEnumerable = (IEnumerable<TSourceInner>)source;

            int sourceCount;
            if (sourceEnumerable is ICollection<TSourceInner> sourceCollection)
                sourceCount = sourceCollection.Count;
            else
                sourceCount = sourceEnumerable.Count();

            var targetSet = new HashSet<TTargetInner>(sourceCount);
            target = targetSet;

            var sourceEnumerator = sourceEnumerable.GetEnumerator();
            while (sourceEnumerator.MoveNext())
            {
                var item = converter.Map(sourceEnumerator.Current, default, graph);
                targetSet.Add(item!);
            }

            return target;
        }
    }
}

#endif
