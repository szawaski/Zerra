using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Zerra;
using Zerra.CQRS.Network;
using Zerra.Encryption;

namespace ZerraDemo.Common
{
    public class DemoCookieApiAuthorizer : IHttpApiAuthorizer
    {
        private const string cookieName = "ZerraDemoCookie";
        private const string cookieHeader = "Cookie";
        private const string authorizeHeader = "Authorize";

        private const SymmetricAlgorithmType encryptionAlgorithm = SymmetricAlgorithmType.AESwithShift;
        private readonly SymmetricKey encryptionKey;
        public DemoCookieApiAuthorizer()
        {
            this.encryptionKey = SymmetricEncryptor.GetKey(Config.GetSetting("AuthenticationKey"));
        }

        public void Authorize(HttpRequestHeader header)
        {
            if (!header.Headers.TryGetValue(cookieHeader, out IList<string> cookieHeaderValue))
            {
                if (!header.Headers.TryGetValue(authorizeHeader, out cookieHeaderValue))
                {
                    return;
                }
            }

            var cookies = CookieParser.CookiesFromString(cookieHeaderValue[0]);
            if (cookies.TryGetValue(cookieName, out string authCookieDataEncoded))
            {
                var authCookieDataEncrypted = Base64UrlEncoder.FromBase64String(authCookieDataEncoded);
                var authCookieDataBytes = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, authCookieDataEncrypted);
                var authCookieData = Encoding.UTF8.GetString(authCookieDataBytes);
                if (authCookieData == "I can access this")
                {
                    var claims = new Claim[] {
                        new Claim(ClaimTypes.Authentication, Boolean.TrueString),
                        new Claim(ClaimTypes.NameIdentifier, "1234", ClaimValueTypes.String),
                        new Claim(ClaimTypes.Name, "Tester", ClaimValueTypes.String),
                        new Claim(ClaimTypes.Role, "Admin", ClaimValueTypes.String)
                    };

                    var identity = new ClaimsIdentity(claims, "Cookies");
                    var principal = new ClaimsPrincipal(identity);
                    System.Threading.Thread.CurrentPrincipal = principal;
                }
            }
        }

        public HttpAuthHeaders BuildAuthHeaders()
        {
            var authCookieData = "I can access this";
            var authCookieDataBytes = Encoding.UTF8.GetBytes(authCookieData);
            var authCookieDataEncrypted = SymmetricEncryptor.Encrypt( encryptionAlgorithm, encryptionKey, authCookieDataBytes);
            var authCookieDataEncoded = Base64UrlEncoder.ToBase64String(authCookieDataEncrypted);

            var cookies = new Dictionary<string, string>
            {
                { cookieName, authCookieDataEncoded }
            };

            var cookieHeaderValue = CookieParser.CookiesToString(cookies);
            var headers = new HttpAuthHeaders
            {
                { cookieHeader, new List<string>() { cookieHeaderValue } }
            };

            return headers;
        }
    }
}
