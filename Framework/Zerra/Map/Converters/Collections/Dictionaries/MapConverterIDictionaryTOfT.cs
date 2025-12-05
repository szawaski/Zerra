// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.SourceGeneration;

namespace Zerra.Map.Converters.Collections.Dictionaries
{
    internal sealed class MapConverterIDictionaryTOfT<TSource, TTarget, TSourceKey, TSourceValue, TTargetKey, TTargetValue> : MapConverter<TSource, TTarget>
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
            converter = MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterIDictionaryTOfT<TSource, TTarget, TSourceKey, TSourceValue, TTargetKey, TTargetValue>), SourceGetter, null, TargetSetter);
        }

        public override TTarget? Map(TSource? source, TTarget? target, Graph? graph)
        {
            if (source is null)
                return default;

            var sourceEnumerable = (IEnumerable<KeyValuePair<TSourceKey, TSourceValue>>)source;

            var targetDictionary = (IDictionary<TTargetKey, TTargetValue>?)target;

            int sourceCount;
            if (sourceEnumerable is ICollection<KeyValuePair<TSourceKey, TSourceValue>> sourceCollection)
                sourceCount = sourceCollection.Count;
            else
                sourceCount = sourceEnumerable.Count();

            if (targetDictionary == null || sourceCount != targetDictionary.Count)
            {
                targetDictionary = new Dictionary<TTargetKey, TTargetValue>(sourceCount);
                target = (TTarget)targetDictionary;
            }
            else
            {
                targetDictionary.Clear();
            }

            var sourceEnumerator = sourceEnumerable.GetEnumerator();
            while (sourceEnumerator.MoveNext())
            {
                converter.MapFromParent(sourceEnumerator, targetDictionary, graph);
            }

            return target;
        }
    }
}