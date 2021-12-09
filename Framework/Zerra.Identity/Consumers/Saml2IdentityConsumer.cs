// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.Cryptography;
using Zerra.Identity.Saml2;
using Zerra.Identity.Saml2.Documents;
using Zerra.Identity.TokenManagers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Zerra.Identity.Consumers
{
    public class Saml2IdentityConsumer : IIdentityConsumer
    {
        public readonly string LoginUrl;
        public readonly string RedirectUrl;
        public readonly string LogoutUrl;
        public readonly string RedirectUrlPostLogout;
        public readonly X509Certificate2 ServiceProviderCert;
        public readonly X509Certificate2 IdentityProviderCert;
        public readonly bool RequiredSignature;
        public readonly bool RequiredEncryption;

        public Saml2IdentityConsumer(string loginUrl, string redirectUrl, string logoutUrl, string redirectUrlPostLogout, X509Certificate2 serviceProviderCert, X509Certificate2 identityProviderCert, bool requiredSignature, bool requiredEncryption)
        {
            this.LoginUrl = loginUrl;
            this.RedirectUrl = redirectUrl;
            this.LogoutUrl = logoutUrl;
            this.RedirectUrlPostLogout = redirectUrlPostLogout;
            this.ServiceProviderCert = serviceProviderCert;
            this.IdentityProviderCert = identityProviderCert;
            this.RequiredSignature = requiredSignature;
            this.RequiredEncryption = requiredEncryption;
        }

        public async ValueTask<IActionResult> Login(string serviceProvider, string state)
        {
            var id = SamlIDManager.Generate(serviceProvider);

            var requestDocument = new Saml2AuthnRequest(
                id: id,
                issuer: serviceProvider, 
                assertionConsumerServiceURL: RedirectUrl,
                bindingType: BindingType.Form
            );

            var requestBinding = Saml2Binding.GetBindingForDocument(requestDocument, BindingType.Form, SignatureAlgorithm.RsaSha256, null, null);
            requestBinding.Sign(ServiceProviderCert, RequiredSignature);
            return requestBinding.GetResponse(LoginUrl);
        }

        public async ValueTask<IdentityModel> Callback(HttpContext context, string serviceProvider)
        {
            var callbackBinding = Saml2Binding.GetBindingForRequest(context.Request, BindingDirection.Response);

            callbackBinding.ValidateSignature(IdentityProviderCert, true);
            callbackBinding.Decrypt(ServiceProviderCert, RequiredEncryption);
            callbackBinding.ValidateFields(new string[] { RedirectUrl });

            var callbackDocument = new Saml2AuthnResponse(callbackBinding);

            SamlIDManager.Validate(serviceProvider, callbackDocument.InResponseTo);

            if (callbackDocument.Audience != serviceProvider)
                throw new IdentityProviderException("Saml Audience is not valid",
                    String.Format("Received: {0}, Expected: {1}", serviceProvider, callbackDocument.Audience));

            if (String.IsNullOrWhiteSpace(callbackDocument.UserID))
                return null;

            var identity = new IdentityModel()
            {
                UserID = callbackDocument.UserID,
                UserName = callbackDocument.UserName,
                Name = callbackDocument.UserName,
                Roles = callbackDocument.Roles,
                ServiceProvider = callbackDocument.Issuer,
                State = null,
                OtherClaims = null
            };

            return identity;
        }

        public async ValueTask<IActionResult> Logout(string serviceProvider, string state)
        {
            var id = SamlIDManager.Generate(serviceProvider);

            var requestDocument = new Saml2LogoutRequest(
                id: id,
                issuer: serviceProvider,
                destination: RedirectUrlPostLogout
            );

            var requestBinding = Saml2Binding.GetBindingForDocument(requestDocument, BindingType.Query, SignatureAlgorithm.RsaSha256, null, null);
            requestBinding.Sign(ServiceProviderCert, RequiredSignature);
            requestBinding.GetResponse(LogoutUrl);
            return requestBinding.GetResponse(LogoutUrl);
        }

        public async ValueTask<LogoutModel> LogoutCallback(HttpContext context, string serviceProvider)
        {
            var callbackBinding = Saml2Binding.GetBindingForRequest(context.Request, BindingDirection.Response);

            callbackBinding.ValidateSignature(IdentityProviderCert, true);
            callbackBinding.ValidateFields(new string[] { RedirectUrl });

            var callbackDocument = new Saml2LogoutResponse(callbackBinding);

            SamlIDManager.Validate(serviceProvider, callbackDocument.InResponseTo);

            if (String.IsNullOrWhiteSpace(callbackDocument.Issuer))
                return null;

            var logout = new LogoutModel()
            {
                ServiceProvider = callbackDocument.Issuer,
                State = null,
                OtherClaims = null
            };

            return logout;
        }
    }
}
