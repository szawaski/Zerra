// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.OAuth2.Bindings;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Zerra.Identity.OAuth2
{
    public abstract class OAuth2Binding : Binding<JObject>
    {
        public const string ClientFormName = "client";

        public static bool IsOAuth2Binding(HttpRequest request)
        {
            if (request.HasFormContentType)
                return request.Form.Keys.Contains(OAuth2Binding.ClientFormName);
            else
                return request.Query.Keys.Contains(OAuth2Binding.ClientFormName);
        }

        public static OAuth2Binding GetBindingForRequest(HttpRequest request, BindingDirection flowDirection)
        {
            if (request.HasFormContentType)
                return new OAuth2FormBinding(request, flowDirection);
            else
                return new OAuth2QueryBinding(request, flowDirection);
        }

        public static OAuth2Binding GetBindingForResponse(WebResponse request, BindingDirection flowDirection)
        {
            return new OAuth2StreamBinding(request, flowDirection);
        }

        public static OAuth2Binding GetBindingForDocument(OAuth2Document document, BindingType bindingType)
        {
            switch (bindingType)
            {
                case BindingType.Null:
                    throw new IdentityProviderException("Cannot have null binding type");
                case BindingType.Form:
                    return new OAuth2FormBinding(document);
                case BindingType.Query:
                    return new OAuth2QueryBinding(document);
                case BindingType.Stream:
                    return new OAuth2StreamBinding(document);
                default:
                    throw new NotImplementedException();
            }
        }

        internal static Dictionary<string, string> GetOtherClaims(JObject document)
        {
            var otherFields = new Dictionary<string, string>();
            var usedKeys = new string[] { OAuth2Binding.ClientFormName };

            foreach (var item in document)
            {
                if (!usedKeys.Contains(item.Key))
                    otherFields.Add(item.Key, item.Value.ToObject<string>());
            }

            return otherFields;
        }

        public void ValidateFields(string[] expectedUrls)
        {
            var redirect = Redirect(this.Document);
            if (expectedUrls == null || (!String.IsNullOrWhiteSpace(redirect) && !expectedUrls.Contains(redirect)))
                throw new IdentityProviderException("OAuth Document Invalid: Redirect");
        }
        private static string Redirect(JObject json)
        {
            var value = json["redirect"]?.ToObject<string>();
            if (String.IsNullOrWhiteSpace(value))
                throw new IdentityProviderException("OAuth Document Missing 'redirect' value");
            return value.Split('?').First();
        }
    }
}
