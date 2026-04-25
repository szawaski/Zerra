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
    }
}
