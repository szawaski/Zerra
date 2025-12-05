// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Map.Converters;
using Zerra.SourceGeneration;

namespace Zerra.Map.Converters.Collections
{
    internal sealed class MapConverterArrayToArray<TSourceInner, TTargetInner> : MapConverter<TSourceInner[], TTargetInner[]>
    {
        private MapConverter converter = null!;

        private static TSourceInner? SourceGetter(object parent) => ((ArrayAccessor<TSourceInner>)parent).Get();
        private static TTargetInner? TargetGetter(object parent) => ((ArrayAccessor<TTargetInner>)parent).Get();
        private static void TargetSetter(object parent, TTargetInner value) => ((ArrayAccessor<TTargetInner>)parent).Set(value);

        protected override void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<TSourceInner>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTargetInner>.GetTypeDetail();
            converter = MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterArrayToArray<TSourceInner, TTargetInner>), SourceGetter, TargetGetter, TargetSetter);
        }
        public override TTargetInner[]? Map(TSourceInner[]? source, TTargetInner[]? target, Graph? graph)
        {
            if (source is null)
                return null;

            var sourceAccessor = new ArrayAccessor<TSourceInner>(source);

            ArrayAccessor<TTargetInner> targetAccessor;
            if (target == null || source.Length != target.Length)
                target = new TTargetInner[source.Length];

            targetAccessor = new ArrayAccessor<TTargetInner>(target);

            for (var i = 0; i < source.Length; i++)
            {
                converter.MapFromParent(sourceAccessor, targetAccessor, graph);
                sourceAccessor.Index++;
                targetAccessor.Index++;
            }

            return target;
        }
    }
}