// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Zerra.Collections;

namespace Zerra.Reflection
{
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

        private static readonly ConcurrentFactoryDictionary<Type, string> niceNames = new();
        private static readonly ConcurrentFactoryDictionary<Type, string> niceFullNames = new();

        private static readonly HashSet<string> discoveredAssemblies = new();
        private static readonly HashSet<Type> discoveredTypes = new();

        private static readonly Queue<Func<Type?>> generationFunctionFromAttributes = new();

        internal static void Discover()
        {
            //Some test solution trigger multiple Config loads so make sure we aren't crossing threads here
            lock (discoveredTypes)
            {
                if (!Config.DiscoveryEnabled)
                    return;

                Config.SetDiscoveryStarted();

                if (Config.AssemblyLoaderEnabled)
                {
                    LoadAssemblies();
                }

                DiscoverAssemblies();

                RunGenerationsFromAttributes();
            }
        }

        private static void LoadAssemblies()
        {
            if (Config.DiscoveryAssemblyNameStartsWiths.Length == 0)
                return;

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
#if NETSTANDARD2_0
                    var assemblyFileName = assemblyFilePath.Split(new char[] { System.IO.Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).Last();
#else
                    var assemblyFileName = assemblyFilePath.Split(System.IO.Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();
#endif
                    if (!Config.DiscoveryAssemblyNameStartsWiths.Any(x => assemblyFileName.StartsWith(x)))
                        continue;

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
                    catch (System.IO.FileNotFoundException)
                    {
                        try
                        {
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
            if (Config.DiscoveryAssemblyNameStartsWiths.Length == 0)
                return;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                    continue;
                if (assembly.FullName is null)
                    continue;
                if (!Config.DiscoveryAssemblyNameStartsWiths.Any(y => assembly.FullName is not null && assembly.FullName.StartsWith(y)))
                    continue;
                if (discoveredAssemblies.Contains(assembly.FullName))
                    continue;
                if (assembly.FullName.StartsWith("System."))
                    continue;
                if (assembly.FullName.StartsWith("Zerra,") || assembly.FullName.StartsWith("Zerra.Repository,"))
                    continue;

                DiscoverAssembly(assembly);
            }
        }
        public static void DiscoverAssembly(Assembly assembly)
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
        public static void DiscoverType(Type typeInAssembly)
        {
            if (!discoveredTypes.Add(typeInAssembly))
                return;

            DiscoverType(typeInAssembly, typeInAssembly.GetInterfaces(), true);
        }
        internal static void DiscoverType(Type typeInAssembly, Type[] interfaceTypes, bool skipDiscoveredCheck)
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
                    if (interfaceType.ContainsGenericParameters)
                    {
                        interfaceTypeName = GetNiceFullName(interfaceType);
                        var typeByInterfaceNameList = typeByInterfaceName.GetOrAdd(interfaceTypeName, static (key) => new());
                        typeByInterfaceNameList.Add(typeInAssembly);
                    }

                    if (!typeInAssembly.IsAbstract && typeInAssembly.IsClass)
                    {
                        var classByInterfaceList = classByInterface.GetOrAdd(interfaceType, static (key) => new());
                        classByInterfaceList.Add(typeInAssembly);

                        if (interfaceType.ContainsGenericParameters)
                        {
                            var classByInterfaceNameList = classByInterfaceName.GetOrAdd(interfaceTypeName!, static (key) => new());
                            classByInterfaceNameList.Add(typeInAssembly);
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

                if (attribute is BaseGenerateAttribute generateAttribute)
                {
                    generationFunctionFromAttributes.Enqueue(() => generateAttribute.Generate(typeInAssembly));
                }
            }
        }

        internal static void RunGenerationsFromAttributes()
        {
#if NETSTANDARD2_0
            while (generationFunctionFromAttributes.Count > 0)
            {
                var func = generationFunctionFromAttributes.Dequeue();
                var newType = func();
                if (newType is not null)
                    DiscoverType(newType);
            }
#else
            while (generationFunctionFromAttributes.TryDequeue(out var func))
            {
                var newType = func();
                if (newType is not null)
                    DiscoverType(newType);
            }
#endif
        }

        internal static void MarkAssemblyAsDiscovered(Assembly assembly)
        {
            if (assembly.FullName is null) throw new ArgumentNullException(nameof(Assembly.FullName));
            _ = discoveredAssemblies.Add(assembly.FullName);
        }

        public static bool HasTypeByInterface(Type interfaceType)
        {
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");

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
        public static bool HasClassByBaseType(Type baseType)
        {
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
        public static bool HasClassByInterface(Type interfaceType)
        {
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");

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
        public static bool HasClassByInterface(Type interfaceType, Type secondaryInterface)
        {
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");
            if (secondaryInterface is null)
                throw new ArgumentNullException(nameof(secondaryInterface));

            List<Type>? classList;
            if (interfaceType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(interfaceType);
                if (!classByInterfaceName.TryGetValue(name, out classList))
                    return false;
            }
            else
            {
                if (!classByInterface.TryGetValue(interfaceType, out classList))
                    return false;
            }

            foreach (var classType in classList)
            {
                if (!interfaceByType.TryGetValue(classType, out var interfaceList))
                    continue;

                if (secondaryInterface is null || interfaceList.Contains(secondaryInterface))
                    return true;
            }

            return false;
        }
        public static unsafe bool HasClassByInterface(Type interfaceType, IReadOnlyList<Type?> secondaryInterfaces, int secondaryInterfaceStartIndex, Type ignoreInterface)
        {
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");
            if (secondaryInterfaces is null)
                throw new ArgumentNullException(nameof(secondaryInterfaces));
            if (secondaryInterfaceStartIndex < 0 || secondaryInterfaceStartIndex > secondaryInterfaces.Count - 1)
                throw new ArgumentOutOfRangeException(nameof(secondaryInterfaceStartIndex));

            List<Type>? classList;
            if (interfaceType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(interfaceType);
                if (!classByInterfaceName.TryGetValue(name, out classList))
                    return false;
            }
            else
            {
                if (!classByInterface.TryGetValue(interfaceType, out classList))
                    return false;
            }

            var levels = stackalloc short[classList.Count];

            for (var j = 0; j < classList.Count; j++)
                levels[j] = -2;

            var firstLevelFound = -1;

            for (var j = 0; j < classList.Count; j++)
            {
                var classType = classList[j];

                if (!interfaceByType.TryGetValue(classType, out var interfaceList))
                {
                    levels[j] = -1;
                    continue;
                }

                for (var i = 0; i < secondaryInterfaces.Count; i++)
                {
                    if (secondaryInterfaces[i] is not null)
                    {
                        var found = false;
                        for (var k = 0; k < interfaceList.Count; k++)
                        {
                            if (interfaceList[k] == secondaryInterfaces[i])
                            {
                                found = true;
                            }
                            if (ignoreInterface is not null && ignoreInterface == interfaceList[k])
                            {
                                found = false;
                                break;
                            }
                        }
                        if (found)
                        {
                            levels[j] = (short)i;
                            if (i >= secondaryInterfaceStartIndex && (i <= firstLevelFound || firstLevelFound == -1))
                                firstLevelFound = i;
                            break;
                        }
                    }
                    else //base provider
                    {
                        var found = false;
                        for (var k = 0; k < interfaceList.Count; k++)
                        {
                            if (ignoreInterface is not null && ignoreInterface == interfaceList[k])
                            {
                                found = true;
                                break;
                            }
                            if (secondaryInterfaces.Contains(interfaceList[k]))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            levels[j] = (short)i;
                            if (i >= secondaryInterfaceStartIndex && (i <= firstLevelFound || firstLevelFound == -1))
                                firstLevelFound = i;
                            break;
                        }
                    }
                }
            }

            var index = -1;
            for (var j = 0; j < classList.Count; j++)
            {
                if (levels[j] == firstLevelFound)
                {
                    if (index == -1)
                    {
                        index = j;
                        break;
                    }
                }
            }

            return index != -1;
        }

        public static Type? GetTypeByInterface(Type interfaceType, bool throwException = true)
        {
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");

            List<Type>? typeList;
            if (interfaceType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(interfaceType);
                if (!typeByInterfaceName.TryGetValue(name, out typeList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {GetNiceName(interfaceType)}");
                    else
                        return null;
                }
            }
            else
            {
                if (!typeByInterface.TryGetValue(interfaceType, out typeList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {GetNiceName(interfaceType)}");
                    else
                        return null;
                }
            }

            if (typeList.Count > 1)
            {
                if (throwException)
                    throw new Exception($"Multiple classes found for {GetNiceName(interfaceType)}");
                else
                    return null;
            }

            return typeList[0];
        }
        public static Type? GetClassByBaseType(Type baseType, bool throwException = true)
        {
            if (baseType is null)
                throw new ArgumentNullException(nameof(baseType));

            List<Type>? classList;
            if (baseType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(baseType);
                if (!classByBaseTypeName.TryGetValue(name, out classList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {GetNiceName(baseType)}");
                    else
                        return null;
                }
            }
            else
            {
                if (!classByBaseType.TryGetValue(baseType, out classList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {GetNiceName(baseType)}");
                    else
                        return null;
                }
            }

            if (classList.Count > 1)
            {
                if (throwException)
                    throw new Exception($"Multiple classes found for {GetNiceName(baseType)}");
                else
                    return null;
            }

            return classList[0];
        }
        public static Type? GetClassByInterface(Type interfaceType, bool throwException = true)
        {
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");

            List<Type>? classList;
            if (interfaceType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(interfaceType);
                if (!classByInterfaceName.TryGetValue(name, out classList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {GetNiceName(interfaceType)}");
                    else
                        return null;
                }
            }
            else
            {
                if (!classByInterface.TryGetValue(interfaceType, out classList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {GetNiceName(interfaceType)}");
                    else
                        return null;
                }
            }

            if (classList.Count > 1)
            {
                if (throwException)
                    throw new Exception($"Multiple classes found for {GetNiceName(interfaceType)}");
                else
                    return null;
            }

            return classList[0];
        }
        public static Type? GetClassByInterface(Type interfaceType, Type secondaryInterface, bool throwException = true)
        {
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");
            if (secondaryInterface is null)
                throw new ArgumentNullException(nameof(secondaryInterface));

            List<Type>? classList;
            if (interfaceType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(interfaceType);
                if (!classByInterfaceName.TryGetValue(name, out classList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {GetNiceName(interfaceType)}");
                    else
                        return null;
                }
            }
            else
            {
                if (!classByInterface.TryGetValue(interfaceType, out classList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {GetNiceName(interfaceType)}");
                    else
                        return null;
                }
            }

            Type? found = null;
            for (var j = 0; j < classList.Count; j++)
            {
                var classType = classList[j];
                if (!interfaceByType.TryGetValue(classType, out var interfaceList))
                    continue;

                if (interfaceList.Contains(secondaryInterface))
                {
                    if (found is null)
                    {
                        found = classType;
                    }
                    else
                    {
                        if (throwException)
                            throw new Exception($"Multiple classes found for {GetNiceName(interfaceType)} with secondary interface type {GetNiceName(secondaryInterface)}");
                        else
                            return null;
                    }
                }
            }

            if (found is null)
            {
                if (throwException)
                    throw new Exception($"No classes found for {GetNiceName(interfaceType)} with secondary interface type {GetNiceName(secondaryInterface)}");
                else
                    return null;
            }

            return found;
        }
        public static unsafe Type? GetClassByInterface(Type interfaceType, IReadOnlyList<Type?> secondaryInterfaces, int secondaryInterfaceStartIndex, Type ignoreInterface, bool throwException = true)
        {
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");
            if (secondaryInterfaces is null)
                throw new ArgumentNullException(nameof(secondaryInterfaces));
            if (secondaryInterfaceStartIndex < 0 || secondaryInterfaceStartIndex > secondaryInterfaces.Count - 1)
                throw new ArgumentOutOfRangeException(nameof(secondaryInterfaceStartIndex));

            List<Type>? classList;
            if (interfaceType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(interfaceType);
                if (!classByInterfaceName.TryGetValue(name, out classList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {GetNiceName(interfaceType)}");
                    else
                        return null;
                }
            }
            else
            {
                if (!classByInterface.TryGetValue(interfaceType, out classList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {GetNiceName(interfaceType)}");
                    else
                        return null;
                }
            }

            var levels = stackalloc short[classList.Count];

            for (var j = 0; j < classList.Count; j++)
                levels[j] = -2;

            var firstLevelFound = -1;

            for (var j = 0; j < classList.Count; j++)
            {
                var classType = classList[j];

                if (!interfaceByType.TryGetValue(classType, out var interfaceList))
                {
                    levels[j] = -1;
                    continue;
                }

                for (var i = 0; i < secondaryInterfaces.Count; i++)
                {
                    if (secondaryInterfaces[i] is not null)
                    {
                        var found = false;
                        for (var k = 0; k < interfaceList.Count; k++)
                        {
                            if (interfaceList[k] == secondaryInterfaces[i])
                            {
                                found = true;
                            }
                            if (ignoreInterface is not null && ignoreInterface == interfaceList[k])
                            {
                                found = false;
                                break;
                            }
                        }
                        if (found)
                        {
                            levels[j] = (short)i;
                            if (i >= secondaryInterfaceStartIndex && (i <= firstLevelFound || firstLevelFound == -1))
                                firstLevelFound = i;
                            break;
                        }
                    }
                    else //base provider
                    {
                        var found = false;
                        for (var k = 0; k < interfaceList.Count; k++)
                        {
                            if (ignoreInterface is not null && ignoreInterface == interfaceList[k])
                            {
                                found = true;
                                break;
                            }
                            if (secondaryInterfaces.Contains(interfaceList[k]))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            levels[j] = (short)i;
                            if (i >= secondaryInterfaceStartIndex && (i <= firstLevelFound || firstLevelFound == -1))
                                firstLevelFound = i;
                            break;
                        }
                    }
                }
            }

            var index = -1;
            for (var j = 0; j < classList.Count; j++)
            {
                if (levels[j] == firstLevelFound)
                {
                    if (index == -1)
                        index = j;
                    else
                        index = -2;
                }
            }

            if (index == -2)
            {
                if (throwException)
                    throw new Exception($"Multiple classes found for {GetNiceName(interfaceType)} with secondary interfaces type {secondaryInterfaces[firstLevelFound]?.GetNiceName()}");
                else
                    return null;
            }
            if (index == -1)
            {
                if (throwException)
                    throw new Exception($"No classes found for {GetNiceName(interfaceType)} with secondary interfaces types {String.Join(", ", secondaryInterfaces.Select(x => x is null ? "null" : GetNiceName(x)))}");
                else
                    return null;
            }

            return classList[index];
        }

        public static IReadOnlyList<Type> GetTypesByInterface(Type interfaceType)
        {
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");

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
        public static IReadOnlyList<Type> GetClassesByBaseType(Type baseType)
        {
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
        public static IReadOnlyList<Type> GetClassesByInterface(Type interfaceType)
        {
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");

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
        public static IReadOnlyList<Type> GetClassesByInterface(Type interfaceType, Type secondaryInterface)
        {
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");
            if (secondaryInterface is null)
                throw new ArgumentNullException(nameof(secondaryInterface));

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

            var list = new List<Type>();
            for (var j = 0; j < classList.Count; j++)
            {
                var classType = classList[j];
                if (!interfaceByType.TryGetValue(classType, out var interfaceList))
                    continue;

                if (secondaryInterface is null || interfaceList.Contains(secondaryInterface))
                {
                    list.Add(classType);
                }
            }

            return list;
        }
        public static unsafe IReadOnlyCollection<Type> GetClassesByInterface(Type interfaceType, IReadOnlyList<Type> secondaryInterfaces, int secondaryInterfaceStartIndex, Type ignoreInterface)
        {
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");
            if (secondaryInterfaces is null)
                throw new ArgumentNullException(nameof(secondaryInterfaces));
            if (secondaryInterfaceStartIndex < 0 || secondaryInterfaceStartIndex > secondaryInterfaces.Count - 1)
                throw new ArgumentOutOfRangeException(nameof(secondaryInterfaceStartIndex));

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

            var levels = stackalloc short[classList.Count];

            for (var j = 0; j < classList.Count; j++)
                levels[j] = -2;

            var firstLevelFound = -1;

            for (var j = 0; j < classList.Count; j++)
            {
                var classType = classList[j];

                if (!interfaceByType.TryGetValue(classType, out var interfaceList))
                {
                    levels[j] = -1;
                    continue;
                }

                for (var i = 0; i < secondaryInterfaces.Count; i++)
                {
                    if (secondaryInterfaces[i] is not null)
                    {
                        var found = false;
                        for (var k = 0; k < interfaceList.Count; k++)
                        {
                            if (interfaceList[k] == secondaryInterfaces[i])
                            {
                                found = true;
                            }
                            if (ignoreInterface is not null && ignoreInterface == interfaceList[k])
                            {
                                found = false;
                                break;
                            }
                        }
                        if (found)
                        {
                            levels[j] = (short)i;
                            if (i >= secondaryInterfaceStartIndex && (i <= firstLevelFound || firstLevelFound == -1))
                                firstLevelFound = i;
                            break;
                        }
                    }
                    else //base provider
                    {
                        var found = false;
                        for (var k = 0; k < interfaceList.Count; k++)
                        {
                            if (ignoreInterface is not null && ignoreInterface == interfaceList[k])
                            {
                                found = true;
                                break;
                            }
                            if (secondaryInterfaces.Contains(interfaceList[k]))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            levels[j] = (short)i;
                            if (i >= secondaryInterfaceStartIndex && (i <= firstLevelFound || firstLevelFound == -1))
                                firstLevelFound = i;
                            break;
                        }
                    }
                }
            }

            var list = new List<Type>();
            for (var j = 0; j < classList.Count; j++)
            {
                if (levels[j] == firstLevelFound)
                    list.Add(classList[j]);
            }

            return list;
        }

        public static void DefineClassByInterface<T>(Type implementationType) => DefineClassByInterface(typeof(T), implementationType);
        public static void DefineClassByInterface(Type interfaceType, Type implementationType)
        {
            if (interfaceType is null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");
            if (implementationType is null)
                throw new ArgumentNullException(nameof(implementationType));
            if (!implementationType.IsClass || implementationType.IsAbstract)
                throw new ArgumentException($"Type {GetNiceName(implementationType)} is not a non-abstract class");
            if (!TypeAnalyzer.GetTypeDetail(implementationType).Interfaces.Contains(interfaceType))
                throw new ArgumentException($"Type {GetNiceName(implementationType)} does not implement {GetNiceName(interfaceType)}");

            _ = classByInterface.AddOrUpdate(interfaceType, (key) => [implementationType], (key, old) => [implementationType]);
        }

        public static IEnumerable<Type> GetTypesFromAttribute(Type attribute)
        {
            if (attribute is null)
                throw new ArgumentNullException(nameof(attribute));
            if (!typeByAttribute.TryGetValue(attribute, out var classList))
                return Type.EmptyTypes;
            return classList.ToArray();
        }

        public static Type GetTypeFromName(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (!typeByName.TryGetValue(name, out var matches))
            {
                var type = Type.GetType(name);
                type ??= ParseType(name);
                matches = typeByName.GetOrAdd(name, static (key) => new());
                lock (matches)
                {
                    //only add null if it's new and will be the only item
                    if (matches.Count == 0 || (type is not null && !matches.Contains(type)))
                        matches.Add(type);
                }
                if (type is null)
                    throw new Exception($"Could not find type {name}. Remember discovery finds assemblies with the same first namespace segment. Additional assemblies must be added with Config class.");
                return type;
            }
            else if (matches.Count == 1)
            {
                var type = matches[0];
                if (type is null)
                    throw new Exception($"Could not find type {name}. Remember discovery finds assemblies with the same first namespace segment. Additional assemblies must be added with Config class.");
                return type;
            }
            else
            {
                throw new Exception($"More than one type matches {name} - {String.Join(", ", matches.Where(x => x is not null).Select(x => x!.AssemblyQualifiedName).ToArray())}");
            }
        }
        public static bool TryGetTypeFromName(string name,
#if NET5_0_OR_GREATER
            [MaybeNullWhen(false)]
#endif
        out Type type)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (!typeByName.TryGetValue(name, out var matches))
            {
                type = Type.GetType(name);
                type ??= ParseType(name);
                matches = typeByName.GetOrAdd(name, static (key) => new());
                lock (matches)
                {
                    if ((matches.Count == 0 || type is not null) && !matches.Contains(type))
                        matches.Add(type);
                }
                return type is not null;
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
                var type = Type.GetType(name);

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

                var name = span.Slice(0, i).ToString();

                //Have to inspect because inner generics or partially constructed generics won't work
                var parameters = type.GetGenericArguments();

                var sb = new StringBuilder();

                if (includeNamespace && type.Namespace is not null)
                    sb.Append(type.Namespace).Append('.');
                sb.Append(name).Append('<');

                for (var j = 0; j < parameters.Length; j++)
                {
                    if (j > 0)
                        sb.Append(',');
                    var parameter = parameters[j];
                    if (parameter.IsGenericParameter)
                        sb.Append('T');
                    else if (includeNamespace)
                        sb.Append(GetNiceFullName(parameter));
                    else
                        sb.Append(GetNiceName(parameter));
                }

                sb.Append('>');
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
                            sb.Append(chars.Slice(start, i + 1 - start).ToString());
                        break;
                    case ',':
                        if (depth == 1)
                            sb.Append("T,");
                        break;
                    case '>':
                        depth--;
                        if (depth == 0)
                        {
                            sb.Append('T');
                            start = i;
                        }
                        break;
                }
            }

            sb.Append(chars.Slice(start, chars.Length - start).ToString());

            return sb.ToString();
        }
    }
}