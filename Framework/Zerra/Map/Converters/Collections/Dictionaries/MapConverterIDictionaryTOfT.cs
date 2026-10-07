// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Map.Converters.Collections.Dictionaries
{
    internal sealed class MapConverterIDictionaryTOfT<TSource, TTarget, TSourceKey, TSourceValue, TTargetKey, TTargetValue> : MapConverter<TSource, TTarget>
        where TSourceKey : notnull
        where TTargetKey : notnull
    {
        private MapConverter<KeyValuePair<TSourceKey, TSourceValue>, KeyValuePair<TTargetKey, TTargetValue>> converter = null!;

        protected override sealed void Setup()
        {
            var sourceTypeDetail = TypeAnalyzer<KeyValuePair<TSourceKey, TSourceValue>>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<KeyValuePair<TTargetKey, TTargetValue>>.GetTypeDetail();
            converter = (MapConverter<KeyValuePair<TSourceKey, TSourceValue>, KeyValuePair<TTargetKey, TTargetValue>>)MapConverterFactory.Get(sourceTypeDetail, targetTypeDetail, nameof(MapConverterIDictionaryTOfT<TSource, TTarget, TSourceKey, TSourceValue, TTargetKey, TTargetValue>), null, null, null);
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
                if (!targetTypeDetail.HasCreator)
                    throw new NotSupportedException($"{targetTypeDetail.Type} has no public parameterless constructor.");
                target = targetTypeDetail.Creator!()!;
                targetDictionary = (IDictionary<TTargetKey, TTargetValue>)target;
            }
            else
            {
                targetDictionary.Clear();
            }

            var sourceEnumerator = sourceEnumerable.GetEnumerator();
            while (sourceEnumerator.MoveNext())
            {
                var item = converter.Map(sourceEnumerator.Current, default, graph);
                targetDictionary[item.Key] = item.Value;
            }

            return target;
        }
    }
}