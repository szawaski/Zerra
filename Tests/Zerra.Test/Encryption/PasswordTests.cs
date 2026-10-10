// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using Xunit;
using Zerra.Encryption;

namespace Zerra.Test.Encryption
{
    public class PasswordTests
    {
        [Fact]
        public void PasswordGenerate()
        {
            var test = Password.GeneratePassword(10, true, true, true, true);
            Assert.Equal(10, test.Length);

            test = Password.GeneratePassword(10, true, false, false, false);
            Assert.Equal(10, test.Length);
            Assert.True(test.All(Char.IsUpper));

            test = Password.GeneratePassword(10, false, true, false, false);
            Assert.Equal(10, test.Length);
            Assert.True(test.All(Char.IsLower));

            test = Password.GeneratePassword(10, false, false, true, false);
            Assert.Equal(10, test.Length);
            Assert.True(test.All(Char.IsNumber));

            test = Password.GeneratePassword(10, false, false, false, true);
            Assert.Equal(10, test.Length);
            Assert.True(test.All(x => !Char.IsLetterOrDigit(x)));

            //long enough that building it on the stack would overflow
            Assert.Equal(2_000_000, Password.GeneratePassword(2_000_000, true, true, true, true).Length);
        }

        [Fact]
        public void PasswordGenerate_HasEverySetChosen()
        {
            for (var i = 0; i < 500; i++)
            {
                var test = Password.GeneratePassword(4, true, true, true, true);
                Assert.Contains(test, Char.IsUpper);
                Assert.Contains(test, Char.IsLower);
                Assert.Contains(test, Char.IsDigit);
                Assert.Contains(test, x => !Char.IsLetterOrDigit(x));
            }
        }

        [Fact]
        public void PasswordGenerate_InvalidArguments()
        {
            _ = Assert.Throws<ArgumentException>(() => Password.GeneratePassword(10, false, false, false, false));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => Password.GeneratePassword(0, true, false, false, false));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => Password.GeneratePassword(3, true, true, true, true));
        }

        [Fact]
        public void PasswordGenerate_EveryLetterEquallyLikely()
        {
            var counts = Password.GeneratePassword(260_000, true, false, false, false).GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
            Assert.Equal(26, counts.Count);
            //about 10,000 each
            Assert.All(counts.Values, x => Assert.InRange(x, 9_000, 11_000));
        }

        [Fact]
        public void RandomNumber_EveryValueEquallyLikely()
        {
            using var rng = RandomNumberGenerator.Create();
            var counts = new int[3];
            for (var i = 0; i < 60_000; i++)
                counts[Password.GetRandomNumber(rng, 0, 2)]++;
            //about 20,000 each, the ends used to get half as many
            Assert.All(counts, x => Assert.InRange(x, 18_500, 21_500));

            Assert.Equal(3, Password.GetRandomNumber(rng, 3, 3));
            Assert.InRange(Password.GetRandomNumber(rng, Int32.MinValue, Int32.MaxValue), Int32.MinValue, Int32.MaxValue);
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => Password.GetRandomNumber(rng, 5, 1));
        }
    }
}
