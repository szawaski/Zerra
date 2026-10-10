// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;

namespace Zerra.Encryption
{
    /// <summary>
    /// Generates random passwords.
    /// </summary>
    public static class Password
    {
        private static readonly char[] passwordCharactersUpper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private static readonly char[] passwordCharactersLower = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
        private static readonly char[] passwordCharactersNumeric = "0123456789".ToCharArray();
        private static readonly char[] passwordCharactersSpecial = " !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~".ToCharArray(); //OWASP

        /// <summary>
        /// Generates a random password from the character sets chosen, with at least one character from each.
        /// </summary>
        /// <param name="length">The length of the password, at least the number of character sets chosen.</param>
        /// <param name="upperCase">Include uppercase letters.</param>
        /// <param name="lowerCase">Include lowercase letters.</param>
        /// <param name="numeric">Include digits.</param>
        /// <param name="owaspSpecialCharacters">Include OWASP's special characters, including space.</param>
        /// <returns>The password.</returns>
        public static string GeneratePassword(int length, bool upperCase, bool lowerCase, bool numeric, bool owaspSpecialCharacters)
        {
            var sets = new List<char[]>(4);
            if (upperCase)
                sets.Add(passwordCharactersUpper);
            if (lowerCase)
                sets.Add(passwordCharactersLower);
            if (numeric)
                sets.Add(passwordCharactersNumeric);
            if (owaspSpecialCharacters)
                sets.Add(passwordCharactersSpecial);
            if (sets.Count == 0)
                throw new ArgumentException("At least one character set must be chosen");
            if (length < sets.Count)
                throw new ArgumentOutOfRangeException(nameof(length), $"Must be at least {sets.Count}, one character from each set chosen");

            var all = sets.SelectMany(x => x).ToArray();
            var chars = new char[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                //one from each set, the rest from all of them, then shuffled so the guaranteed ones aren't always first
                for (var i = 0; i < sets.Count; i++)
                    chars[i] = sets[i][GetRandomNumber(rng, 0, sets[i].Length - 1)];
                for (var i = sets.Count; i < length; i++)
                    chars[i] = all[GetRandomNumber(rng, 0, all.Length - 1)];
                for (var i = length - 1; i > 0; i--)
                {
                    var j = GetRandomNumber(rng, 0, i);
                    (chars[i], chars[j]) = (chars[j], chars[i]);
                }
            }
            return new string(chars);
        }

        /// <summary>
        /// Gets a random number where every value is equally likely.
        /// </summary>
        /// <param name="rng">The random number generator.</param>
        /// <param name="minValue">The smallest value.</param>
        /// <param name="maxValue">The largest value, included.</param>
        /// <returns>The random number.</returns>
        public static int GetRandomNumber(RandomNumberGenerator rng, int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));
            if (minValue == maxValue)
                return minValue;

            //values above the largest multiple of the range are rejected, so none are more likely than others
            var range = (ulong)((long)maxValue - minValue + 1);
            var limit = UInt32.MaxValue + 1UL - ((UInt32.MaxValue + 1UL) % range);
#if NETSTANDARD2_0
            var buffer = new byte[4];
#else
            Span<byte> buffer = stackalloc byte[4];
#endif
            for (; ; )
            {
                rng.GetBytes(buffer);
#if NETSTANDARD2_0
                var value = BitConverter.ToUInt32(buffer, 0);
#else
                var value = BitConverter.ToUInt32(buffer);
#endif
                if (value < limit)
                    return (int)(minValue + (long)(value % range));
            }
        }
    }
}
