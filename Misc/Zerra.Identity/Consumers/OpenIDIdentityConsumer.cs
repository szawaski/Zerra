// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.Jwt;
using Zerra.Identity.OpenID;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Zerra.Identity.OpenID.Documents;
using Zerra.Identity.TokenManagers;
using System.Text;
using System.Security.Cryptography;
using Zerra.Encryption;
using System.Net.Http;

namespace Zerra.Identity.Consumers
{
    public sealed class OpenIDIdentityConsumer : IIdentityConsumer
    {
        private readonly string serviceProvider;
        private readonly string secret;
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
        private readonly string acrValues;

        public OpenIDIdentityConsumer(string serviceProvider, string secret, string loginUrl, string redirectUrl, string logoutUrl, string tokenUrl, string userInfoUrl, string redirectUrlPostLogout, string identityProviderCertUrl, string scope, bool requiredSignature, OpenIDResponseType responseType, string acrValues)
        {
            this.serviceProvider = serviceProvider;
            this.secret = secret;
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
            this.acrValues = acrValues;
        }

        public static async Task<OpenIDIdentityConsumer> FromMetadata(string serviceProvider, string secret, string metadataUrl, string redirectUrl, string redirectUrlPostLogout, string scope, OpenIDResponseType responseType, string acrValues)
        {
            OpenIDMetadataResponse document;
#if NET48
            var request = WebRequest.Create(metadataUrl);
            using (var response = await request.GetResponseAsync())
            {
                var stream = response.GetResponseStream();
                var binding = OpenIDBinding.GetBindingForResponse(stream, BindingDirection.Response);
                document = new OpenIDMetadataResponse(binding);
            }
#else
            using (var client = new HttpClient())
            using (var response = await client.GetAsync(metadataUrl))
            {
                var stream = await response.Content.ReadAsStreamAsync();
                var binding = OpenIDBinding.GetBindingForResponse(stream, BindingDirection.Response);
                document = new OpenIDMetadataResponse(binding);
            }
#endif

            if (!document.ScopesSupported.Contains("openid"))
                throw new IdentityProviderException("OpenID Scope Not Supported From This Service.");

            if (String.IsNullOrWhiteSpace(scope))
            {
                var sb = new StringBuilder();
                _ = sb.Append("openid");
                if (document.ScopesSupported.Contains("profile"))
                    _ = sb.Append("+profile");
                if (document.ScopesSupported.Contains("email"))
                    _ = sb.Append("+email");
                if (document.ScopesSupported.Contains("offline_access"))
                    _ = sb.Append("+offline_access");

                scope = sb.ToString();
            }

            return new OpenIDIdentityConsumer(
                serviceProvider: serviceProvider,
                secret: secret,
                loginUrl: document.LoginUrl,
                redirectUrl: redirectUrl,
                logoutUrl: document.LogoutUrl,
                tokenUrl: document.TokenUrl,
                userInfoUrl: document.UserInfoUrl,
                redirectUrlPostLogout: redirectUrlPostLogout,
                identityProviderCertUrl: document.KeysUrl,
                scope: scope,
                requiredSignature: true,
                responseType: responseType,
                acrValues: acrValues
            );
        }

        public ValueTask<IdentityHttpResponse> Login(string state = null)
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
                nonce: nonce,
                acrValues: this.acrValues
            );

