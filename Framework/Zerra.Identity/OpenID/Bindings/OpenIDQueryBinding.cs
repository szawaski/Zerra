// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        internal OpenIDQueryBinding(HttpRequest request, BindingDirection bindingDirection)
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
            StringBuilder sb = new StringBuilder();
            sb.Append(baseUrl);
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
            bool first = true;
            foreach (var jsonObject in this.Document)
            {
                if (first)
                {
                    first = false;
                    sb.Append('?');
                }
                else
                {
                    sb.Append('&');
                }

                var value = jsonObject.Value.ToObject<string>();
                sb.Append(WebUtility.UrlEncode(jsonObject.Key)).Append('=').Append(WebUtility.UrlEncode(value));
            }
        }

        public override IActionResult GetResponse(string url)
        {
            if (String.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Required url");

            string redirectUrl = GetRedirectUrl(url);
            return new RedirectResult(redirectUrl);
        }
    }
}
