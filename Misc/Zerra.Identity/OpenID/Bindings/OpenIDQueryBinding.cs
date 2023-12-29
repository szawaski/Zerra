// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text;

namespace Zerra.Identity.OpenID.Bindings
{
    internal class OpenIDQueryBinding : OpenIDBinding
    {
        public override BindingType BindingType => BindingType.Query;

        internal OpenIDQueryBinding(OpenIDDocument document)
        {
            this.BindingDirection = document.BindingDirection;

            this.Document = document.GetJson();
        }

        internal OpenIDQueryBinding(IdentityHttpRequest request, BindingDirection bindingDirection)
        {
            this.BindingDirection = bindingDirection;

            this.Document = new JObject();
            foreach (var queryItem in request.Query)
            {
                this.Document.Add(queryItem.Key, JToken.FromObject((string)queryItem.Value));
            }
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
            var first = true;
            foreach (var jsonObject in this.Document)
            {
                if (first)
                {
                    first = false;
                    _ = sb.Append('?');
                }
                else
                {
                    _ = sb.Append('&');
                }

                var value = jsonObject.Value.ToObject<string>();
                _ = sb.Append(WebUtility.UrlEncode(jsonObject.Key)).Append('=').Append(WebUtility.UrlEncode(value));
            }
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
