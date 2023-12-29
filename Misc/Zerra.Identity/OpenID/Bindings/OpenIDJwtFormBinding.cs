// Copyright © KaKush LLC
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

        private readonly string accessToken;
        public override string AccessToken { get { return accessToken; } }

        internal OpenIDJwtFormBinding(OpenIDDocument document, XmlSignatureAlgorithmType? signatureAlgorithm)
        {
            this.BindingDirection = document.BindingDirection;
            this.SignatureAlgorithm = signatureAlgorithm;

            this.SignatureAlgorithm ??= Cryptography.XmlSignatureAlgorithmType.RsaSha256;

            this.Document = document.GetJson();
        }

        internal OpenIDJwtFormBinding(IdentityHttpRequest request, BindingDirection bindingDirection)
        {
            this.BindingDirection = bindingDirection;

            if (request.Form.ContainsKey("error"))
            {
                if (request.Form.ContainsKey("error_description"))
                    throw new IdentityProviderException(request.Form["error_description"]);
                else
                    throw new IdentityProviderException(request.Form["error"]);
            }

            string token;
            if (request.Form.ContainsKey(OpenIDJwtBinding.IdTokenFormName))
            {
                token = request.Form[OpenIDJwtBinding.IdTokenFormName];
                accessToken = token;
            }
            else if (request.Form.ContainsKey(OpenIDJwtBinding.AccessTokenFormName))
            {
                token = request.Form[OpenIDJwtBinding.AccessTokenFormName];
                accessToken = token;
            }
            else
            {
                throw new IdentityProviderException("Missing JWT Token");
            }

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
                this.Document.Add(formValue.Key, (string)formValue.Value);

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
            inputs.Add(OpenIDJwtBinding.IdTokenFormName, token);

            var otherClaims = GetOtherClaims(this.Document);
            foreach (var otherClaim in otherClaims)
                inputs.Add(otherClaim.Key, otherClaim.Value);

            return inputs;
        }

        private static string GeneratePost(string url, IEnumerable<KeyValuePair<string, string>> inputs)
        {
            var sb = new StringBuilder();

            _ = sb.Append("<html><head><title>Working...</title></head><body>");
            _ = sb.Append("<form method=\"POST\" name=\"name=\"hiddenform\" action=\"").Append(url).Append("\">");
            if (inputs != null)
            {
                foreach (var input in inputs)
                    _ = sb.Append(String.Format("<input type=\"hidden\" name=\"{0}\" value=\"{1}\">", WebUtility.HtmlEncode(input.Key), WebUtility.HtmlEncode(input.Value)));
            }
            _ = sb.Append("<noscript><p>Script is disabled. Click Submit to continue.</p><input type=\"submit\" value=\"Submit\" /></noscript>");
            _ = sb.Append("</form>");
            _ = sb.Append("<script language=\"javascript\">document.forms[0].submit();</script>");
            _ = sb.Append("</body></html>");

            return sb.ToString();
        }

        public override string GetContent()
        {
            var inputs = GetInputs();

            var sb = new StringBuilder();
            foreach (var input in inputs)
            {
                if (sb.Length > 0)
                    _ = sb.Append('&');
                _ = sb.Append(WebUtility.HtmlEncode(input.Key)).Append('=').Append(WebUtility.HtmlEncode(input.Value));
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
