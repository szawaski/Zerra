// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Zerra.Identity
{
    public static class XmlHelper
    {
        public static XmlElement GetSingleElementRequired(this XmlElement element, string prefix, string name, bool deep)
        {
            if (element == null || String.IsNullOrWhiteSpace(name))
                return null;

            var list = new List<XmlElement>();
            GetElements(element, prefix, name, list, deep, false);

            if (list.Count == 0)
                throw new IdentityProviderException(String.Format("Xml missing {0} element", name));
            if (list.Count > 1)
                throw new IdentityProviderException(String.Format("Xml has more than one {0} element", name));
            return list[0];
        }

        public static XmlElement GetSingleElement(this XmlElement element, string prefix, string name, bool deep)
        {
            if (element == null || String.IsNullOrWhiteSpace(name))
                return null;

            var list = new List<XmlElement>();
            GetElements(element, prefix, name, list, deep, false);

            if (list.Count == 0)
                return null;
            if (list.Count > 1)
                throw new IdentityProviderException(String.Format("Xml has more than one {0} element", name));
            return list[0];
        }

        public static XmlElement GetFirstElement(this XmlElement element, string prefix, string name, bool deep)
        {
            if (element == null || String.IsNullOrWhiteSpace(name))
                return null;

            var list = new List<XmlElement>();
            GetElements(element, prefix, name, list, deep, true);

            if (list.Count == 0)
                return null;
            return list[0];
        }

        public static List<XmlElement> GetElements(this XmlElement element, string prefix, string name, bool deep)
        {
            if (element == null || String.IsNullOrWhiteSpace(name))
                return null;

            var list = new List<XmlElement>();
            GetElements(element, prefix, name, list, deep, false);
            return list;
        }

        private static void GetElements(XmlElement element, string prefix, string name, List<XmlElement> list, bool deep, bool single)
        {
            foreach (var node in element.ChildNodes.OfType<XmlElement>())
            {
                if (deep)
                {
                    GetElements(node, prefix, name, list, deep, single);
                    if (single && list.Count > 0)
                    {
                        break;
                    }
                }
                if ((prefix == null || node.Prefix == prefix) && node.LocalName == name)
                {
                    list.Add(node);
                    if (single)
                    {
                        break;
                    }
                }
            }
        }

        public static string GetAttributeRequired(this XmlElement element, string name)
        {
            var attribute = element.GetAttribute(name);
            if (String.IsNullOrWhiteSpace(attribute))
                throw new IdentityProviderException(String.Format("Xml missing attribute {0}", name));
            return attribute;
        }

        public static void SetPrefix(string prefix, XmlNode node)
        {
            foreach (XmlNode n in node.ChildNodes)
                SetPrefix(prefix, n);
            node.Prefix = prefix;
        }
    }
}
