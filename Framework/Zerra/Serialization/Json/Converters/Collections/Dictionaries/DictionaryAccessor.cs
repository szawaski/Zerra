using System.Globalization;

namespace Zerra.Serialization.Json.Converters.Collections.Dictionaries
{
    internal sealed class DictionaryAccessor<TKey, TValue>
        where TKey: notnull
    {
        private readonly Dictionary<TKey, TValue> dictionary;
        public Dictionary<TKey, TValue> Dictionary => dictionary;

        private TKey? key;
        public string? GetCurrentKeyString(bool enumAsNumber)
        {
            if (key is null)
                return null;
            if (typeof(TKey) == typeof(string))
                return (string)(object)key;
            if (typeof(TKey) == typeof(DateTime) || typeof(TKey) == typeof(DateTimeOffset))
            {
#if NETSTANDARD2_0
                return typeof(TKey) == typeof(DateTime) ? ((DateTime)(object)key).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK", CultureInfo.InvariantCulture) : ((DateTimeOffset)(object)key).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK", CultureInfo.InvariantCulture);
#else
                Span<char> dateChars = stackalloc char[33];
                int dateLength;
                if (typeof(TKey) == typeof(DateTime))
                    _ = ((DateTime)(object)key).TryFormat(dateChars, out dateLength, "O", CultureInfo.InvariantCulture);
                else
                    _ = ((DateTimeOffset)(object)key).TryFormat(dateChars, out dateLength, "O", CultureInfo.InvariantCulture);
                var fractionEnd = 27;
                while (fractionEnd > 20 && dateChars[fractionEnd - 1] == '0')
                    fractionEnd--;
                if (fractionEnd == 20)
                    fractionEnd = 19;
                dateChars.Slice(27, dateLength - 27).CopyTo(dateChars.Slice(fractionEnd));
                return new string(dateChars.Slice(0, fractionEnd + dateLength - 27));
#endif
            }
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
