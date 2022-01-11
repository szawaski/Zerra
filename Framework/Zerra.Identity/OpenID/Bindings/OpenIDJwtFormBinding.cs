﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.Jwt;
using Zerra.Identity.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Zerra.Identity.OpenID.Bindings
{
    internal class OpenIDJwtFormBinding : OpenIDJwtBinding
    {
        public override BindingType BindingType => BindingType.Form;

        internal OpenIDJwtFormBinding(OpenIDDocument document, XmlSignatureAlgorithmType? signatureAlgorithm)
        {
            this.BindingDirection = document.BindingDirection;
            this.SignatureAlgorithm = signatureAlgorithm;

            if (this.SignatureAlgorithm == null)
                this.SignatureAlgorithm = Cryptography.XmlSignatureAlgorithmType.RsaSha256;

            this.Document = document.GetJson();
        }

        internal OpenIDJwtFormBinding(IdentityHttpRequest request, BindingDirection bindingDirection)
        {
            this.BindingDirection = bindingDirection;

            var token = (string)request.Form[OpenIDJwtBinding.TokenFormName];
            if (String.IsNullOrEmpty(token))
                throw new IdentityProviderException("Missing JWT Token");

            var parts = token.Split(OpenIDJwtFormBinding.tokenDelimiter);
            var jwtHeaderString = DecodeJwt(parts[0]);
            var jwtPayloadString = DecodeJwt(parts[1]);
            if (parts.Length > 2 && !String.IsNullOrWhiteSpace(parts[2]))
            {
                this.Signature = parts[2];
            }

            this.singingInput = parts[0] + OpenIDJwtFormBinding.tokenDelimiter + parts[1];

            var jwtHeader = JsonConvert.DeserializeObject<JwtHeader>(jwtHeaderString);
            DeserializeJwtPayload(jwtPayloadString);

            foreach (var formValue in request.Form)
            {
                if (formValue.Key != OpenIDJwtBinding.TokenFormName && !this.Document.ContainsKey(OpenIDJwtBinding.TokenFormName))
                {
                    this.Document.Add(formValue.Key, (string)formValue.Value);
                }
            }

            if (!this.Document.ContainsKey(nameof(JwtHeader.X509Thumbprint)) && jwtHeader.X509Thumbprint != null)
                this.Document.Add(nameof(JwtHeader.X509Thumbprint), JToken.FromObject(jwtHeader.X509Thumbprint));
            if (!this.Document.ContainsKey(nameof(JwtHeader.KeyID)) && jwtHeader.KeyID != null)
                this.Document.Add(nameof(JwtHeader.KeyID), JToken.FromObject(jwtHeader.KeyID));

            this.SignatureAlgorithm = Algorithms.GetSignatureAlgorithmFromJwt(jwtHeader.Algorithm);
        }

        private Dictionary<string, string> GetInputs()
        {
            var token = BuildToken();

            var inputs = new Dictionary<string, string>();
            inputs.Add(OpenIDJwtBinding.TokenFormName, token);

            var otherClaims = GetOtherClaims(this.Document);
            foreach (var otherClaim in otherClaims)
                inputs.Add(otherClaim.Key, otherClaim.Value);

            return inputs;
        }

        private static string GeneratePost(string url, IEnumerable<KeyValuePair<string, string>> inputs)
        {
            var sb = new StringBuilder();

            sb.Append("<html><head><title>Working...</title></head><body>");
            sb.Append("<form method=\"POST\" name=\"name=\"hiddenform\" action=\"").Append(url).Append("\">");
            if (inputs != null)
            {
                foreach (KeyValuePair<string, string> input in inputs)
                    sb.Append(String.Format("<input type=\"hidden\" name=\"{0}\" value=\"{1}\">", WebUtility.HtmlEncode(input.Key), WebUtility.HtmlEncode(input.Value)));
            }
            sb.Append("<noscript><p>Script is disabled. Click Submit to continue.</p><input type=\"submit\" value=\"Submit\" /></noscript>");
            sb.Append("</form>");
            sb.Append("<script language=\"javascript\">document.forms[0].submit();</script>");
            sb.Append("</body></html>");

            return sb.ToString();
        }

        public override string GetContent()
        {
            var inputs = GetInputs();

            var sb = new StringBuilder();
            foreach (var input in inputs)
            {
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append(WebUtility.HtmlEncode(input.Key)).Append('=').Append(WebUtility.HtmlEncode(input.Value));
            }
            return sb.ToString();
        }

        public override IdentityHttpResponse GetResponse(string url)
        {
            if (String.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Required url");

            var inputs = GetInputs();
            var content = GeneratePost(url, inputs);

            return new IdentityHttpResponse("text/html", content);
        }
    }
}