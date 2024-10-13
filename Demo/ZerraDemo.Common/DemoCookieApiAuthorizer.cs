using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Zerra;
using Zerra.CQRS.Network;
using Zerra.Encryption;

namespace ZerraDemo.Common
{
    public sealed class DemoCookieApiAuthorizer : ICqrsAuthorizer
    {
        private const string cookieName = "ZerraDemoCookie";
        private const string cookieHeader = "Cookie";
        private const string authorizeHeader = "Authorize";

        private const SymmetricAlgorithmType encryptionAlgorithm = SymmetricAlgorithmType.AESwithShift;
        private readonly SymmetricKey encryptionKey;
        public DemoCookieApiAuthorizer()
        {
            var authenticationKey = Config.GetSetting("AuthenticationKey");
            if (authenticationKey is null)
                throw new Exception("Missing Config AuthenticationKey");
            this.encryptionKey = SymmetricEncryptor.GetKey(authenticationKey);
        }

        public void Authorize(IDictionary<string, IList<string?>> headers)
        {
            if (!headers.TryGetValue(cookieHeader, out var cookieHeaderValue))
            {
                if (!headers.TryGetValue(authorizeHeader, out cookieHeaderValue))
                {
                    return;
                }
            }

            var cookieValue = cookieHeaderValue[0];
            if (String.IsNullOrWhiteSpace(cookieValue))
                return;

            var cookies = CookieParser.CookiesFromString(cookieValue);
            if (cookies.TryGetValue(cookieName, out var authCookieDataEncoded))
            {
                var authCookieDataEncrypted = Base64UrlEncoder.FromBase64String(authCookieDataEncoded);
                var authCookieDataBytes = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, authCookieDataEncrypted);
                var authCookieData = Encoding.UTF8.GetString(authCookieDataBytes);
                if (authCookieData == "I can access this")
                {
                    var claims = new Claim[] {
                        new (ClaimTypes.Authentication, Boolean.TrueString),
                        new (ClaimTypes.NameIdentifier, "1234", ClaimValueTypes.String),
                        new (ClaimTypes.Name, "Tester", ClaimValueTypes.String),
                        new (ClaimTypes.Role, "Admin", ClaimValueTypes.String)
                    };

                    var identity = new ClaimsIdentity(claims, "Cookies");
                    var principal = new ClaimsPrincipal(identity);
                    System.Threading.Thread.CurrentPrincipal = principal;
                }
            }
        }

        public IDictionary<string, IList<string?>> BuildAuthHeaders()
        {
            var authCookieData = "I can access this";
            var authCookieDataBytes = Encoding.UTF8.GetBytes(authCookieData);
            var authCookieDataEncrypted = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, authCookieDataBytes);
            var authCookieDataEncoded = Base64UrlEncoder.ToBase64String(authCookieDataEncrypted);

            var cookies = new Dictionary<string, string>
            {
                { cookieName, authCookieDataEncoded }
            };

            var cookieHeaderValue = CookieParser.CookiesToString(cookies);
            var headers = new Dictionary<string, IList<string?>>
            {
                { cookieHeader, new List<string?>() { cookieHeaderValue } }
            };

            return headers;
        }
    }
}
