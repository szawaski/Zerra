// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NETSTANDARD2_0
using Zerra.Buffers;
#else
using System.Buffers.Text;
#endif

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
#if NETSTANDARD2_0
            if (inArray.Length == 0)
                return String.Empty;

            var rentedBytes = ArrayPoolHelper<byte>.Rent(inArray.Length);
            inArray.CopyTo(rentedBytes);
            var rentedChars = ArrayPoolHelper<char>.Rent((inArray.Length + 2) / 3 * 4);

            var length = Convert.ToBase64CharArray(rentedBytes, 0, inArray.Length, rentedChars, 0);
            ArrayPoolHelper<byte>.Return(rentedBytes);

            if (rentedChars[length - 1] == '=')
            {
                length--;
                if (rentedChars[length - 1] == '=')
                    length--;
            }

            for (var i = 0; i < length; i++)
            {
                switch (rentedChars[i])
                {
                    case '+':
                        rentedChars[i] = '-';
                        break;
                    case '/':
                        rentedChars[i] = '_';
                        break;
                }
            }

            var result = new string(rentedChars, 0, length);
            ArrayPoolHelper<char>.Return(rentedChars);
            return result;
#else
            return Base64Url.EncodeToString(inArray);
#endif
        }

        /// <summary>
        /// Converts a URL-safe base64 string into bytes.
        /// </summary>
        /// <param name="chars">The string to convert.</param>
        /// <returns>The resulting bytes.</returns>
        /// <exception cref="FormatException">Thrown when the string format is invalid.</exception>
        public static byte[] FromBase64UrlString(ReadOnlySpan<char> chars)
        {
#if NETSTANDARD2_0
            var filteredLength = (chars.Length % 4) switch
            {
                0 => chars.Length,
                2 => chars.Length + 2,
                3 => chars.Length + 1,
                _ => throw new FormatException("Invalid string"),
            };

            if (filteredLength == 0)
                return Array.Empty<byte>();

            var rentedChars = ArrayPoolHelper<char>.Rent(filteredLength);

            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                rentedChars[i] = c switch
                {
                    '-' => '+',
                    '_' => '/',
                    _ => c,
                };
            }

            for (var i = chars.Length; i < filteredLength; i++)
                rentedChars[i] = '=';

            try
            {
                return Convert.FromBase64CharArray(rentedChars, 0, filteredLength);
            }
            finally
            {
                ArrayPoolHelper<char>.Return(rentedChars);
            }
#else
            return Base64Url.DecodeFromChars(chars);
#endif
        }
    }
}
