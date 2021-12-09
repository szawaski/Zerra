// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Zerra.Identity.OpenID.Bindings
{
    internal class OpenIDFormBinding : OpenIDBinding
    {
        public override BindingType BindingType => BindingType.Form;

        internal OpenIDFormBinding(OpenIDDocument document)
        {
            this.BindingDirection = document.BindingDirection;

            this.Document = document.GetJson();
        }

        internal OpenIDFormBinding(HttpRequest request, BindingDirection bindingDirection)
        {
            this.BindingDirection = bindingDirection;

            this.Document = new JObject();
            foreach (var formItem in request.Form)
            {
                this.Document.Add(formItem.Key, JToken.FromObject((string)formItem.Value));
            }
        }

        private Dictionary<string, string> GetInputs()
        {
            var inputs = new Dictionary<string, string>();

            foreach (var jsonObject in this.Document)
            {
                var value = jsonObject.Value.ToObject<string>();
                inputs.Add(jsonObject.Key, value);
            }

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
            foreach(var input in inputs)
            {
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append(WebUtility.HtmlEncode(input.Key)).Append('=').Append(WebUtility.HtmlEncode(input.Value));
            }
            return sb.ToString();
        }

        public override IActionResult GetResponse(string url)
        {
            if (String.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Required url");

            var inputs = GetInputs();
            var content = GeneratePost(url, inputs);

            return new ContentResult()
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = content
            };
        }
    }
}
