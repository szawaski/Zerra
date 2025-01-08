// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Zerra.Encryption
{
    /// <summary>
    /// A helper for generating passwords and randomness.
    /// </summary>
    public static class Password
    {
        private static readonly char[] passwordCharactersUpper = "ABCDEFGHIJKLMNOPQRSTUVWXUYZ".ToCharArray();
        private static readonly char[] passwordCharactersLower = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
        private static readonly char[] passwordCharactersNumeric = "0123456789".ToCharArray();
        private static readonly char[] passwordCharactersSpecial = " !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~".ToCharArray(); //OWASP

        /// <summary>
        /// Generates a random password within the given parameters.
        /// </summary>
        /// <param name="length">The length of the password.</param>
        /// <param name="upperCase">The password may include upper case characters.</param>
        /// <param name="lowerCase">The password may include lower case characters.</param>
        /// <param name="numeric">The password may contain numbers.</param>
        /// <param name="owaspSpecialCharacters">The password may contain special characters defined by the Open Web Application Security Project (OWASP).</param>
        /// <returns>The new random password.</returns>
        public static unsafe string GeneratePassword(int length, bool upperCase, bool lowerCase, bool numeric, bool owaspSpecialCharacters)
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

                var chars = stackalloc char[length];
                for (var i = 0; i < length; i++)
                {
                    var random = GetRandomNumber(rng, 0, passwordChars.Count - 1);
                    chars[i] = passwordChars[random];
                }
                return new string(chars);
            }
        }

#if NETSTANDARD2_0
        /// <summary>
        /// Given a <see cref="RandomNumberGenerator"/> this produces an integer within the range of the constrains.
        /// </summary>
        /// <param name="rng">The random number generator</param>
        /// <param name="minValue">The smallest allowed value of the random number</param>
        /// <param name="maxValue">The largest allowed value of the random number</param>
        /// <returns>The new random number</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static unsafe int GetRandomNumber(RandomNumberGenerator rng, int minValue, int maxValue)
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
#else
        /// <summary>
        /// Given a <see cref="RandomNumberGenerator"/> this produces an integer within the range of the constrains.
        /// </summary>
        /// <param name="rng">The random number generator</param>
        /// <param name="minValue">The smallest allowed value of the random number</param>
        /// <param name="maxValue">The largest allowed value of the random number</param>
        /// <returns>The new random number</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static int GetRandomNumber(RandomNumberGenerator rng, int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));
            if (minValue == maxValue)
                return minValue;
            long diff = maxValue - minValue;
            Span<byte> intBuffer = stackalloc byte[4];

            rng.GetBytes(intBuffer);
            var rand = BitConverter.ToUInt32(intBuffer);
            return (int)Math.Round(((rand / (double)UInt32.MaxValue) * diff) + minValue);
        }
#endif
    }
}
