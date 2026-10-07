using System.Globalization;

namespace Zerra.Serialization.Json.Converters.Collections.Dictionaries
{
    internal sealed class IDictionaryAccessor<TKey, TValue>
        where TKey : notnull
    {
        private readonly IDictionary<TKey, TValue> dictionary;
        public IDictionary<TKey, TValue> Dictionary => dictionary;

        private TKey? key;
        public string? GetCurrentKeyString(bool enumAsNumber)
        {
            if (key is null)
                return null;
            if (typeof(TKey) == typeof(string))
                return (string)(object)key;
            if (typeof(TKey) == typeof(DateTime))
                return ((DateTime)(object)key).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK", CultureInfo.InvariantCulture);
            if (typeof(TKey) == typeof(DateTimeOffset))
                return ((DateTimeOffset)(object)key).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK", CultureInfo.InvariantCulture);
#if !NETSTANDARD2_0
            if (typeof(TKey) == typeof(DateOnly))
                return ((DateOnly)(object)key).ToString("O", CultureInfo.InvariantCulture);
            if (typeof(TKey) == typeof(TimeOnly))
                return ((TimeOnly)(object)key).ToTimeSpan().ToString("c", CultureInfo.InvariantCulture);
#endif
            if (typeof(TKey).IsEnum)
                return enumAsNumber ? ((Enum)(object)key).ToString("D") : EnumName.GetName(typeof(TKey), key);
            if (key is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            return key.ToString();
        }

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
