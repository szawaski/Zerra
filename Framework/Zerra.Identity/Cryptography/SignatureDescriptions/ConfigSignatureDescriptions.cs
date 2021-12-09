// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Cryptography;

namespace Zerra.Identity.Cryptography
{
    public static class ConfigSignatureDescriptions
    {
        private static object locker = new object();
        private static bool registered = false;
        public static void Add()
        {
            if (registered)
                return;

            lock (locker)
            {
                if (registered)
                    return;

                CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA1SignatureDescription), Algorithms.RsaSha1Url);
                CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA256SignatureDescription), Algorithms.RsaSha256Url);
                CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA384SignatureDescription), Algorithms.RsaSha384Url);
                CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA512SignatureDescription), Algorithms.RsaSha512Url);
                registered = true;
            }
        }
    }
}
