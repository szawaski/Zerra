// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;
using Zerra.Collections;

namespace Zerra.Reflection
{
    /// <summary>
    /// Readable type names without assembly information, generics are written with their arguments such as System.Collections.Generic.List&lt;System.Int32&gt;.
    /// </summary>
    internal static class TypeNames
    {
        private static readonly ConcurrentFactoryDictionary<Type, string> fullNames = new();
        private static readonly ConcurrentFactoryDictionary<Type, string> fullGenericNames = new();

        /// <summary>
        /// The full name with the generic arguments' full names such as System.Collections.Generic.Dictionary&lt;System.String,System.Int32&gt;, a generic parameter is T.
        /// </summary>
        public static string GetFullName(Type type) => fullNames.GetOrAdd(type, static (type) => Generate(type, Mode.Full));

        /// <summary>
        /// The full name with every generic argument as T such as System.Collections.Generic.Dictionary&lt;T,T&gt;.
        /// </summary>
        public static string GetFullGenericName(Type type) => fullGenericNames.GetOrAdd(type, static (type) => Generate(type, Mode.Generic));

        private enum Mode : byte
        {
            Full,
            Generic
        }

        private static string Generate(Type type, Mode mode)
        {
            if (!type.IsGenericType && !type.IsArray && !type.IsGenericParameter)
            {
                if (type.FullName is not null)
                    return type.FullName;
                if (type.Namespace is not null)
                    return $"{type.Namespace}.{type.Name}";
                return type.Name;
            }

            var sb = new StringBuilder();
            Append(sb, type, mode);
            return sb.ToString();
        }

        private static void Append(StringBuilder sb, Type type, Mode mode)
        {
            if (type.IsGenericParameter)
            {
                _ = sb.Append('T');
                return;
            }

            if (type.IsArray)
            {
                Append(sb, type.GetElementType()!, mode);
                _ = sb.Append('[');
                var rank = type.GetArrayRank();
                for (var i = 1; i < rank; i++)
                    _ = sb.Append(',');
                _ = sb.Append(']');
                return;
            }

            //a generic's FullName has its arguments' assembly qualified names, its definition's doesn't
            var name = (type.IsGenericType ? type.GetGenericTypeDefinition() : type).FullName;
            if (name is null)
            {
                if (type.Namespace is not null)
                    _ = sb.Append(type.Namespace).Append('.');
                name = type.Name;
            }

            var start = 0;
            for (var i = 0; i < name.Length; i++)
            {
                if (name[i] != '`')
                    continue;
                _ = sb.Append(name, start, i - start);
                i++;
                while (i < name.Length && name[i] >= '0' && name[i] <= '9')
                    i++;
                start = i;
                i--;
            }
            _ = sb.Append(name, start, name.Length - start);

            if (!type.IsGenericType)
                return;

            _ = sb.Append('<');
            var arguments = type.GetGenericArguments();
            for (var i = 0; i < arguments.Length; i++)
            {
                if (i > 0)
                    _ = sb.Append(',');
                if (mode == Mode.Generic)
                    _ = sb.Append('T');
                else
                    Append(sb, arguments[i], mode);
            }
            _ = sb.Append('>');
        }
    }
}
