// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.SourceGeneration;

namespace Zerra.Map.Converters.Collections
{
    internal sealed class MapConverterArray<TSource, TSourceInner, TTargetInner> : MapConverter<TSource, TTargetInner[]>
    {
        private MapConverter converter = null!;

        private static TSourceInner? SourceGetter(object parent) => ((IEnumerator<TSourceInner>)parent).Current;
        private static TTargetInner? TargetGetter(object parent) => ((ArrayAccessor<TTargetInner>)parent).Get();
        private static void TargetSetter(object parent, TTargetInner value) => ((ArrayAccessor<TTargetInner>)parent).Set(value);

        protected override void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<TSourceInner>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTargetInner>.GetTypeDetail();
            converter = MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterArray<TSource, TSourceInner, TTargetInner>), SourceGetter, TargetGetter, TargetSetter);
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

            ArrayAccessor<TTargetInner> targetAccessor;
            if (target == null || sourceCount != target.Length)
                target = new TTargetInner[sourceCount];

            targetAccessor = new ArrayAccessor<TTargetInner>(target);

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