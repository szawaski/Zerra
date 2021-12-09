// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Zerra.Identity.TokenManagers
{
    public static class AuthTokenManager
    {
        private const int accessCodeExpirationSeconds = 90;
        private const int authExpirationSeconds = 1800; //30min

        private static readonly ConcurrentDictionary<string, AccessCodeInfo> accessCodes = new ConcurrentDictionary<string, AccessCodeInfo>();
        private static readonly ConcurrentDictionary<string, AuthInfo> auths = new ConcurrentDictionary<string, AuthInfo>();

        private static readonly Thread expireThread;
        static AuthTokenManager()
        {
            var threadStart = new ThreadStart(ExpireItems);
            expireThread = new Thread(threadStart);
            expireThread.Priority = ThreadPriority.Lowest;
            expireThread.Start();
        }

        public static string GenerateAccessCode(string serviceProvider, IdentityModel identity)
        {
            var code = Guid.NewGuid().ToString();
            var token = Guid.NewGuid().ToString();

            var auth = new AuthInfo
            {
                ServiceProvider = serviceProvider,
                Token = token,
                Identity = identity,
                Time = DateTime.Now
            };
            auths.TryAdd(token, auth);

            var accessCode = new AccessCodeInfo
            {
                ServiceProvider = serviceProvider,
                AccessCode = code,
                Token = token,
                Time = DateTime.Now
            };
            accessCodes.TryAdd(code, accessCode);

            return code;
        }

        public static string GetToken(string serviceProvider, string code)
        {
            accessCodes.TryRemove(code, out AccessCodeInfo accessCode);

            if (accessCode == null)
                return null;
            if (serviceProvider != accessCode.ServiceProvider)
                return null;
            if (accessCode.Time < DateTime.Now.AddSeconds(-accessCodeExpirationSeconds))
                return null;

            return accessCode.Token;
        }

        public static IdentityModel GetIdentity(string serviceProvider, string token)
        {
            auths.TryGetValue(token, out AuthInfo auth);

            if (auth == null)
                return null;
            if (serviceProvider != auth.ServiceProvider)
                return null;
            if (auth.Time < DateTime.Now.AddSeconds(-authExpirationSeconds))
                return null;

            return auth.Identity;
        }

        public static void ExpireItems()
        {
            while (true)
            {
                //ToArray is special ConcurrentDictionary method to capture state
                var expiredAccessCodes = accessCodes.ToArray().Where(x => x.Value.Time < DateTime.Now.AddSeconds(-accessCodeExpirationSeconds)).ToArray();
                foreach (var expired in expiredAccessCodes)
                {
                    accessCodes.TryRemove(expired.Key, out AccessCodeInfo removed);
                }

                //ToArray is special ConcurrentDictionary method to capture state
                var expiredAuths = auths.ToArray().Where(x => x.Value.Time < DateTime.Now.AddSeconds(-authExpirationSeconds)).ToArray();
                foreach (var expired in expiredAuths)
                {
                    auths.TryRemove(expired.Key, out AuthInfo removed);
                }

                Thread.Sleep(60000);
            }
        }

        private class AccessCodeInfo
        {
            public string ServiceProvider { get; set; }
            public string AccessCode { get; set; }
            public string Token { get; set; }
            public DateTime Time { get; set; }
        }

        private class AuthInfo
        {
            public string ServiceProvider { get; set; }
            public string Token { get; set; }
            public IdentityModel Identity { get; set; }
            public DateTime Time { get; set; }
        }
    }
}
