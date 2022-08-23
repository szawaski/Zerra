// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.OAuth2;
using Zerra.Identity.OAuth2.Documents;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;

namespace Zerra.Identity.Consumers
{
    public class OAuth2IdentityConsumer : IIdentityConsumer
    {
        private readonly string serviceProvider;
        private readonly string loginUrl;
        private readonly string redirectUrl;
        private readonly string tokenUrl;
        private readonly string identityUrl;
        private readonly string logoutUrl;
        private readonly string redirectUrlPostLogout;

        public OAuth2IdentityConsumer(string serviceProvider, string loginUrl, string redirectUrl, string tokenUrl, string identityUrl, string logoutUrl, string redirectUrlPostLogout)
        {
            this.serviceProvider = serviceProvider;
            this.loginUrl = loginUrl;
            this.redirectUrl = redirectUrl;
            this.tokenUrl = tokenUrl;
            this.identityUrl = identityUrl;
            this.logoutUrl = logoutUrl;
            this.redirectUrlPostLogout = redirectUrlPostLogout;
        }

        public ValueTask<IdentityHttpResponse> Login(string state)
        {
            var requestDocument = new OAuth2LoginRequest(serviceProvider, redirectUrl, state);
            var requestBinding = OAuth2Binding.GetBindingForDocument(requestDocument, BindingType.Form);
            var response = requestBinding.GetResponse(loginUrl);
            return new ValueTask<IdentityHttpResponse>(response);
        }

        public async ValueTask<IdentityModel> LoginCallback(IdentityHttpRequest request)
        {
            var callbackBinding = OAuth2Binding.GetBindingForRequest(request, BindingDirection.Response);

            var callbackDocument = new OAuth2LoginResponse(callbackBinding);

            var callbackServiceProvider = callbackDocument.ServiceProvider;
            var code = callbackDocument.AccessCode;

            if (code == null)
                return null;

            if (serviceProvider != callbackServiceProvider)
                throw new IdentityProviderException("Service Providers do not match", $"Received: {serviceProvider}, Expected: {callbackServiceProvider}");

            //Get Token--------------------
            var requestTokenDocument = new OAuth2TokenRequest(serviceProvider, code);
            var requestTokenBinding = OAuth2Binding.GetBindingForDocument(requestTokenDocument, BindingType.Query);

            var requestTokenAction = requestTokenBinding.GetResponse(tokenUrl);

            OAuth2Binding responseTokenBinding;
#if NET48
            var requestToken = WebRequest.Create(requestTokenAction.RedirectUrl);
            using (var response = await requestToken.GetResponseAsync())
            {
                var stream = response.GetResponseStream();
                responseTokenBinding = OAuth2Binding.GetBindingForResponse(stream, BindingDirection.Response);
            }
#else
            using (var client = new HttpClient())
            using (var response = await client.GetAsync(requestTokenAction.RedirectUrl))
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                responseTokenBinding = OAuth2Binding.GetBindingForResponse(stream, BindingDirection.Response);
            }
#endif
            var responseTokenDocument = new OAuth2TokenResponse(responseTokenBinding);

            //Get Identity---------------
            var requestIdentityDocument = new OAuth2IdentityRequest(serviceProvider, responseTokenDocument.Token);
            var requestIdentityBinding = OAuth2Binding.GetBindingForDocument(requestIdentityDocument, BindingType.Query);



            var requestIdentityAction = requestIdentityBinding.GetResponse(identityUrl);

            OAuth2Binding responseIdentityBinding;
#if NET48
            var requestIdentity = WebRequest.Create(requestIdentityAction.RedirectUrl);
            using (var response = await requestIdentity.GetResponseAsync())
            {
                var stream = response.GetResponseStream();
                responseIdentityBinding = OAuth2Binding.GetBindingForResponse(stream, BindingDirection.Response);
            }
#else
            using (var client = new HttpClient())
            using (var response = await client.GetAsync(requestIdentityAction.RedirectUrl))
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                responseIdentityBinding = OAuth2Binding.GetBindingForResponse(stream, BindingDirection.Response);
            }
#endif
            var responseIdentityDocument = new OAuth2IdentityResponse(responseIdentityBinding);

            if (responseIdentityDocument.ServiceProvider != serviceProvider)
                return null;

            if (String.IsNullOrWhiteSpace(responseIdentityDocument.UserID))
                return null;

            var identity = new IdentityModel()
            {
                UserID = responseIdentityDocument.UserID,
                UserName = responseIdentityDocument.UserName,
                Name = responseIdentityDocument.UserName,
                ServiceProvider = responseIdentityDocument.ServiceProvider,
                Roles = responseIdentityDocument.Roles,
                OtherClaims = null,
                State = null,
                AccessToken = responseTokenDocument.Token
            };

            return identity;
        }

        public ValueTask<IdentityHttpResponse> Logout(string state)
        {
            var requestDocument = new OAuth2LogoutRequest(
                serviceProvider: serviceProvider,
                redirectUrl: redirectUrlPostLogout,
                state: state
            );

            var requestBinding = OAuth2Binding.GetBindingForDocument(requestDocument, BindingType.Query);
            var response = requestBinding.GetResponse(logoutUrl);
            return new ValueTask<IdentityHttpResponse>(response);
        }

        public ValueTask<LogoutModel> LogoutCallback(IdentityHttpRequest request)
        {
            var callbackBinding = OAuth2Binding.GetBindingForRequest(request, BindingDirection.Response);

            var callbackDocument = new OAuth2LogoutResponse(callbackBinding);

            var logout = new LogoutModel()
            {
                ServiceProvider = serviceProvider,
                OtherClaims = callbackDocument.OtherClaims,
                State = callbackDocument.State
            };

            return new ValueTask<LogoutModel>(logout);
        }
    }
}
