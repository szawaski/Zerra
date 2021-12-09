// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.OAuth2;
using Zerra.Identity.OAuth2.Documents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Zerra.Identity.Consumers
{
    public class OAuth2IdentityConsumer : IIdentityConsumer
    {
        public readonly string LoginUrl;
        public readonly string RedirectUrl;
        public readonly string TokenUrl;
        public readonly string IdentityUrl;
        public readonly string LogoutUrl;
        public readonly string RedirectUrlPostLogout;

        public OAuth2IdentityConsumer(string loginUrl, string redirectUrl, string tokenUrl, string identityUrl, string logoutUrl, string redirectUrlPostLogout)
        {
            this.LoginUrl = loginUrl;
            this.RedirectUrl = redirectUrl;
            this.TokenUrl = tokenUrl;
            this.IdentityUrl = identityUrl;
            this.LogoutUrl = logoutUrl;
            this.RedirectUrlPostLogout = redirectUrlPostLogout;
        }

        public async ValueTask<IActionResult> Login(string serviceProvider, string state)
        {
            var requestDocument = new OAuth2LoginRequest(serviceProvider, RedirectUrl, state);
            var requestBinding = OAuth2Binding.GetBindingForDocument(requestDocument, BindingType.Form);
            return requestBinding.GetResponse(LoginUrl);
        }

        public async ValueTask<IdentityModel> Callback(HttpContext context, string serviceProvider)
        {
            var callbackBinding = OAuth2Binding.GetBindingForRequest(context.Request, BindingDirection.Response);

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

            var requestTokenAction = (RedirectResult)requestTokenBinding.GetResponse(TokenUrl);
            var requestToken = WebRequest.Create(requestTokenAction.Url);
            var responseToken = await requestToken.GetResponseAsync();

            var responseTokenBinding = OAuth2Binding.GetBindingForResponse(responseToken, BindingDirection.Response);
            var responseTokenDocument = new OAuth2TokenResponse(responseTokenBinding);

            //Get Identity---------------
            var requestIdentityDocument = new OAuth2IdentityRequest(serviceProvider, responseTokenDocument.Token);
            var requestIdentityBinding = OAuth2Binding.GetBindingForDocument(requestIdentityDocument, BindingType.Query);

            var requestIdentityAction = (RedirectResult)requestIdentityBinding.GetResponse(IdentityUrl);
            var requestIdentity = WebRequest.Create(requestIdentityAction.Url);
            var responseIdentity = await requestIdentity.GetResponseAsync();

            var responseIdentityBinding = OAuth2Binding.GetBindingForResponse(responseIdentity, BindingDirection.Response);
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
                OtherClaims = null
            };

            return identity;
        }

        public async ValueTask<IActionResult> Logout(string serviceProvider, string state)
        {
            var requestDocument = new OAuth2LogoutRequest(
                serviceProvider: serviceProvider,
                redirectUrl: RedirectUrlPostLogout,
                state: state
            );

            var requestBinding = OAuth2Binding.GetBindingForDocument(requestDocument , BindingType.Query);
            return requestBinding.GetResponse(LogoutUrl);
        }

        public async ValueTask<LogoutModel> LogoutCallback(HttpContext context, string serviceProvider)
        {
            var callbackBinding = OAuth2Binding.GetBindingForRequest(context.Request, BindingDirection.Response);

            var callbackDocument = new OAuth2LogoutResponse(callbackBinding);

            var logout = new LogoutModel()
            {
                ServiceProvider = serviceProvider,
                OtherClaims = callbackDocument.OtherClaims,
                State = callbackDocument.State
            };

            return logout;
        }
    }
}
