// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.Jwt;
using Zerra.Identity.OpenID;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Zerra.Identity.OpenID.Documents;
using Zerra.Identity.TokenManagers;
using System.Text;
using System.Security.Cryptography;
using Zerra.Encryption;

namespace Zerra.Identity.Consumers
{
    public class OpenIDIdentityConsumer : IIdentityConsumer
    {
        private readonly string serviceProvider;
        private readonly string loginUrl;
        private readonly string redirectUrl;
        private readonly string logoutUrl;
        private readonly string tokenUrl;
        private readonly string userInfoUrl;
        private readonly string redirectUrlPostLogout;
        private readonly string identityProviderCertUrl;
        private readonly string scope;
        private readonly bool requiredSignature;
        private readonly OpenIDResponseType responseType;

        public OpenIDIdentityConsumer(string serviceProvider, string loginUrl, string redirectUrl, string logoutUrl, string tokenUrl, string userInfoUrl, string redirectUrlPostLogout, string identityProviderCertUrl, string scope, bool requiredSignature, OpenIDResponseType responseType)
        {
            this.serviceProvider = serviceProvider;
            this.loginUrl = loginUrl;
            this.redirectUrl = redirectUrl;
            this.logoutUrl = logoutUrl;
            this.tokenUrl = tokenUrl;
            this.userInfoUrl = userInfoUrl;
            this.redirectUrlPostLogout = redirectUrlPostLogout;
            this.identityProviderCertUrl = identityProviderCertUrl;
            this.scope = scope;
            this.requiredSignature = requiredSignature;
            this.responseType = responseType;
        }

        public static async Task<OpenIDIdentityConsumer> FromMetadata(string serviceProvider, string metadataUrl, string redirectUrl, string redirectUrlPostLogout, OpenIDResponseType responseType)
        {
            var request = WebRequest.Create(metadataUrl);
            var response = await request.GetResponseAsync();
            var binding = OpenIDBinding.GetBindingForResponse(response, BindingDirection.Response);
            var document = new OpenIDMetadataResponse(binding);

            if (!document.ScopesSupported.Contains("openid"))
                throw new IdentityProviderException("OpenID Scope Not Supported From This Service.");

            var sb = new StringBuilder();
            sb.Append("openid");
            if (document.ScopesSupported.Contains("profile"))
                sb.Append("+profile");
            if (document.ScopesSupported.Contains("email"))
                sb.Append("+email");
            if (document.ScopesSupported.Contains("offline_access"))
                sb.Append("+offline_access");

            var scope = sb.ToString();

            return new OpenIDIdentityConsumer(
                serviceProvider,
                document.LoginUrl,
                redirectUrl,
                document.LogoutUrl,
                document.TokenUrl,
                document.UserInfoUrl,
                redirectUrlPostLogout,
                document.KeysUrl,
                scope,
                true,
                responseType
            );
        }

        public async ValueTask<IActionResult> Login(string state)
        {
            var nonce = NonceManager.Generate(serviceProvider);

            var requestDocument = new OpenIDLoginRequest(
                serviceProvider: serviceProvider,
                redirectUrl: redirectUrl,
                responseType: this.responseType,
                responseMode: OpenIDResponseMode.form_post,
                bindingType: BindingType.Form,
                scope: this.scope,
                state: state,
                nonce: nonce
            );

            var requestBinding = OpenIDBinding.GetBindingForDocument(requestDocument, BindingType.Form);
            return requestBinding.GetResponse(loginUrl);
        }

