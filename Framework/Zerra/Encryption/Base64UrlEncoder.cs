// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Buffers;

namespace Zerra.Encryption
{
    /// <summary>
    /// Deals with base64 URL-safe string encoding.
    /// </summary>
    public static class Base64UrlEncoder
    {
        /// <summary>
        /// Converts bytes into a base64 URL-safe string.
        /// </summary>
        /// <param name="inArray">The bytes to convert.</param>
        /// <returns>The resulting string.</returns>
        public static string ToBase64UrlString(ReadOnlySpan<byte> inArray)
        {
            var arrayString = Convert.ToBase64String(inArray);
            var chars = arrayString.AsSpan();

            var length = chars.Length;
            if (chars.Length > 0 && chars[^1] == '=')
            {
                length--;
                if (chars.Length > 1 && chars[^2] == '=')
                    length--;
            }

            scoped Span<char> filtered;
            char[]? rentedArray = null;
            if (length <= 128)
            {
                filtered = stackalloc char[length];
            }
            else
            {
                rentedArray = ArrayPoolHelper<char>.Rent(length);
                filtered = rentedArray;
            }
            for (var i = 0; i < length; i++)
            {
                var c = chars[i];
                filtered[i] = c switch
                {
                    '+' => '-',
                    '/' => '_',
                    _ => c,
                };
            }
            var filteredString = filtered[..length].ToString();
            if (rentedArray != null)
                ArrayPoolHelper<char>.Return(rentedArray);
            return filteredString;
        }

        /// <summary>
        /// Converts a URL-safe base64 string into bytes.
        /// </summary>
        /// <param name="s">The string to convert.</param>
        /// <returns>The resulting bytes.</returns>
        /// <exception cref="FormatException"></exception>
        public static byte[] FromBase64UrlString(ReadOnlySpan<char> chars)
        {
            var filteredLength = (chars.Length % 4) switch
            {
                0 => chars.Length,
                2 => chars.Length + 2,
                3 => chars.Length + 1,
                _ => throw new FormatException("Invalid string"),
            };

            scoped Span<char> filtered;
            char[]? rentedArray = null;
            if (filteredLength <= 128)
            {
                filtered = stackalloc char[filteredLength];
            }
            else
            {
                rentedArray = ArrayPoolHelper<char>.Rent(filteredLength);
                filtered = rentedArray;
            }

            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                filtered[i] = c switch
                {
                    '-' => '+',
                    '_' => '/',
                    _ => c,
                };
            }

            switch (chars.Length % 4)
            {
                case 2:
                    filtered[filteredLength - 2] = '=';
                    filtered[filteredLength - 1] = '=';
                    break;
                case 3:
                    filtered[filteredLength - 1] = '=';
                    break;
            }

            var filteredString = filtered[..filteredLength].ToString();
            if (rentedArray != null)
                ArrayPoolHelper<char>.Return(rentedArray);
            return Convert.FromBase64String(filteredString);
        }
    }
}