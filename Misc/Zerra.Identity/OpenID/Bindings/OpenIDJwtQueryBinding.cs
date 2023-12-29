// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.Jwt;
using Zerra.Identity.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text;

namespace Zerra.Identity.OpenID.Bindings
{
    internal class OpenIDJwtQueryBinding : OpenIDJwtBinding
    {
        public override BindingType BindingType => BindingType.Query;

        private readonly string accessToken;
        public override string AccessToken { get { return accessToken; } }

        internal OpenIDJwtQueryBinding(OpenIDDocument document, XmlSignatureAlgorithmType? signatureAlgorithm)
        {
            this.BindingDirection = document.BindingDirection;
            this.SignatureAlgorithm = signatureAlgorithm;

            this.SignatureAlgorithm ??= Cryptography.XmlSignatureAlgorithmType.RsaSha256;

            this.Document = document.GetJson();
        }

        internal OpenIDJwtQueryBinding(IdentityHttpRequest request, BindingDirection bindingDirection)
        {
            this.BindingDirection = bindingDirection;

            string token;
            if (request.Query.ContainsKey(OpenIDJwtBinding.IdTokenFormName))
            {
                token = request.Query[OpenIDJwtBinding.IdTokenFormName];
                accessToken = token;
            }
            else if (request.Query.ContainsKey(OpenIDJwtBinding.AccessTokenFormName))
            {
                token = request.Query[OpenIDJwtBinding.AccessTokenFormName];
                accessToken = token;
            }
            else
            {
                throw new IdentityProviderException("Missing JWT Token");
            }

            var parts = token.Split(OpenIDJwtQueryBinding.tokenDelimiter);
            var jwtHeaderString = DecodeJwt(parts[0]);
            var jwtPayloadString = DecodeJwt(parts[1]);
            if (!String.IsNullOrWhiteSpace(parts[2]))
            {
                this.Signature = parts[2];
            }

            this.singingInput = parts[0] + OpenIDJwtQueryBinding.tokenDelimiter + parts[1];

            var jwtHeader = JsonConvert.DeserializeObject<JwtHeader>(jwtHeaderString);
            DeserializeJwtPayload(jwtPayloadString);

            foreach (var queryValue in request.Query)
                this.Document.Add(queryValue.Key, (string)queryValue.Value);

            if (!this.Document.ContainsKey(nameof(JwtHeader.X509Thumbprint)) && jwtHeader.X509Thumbprint != null)
                this.Document.Add(nameof(JwtHeader.X509Thumbprint), JToken.FromObject(jwtHeader.X509Thumbprint));

            this.SignatureAlgorithm = Algorithms.GetSignatureAlgorithmFromJwt(jwtHeader.Algorithm);
        }

        private string GetRedirectUrl(string baseUrl)
        {
            var sb = new StringBuilder();
            _ = sb.Append(baseUrl);
            GenerateQueryString(sb);
            return sb.ToString();
        }

        public override string GetContent()
        {
            var sb = new StringBuilder();
            GenerateQueryString(sb);
            return sb.ToString();
        }
        private void GenerateQueryString(StringBuilder sb)
        {
            var token = BuildToken();

            _ = sb.Append('?');
            _ = sb.Append(OpenIDJwtBinding.IdTokenFormName).Append('=').Append(WebUtility.UrlEncode(token));

            var otherClaims = GetOtherClaims(this.Document);
            foreach (var otherClaim in otherClaims)
                _ = sb.Append(WebUtility.UrlEncode(otherClaim.Key)).Append('=').Append(WebUtility.UrlEncode(otherClaim.Value));
        }

        public override IdentityHttpResponse GetResponse(string url)
        {
            if (String.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Required url");

            var redirectUrl = GetRedirectUrl(url);
            return new IdentityHttpResponse(redirectUrl);
        }
    }
}
