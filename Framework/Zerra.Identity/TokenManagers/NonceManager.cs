// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Zerra.Identity.TokenManagers
{
    public static class NonceManager
    {
        private const int expirationSeconds = 300; //5min

        private static readonly ConcurrentDictionary<string, NonceInfo> nonces = new ConcurrentDictionary<string, NonceInfo>();

        private static readonly Thread expireThread;
        static NonceManager()
        {
            var threadStart = new ThreadStart(ExpireItems);
            expireThread = new Thread(threadStart)
            {
                Priority = ThreadPriority.Lowest
            };
            expireThread.Start();
        }

        public static string Generate(string serviceProvider)
        {
            var nonce = Guid.NewGuid().ToString();

            var nonceInfo = new NonceInfo
            {
                ServiceProvider = serviceProvider,
                Nonce = nonce,
                Time = DateTime.Now
            };
            nonces.TryAdd(nonce, nonceInfo);

            return nonce;
        }

        public static void Validate(string serviceProvider, string nonce)
        {
            nonces.TryRemove(nonce, out var nonceInfo);

            var valid = true;
            if (nonceInfo == null)
                valid = false;
            else if (serviceProvider != nonceInfo.ServiceProvider)
                valid = false;
            else if (nonceInfo.Time < DateTime.Now.AddSeconds(-expirationSeconds))
                valid = false;

            if (!valid)
            {
                throw new IdentityProviderException("Nonce is not valid. Authentication process may have timed out.");
            }
        }

        public static void ExpireItems()
        {
            while (true)
            {
                //ToArray is special ConcurrentDictionary method to capture state
                var expiredNonces = nonces.ToArray().Where(x => x.Value.Time < DateTime.Now.AddSeconds(-expirationSeconds)).ToArray();
                foreach (var nonceInfo in expiredNonces)
                {
                    nonces.TryRemove(nonceInfo.Key, out var removed);
                }

                Thread.Sleep(60000);
            }
        }

        private class NonceInfo
        {
            public string ServiceProvider { get; set; }
            public string Nonce { get; set; }
            public DateTime Time { get; set; }
        }
    }
}
