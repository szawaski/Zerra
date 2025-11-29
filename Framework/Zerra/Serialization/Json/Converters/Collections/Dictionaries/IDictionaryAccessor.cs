namespace Zerra.Serialization.Json.Converters.Collections.Dictionaries
{
    public sealed class IDictionaryAccessor<TKey, TValue>
        where TKey : notnull
    {
        private readonly IDictionary<TKey, TValue> dictionary;
        public IDictionary<TKey, TValue> Dictionary => dictionary;

        private TKey? key;
        public string? CurrentKeyString => key?.ToString();

        public IDictionaryAccessor(IDictionary<TKey, TValue> dictionary)
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