            var requestBinding = OpenIDBinding.GetBindingForDocument(requestDocument, BindingType.Form);
            var response = requestBinding.GetResponse(loginUrl);
            return new ValueTask<IdentityHttpResponse>(response);
        }

        public async ValueTask<IdentityModel> LoginCallback(IdentityHttpRequest request)
        {
            OpenIDJwtBinding callbackBinding;

            if (OpenIDJwtBinding.IsCodeBinding(request))
            {
                var callbackCodeBinding = OpenIDBinding.GetBindingForRequest(request, BindingDirection.Response);

                var callbackCodeDocument = new OpenIDLoginResponse(callbackCodeBinding);
                if (!String.IsNullOrWhiteSpace(callbackCodeDocument.Error))
                    throw new IdentityProviderException($"{callbackCodeDocument.Error}: {callbackCodeDocument.ErrorDescription}");

                //Get Token--------------------
                var requestTokenDocument = new OpenIDTokenRequest(callbackCodeDocument.AccessCode, this.secret, OpenIDGrantType.authorization_code, redirectUrl);
                var requestTokenBinding = OpenIDBinding.GetBindingForDocument(requestTokenDocument, BindingType.Form);

#if NET48
                var requestTokenBody = requestTokenBinding.GetContent();
                var requestToken = WebRequest.Create(tokenUrl);
                requestToken.Method = "POST";
                requestToken.ContentType = "application/x-www-form-urlencoded";
                var requestTokenBodyBytes = Encoding.UTF8.GetBytes(requestTokenBody);
                requestToken.ContentLength = requestTokenBodyBytes.Length;
                using (var stream = await requestToken.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(requestTokenBodyBytes, 0, requestTokenBodyBytes.Length);
                    await stream.FlushAsync();
                }

                WebResponse responseToken;
                try
                {
                    responseToken = await requestToken.GetResponseAsync();
                }
                catch (WebException ex)
                {
                    if (ex.Response is null)
                        throw ex;
                    var responseTokenStream = ex.Response.GetResponseStream();
                    var error = await new StreamReader(responseTokenStream).ReadToEndAsync();
                    ex.Response.Close();
                    ex.Response.Dispose();
                    throw new IdentityProviderException(error);
                }

                callbackBinding = OpenIDJwtBinding.GetBindingForResponse(responseToken.GetResponseStream(), BindingDirection.Response);
#else
                var requestTokenBody = requestTokenBinding.GetContent();
                using (var client = new HttpClient())
                using (var content = new StringContent(requestTokenBody, Encoding.UTF8, "application/x-www-form-urlencoded"))
                using (var response = await client.PostAsync(tokenUrl, content))
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    callbackBinding = OpenIDJwtBinding.GetBindingForResponse(stream, BindingDirection.Response);
                }
#endif
            }
            else
            {
                callbackBinding = OpenIDJwtBinding.GetBindingForRequest(request, BindingDirection.Response);
            }

            var callbackDocument = new OpenIDLoginResponse(callbackBinding);
            if (!String.IsNullOrWhiteSpace(callbackDocument.Error))
                throw new IdentityProviderException($"{callbackDocument.Error}: {callbackDocument.ErrorDescription}");

            NonceManager.Validate(serviceProvider, callbackDocument.Nonce);

            if (callbackDocument.Audience != serviceProvider)
                throw new IdentityProviderException("OpenID Audience is not valid", $"Received: {serviceProvider}, Expected: {callbackDocument.Audience}");

            var keys = await GetSignaturePublicKeys(this.identityProviderCertUrl);
            var key = keys.FirstOrDefault(x => x.X509Thumbprint == callbackDocument.X509Thumbprint);
            key ??= keys.FirstOrDefault(x => x.KeyID == callbackDocument.KeyID);
            if (key is null)
                throw new IdentityProviderException("Identity Provider OpenID certificate not found from Json Key Url");
            if (key.KeyType != "RSA")
                throw new IdentityProviderException("Identity Provider OpenID only supporting RSA at the moment");

            RSA rsa;
            if (key.X509Certificates is null || key.X509Certificates.Length == 0)
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
#if NET9_0_OR_GREATER
                var cert = X509CertificateLoader.LoadCertificate(certBytes);
#else
                var cert = new X509Certificate2(certBytes);
#endif
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
                State = callbackDocument.State,
                AccessToken = callbackBinding.AccessToken,
            };

            return identity;
        }

        private static async Task<JwtKey[]> GetSignaturePublicKeys(string url)
        {
#if NET48
            var request = WebRequest.Create(url);
            using (var response = await request.GetResponseAsync())
            using (var stream = response.GetResponseStream())
            {
                var content = await new StreamReader(stream).ReadToEndAsync();
                var keys = JsonConvert.DeserializeObject<JwtKeys>(content);

                if (keys is null)
                    return null;

                return keys.keys;
            }
#else
            using (var client = new HttpClient())
            using (var response = await client.GetAsync(url))
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                var content = await new StreamReader(stream).ReadToEndAsync();

                var keys = JsonConvert.DeserializeObject<JwtKeys>(content);

                if (keys is null)
                    return null;

                return keys.keys;
            }
#endif
        }

        public ValueTask<IdentityHttpResponse> Logout(string state)
        {
            var requestDocument = new OpenIDLogoutRequest(
                serviceProvider: serviceProvider,
                redirectUrl: redirectUrlPostLogout,
                state: state
            );

            var requestBinding = OpenIDBinding.GetBindingForDocument(requestDocument, BindingType.Query);
            var response = requestBinding.GetResponse(logoutUrl);
            return new ValueTask<IdentityHttpResponse>(response);
        }

        public ValueTask<LogoutModel> LogoutCallback(IdentityHttpRequest request)
        {
            var callbackBinding = OpenIDBinding.GetBindingForRequest(request, BindingDirection.Response);

            var callbackDocument = new OpenIDLogoutResponse(callbackBinding);

            var logout = new LogoutModel()
            {
                ServiceProvider = serviceProvider,
                State = callbackDocument.State,
                OtherClaims = callbackDocument.OtherClaims
            };

            return new ValueTask<LogoutModel>(logout);
        }
    }
}
