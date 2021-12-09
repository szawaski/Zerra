// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Identity
{
    public static class Base64Url
    {
        public static byte[] FromBase64String(string s)
        {
            var output = s;
            output = output.Replace('-', '+');
            output = output.Replace('_', '/');
            switch (output.Length % 4)
            {
                case 2: output += "=="; break;
                case 3: output += "="; break;
            }
            var converted = Convert.FromBase64String(output);
            return converted;
        }

        private static readonly char[] padding = { '=' };
        public static string ToBase64String(byte[] b)
        {
            return Convert.ToBase64String(b).TrimEnd(padding).Replace('+', '-').Replace('/', '_');
        }
    }
}
