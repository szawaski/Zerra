// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Encryption;

namespace Zerra.Test.Encryption
{
    public class Base64Tests
    {
        [Fact]
        public void Base64UrlEncodeDecode()
        {
            var bytes = new byte[byte.MaxValue + 1];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)i;
            
            var str = Base64UrlEncoder.ToBase64UrlString(bytes);
            var decodedBytes = Base64UrlEncoder.FromBase64UrlString(str);
            Assert.True(bytes.SequenceEqual(decodedBytes));
        }

        [Fact]
        public void Base64Url_EveryLength()
        {
            //each remainder of the length, short ones on the stack and long ones rented
            for (var length = 0; length <= 120; length++)
            {
                var bytes = Enumerable.Range(0, length).Select(x => (byte)(x * 37 + 251)).ToArray();
                var encoded = Base64UrlEncoder.ToBase64UrlString(bytes);
                Assert.DoesNotContain('=', encoded);
                Assert.DoesNotContain('+', encoded);
                Assert.DoesNotContain('/', encoded);
                Assert.Equal(bytes, Base64UrlEncoder.FromBase64UrlString(encoded));
            }
            _ = Assert.Throws<FormatException>(() => Base64UrlEncoder.FromBase64UrlString("abcde"));
        }
    }
}
