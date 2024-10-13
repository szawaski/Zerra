// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Zerra.Identity.TokenManagers
{
    public static class SamlIDManager
    {
        private const int expirationSeconds = 1800;

        private static readonly ConcurrentDictionary<string, SamlIDInfo> ids = new ConcurrentDictionary<string, SamlIDInfo>();

        private static readonly Thread expireThread;
        static SamlIDManager()
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
            var samlID = "_" + Guid.NewGuid().ToString();

            var samlIDInfo = new SamlIDInfo
            {
                ServiceProvider = serviceProvider,
                Nonce = samlID,
                Time = DateTime.Now
            };
            _ = ids.TryAdd(samlID, samlIDInfo);

            return samlID;
        }

        public static void Validate(string serviceProvider, string samlID)
        {
            _ = ids.TryRemove(samlID, out var samlIDInfo);

            var valid = true;
            if (samlIDInfo is null)
                valid = false;
            else if (serviceProvider != samlIDInfo.ServiceProvider)
                valid = false;
            else if(samlIDInfo.Time < DateTime.Now.AddSeconds(-expirationSeconds))
                valid = false;

            if (!valid)
            {
                throw new IdentityProviderException("Saml Document ID is not valid. Authentication process may have timed out.");
            }
        }

        public static void ExpireItems()
        {
            while (true)
            {
                //ToArray is special ConcurrentDictionary method to capture state
                var expiredIDs = ids.ToArray().Where(x => x.Value.Time < DateTime.Now.AddSeconds(-expirationSeconds)).ToArray();
                foreach (var samlIDInfo in expiredIDs)
                {
                    _ = ids.TryRemove(samlIDInfo.Key, out var removed);
                }

                Thread.Sleep(60000);
            }
        }

        private sealed class SamlIDInfo
        {
            public string ServiceProvider { get; set; }
            public string Nonce { get; set; }
            public DateTime Time { get; set; }
        }
    }
}
