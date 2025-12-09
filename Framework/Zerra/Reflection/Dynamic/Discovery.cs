// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.Collections;

namespace Zerra.Reflection.Dynamic
{
    /// <summary>
    /// Provides runtime type discovery and lookup capabilities for finding types based on interfaces, base types, and attributes.
    /// Supports both generic and non-generic type matching with caching for performance optimization.
    /// </summary>
    /// <remarks>
    /// <see cref="Initialize"/> must be called before using any lookup methods.
    /// Discovery scans all loaded assemblies to build type relationship caches.
    /// </remarks>
    [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
    public static class Discovery
    {
        private static readonly ConcurrentDictionary<Type, List<Type>> typeByInterface = new();
        private static readonly ConcurrentDictionary<string, List<Type>> typeByInterfaceName = new();
        private static readonly ConcurrentDictionary<Type, List<Type>> classByInterface = new();
        private static readonly ConcurrentDictionary<string, List<Type>> classByInterfaceName = new();
        private static readonly ConcurrentDictionary<Type, List<Type>> classByBaseType = new();
        private static readonly ConcurrentDictionary<string, List<Type>> classByBaseTypeName = new();
        private static readonly ConcurrentDictionary<Type, List<Type>> typeByAttribute = new();

        private static readonly ConcurrentDictionary<Type, List<Type>> interfaceByType = new();

        private static readonly ConcurrentDictionary<string, ConcurrentReadWriteList<Type?>> typeByName = new();

        private static readonly ConcurrentFactoryDictionary<Type, string> niceFullNames = new();
        private static readonly ConcurrentFactoryDictionary<Type, string> niceFullGenericNames = new();

        private static readonly HashSet<string> discoveredAssemblies = new();
        private static readonly HashSet<Type> discoveredTypes = new();

        private static bool discovered = false;

        /// <summary>
        /// Runs type discovery to scan assemblies and cache type information for lookup operations.
        /// </summary>
        /// <param name="forceLoadAssemblies">If true, attempts to load all assembly files from the application base directory before discovering types.</param>
        public static void Initialize(bool forceLoadAssemblies)
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                throw new NotSupportedException($"{nameof(Discovery)}.{nameof(Initialize)} not supported.  Dynamic code generation is not supported in this build configuration.");

            if (discovered)
                return;
            lock (discoveredTypes)
            {
                if (discovered)
                    return;

                if (forceLoadAssemblies)
                {
                    LoadAssemblies();
                }

                DiscoverAssemblies();

                discovered = true;
            }
        }

        private static void LoadAssemblies()
        {
            var loadedAssemblies = new HashSet<string>();
            foreach (var currentAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (currentAssembly.IsDynamic)
                    continue;
                if (!String.IsNullOrEmpty(currentAssembly.FullName))
                    _ = loadedAssemblies.Add(currentAssembly.FullName);
            }

            var assemblyPath = AppDomain.CurrentDomain.BaseDirectory;

            var assemblyFilePaths = System.IO.Directory.GetFiles(assemblyPath, "*.dll");

            foreach (var assemblyFilePath in assemblyFilePaths)
            {
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(assemblyFilePath);

                    if (assemblyName.Name is not null && assemblyName.Name.EndsWith(".Web.Views"))
                        continue;

                    if (!loadedAssemblies.Add(assemblyName.FullName))
                        continue;

                    try
                    {
                        var assembly = Assembly.Load(assemblyName);
                        Console.WriteLine($"Discovery Loaded: {assembly.ToString()}");
                    }
                    catch (FileNotFoundException)
                    {
                        try
                        {
#if NETSTANDARD2_0
                            var assemblyFileName = assemblyFilePath.Split(new char[] { System.IO.Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).Last();
#else
                            var assemblyFileName = assemblyFilePath.Split(System.IO.Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();
#endif
                            var assembly = Assembly.LoadFrom(assemblyFileName);
                            Console.WriteLine($"Discovery Loaded: {assembly.ToString()}");
                        }
                        catch (Exception)
                        {
                            //何も
                        }
                    }
                    catch (Exception)
                    {
                        //何も
                    }
                }
                catch { }
            }
        }
        private static void DiscoverAssemblies()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                    continue;
                if (assembly.FullName is null)
                    continue;
                if (discoveredAssemblies.Contains(assembly.FullName))
                    continue;
                if (assembly.FullName.StartsWith("System."))
                    continue;
                if (assembly.FullName.StartsWith("Zerra,") || assembly.FullName.StartsWith("Zerra."))
                    continue;

                DiscoverAssembly(assembly);
            }
        }
        private static void DiscoverAssembly(Assembly assembly)
        {
            if (assembly.FullName is null) throw new ArgumentNullException(nameof(Assembly.FullName));

            if (!discoveredAssemblies.Add(assembly.FullName))
                return;

            Type[]? typesInAssembly = null;
            try
            {
                typesInAssembly = assembly.GetTypes();
            }
            catch { }

            if (typesInAssembly is null)
                return;

            foreach (var typeInAssembly in typesInAssembly)
            {
                if (String.IsNullOrWhiteSpace(typeInAssembly.FullName))
                    continue;
                DiscoverType(typeInAssembly);
            }
        }
        private static void DiscoverType(Type typeInAssembly)
        {
            if (!discoveredTypes.Add(typeInAssembly))
                return;

            DiscoverType(typeInAssembly, typeInAssembly.GetInterfaces(), true);
        }
        private static void DiscoverType(Type typeInAssembly, Type[] interfaceTypes, bool skipDiscoveredCheck)
        {
            if (!skipDiscoveredCheck && !discoveredTypes.Add(typeInAssembly))
                return;

            var typeList1 = typeByName.GetOrAdd(typeInAssembly.Name, static (key) => new());
            typeList1.Add(typeInAssembly);
            if (typeInAssembly.FullName is not null && typeInAssembly.Name != typeInAssembly.FullName)
            {
                var typeList2 = typeByName.GetOrAdd(typeInAssembly.FullName, static (key) => new());
                typeList2.Add(typeInAssembly);
            }

            var test = typeInAssembly.Name;
            if (interfaceTypes.Length > 0)
            {
                var interfaceByTypeList = interfaceByType.GetOrAdd(typeInAssembly, static (key) => new());

                foreach (var interfaceType in interfaceTypes)
                {
                    interfaceByTypeList.Add(interfaceType);

                    var typeByInterfaceList = typeByInterface.GetOrAdd(interfaceType, static (key) => new());
                    typeByInterfaceList.Add(typeInAssembly);

                    string? interfaceTypeName = null;
                    string? interfaceTypeGenericName = null;
                    if (interfaceType.IsGenericType)
                    {
                        interfaceTypeName = GetNiceFullName(interfaceType);
                        var typeByInterfaceNameList = typeByInterfaceName.GetOrAdd(interfaceTypeName, static (key) => new());
                        if (!typeByInterfaceNameList.Contains(typeInAssembly))
                            typeByInterfaceNameList.Add(typeInAssembly);

                        interfaceTypeGenericName = GetNiceFullGenericName(interfaceType);
                        var typeByInterfaceGenericNameList = typeByInterfaceName.GetOrAdd(interfaceTypeGenericName, static (key) => new());
                        if (!typeByInterfaceGenericNameList.Contains(typeInAssembly))
                            typeByInterfaceGenericNameList.Add(typeInAssembly);
                    }

                    if (!typeInAssembly.IsAbstract && typeInAssembly.IsClass)
                    {
                        var classByInterfaceList = classByInterface.GetOrAdd(interfaceType, static (key) => new());
                        classByInterfaceList.Add(typeInAssembly);

                        if (interfaceType.IsGenericType)
                        {
                            var classByInterfaceNameList = classByInterfaceName.GetOrAdd(interfaceTypeName!, static (key) => new());
                            if (!classByInterfaceNameList.Contains(typeInAssembly))
                                classByInterfaceNameList.Add(typeInAssembly);

                            var typeByInterfaceGenericNameList = classByInterfaceName.GetOrAdd(interfaceTypeGenericName!, static (key) => new());
                            if (!typeByInterfaceGenericNameList.Contains(typeInAssembly))
                                typeByInterfaceGenericNameList.Add(typeInAssembly);
                        }
                    }
                }
            }

            Type? baseType = typeInAssembly.BaseType;
            while (baseType is not null)
            {
                var classByBaseList = classByBaseType.GetOrAdd(baseType, static (key) => new());
                classByBaseList.Add(typeInAssembly);

                if (baseType.ContainsGenericParameters)
                {
                    var baseTypeName = GetNiceFullName(baseType);
                    var classByBaseNameList = classByBaseTypeName.GetOrAdd(baseTypeName, static (key) => new());
                    classByBaseNameList.Add(typeInAssembly);
                }

                baseType = baseType.BaseType;
            }

            var attributes = typeInAssembly.GetCustomAttributes();
            var attributeTypes = attributes.Select(x => x.GetType()).Distinct().ToArray();
            foreach (var attribute in attributes)
            {
                var thisAttributeType = attribute.GetType();
                while (thisAttributeType is not null && thisAttributeType != typeof(Attribute))
                {
                    var list = typeByAttribute.GetOrAdd(thisAttributeType, static (key) => new());
                    list.Add(typeInAssembly);
                    thisAttributeType = thisAttributeType.BaseType;
                }
            }
        }

