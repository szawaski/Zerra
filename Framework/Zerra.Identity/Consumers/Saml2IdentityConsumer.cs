// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.Cryptography;
using Zerra.Identity.Saml2;
using Zerra.Identity.Saml2.Documents;
using Zerra.Identity.TokenManagers;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Zerra.Identity.Consumers
{
    public class Saml2IdentityConsumer : IIdentityConsumer
    {
        private readonly string serviceProvider;
        private readonly string loginUrl;
        private readonly string redirectUrl;
        private readonly string logoutUrl;
        private readonly string redirectUrlPostLogout;
        private readonly X509Certificate2 serviceProviderCert;
        private readonly X509Certificate2 identityProviderCert;
        private readonly bool requiredSignature;
        private readonly bool requiredEncryption;

        public Saml2IdentityConsumer(string serviceProvider, string loginUrl, string redirectUrl, string logoutUrl, string redirectUrlPostLogout, X509Certificate2 serviceProviderCert, X509Certificate2 identityProviderCert, bool requiredSignature, bool requiredEncryption)
        {
            this.serviceProvider = serviceProvider;
            this.loginUrl = loginUrl;
            this.redirectUrl = redirectUrl;
            this.logoutUrl = logoutUrl;
            this.redirectUrlPostLogout = redirectUrlPostLogout;
            this.serviceProviderCert = serviceProviderCert;
            this.identityProviderCert = identityProviderCert;
            this.requiredSignature = requiredSignature;
            this.requiredEncryption = requiredEncryption;
        }

        public ValueTask<IdentityHttpResponse> Login(string state)
        {
            var id = SamlIDManager.Generate(serviceProvider);

            var requestDocument = new Saml2AuthnRequest(
                id: id,
                issuer: serviceProvider,
                assertionConsumerServiceURL: redirectUrl,
                bindingType: BindingType.Form
            );

            var requestBinding = Saml2Binding.GetBindingForDocument(requestDocument, BindingType.Form, XmlSignatureAlgorithmType.RsaSha256, null, null);
            requestBinding.Sign(serviceProviderCert, requiredSignature);
            var response = requestBinding.GetResponse(loginUrl);
            return new ValueTask<IdentityHttpResponse>(response);
        }

        public ValueTask<IdentityModel> LoginCallback(IdentityHttpRequest request)
        {
            var callbackBinding = Saml2Binding.GetBindingForRequest(request, BindingDirection.Response);

            callbackBinding.ValidateSignature(identityProviderCert, true);
            callbackBinding.Decrypt(serviceProviderCert, requiredEncryption);
            callbackBinding.ValidateFields(new string[] { redirectUrl });

            var callbackDocument = new Saml2AuthnResponse(callbackBinding);

            SamlIDManager.Validate(serviceProvider, callbackDocument.InResponseTo);

            if (callbackDocument.Audience != serviceProvider)
                throw new IdentityProviderException("Saml Audience is not valid",
                    String.Format("Received: {0}, Expected: {1}", serviceProvider, callbackDocument.Audience));

            if (String.IsNullOrWhiteSpace(callbackDocument.UserID))
                return new ValueTask<IdentityModel>((IdentityModel)null);

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

            return new ValueTask<IdentityModel>(identity);
        }

        public ValueTask<IdentityHttpResponse> Logout(string state)
        {
            var id = SamlIDManager.Generate(serviceProvider);

            var requestDocument = new Saml2LogoutRequest(
                id: id,
                issuer: serviceProvider,
                destination: redirectUrlPostLogout
            );

            var requestBinding = Saml2Binding.GetBindingForDocument(requestDocument, BindingType.Query, XmlSignatureAlgorithmType.RsaSha256, null, null);
            requestBinding.Sign(serviceProviderCert, requiredSignature);
            _ = requestBinding.GetResponse(logoutUrl);
            var response = requestBinding.GetResponse(logoutUrl);
            return new ValueTask<IdentityHttpResponse>(response);
        }

        public ValueTask<LogoutModel> LogoutCallback(IdentityHttpRequest request)
        {
            var callbackBinding = Saml2Binding.GetBindingForRequest(request, BindingDirection.Response);

            callbackBinding.ValidateSignature(identityProviderCert, true);
            callbackBinding.ValidateFields(new string[] { redirectUrl });

            var callbackDocument = new Saml2LogoutResponse(callbackBinding);

            SamlIDManager.Validate(serviceProvider, callbackDocument.InResponseTo);

            if (String.IsNullOrWhiteSpace(callbackDocument.Issuer))
                return new ValueTask<LogoutModel>((LogoutModel)null);

            var logout = new LogoutModel()
            {
                ServiceProvider = callbackDocument.Issuer,
                State = null,
                OtherClaims = null
            };

            return new ValueTask<LogoutModel>(logout);
        }
    }
}
