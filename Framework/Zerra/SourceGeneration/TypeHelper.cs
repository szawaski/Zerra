// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.Collections;

namespace Zerra.SourceGeneration
{
    public static class TypeHelper
    {
        private static readonly ConcurrentFactoryDictionary<Type, string> niceNames = new();
        private static readonly ConcurrentFactoryDictionary<Type, string> niceFullNames = new();
        private static readonly ConcurrentFactoryDictionary<string, ConcurrentList<Type?>> typeByName = new();

        public static string GetNiceName(Type it)
        {
            if (it is null)
                return "null";
            var name = niceNames.GetOrAdd(it, static (it) => GenerateNiceName(it, false));
            return name;
        }
        public static string GetNiceFullName(Type it)
        {
            if (it is null)
                return "null";
            var name = niceFullNames.GetOrAdd(it, static (it) => GenerateNiceName(it, true));
            return name;
        }

        private static string GenerateNiceName(Type type, bool includeNamespace)
        {
            if (type.ContainsGenericParameters)
            {
                var span = type.Name.AsSpan();
                var i = 0;
                for (; i < span.Length; i++)
                {
                    if (span[i] == '`')
                        break;
                }

#if NETSTANDARD2_0
                var name = span.Slice(0, i).ToString();
#else
                var name = span.Slice(0, i);
#endif

                //Have to inspect because inner generics or partially constructed generics won't work
                var parameters = type.GetGenericArguments();

                var sb = new StringBuilder();

                if (includeNamespace && type.Namespace is not null)
                    _ = sb.Append(type.Namespace).Append('.');
                _ = sb.Append(name).Append('<');

                for (var j = 0; j < parameters.Length; j++)
                {
                    if (j > 0)
                        _ = sb.Append(',');
                    var parameter = parameters[j];
                    if (parameter.IsGenericParameter)
                        _ = sb.Append('T');
                    else if (includeNamespace)
                        _ = sb.Append(GetNiceFullName(parameter));
                    else
                        _ = sb.Append(GetNiceName(parameter));
                }

                _ = sb.Append('>');
                return sb.ToString();
            }
            else if ((type.IsGenericType || type.IsArray) && type.FullName is not null)
            {
                var sb = new StringBuilder();

                var openGeneric = 0;
                var openGenericArray = 0;
                var nameStart = 0;
                var inArray = false;
                var span = type.FullName.AsSpan();
                var i = 0;
                for (; i < span.Length; i++)
                {
                    var c = span[i];
                    switch (c)
                    {
                        case '[':
                            if (nameStart == -1)
                            {
                                if (openGeneric == openGenericArray)
                                {
                                    openGeneric++;
                                }
                                else
                                {
                                    openGenericArray++;
                                    if (nameStart != -1)
                                    {
#if NETSTANDARD2_0
                                        sb.Append(span.Slice(nameStart, i - 1 - nameStart).ToString());
#else
                                        sb.Append(span.Slice(nameStart, i - 1 - nameStart));
#endif
                                        nameStart = -1;
                                    }
                                    nameStart = i + 1;
                                    if (i > 0 && span[i - 1] != ',')
                                        sb.Append('<');
                                }
                            }
                            else
                            {
#if NETSTANDARD2_0
                                sb.Append(span.Slice(nameStart, i + 1 - nameStart).ToString());
#else
                                sb.Append(span.Slice(nameStart, i + 1 - nameStart));
#endif
                                nameStart = -1;
                                inArray = true;
                            }
                            break;
                        case '.':
                            if (!includeNamespace && nameStart != -1)
                            {
                                nameStart = i + 1;
                            }
                            break;
                        case ',':
                            if (inArray)
                            {
                                sb.Append(',');
                            }
                            else if (openGenericArray != openGeneric)
                            {
                                sb.Append(',');
                            }
                            else if (nameStart != -1)
                            {
#if NETSTANDARD2_0
                                sb.Append(span.Slice(nameStart, i - nameStart).ToString());
#else
                                sb.Append(span.Slice(nameStart, i - nameStart));
#endif
                                nameStart = -1;
                            }
                            break;
                        case '`':
                            if (nameStart != -1)
                            {
#if NETSTANDARD2_0
                                sb.Append(span.Slice(nameStart, i - nameStart).ToString());
#else
                                sb.Append(span.Slice(nameStart, i - nameStart));
#endif
                                nameStart = -1;
                            }
                            break;
                        case ']':
                            if (inArray)
                            {
                                sb.Append(']');
                                inArray = false;
                            }
                            else if (nameStart == -1)
                            {
                                if (openGenericArray == openGeneric)
                                {
                                    openGenericArray--;
                                }
                                else
                                {
                                    openGeneric--;
                                    nameStart = i + 1;
                                    sb.Append('>');
                                }
                            }
                            break;
                    }
                }

                return sb.ToString();
            }
            else
            {
                if (includeNamespace)
                {
                    if (type.FullName is not null)
                        return type.FullName;
                    if (type.Namespace is not null)
                        return $"{type.Namespace}.{type.Name}";
                    return type.Name;
                }
                else
                {
                    return type.Name;
                }
            }
        }

