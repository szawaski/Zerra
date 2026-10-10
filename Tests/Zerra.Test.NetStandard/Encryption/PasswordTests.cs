// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;
using Xunit;
using Zerra.Encryption;

namespace Zerra.Test.NetStandard.Encryption
{
    public class PasswordTests
    {
        [Fact]
        public void PasswordGenerate()
        {
            for (var i = 0; i < 200; i++)
            {
                var test = Password.GeneratePassword(4, true, true, true, true);
                Assert.Equal(4, test.Length);
                Assert.Contains(test, Char.IsUpper);
                Assert.Contains(test, Char.IsLower);
                Assert.Contains(test, Char.IsDigit);
                Assert.Contains(test, x => !Char.IsLetterOrDigit(x));
            }
            _ = Assert.Throws<ArgumentException>(() => Password.GeneratePassword(10, false, false, false, false));
        }

        [Fact]
        public void RandomNumber_EveryValueEquallyLikely()
        {
            using var rng = RandomNumberGenerator.Create();
            var counts = new int[3];
            for (var i = 0; i < 60_000; i++)
                counts[Password.GetRandomNumber(rng, 0, 2)]++;
            Assert.All(counts, x => Assert.InRange(x, 18_500, 21_500));
        }
    }
}
