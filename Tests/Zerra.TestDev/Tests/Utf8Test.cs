// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zerra.TestDev
{
    public static class Utf8Test
    {
        private static void Test()
        {
            var chars = new char[(int)char.MaxValue];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = (char)i;

            var charsFromBytes = new char[(int)byte.MaxValue];
            for (byte i = 0; i < charsFromBytes.Length; i++)
                charsFromBytes[i] = (char)i;

            var utf16 = new Dictionary<char, byte[]>();
            var utf8 = new Dictionary<char, byte[]>();
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                utf16.Add(c, Encoding.Unicode.GetBytes([c]));
                utf8.Add(c, Encoding.UTF8.GetBytes([c]));
            }
            var max16 = utf16.Values.Select(x => x.Length).Max();
            var max8 = utf8.Values.Select(x => x.Length).Max();

            var oneByte = utf8.Where(x => x.Value.Length == 1).ToDictionary(x => x.Key, x => x.Value[0]);
            var oneByteMin = oneByte.Values.Min();
            var oneByteMax = oneByte.Values.Max();
            var oneByteRange = oneByte.Values.Distinct().ToArray();

            var twoByte = utf8.Where(x => x.Value.Length == 2).ToDictionary(x => x.Key, x => x.Value[0]);
            var twoByteView = utf8.Where(x => x.Value.Length == 2).ToDictionary(x => x.Key, x => $"[{x.Value[0]},{x.Value[1]}]");
            var twoByteMin = twoByte.Values.Min();
            var twoByteMax = twoByte.Values.Max();
            var twoByteRange = twoByte.Values.Distinct().ToArray();

            var threeByte = utf8.Where(x => x.Value.Length == 3).ToDictionary(x => x.Key, x => x.Value[0]);
            var threeByteView = utf8.Where(x => x.Value.Length == 3).ToDictionary(x => x.Key, x => $"[{x.Value[0]},{x.Value[1]},{x.Value[2]}]");
            var threeByteMin = threeByte.Values.Min();
            var threeByteMax = threeByte.Values.Max();
            var threeByteRange = threeByte.Values.Distinct().ToArray();
        }
    }
}
