// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Map.Converters.Collections.Dictionaries
{
    internal sealed class MapConverterIDictionaryT<TSource, TSourceKey, TSourceValue, TTargetKey, TTargetValue> : MapConverter<TSource, IDictionary<TTargetKey, TTargetValue>>
        where TSourceKey : notnull
        where TTargetKey : notnull
    {
        private MapConverter<KeyValuePair<TSourceKey, TSourceValue>, KeyValuePair<TTargetKey, TTargetValue>> converter = null!;

        protected override sealed void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<KeyValuePair<TSourceKey, TSourceValue>>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<KeyValuePair<TTargetKey, TTargetValue>>.GetTypeDetail();
            converter = (MapConverter<KeyValuePair<TSourceKey, TSourceValue>, KeyValuePair<TTargetKey, TTargetValue>>)MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterIDictionaryT<TSource, TSourceKey, TSourceValue, TTargetKey, TTargetValue>), null, null, null);
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
                var item = converter.Map(sourceEnumerator.Current, default, graph);
                target[item.Key] = item.Value;
            }

            return target;
        }
    }
}