// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.IO;

namespace Zerra.CQRS.Network
{
    public static class Base64UrlEncoder
    {
        public static string ToBase64String(byte[] inArray)
        {
            var arrayString = Convert.ToBase64String(inArray);
            var chars = arrayString.AsSpan();

            int filteredLength = chars.Length;
            if (chars.Length > 0 && chars[chars.Length - 1] == '=')
            {
                filteredLength--;
                if (chars.Length > 1 && chars[chars.Length - 2] == '=')
                    filteredLength--;
            }

            var filtered = BufferArrayPool<char>.Rent(filteredLength);
            for (var i = 0; i < filteredLength; i++)
            {
                var c = chars[i];
                filtered[i] = c switch
                {
                    '+' => '-',
                    '/' => '_',
                    _ => c,
                };
            }
            var filteredString = new String(filtered, 0, filteredLength);
            Array.Clear(filtered, 0, filteredLength);
            BufferArrayPool<char>.Return(filtered);
            return filteredString;
        }

        public static byte[] FromBase64String(string s)
        {
            var chars = s.ToCharArray();
            var filteredLength = (chars.Length % 4) switch
            {
                0 => chars.Length,
                2 => chars.Length + 2,
                3 => chars.Length + 1,
                _ => throw new FormatException("Invalid string"),
            };
            var filtered = BufferArrayPool<char>.Rent(filteredLength);
            for (var i = 0; i < filteredLength; i++)
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
                    filtered[filteredLength - 1] = '=';
                    filtered[filteredLength] = '=';
                    break;
                case 3:
                    filtered[filteredLength - 1] = '=';
                    break;
            }

            var filteredString = new String(filtered, 0, filteredLength);
            Array.Clear(filtered, 0, filteredLength);
            BufferArrayPool<char>.Return(filtered);
            return Convert.FromBase64String(filteredString);
        }
    }
}