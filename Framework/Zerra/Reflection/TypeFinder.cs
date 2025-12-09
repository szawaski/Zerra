// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Zerra.Collections;

namespace Zerra.Reflection
{
    public static class TypeFinder
    {
        //private static readonly ConcurrentFactoryDictionary<Type, string> niceNames = new();
        //private static readonly ConcurrentFactoryDictionary<Type, string> niceFullNames = new();
        private static readonly ConcurrentFactoryDictionary<string, ConcurrentList<Type?>> typeByName = new();

        /// <summary>
        /// Resolves a type by its name string.
        /// Supports simple names, fully-qualified names, generic types, and array types.
        /// First attempts standard Type.GetType resolution, then falls back to custom parsing if dynamic code is supported.
        /// Results are cached for performance.
        /// </summary>
        /// <param name="name">The type name to resolve. Cannot be null or whitespace.</param>
        /// <returns>The resolved type.</returns>
        /// <exception cref="ArgumentNullException">Thrown if name is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the type cannot be found.</exception>
        /// <exception cref="Exception">Thrown if multiple types match the given name.</exception>
        public static Type GetTypeFromName(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (!typeByName.TryGetValue(name, out var matches))
            {
#pragma warning disable IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
                var type = Type.GetType(name);
#pragma warning restore IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
                if (type == null)
                    throw new InvalidOperationException($"Could not find type {name}. It may have been trimmed depending on the build configuration.");
                matches = typeByName.GetOrAdd(name, static (key) => new());
                lock (matches)
                {
                    if (matches.Count == 0 || !matches.Contains(type))
                        matches.Add(type);
                }
                return type;
            }
            else if (matches.Count == 1)
            {
                var type = matches[0];
                if (type == null)
                    throw new InvalidOperationException($"Could not find type {name}");
                return type;
            }
            else
            {
                throw new Exception($"More than one type matches {name} - {String.Join(", ", matches.Where(x => x is not null).Select(x => x!.AssemblyQualifiedName).ToArray())}");
            }
        }
        /// <summary>
        /// Attempts to resolve a type by its name string.
        /// Supports simple names, fully-qualified names, generic types, and array types.
        /// First attempts standard Type.GetType resolution, then falls back to custom parsing if dynamic code is supported.
        /// Results are cached for performance.
        /// </summary>
        /// <param name="name">The type name to resolve. Cannot be null or whitespace.</param>
        /// <param name="type">The resolved type if found; otherwise null.</param>
        /// <returns>True if the type was successfully resolved; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if name is null or whitespace.</exception>
        /// <exception cref="Exception">Thrown if multiple types match the given name.</exception>
        public static bool TryGetTypeFromName(string name, [NotNullWhen(true)] out Type? type)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (!typeByName.TryGetValue(name, out var matches))
            {
#pragma warning disable IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
                type = Type.GetType(name);
#pragma warning restore IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
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

        /// <summary>
        /// Registers a type for lookup by its nice and fully-qualified names.
        /// Caches the type so it can be resolved by either name format.
        /// Useful for registering types generated by source generators or dynamically loaded types.
        /// </summary>
        /// <param name="type">The type to register.</param>
        public static void Register(Type type)
        {
            //var niceName = GetNiceName(type);
            //var niceFullName = GetNiceFullName(type);

            if (type.AssemblyQualifiedName != null)
            {
                var types = typeByName.GetOrAdd(type.AssemblyQualifiedName, static (key) => new());
                if (!types.Contains(type))
                    types.Add(type);
            }

            //types = typeByName.GetOrAdd(niceName, static (key) => new());
            //if (!types.Contains(type))
            //    types.Add(type);

            //types = typeByName.GetOrAdd(niceFullName, static (key) => new());
            //if (!types.Contains(type))
            //    types.Add(type);
        }
    }
}
