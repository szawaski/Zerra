// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.SourceGeneration;

namespace Zerra.Map
{
    internal sealed class MapConverterIListT<TSource, TSourceInner, TTargetInner> : MapConverter<TSource, IList<TTargetInner>>
    {
        private MapConverter converter = null!;

        private static TSourceInner? SourceGetter(object parent) => ((IEnumerator<TSourceInner>)parent).Current;
        private static TTargetInner? TargetGetter(object parent) => ((ListAccessor<TTargetInner>)parent).Get();
        private static void TargetSetter(object parent, TTargetInner value) => ((ListAccessor<TTargetInner>)parent).Set(value);

        protected override sealed void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<TSourceInner>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTargetInner>.GetTypeDetail();
            converter = MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterIListT<TSource, TSourceInner, TTargetInner>), SourceGetter, TargetGetter, TargetSetter);
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

            var targetAccessor = new ListAccessor<TTargetInner>(target);

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