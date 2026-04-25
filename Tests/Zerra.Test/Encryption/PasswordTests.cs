// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

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
        }
    }
}
