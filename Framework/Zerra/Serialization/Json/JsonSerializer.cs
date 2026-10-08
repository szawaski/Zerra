// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
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
        /// <param name="type">The type to register the converter for.</param>
        /// <param name="converter">A factory function that creates instances of the converter.</param>
        public static void AddConverter(Type type, Func<JsonConverter> converter) => JsonConverterFactory.AddConverter(type, converter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowIfNotWhitespace(ReadOnlySpan<byte> remaining)
        {
            for (var i = 0; i < remaining.Length; i++)
            {
                var b = remaining[i];
                if (b != (byte)' ' && b != (byte)'\n' && b != (byte)'\r' && b != (byte)'\t')
                    throw new FormatException("Invalid JSON format, unexpected content after the value");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowIfNotWhitespace(ReadOnlySpan<char> remaining)
        {
            for (var i = 0; i < remaining.Length; i++)
            {
                var c = remaining[i];
                if (c != ' ' && c != '\n' && c != '\r' && c != '\t')
                    throw new FormatException("Invalid JSON format, unexpected content after the value");
            }
        }
    }
}