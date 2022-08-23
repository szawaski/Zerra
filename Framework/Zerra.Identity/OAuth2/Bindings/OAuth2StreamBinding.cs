// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System.IO;
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

        internal OAuth2StreamBinding(Stream stream, BindingDirection flowDirection)
        {
            this.BindingDirection = flowDirection;

            using (var sr = new System.IO.StreamReader(stream))
            {
                var body = sr.ReadToEnd();
                this.Document = JObject.Parse(body);
            }
        }

        public override string GetContent()
        {
            var json = this.Document.ToString();
            return json;
        }

        public override IdentityHttpResponse GetResponse(string url = null)
        {
            var content = GetContent();
            return new IdentityHttpResponse("text/html", content);
        }
    }
}
