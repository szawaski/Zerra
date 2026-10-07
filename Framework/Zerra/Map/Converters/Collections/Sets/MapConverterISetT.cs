// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Map.Converters.Collections.Sets
{
    internal sealed class MapConverterISetT<TSource, TSourceInner, TTargetInner> : MapConverter<TSource, ISet<TTargetInner>>
    {
        private MapConverter<TSourceInner, TTargetInner> converter = null!;

        protected override sealed void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<TSourceInner>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTargetInner>.GetTypeDetail();
            converter = (MapConverter<TSourceInner, TTargetInner>)MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterISetT<TSource, TSourceInner, TTargetInner>), null, null, null);
        }

        public override ISet<TTargetInner>? Map(TSource? source, ISet<TTargetInner>? target, Graph? graph)
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
#if NETSTANDARD2_0
                target = new HashSet<TTargetInner>();
#else
                target = new HashSet<TTargetInner>(sourceCount);
#endif
            else
                target.Clear();

            var sourceEnumerator = sourceEnumerable.GetEnumerator();
            while (sourceEnumerator.MoveNext())
            {
                var item = converter.Map(sourceEnumerator.Current, default, graph);
                target.Add(item!);
            }

            return target;
        }
    }
}