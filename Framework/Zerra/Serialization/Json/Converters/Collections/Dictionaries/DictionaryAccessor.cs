using System.Collections.Generic;

namespace Zerra.Serialization.Json.Converters.Collections.Dictionaries
{
    public sealed class DictionaryAccessor<TKey, TValue>
        where TKey: notnull
    {
        private readonly Dictionary<TKey, TValue> dictionary;
        public Dictionary<TKey, TValue> Dictionary => dictionary;

        private TKey? key;

        public DictionaryAccessor(Dictionary<TKey, TValue> dictionary)
        {
            this.dictionary = dictionary;
        }

        public void SetKey(TKey key)
        {
            this.key = key;
        }

        public void Add(TValue value)
        {
            dictionary.Add(key!, value);
        }
    }
}
