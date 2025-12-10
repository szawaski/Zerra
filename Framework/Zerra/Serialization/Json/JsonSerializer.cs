// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json.Converters;

namespace Zerra.Serialization.Json
{
    /// <summary>
    /// Converts objects to JSON and back with comprehensive formatting and type support options.
    /// </summary>
    public partial class JsonSerializer
    {
        private const int defaultBufferSize = 16 * 1024;

        private static readonly JsonSerializerOptions defaultOptions = new();

        /// <summary>
        /// Registers a custom converter for a specified type. This must be called before the first serialization or deserialization takes place.
        /// </summary>
        /// <param name="converterType">The type to register the converter for.</param>
        /// <param name="converter">A factory function that creates instances of the converter.</param>
        public static void AddConverter(Type converterType, Func<JsonConverter> converter) => JsonConverterFactory.AddConverter(converterType, converter);
    }
}