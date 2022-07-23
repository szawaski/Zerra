// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Zerra.Encryption
{
    public static class Password
    {
        private static readonly char[] passwordCharactersUpper = "ABCDEFGHIJKLMNOPQRSTUVWXUYZ".ToCharArray();
        private static readonly char[] passwordCharactersLower = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
        private static readonly char[] passwordCharactersNumeric = "0123456789".ToCharArray();
        private static readonly char[] passwordCharactersSpecial = " !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~".ToCharArray(); //OWASP
        public static string GeneratePassword(int length, bool upperCase, bool lowerCase, bool numeric, bool owaspSpecialCharacters)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var passwordChars = new List<char>();
                if (upperCase)
                    passwordChars.AddRange(passwordCharactersUpper);
                if (lowerCase)
                    passwordChars.AddRange(passwordCharactersLower);
                if (numeric)
                    passwordChars.AddRange(passwordCharactersNumeric);
                if (owaspSpecialCharacters)
                    passwordChars.AddRange(passwordCharactersSpecial);

                var chars = new char[length];
                for (var x = 0; x < length; x++)
                {
                    var random = GetRandomNumber(rng, 0, passwordChars.Count);
                    chars[x] = passwordChars[random];
                }
                return new string(chars);
            }
        }

        public static int GetRandomNumber(RandomNumberGenerator rng, int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));
            if (minValue == maxValue)
                return minValue;
            long diff = maxValue - minValue;
            var intBuffer = new byte[4];

            rng.GetBytes(intBuffer);
            var rand = BitConverter.ToUInt32(intBuffer, 0);
            return (int)Math.Round(((rand / (double)UInt32.MaxValue) * diff) + minValue);
        }
    }
}