        /// <summary>
        /// Determines whether there are any types that implement the specified interface.
        /// </summary>
        /// <param name="interfaceType">The interface type to check for implementations.</param>
        /// <returns>True if at least one implementation of the interface was found; otherwise false.</returns>
        /// <exception cref="InvalidOperationException">Thrown when discovery has not been run.</exception>
        /// <exception cref="ArgumentNullException">Thrown when interfaceType is null.</exception>
        /// <exception cref="ArgumentException">Thrown when interfaceType is not an interface.</exception>
        public static bool HasTypeByInterface(Type interfaceType)
        {
            if (!discovered)
                throw new InvalidOperationException($"Discovery has not been run. Call {nameof(Discovery)}.{nameof(Initialize)}() first.");
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.Name} is not an interface");

            if (interfaceType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(interfaceType);
                return typeByInterfaceName.ContainsKey(name);
            }
            else
            {
                return typeByInterface.ContainsKey(interfaceType);
            }
        }
        /// <summary>
        /// Determines whether there are any classes that inherit from the specified base type.
        /// </summary>
        /// <param name="baseType">The base type to check for derived classes.</param>
        /// <returns>True if at least one class deriving from the base type was found; otherwise false.</returns>
        /// <exception cref="InvalidOperationException">Thrown when discovery has not been run.</exception>
        /// <exception cref="ArgumentNullException">Thrown when baseType is null.</exception>
        public static bool HasClassByBaseType(Type baseType)
        {
            if (!discovered)
                throw new InvalidOperationException($"Discovery has not been run. Call {nameof(Discovery)}.{nameof(Initialize)}() first.");
            if (baseType is null)
                throw new ArgumentNullException(nameof(baseType));

            if (baseType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(baseType);
                return classByBaseTypeName.ContainsKey(name);
            }
            else
            {
                return classByBaseType.ContainsKey(baseType);
            }
        }
        /// <summary>
        /// Determines whether there are any classes that implement the specified interface.
        /// </summary>
        /// <param name="interfaceType">The interface type to check for implementations.</param>
        /// <returns>True if at least one class implementing the interface was found; otherwise false.</returns>
        /// <exception cref="InvalidOperationException">Thrown when discovery has not been run.</exception>
        /// <exception cref="ArgumentNullException">Thrown when interfaceType is null.</exception>
        /// <exception cref="ArgumentException">Thrown when interfaceType is not an interface.</exception>
        public static bool HasClassByInterface(Type interfaceType)
        {
            if (!discovered)
                throw new InvalidOperationException($"Discovery has not been run. Call {nameof(Discovery)}.{nameof(Initialize)}() first.");
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.Name} is not an interface");

            if (interfaceType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(interfaceType);
                return classByInterfaceName.ContainsKey(name);
            }
            else
            {
                return classByInterface.ContainsKey(interfaceType);
            }
        }

        /// <summary>
        /// Gets a type that implements the specified interface.
        /// </summary>
        /// <param name="interfaceType">The interface type to look up.</param>
        /// <param name="throwException">If true, throws an exception if no implementation or multiple implementations are found; otherwise returns null.</param>
        /// <returns>The type implementing the interface, or null if not found and throwException is false.</returns>
        /// <exception cref="InvalidOperationException">Thrown when discovery has not been run.</exception>
        /// <exception cref="ArgumentNullException">Thrown when interfaceType is null.</exception>
        /// <exception cref="ArgumentException">Thrown when interfaceType is not an interface.</exception>
        /// <exception cref="Exception">Thrown when no implementations or multiple implementations are found and throwException is true.</exception>
        public static Type? GetTypeByInterface(Type interfaceType, bool throwException = true)
        {
            if (!discovered)
                throw new InvalidOperationException($"Discovery has not been run. Call {nameof(Discovery)}.{nameof(Initialize)}() first.");
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.Name} is not an interface");

            List<Type>? typeList;
            if (interfaceType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(interfaceType);
                if (!typeByInterfaceName.TryGetValue(name, out typeList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {interfaceType.Name}");
                    else
                        return null;
                }
            }
            else
            {
                if (!typeByInterface.TryGetValue(interfaceType, out typeList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {interfaceType.Name}");
                    else
                        return null;
                }
            }

            if (typeList.Count > 1)
            {
                if (throwException)
                    throw new Exception($"Multiple classes found for {interfaceType.Name}");
                else
                    return null;
            }

            return typeList[0];
        }
        /// <summary>
        /// Gets a class that inherits from the specified base type.
        /// </summary>
        /// <param name="baseType">The base type to look up.</param>
        /// <param name="throwException">If true, throws an exception if no implementation or multiple implementations are found; otherwise returns null.</param>
        /// <returns>The class inheriting from the base type, or null if not found and throwException is false.</returns>
        /// <exception cref="InvalidOperationException">Thrown when discovery has not been run.</exception>
        /// <exception cref="ArgumentNullException">Thrown when baseType is null.</exception>
        /// <exception cref="Exception">Thrown when no implementations or multiple implementations are found and throwException is true.</exception>
        public static Type? GetClassByBaseType(Type baseType, bool throwException = true)
        {
            if (!discovered)
                throw new InvalidOperationException($"Discovery has not been run. Call {nameof(Discovery)}.{nameof(Initialize)}() first.");
            if (baseType is null)
                throw new ArgumentNullException(nameof(baseType));

            List<Type>? classList;
            if (baseType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(baseType);
                if (!classByBaseTypeName.TryGetValue(name, out classList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {baseType.Name}");
                    else
                        return null;
                }
            }
            else
            {
                if (!classByBaseType.TryGetValue(baseType, out classList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {baseType.Name}");
                    else
                        return null;
                }
            }

            if (classList.Count > 1)
            {
                if (throwException)
                    throw new Exception($"Multiple classes found for {baseType.Name}");
                else
                    return null;
            }

            return classList[0];
        }
        /// <summary>
        /// Gets a class that implements the specified interface.
        /// </summary>
        /// <param name="interfaceType">The interface type to look up.</param>
        /// <param name="throwException">If true, throws an exception if no implementation or multiple implementations are found; otherwise returns null.</param>
        /// <returns>The class implementing the interface, or null if not found and throwException is false.</returns>
        /// <exception cref="InvalidOperationException">Thrown when discovery has not been run.</exception>
        /// <exception cref="ArgumentNullException">Thrown when interfaceType is null.</exception>
        /// <exception cref="ArgumentException">Thrown when interfaceType is not an interface.</exception>
        /// <exception cref="Exception">Thrown when no implementations or multiple implementations are found and throwException is true.</exception>
        public static Type? GetClassByInterface(Type interfaceType, bool throwException = true)
        {
            if (!discovered)
                throw new InvalidOperationException($"Discovery has not been run. Call {nameof(Discovery)}.{nameof(Initialize)}() first.");
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.Name} is not an interface");

            List<Type>? classList;
            if (interfaceType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(interfaceType);
                if (!classByInterfaceName.TryGetValue(name, out classList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {interfaceType.Name}");
                    else
                        return null;
                }
            }
            else
            {
                if (!classByInterface.TryGetValue(interfaceType, out classList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {interfaceType.Name}");
                    else
                        return null;
                }
            }

            if (classList.Count > 1)
            {
                if (throwException)
                    throw new Exception($"Multiple classes found for {interfaceType.Name}");
                else
                    return null;
            }

            return classList[0];
        }

        /// <summary>
        /// Gets all types that implement the specified interface.
        /// </summary>
        /// <param name="interfaceType">The interface type to look up.</param>
        /// <returns>A read-only list of types implementing the interface; empty if none found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when discovery has not been run.</exception>
        /// <exception cref="ArgumentNullException">Thrown when interfaceType is null.</exception>
        /// <exception cref="ArgumentException">Thrown when interfaceType is not an interface.</exception>
        public static IReadOnlyList<Type> GetTypesByInterface(Type interfaceType)
        {
            if (!discovered)
                throw new InvalidOperationException($"Discovery has not been run. Call {nameof(Discovery)}.{nameof(Initialize)}() first.");
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.Name} is not an interface");

            List<Type>? typeList;
            if (interfaceType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(interfaceType);
                if (!typeByInterfaceName.TryGetValue(name, out typeList))
                    return Type.EmptyTypes;
            }
            else
            {
                if (!typeByInterface.TryGetValue(interfaceType, out typeList))
                    return Type.EmptyTypes;
            }

            return typeList;
        }
        /// <summary>
        /// Gets all classes that inherit from the specified base type.
        /// </summary>
        /// <param name="baseType">The base type to look up.</param>
        /// <returns>A read-only list of classes deriving from the base type; empty if none found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when discovery has not been run.</exception>
        /// <exception cref="ArgumentNullException">Thrown when baseType is null.</exception>
        public static IReadOnlyList<Type> GetClassesByBaseType(Type baseType)
        {
            if (!discovered)
                throw new InvalidOperationException($"Discovery has not been run. Call {nameof(Discovery)}.{nameof(Initialize)}() first.");
            if (baseType is null)
                throw new ArgumentNullException(nameof(baseType));

            List<Type>? typeList;
            if (baseType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(baseType);
                if (!classByBaseTypeName.TryGetValue(name, out typeList))
                    return Type.EmptyTypes;
            }
            else
            {
                if (!classByBaseType.TryGetValue(baseType, out typeList))
                    return Type.EmptyTypes;
            }

            return typeList;
        }
        /// <summary>
        /// Gets all classes that implement the specified interface.
        /// </summary>
        /// <param name="interfaceType">The interface type to look up.</param>
        /// <returns>A read-only list of classes implementing the interface; empty if none found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when discovery has not been run.</exception>
        /// <exception cref="ArgumentNullException">Thrown when interfaceType is null.</exception>
        /// <exception cref="ArgumentException">Thrown when interfaceType is not an interface.</exception>
        public static IReadOnlyList<Type> GetClassesByInterface(Type interfaceType)
        {
            if (!discovered)
                throw new InvalidOperationException($"Discovery has not been run. Call {nameof(Discovery)}.{nameof(Initialize)}() first.");
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.Name} is not an interface");

            List<Type>? classList;
            if (interfaceType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(interfaceType);
                if (!classByInterfaceName.TryGetValue(name, out classList))
                    return Type.EmptyTypes;
            }
            else
            {
                if (!classByInterface.TryGetValue(interfaceType, out classList))
                    return Type.EmptyTypes;
            }

            return classList;
        }

        /// <summary>
        /// Defines a class implementation for an interface type.
        /// </summary>
        /// <typeparam name="T">The interface type.</typeparam>
        /// <param name="implementationType">The class type that implements the interface.</param>
        /// <exception cref="InvalidOperationException">Thrown when discovery has not been run.</exception>
        /// <exception cref="ArgumentNullException">Thrown when implementationType is null.</exception>
        /// <exception cref="ArgumentException">Thrown when implementationType is not a non-abstract class or does not implement the interface.</exception>
        public static void DefineClassByInterface<T>(Type implementationType) => DefineClassByInterface(typeof(T), implementationType);
        /// <summary>
        /// Defines a class implementation for an interface type.
        /// </summary>
        /// <param name="interfaceType">The interface type.</param>
        /// <param name="implementationType">The class type that implements the interface.</param>
        /// <exception cref="InvalidOperationException">Thrown when discovery has not been run.</exception>
        /// <exception cref="ArgumentNullException">Thrown when interfaceType or implementationType is null.</exception>
        /// <exception cref="ArgumentException">Thrown when interfaceType is not an interface, implementationType is not a non-abstract class, or implementationType does not implement interfaceType.</exception>
        public static void DefineClassByInterface(Type interfaceType, Type implementationType)
        {
            if (!discovered)
                throw new InvalidOperationException($"Discovery has not been run. Call {nameof(Discovery)}.{nameof(Initialize)}() first.");
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.Name} is not an interface");
            if (implementationType is null)
                throw new ArgumentNullException(nameof(implementationType));
            if (!implementationType.IsClass || implementationType.IsAbstract)
                throw new ArgumentException($"Type {implementationType.Name} is not a non-abstract class");
            if (!TypeAnalyzer.GetTypeDetail(implementationType).Interfaces.Contains(interfaceType))
                throw new ArgumentException($"Type {implementationType.Name} does not implement {interfaceType.Name}");

            _ = classByInterface.AddOrUpdate(interfaceType, (key) => [implementationType], (key, old) => [implementationType]);
        }

        /// <summary>
        /// Gets all types that are decorated with the specified attribute.
        /// </summary>
        /// <param name="attribute">The attribute type to look up.</param>
        /// <returns>A read-only list of types decorated with the attribute; empty if none found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when discovery has not been run.</exception>
        /// <exception cref="ArgumentNullException">Thrown when attribute is null.</exception>
        public static IReadOnlyList<Type> GetTypesFromAttribute(Type attribute)
        {
            if (!discovered)
                throw new InvalidOperationException($"Discovery has not been run. Call {nameof(Discovery)}.{nameof(Initialize)}() first.");
            if (attribute is null)
                throw new ArgumentNullException(nameof(attribute));
            if (!typeByAttribute.TryGetValue(attribute, out var classList))
                return Type.EmptyTypes;
            return classList;
        }

        private static string GetNiceFullName(Type it)
        {
            if (it is null)
                return "null";
            var name = niceFullNames.GetOrAdd(it, static (it) => GenerateNiceName(it, false));
            return name;
        }

        private static string GetNiceFullGenericName(Type it)
        {
            if (it is null)
                return "null";
            var name = niceFullGenericNames.GetOrAdd(it, static (it) => GenerateNiceName(it, true));
            return name;
        }

        private static string GenerateNiceName(Type type, bool generic)
        {
            if (type.IsGenericType && (generic || type.ContainsGenericParameters))
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

                if (type.Namespace is not null)
                    _ = sb.Append(type.Namespace).Append('.');
                _ = sb.Append(name).Append('<');

                for (var j = 0; j < parameters.Length; j++)
                {
                    if (j > 0)
                        _ = sb.Append(',');
                    var parameter = parameters[j];
                    if (parameter.IsGenericParameter || generic)
                        _ = sb.Append('T');
                    else
                        _ = sb.Append(GetNiceFullName(parameter));
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
                if (type.FullName is not null)
                    return type.FullName;
                if (type.Namespace is not null)
                    return $"{type.Namespace}.{type.Name}";
                return type.Name;
            }
        }
    }
}