// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;
using System.IO;

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

        internal OpenIDStreamBinding(Stream stream, BindingDirection bindingDirection)
        {
            this.BindingDirection = bindingDirection;

            using (var sr = new StreamReader(stream))
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
