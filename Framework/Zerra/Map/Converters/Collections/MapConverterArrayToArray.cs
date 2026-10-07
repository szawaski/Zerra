// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Map.Converters.Collections
{
    internal sealed class MapConverterArrayToArray<TSourceInner, TTargetInner> : MapConverter<TSourceInner[], TTargetInner[]>
    {
        private MapConverter<TSourceInner, TTargetInner> converter = null!;

        protected override void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<TSourceInner>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTargetInner>.GetTypeDetail();
            converter = (MapConverter<TSourceInner, TTargetInner>)MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterArrayToArray<TSourceInner, TTargetInner>), null, null, null);
        }
        public override TTargetInner[]? Map(TSourceInner[]? source, TTargetInner[]? target, Graph? graph)
        {
            if (source is null)
                return null;

            if (target == null || source.Length != target.Length)
                target = new TTargetInner[source.Length];

            for (var i = 0; i < source.Length; i++)
                target[i] = converter.Map(source[i], target[i], graph)!;

            return target;
        }
    }
}