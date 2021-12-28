// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System.Net;

namespace Zerra.Identity.OpenID.Bindings
{
    internal class OpenIDStreamBinding : OpenIDBinding
    {
        public override BindingType BindingType => BindingType.Stream;

        internal OpenIDStreamBinding(OpenIDDocument document)
        {
            this.BindingDirection = document.BindingDirection;

            this.Document = document.GetJson();
        }

        internal OpenIDStreamBinding(WebResponse response, BindingDirection bindingDirection)
        {
            this.BindingDirection = bindingDirection;

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

        public override IdentityHttpResponse GetResponse(string url = null)
        {
            var content = GetContent();
            return new IdentityHttpResponse("text/html", content);
        }
    }
}
