// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Xml;

namespace Zerra.Identity.Saml2
{
    public abstract class Saml2Document
    {
        public abstract BindingDirection BindingDirection { get; }
        public abstract XmlDocument GetSaml();
    }
}
