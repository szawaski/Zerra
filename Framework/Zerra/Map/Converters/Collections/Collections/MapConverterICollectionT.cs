// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.SourceGeneration;

namespace Zerra.Map.Converters.Collections.Collections
{
    internal sealed class MapConverterICollectionT<TSource, TSourceInner, TTargetInner> : MapConverter<TSource, ICollection<TTargetInner>>
    {
        private MapConverter converter = null!;

        private static TSourceInner? SourceGetter(object parent) => ((IEnumerator<TSourceInner>)parent).Current;
        private static void TargetSetter(object parent, TTargetInner value) => ((ICollection<TTargetInner>)parent).Add(value);

        protected override sealed void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<TSourceInner>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTargetInner>.GetTypeDetail();
            converter = MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterICollectionT<TSource, TSourceInner, TTargetInner>), SourceGetter, null, TargetSetter);
        }

        public override ICollection<TTargetInner>? Map(TSource? source, ICollection<TTargetInner>? target, Graph? graph)
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
            else
                target.Clear();

            var sourceEnumerator = sourceEnumerable.GetEnumerator();
            while (sourceEnumerator.MoveNext())
            {
                converter.MapFromParent(sourceEnumerator, target, graph);
            }

            return target;
        }
    }
}