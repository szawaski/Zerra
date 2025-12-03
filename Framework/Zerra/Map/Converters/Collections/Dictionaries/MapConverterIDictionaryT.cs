// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.SourceGeneration;

namespace Zerra.Map
{
    internal sealed class MapConverterIDictionaryT<TSource, TSourceKey, TSourceValue, TTargetKey, TTargetValue> : MapConverter<TSource, IDictionary<TTargetKey, TTargetValue>>
        where TSourceKey : notnull
        where TTargetKey : notnull
    {
        private MapConverter converter = null!;

        private static KeyValuePair<TSourceKey, TSourceValue> SourceGetter(object parent) => ((IEnumerator<KeyValuePair<TSourceKey, TSourceValue>>)parent).Current;
        private static void TargetSetter(object parent, KeyValuePair<TTargetKey, TTargetValue> value) => ((IDictionary<TTargetKey, TTargetValue>)parent)[value.Key] = value.Value;

        protected override sealed void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<KeyValuePair<TSourceKey, TSourceValue>>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<KeyValuePair<TTargetKey, TTargetValue>>.GetTypeDetail();
            converter = MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterIDictionaryT<TSource, TSourceKey, TSourceValue, TTargetKey, TTargetValue>), SourceGetter, null, TargetSetter);
        }

        public override IDictionary<TTargetKey, TTargetValue>? Map(TSource? source, IDictionary<TTargetKey, TTargetValue>? target, Graph? graph)
        {
            if (source is null)
                return null;

            var sourceEnumerable = (IEnumerable<KeyValuePair<TSourceKey, TSourceValue>>)source;

            int sourceCount;
            if (sourceEnumerable is ICollection<KeyValuePair<TSourceKey, TSourceValue>> sourceCollection)
                sourceCount = sourceCollection.Count;
            else
                sourceCount = sourceEnumerable.Count();

            if (target == null || sourceCount != target.Count)
                target = new Dictionary<TTargetKey, TTargetValue>(sourceCount);
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