// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;

namespace Zerra.Identity.OpenID
{
    public abstract class OpenIDDocument
    {
        public abstract BindingDirection BindingDirection { get; }
        public abstract JObject GetJson();
    }
}
