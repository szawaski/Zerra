﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Buffers;

namespace Zerra.Encryption
{
    public static class Base64UrlEncoder
    {
        public static string ToBase64String(byte[] inArray)
        {
            var arrayString = Convert.ToBase64String(inArray);
            var chars = arrayString.AsSpan();

            var filteredLength = chars.Length;
            if (chars.Length > 0 && chars[chars.Length - 1] == '=')
            {
                filteredLength--;
                if (chars.Length > 1 && chars[chars.Length - 2] == '=')
                    filteredLength--;
            }

            var filtered = ArrayPoolHelper<char>.Rent(filteredLength);
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
            var filteredString = new string(filtered, 0, filteredLength);
            ArrayPoolHelper<char>.Return(filtered);
            return filteredString;
        }

        public static byte[] FromBase64String(string s)
        {
            var chars = s.AsSpan();
            var filteredLength = (chars.Length % 4) switch
            {
                0 => chars.Length,
                2 => chars.Length + 2,
                3 => chars.Length + 1,
                _ => throw new FormatException("Invalid string"),
            };
            var filtered = ArrayPoolHelper<char>.Rent(filteredLength);
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

            var filteredString = new string(filtered, 0, filteredLength);
            ArrayPoolHelper<char>.Return(filtered);
            return Convert.FromBase64String(filteredString);
        }
    }
}