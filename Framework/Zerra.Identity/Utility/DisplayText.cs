// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Zerra.Identity
{
    public class DisplayText : Attribute
    {
        public string Text { get; set; }
        public DisplayText(string text) { this.Text = text; }
    }

    public static class DisplayTextExtensions
    {
        private class EnumValueDisplayText
        {
            public string Name { get; set; }
            public DisplayText Attribute { get; set; }
        }
        private static readonly ConcurrentDictionary<Type, EnumValueDisplayText[]> enumDisplayTextAttributes = new ConcurrentDictionary<Type, EnumValueDisplayText[]>();
        private static EnumValueDisplayText[] GetEnumDisplayTextAttributes(Type type)
        {
            if (!enumDisplayTextAttributes.TryGetValue(type, out EnumValueDisplayText[] attributes))
            {
                List<EnumValueDisplayText> attributeList = new List<EnumValueDisplayText>();
                foreach (var fieldInfo in type.GetFields())
                {
                    var attribute = fieldInfo.GetCustomAttribute<DisplayText>(false);
                    if (attribute != null)
                        attributeList.Add(new EnumValueDisplayText() { Name = fieldInfo.Name, Attribute = attribute });
                }
                attributes = enumDisplayTextAttributes.GetOrAdd(type, attributeList.ToArray());
            };
            return attributes;
        }

        public static string DisplayText<T>(this T value)
            where T : Enum
        {
            var type = typeof(T);

            var attributes = GetEnumDisplayTextAttributes(type);

            string name = value.ToString();
            var attribute = attributes.FirstOrDefault(x => x.Name == name);
            if (attribute != null)
                return attribute.Attribute.Text;
            else
                return name;
        }

        public static T Parse<T>(string enumString)
            where T : Enum
        {
            var type = typeof(T);
            var values = Enum.GetValues(type);
            var attributes = GetEnumDisplayTextAttributes(type);

            foreach (T value in values)
            {
                string name = value.ToString();
                var attribute = attributes.FirstOrDefault(x => x.Name == name);

                if (attribute != null)
                {
                    if (enumString.ToLower() == attribute.Attribute.Text.ToLower())
                        return (T)value;
                }
                else
                {
                    if (enumString == name)
                        return (T)value;
                }
            }

            throw new Exception(String.Format("Could not parse \"{0}\" into enum type {1}", enumString, type.Name));
        }

        public static bool TryParse<T>(string enumString, out T e)
        {
            var type = typeof(T);
            var values = Enum.GetValues(type);
            var attributes = GetEnumDisplayTextAttributes(type);

            foreach (T value in values)
            {
                string name = value.ToString();
                var attribute = attributes.FirstOrDefault(x => x.Name == name);

                if (attribute != null)
                {
                    if (enumString.ToLower() == attribute.Attribute.Text.ToLower())
                    {
                        e = (T)value;
                        return true;
                    }
                }
                else
                {
                    if (enumString == name)
                    {
                        e = (T)value;
                        return true;
                    }
                }
            }

            e = default;
            return false;
        }
    }

}
