// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.OAuth2.Bindings;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;

namespace Zerra.Identity.OAuth2
{
    public abstract class OAuth2Binding : Binding<JObject>
    {
        public const string ClientFormName = "client";

        public static bool IsOAuth2Binding(IdentityHttpRequest request)
        {
            if (request.HasFormContentType)
                return request.Form.Keys.Contains(OAuth2Binding.ClientFormName);
            else
                return request.Query.Keys.Contains(OAuth2Binding.ClientFormName);
        }

        public static OAuth2Binding GetBindingForRequest(IdentityHttpRequest request, BindingDirection flowDirection)
        {
            if (request.HasFormContentType)
                return new OAuth2FormBinding(request, flowDirection);
            else
                return new OAuth2QueryBinding(request, flowDirection);
        }

        public static OAuth2Binding GetBindingForResponse(Stream stream, BindingDirection flowDirection)
        {
            return new OAuth2StreamBinding(stream, flowDirection);
        }

        public static OAuth2Binding GetBindingForDocument(OAuth2Document document, BindingType bindingType)
        {
            return bindingType switch
            {
                BindingType.Null => throw new IdentityProviderException("Cannot have null binding type"),
                BindingType.Form => new OAuth2FormBinding(document),
                BindingType.Query => new OAuth2QueryBinding(document),
                BindingType.Stream => new OAuth2StreamBinding(document),
                _ => throw new NotImplementedException(),
            };
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
