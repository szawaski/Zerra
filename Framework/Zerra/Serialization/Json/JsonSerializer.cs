// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json.Converters;

namespace Zerra.Serialization.Json
{
    public partial class JsonSerializer
    {
        private const int defaultBufferSize = 16 * 1024;

        private static readonly JsonSerializerOptions defaultOptions = new();

        public static void AddConverter(Type converterType, Func<JsonConverter> converter) => JsonConverterFactory.AddConverter(converterType, converter);
    }
}