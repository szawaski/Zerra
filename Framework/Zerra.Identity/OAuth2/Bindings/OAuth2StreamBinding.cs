// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net;

namespace Zerra.Identity.OAuth2.Bindings
{
    internal class OAuth2StreamBinding : OAuth2Binding
    {
        public override BindingType BindingType => BindingType.Stream;

        internal OAuth2StreamBinding(OAuth2Document document)
        {
            this.BindingDirection = document.BindingDirection;

            this.Document = document.GetJson();
        }

        internal OAuth2StreamBinding(WebResponse response, BindingDirection flowDirection)
        {
            this.BindingDirection = flowDirection;

            var stream = response.GetResponseStream();
            var sr = new System.IO.StreamReader(stream);
            var body = sr.ReadToEnd();
            response.Close();

            this.Document = JObject.Parse(body);
        }

        public override string GetContent()
        {
            var json = this.Document.ToString();
            return json;
        }

        public override IActionResult GetResponse(string url = null)
        {
            var content = GetContent();
            return new ContentResult()
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = content
            };
        }
    }
}
