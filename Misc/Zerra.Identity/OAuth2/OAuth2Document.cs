// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Newtonsoft.Json.Linq;

namespace Zerra.Identity.OAuth2
{
    public abstract class OAuth2Document
    {
        public abstract BindingDirection BindingDirection { get; }
        public abstract JObject GetJson();
    }
}
