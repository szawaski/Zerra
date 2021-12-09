﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Identity.OpenID.Bindings;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;

namespace Zerra.Identity.OpenID
{
    public abstract class OpenIDBinding : Binding<JObject>
    {
        public const string ClientFormName = "client_id";

        //public static bool IsOpenIDBinding(HttpRequest request)
        //{
        //    if (request.HasFormContentType)
        //        return request.Form.Keys.Contains(OpenIDBinding.ClientFormName);
        //    else
        //        return request.Query.Keys.Contains(OpenIDBinding.ClientFormName);
        //}

        public static OpenIDBinding GetBindingForRequest(HttpRequest request, BindingDirection flowDirection)
        {
            if (request.HasFormContentType)
                return new OpenIDFormBinding(request, flowDirection);
            else
                return new OpenIDQueryBinding(request, flowDirection);
        }

        public static OpenIDBinding GetBindingForResponse(WebResponse request, BindingDirection flowDirection)
        {
            return new OpenIDStreamBinding(request, flowDirection);
        }

        public static OpenIDBinding GetBindingForDocument(OpenIDDocument document, BindingType bindingType)
        {
            switch (bindingType)
            {
                case BindingType.Null:
                    throw new IdentityProviderException("Cannot have null binding type");
                case BindingType.Form:
                    return new OpenIDFormBinding(document);
                case BindingType.Query:
                    return new OpenIDQueryBinding(document);
                case BindingType.Stream:
                    return new OpenIDStreamBinding(document);
                default:
                    throw new NotImplementedException();
            }
        }

        public void ValidateFields(string[] expectedUrls)
        {
            var redirect = Redirect(this.Document);
            if (expectedUrls == null || (!String.IsNullOrWhiteSpace(redirect) && !expectedUrls.Contains(redirect)))
                throw new IdentityProviderException("OpenID Document Invalid: Redirect");
        }
        private static string Redirect(JObject json)
        {
            var value = json["redirect_uri"]?.ToObject<string>();
            if (String.IsNullOrWhiteSpace(value))
                throw new IdentityProviderException("OpenID Document Missing 'redirect_uri' value");
            return value;
        }
    }
}