        public static string MakeNiceNameGeneric(string typeName)
        {
            var sb = new StringBuilder();

            var chars = typeName.AsSpan();
            var start = 0;
            var depth = 0;
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                switch (c)
                {
                    case '<':
                        depth++;
                        if (depth == 1)
#if NETSTANDARD2_0
                            _ = sb.Append(chars.Slice(start, i + 1 - start).ToString());
#else
                            _ = sb.Append(chars[start..(i + 1)]);
#endif
                        break;
                    case ',':
                        if (depth == 1)
                            _ = sb.Append("T,");
                        break;
                    case '>':
                        depth--;
                        if (depth == 0)
                        {
                            _ = sb.Append('T');
                            start = i;
                        }
                        break;
                }
            }

#if NETSTANDARD2_0
            _ = sb.Append(chars.Slice(start, chars.Length - start).ToString());
#else
            _ = sb.Append(chars[start..]);
#endif

            return sb.ToString();
        }

        public static Type GetTypeFromName(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (!typeByName.TryGetValue(name, out var matches))
            {
#pragma warning disable IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
                var type = Type.GetType(name);
#pragma warning restore IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
                if (RuntimeFeature.IsDynamicCodeSupported)
                    type ??= ParseType(name);
                if (type is null)
                    throw new InvalidOperationException($"Could not find type {name}");
                matches = typeByName.GetOrAdd(name, static (key) => new());
                lock (matches)
                {
                    if (matches.Count == 0 || (type is not null && !matches.Contains(type)))
                        matches.Add(type);
                }
                return type;
            }
            else if (matches.Count == 1)
            {
                var type = matches[0];
                if (type is null)
                    throw new InvalidOperationException($"Could not find type {name}");
                return type;
            }
            else
            {
                throw new Exception($"More than one type matches {name} - {String.Join(", ", matches.Where(x => x is not null).Select(x => x!.AssemblyQualifiedName).ToArray())}");
            }
        }
        public static bool TryGetTypeFromName(string name, [MaybeNullWhen(false)] out Type? type)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (!typeByName.TryGetValue(name, out var matches))
            {
#pragma warning disable IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
                type = Type.GetType(name);
#pragma warning restore IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
                if (RuntimeFeature.IsDynamicCodeSupported)
                    type ??= ParseType(name);
                if (type != null)
                {
                    matches = typeByName.GetOrAdd(name, static (key) => new());
                    lock (matches)
                    {
                        if ((matches.Count == 0 || type is not null) && !matches.Contains(type))
                            matches.Add(type);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (matches.Count == 1)
            {
                type = matches[0];
                return type is not null;
            }
            else
            {
                type = null;
                return false;
            }
        }

        public static void Register(Type type)
        {
            var niceName = GetNiceName(type);
            var niceFullName = GetNiceFullName(type);

            var types = typeByName.GetOrAdd(niceName, static (key) => new());
            if (!types.Contains(type))
                types.Add(type);

            types = typeByName.GetOrAdd(niceFullName, static (key) => new());
            if (!types.Contains(type))
                types.Add(type);
        }
        internal static void Register(Type type, string niceName, string niceFullName)
        {
            _ = niceNames.TryAdd(type, niceName);
            var types = typeByName.GetOrAdd(niceName, static (key) => new());
            if (!types.Contains(type))
                types.Add(type);

            _ = niceNames.TryAdd(type, niceFullName);
            types = typeByName.GetOrAdd(niceFullName, static (key) => new());
            if (!types.Contains(type))
                types.Add(type);
        }

#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#pragma warning disable IL2055 // Either the type on which the MakeGenericType is called can't be statically determined, or the type parameters to be used for generic arguments can't be statically determined.

        private static unsafe Type? ParseType(string name)
        {
            var index = 0;
            var chars = name.AsSpan();
            string? currentName = null;

            char[]? rented = null;
            scoped Span<char> current;
            if (name.Length <= 128)
            {
                current = stackalloc char[name.Length];
            }
            else
            {
                rented = ArrayPool<char>.Shared.Rent(name.Length);
                current = rented;
            }

            try
            {
                var i = 0;

                var genericArguments = new List<Type>();
                var arrayDimensions = new List<int>();

                var expectingGenericOpen = false;
                var expectingGenericComma = true;
                var openGeneric = false;
                var openGenericType = false;
                var openArray = false;
                var openArrayOneDimension = false;
                var openGenericTypeSubBrackets = 0;

                var done = false;
                while (index < chars.Length)
                {
                    var c = chars[index++];

                    switch (c)
                    {
                        case '`':
                            {
                                if (openGenericType)
                                {
                                    current[i++] = c;
                                }
                                else if (openArray || (openGeneric && !openGenericType))
                                {
                                    throw new Exception($"{nameof(ParseType)} Unexpected '{c}' at position {index - 1}");
                                }
                                else
                                {
                                    expectingGenericOpen = true;
                                    current[i++] = c;
                                }
                                break;
                            }
                        case '[':
                            {
                                if (openArray)
                                {
                                    throw new Exception($"{nameof(ParseType)} Unexpected '{c}' at position {index - 1}");
                                }

                                if (openGenericType)
                                {
                                    current[i++] = c;
                                    openGenericTypeSubBrackets++;
                                }
                                else if (expectingGenericOpen)
                                {
                                    expectingGenericOpen = false;
                                    openGeneric = true;

                                    if (i == 0)
                                        throw new Exception($"{nameof(ParseType)} Unexpected '{c}' at position {index - 1}");
                                    currentName = current.Slice(0, i).ToString();
                                    i = 0;
                                }
                                else if (openGeneric)
                                {
                                    openGenericType = true;
                                }
                                else
                                {
                                    openArray = true;

                                    if (currentName is null)
                                    {
                                        if (i == 0)
                                            throw new Exception($"{nameof(ParseType)} Unexpected '{c}' at position {index - 1}");
                                        currentName = current.Slice(0, i).ToString();
                                        i = 0;
                                    }
                                }

                                break;
                            }
                        case ',':
                            {

                                if (!openGeneric || openGenericType || (openArray && !openArrayOneDimension))
                                {
                                    current[i++] = c;
                                }
                                else if (openGeneric && !openGenericType && expectingGenericComma)
                                {
                                    expectingGenericComma = false;
                                }
                                else
                                {
                                    throw new Exception($"{nameof(ParseType)} Unexpected '{c}' at position {index - 1}");
                                }
                                break;
                            }
                        case '*':
                            {
                                if (openGenericType)
                                {
                                    current[i++] = c;
                                }
                                else if (openArray)
                                {
                                    if (i > 0)
                                        throw new Exception($"{nameof(ParseType)} Unexpected {c}");
                                    openArrayOneDimension = true;
                                    current[i++] = c;
                                }
                                else
                                {
                                    throw new Exception($"{nameof(ParseType)} Unexpected {c}");
                                }
                                break;
                            }
                        case ']':
                            {
                                if (openGenericTypeSubBrackets > 0)
                                {
                                    current[i++] = c;
                                    openGenericTypeSubBrackets--;
                                }
                                else if (openGenericType)
                                {
                                    openGenericType = false;
                                    var genericArgumentName = current.Slice(0, i).ToString();
                                    i = 0;
                                    var genericArgumentType = GetTypeFromName(genericArgumentName);
                                    genericArguments.Add(genericArgumentType);
                                    expectingGenericComma = true;
                                }
                                else if (openGeneric)
                                {
                                    openGeneric = false;
                                }
                                else if (openArray)
                                {
                                    openArray = false;
                                    if (i > 0)
                                    {
                                        if (openArrayOneDimension)
                                            arrayDimensions.Add(1);
                                        else
                                            arrayDimensions.Add(i + 1);
                                    }
                                    else
                                    {
                                        arrayDimensions.Add(0);
                                    }
                                    openArrayOneDimension = false;
                                    i = 0;
                                }
                                else
                                {
                                    throw new Exception($"{nameof(ParseType)} Unexpected '{c}' at position {index - 1}");
                                }
                                break;
                            }
                        default:
                            {
                                if (openArray || (openGeneric && !openGenericType))
                                {
                                    throw new Exception($"{nameof(ParseType)} Unexpected {c}");
                                }

                                current[i++] = c;
                                break;
                            }
                    }

                    if (done)
                        break;
                }

                currentName ??= current.Slice(0, i).ToString();

                var type = GetTypeFromNameWithoutParse(currentName);
                if (type is null)
                    return null;

                if (genericArguments.Count > 0)
                {
                    type = type.MakeGenericType(genericArguments.ToArray());
                }
                foreach (var arrayDimention in arrayDimensions)
                {
                    if (arrayDimention > 0)
                        type = type.MakeArrayType(arrayDimention);
                    else
                        type = type.MakeArrayType();
                }

                return type;
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<char>.Shared.Return(rented);
                }
            }
        }

        private static Type? GetTypeFromNameWithoutParse(string name)
        {
            if (!typeByName.TryGetValue(name, out var matches))
            {
#pragma warning disable IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
                var type = Type.GetType(name);
#pragma warning restore IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.

                matches = typeByName.GetOrAdd(name, static (key) => new());
                lock (matches)
                {
                    //only add null if it's new and will be the only item
                    if (matches.Count == 0 || (type is not null && !matches.Contains(type)))
                        matches.Add(type);
                }
                return type;
            }
            else if (matches.Count == 1)
            {
                var type = matches[0];
                return type;
            }
            else
            {
                throw new Exception($"More than one type matches {name} - {String.Join(", ", matches.Where(x => x is not null).Select(x => x!.AssemblyQualifiedName).ToArray())}");
            }
        }

#pragma warning restore IL2055 // Either the type on which the MakeGenericType is called can't be statically determined, or the type parameters to be used for generic arguments can't be statically determined.
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
    }
}
