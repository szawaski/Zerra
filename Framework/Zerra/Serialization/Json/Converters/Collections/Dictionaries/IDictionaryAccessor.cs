using System.Globalization;

namespace Zerra.Serialization.Json.Converters.Collections.Dictionaries
{
    internal sealed class IDictionaryAccessor<TKey, TValue>
        where TKey : notnull
    {
        private readonly IDictionary<TKey, TValue> dictionary;
        public IDictionary<TKey, TValue> Dictionary => dictionary;

        private TKey? key;
        //the same names the dictionary converters write
        public string? CurrentKeyString => key switch
        {
            null => null,
            string str => str,
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
#if !NETSTANDARD2_0
            DateOnly dateOnly => dateOnly.ToString("O", CultureInfo.InvariantCulture),
            TimeOnly timeOnly => timeOnly.ToTimeSpan().ToString("c", CultureInfo.InvariantCulture),
#endif
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => key.ToString(),
        };

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