        public async ValueTask<IdentityModel> Callback(HttpContext context)
        {
            if (OpenIDJwtBinding.IsOpenIDJwtBinding(context.Request))
            {
                var callbackBinding = OpenIDJwtBinding.GetBindingForRequest(context.Request, BindingDirection.Response);

                var callbackDocument = new OpenIDLoginResponse(callbackBinding);
                if (!String.IsNullOrWhiteSpace(callbackDocument.Error))
                    throw new IdentityProviderException($"{callbackDocument.Error}: {callbackDocument.ErrorDescription}");

                NonceManager.Validate(serviceProvider, callbackDocument.Nonce);

                if (callbackDocument.Audience != serviceProvider)
                    throw new IdentityProviderException("OpenID Audience is not valid", $"Received: {serviceProvider}, Expected: {callbackDocument.Audience}");

                var keys = await GetSignaturePublicKeys(this.identityProviderCertUrl);
                var key = keys.FirstOrDefault(x => x.X509Thumbprint == callbackDocument.X509Thumbprint);
                if (key == null)
                    key = keys.FirstOrDefault(x => x.KeyID == callbackDocument.KeyID);
                if (key == null)
                    throw new IdentityProviderException("Identity Provider OpenID certificate not found from Json Key Url");
                if (key.KeyType != "RSA")
                    throw new IdentityProviderException("Identity Provider OpenID only supporting RSA at the moment");

                RSA rsa;
                if (key.X509Certificates == null || key.X509Certificates.Length == 0)
                {
                    var rsaParams = new RSAParameters()
                    {
                        Modulus = Base64UrlEncoder.FromBase64String(key.Modulus),
                        Exponent = Base64UrlEncoder.FromBase64String(key.Exponent)
                    };
                    rsa = RSA.Create();
                    rsa.ImportParameters(rsaParams);
                }
                else
                {
                    var certString = key.X509Certificates.First();
                    var certBytes = Convert.FromBase64String(certString);
                    var cert = new X509Certificate2(certBytes);
                    rsa = cert.GetRSAPublicKey();
                }

                callbackBinding.ValidateSignature(rsa, requiredSignature);
                callbackBinding.ValidateFields();

                var identity = new IdentityModel()
                {
                    UserID = callbackDocument.UserID,
                    UserName = callbackDocument.UserName ?? callbackDocument.Emails?.FirstOrDefault(),
                    Name = callbackDocument.Name,
                    Roles = callbackDocument.Roles,
                    ServiceProvider = callbackDocument.Issuer,
                    OtherClaims = callbackDocument.OtherClaims,
                    State = callbackDocument.State
                };

                return identity;
            }
            else
            {
                throw new IdentityProviderException($"OpenID code flow not currently supported");

                var callbackBinding = OpenIDBinding.GetBindingForRequest(context.Request, BindingDirection.Response);

                var callbackDocument = new OpenIDLoginResponse(callbackBinding);
                if (!String.IsNullOrWhiteSpace(callbackDocument.Error))
                    throw new IdentityProviderException($"{callbackDocument.Error}: {callbackDocument.ErrorDescription}");

                var redirectUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}";

                //Get Token--------------------
                var requestTokenDocument = new OpenIDTokenRequest(callbackDocument.AccessCode, redirectUrl);
                var requestTokenBinding = OpenIDBinding.GetBindingForDocument(requestTokenDocument, BindingType.Form);

                var requestTokenBody = requestTokenBinding.GetContent();
                var requestToken = WebRequest.Create(tokenUrl);
                requestToken.Method = "POST";
                requestToken.ContentType = "application/x-www-form-urlencoded";

                var data = Encoding.UTF8.GetBytes(requestTokenBody);
                requestToken.ContentLength = data.Length;
                using (var stream = await requestToken.GetRequestStreamAsync())
                {
#if NETSTANDARD2_0
                    await stream.WriteAsync(data, 0, data.Length);
#else
                    await stream.WriteAsync(data.AsMemory());
#endif
                }

                var responseToken = await requestToken.GetResponseAsync();

                var responseTokenBinding = OpenIDJwtBinding.GetBindingForResponse(responseToken, BindingDirection.Response);
                var responseTokenDocument = new OpenIDTokenResponse(responseTokenBinding);

                throw new NotImplementedException();
            }
        }

        private static async Task<JwtKey[]> GetSignaturePublicKeys(string url)
        {
            var request = WebRequest.Create(url);
            var response = await request.GetResponseAsync();
            var stream = response.GetResponseStream();
            var content = new StreamReader(stream).ReadToEnd();
            response.Close();
            response.Dispose();

            var keys = JsonConvert.DeserializeObject<JwtKeys>(content);

            if (keys == null)
                return null;

            return keys.keys;
        }

        public async ValueTask<IActionResult> Logout(string state)
        {
            var requestDocument = new OpenIDLogoutRequest(
                serviceProvider: serviceProvider,
                redirectUrl: redirectUrlPostLogout,
                state: state
            );

            var requestBinding = OpenIDBinding.GetBindingForDocument(requestDocument, BindingType.Query);
            return requestBinding.GetResponse(logoutUrl);
        }

        public async ValueTask<LogoutModel> LogoutCallback(HttpContext context)
        {
            var callbackBinding = OpenIDBinding.GetBindingForRequest(context.Request, BindingDirection.Response);

            var callbackDocument = new OpenIDLogoutResponse(callbackBinding);

            var logout = new LogoutModel()
            {
                ServiceProvider = serviceProvider,
                State = callbackDocument.State,
                OtherClaims = callbackDocument.OtherClaims
            };

            return logout;
        }
    }
}
