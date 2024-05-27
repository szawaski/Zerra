// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using Zerra.Collections;

namespace Zerra.Reflection
{
    public static class Discovery
    {
        private static readonly ConcurrentDictionary<Type, List<Type>> typeByInterface;
        private static readonly ConcurrentDictionary<string, List<Type>> typeByInterfaceName;
        private static readonly ConcurrentDictionary<Type, List<Type>> classByInterface;
        private static readonly ConcurrentDictionary<string, List<Type>> classByInterfaceName;
        private static readonly ConcurrentDictionary<Type, List<Type>> classByBase;
        private static readonly ConcurrentDictionary<string, List<Type>> classByBaseName;
        private static readonly ConcurrentDictionary<Type, List<Type>> typeByAttribute;

        private static readonly ConcurrentDictionary<Type, List<Type>> interfaceByType;

        private static readonly ConcurrentDictionary<string, ConcurrentReadWriteList<Type>> typeByName;

        private static readonly ConcurrentFactoryDictionary<Type, string> niceNames = new();
        private static readonly ConcurrentFactoryDictionary<Type, string> niceFullNames = new();

        private static readonly HashSet<string> discoveredAssemblies;

        static Discovery()
        {
            Config.SetDiscoveryStarted();

            interfaceByType = new();
            typeByInterfaceName = new();
            classByInterface = new();
            classByInterfaceName = new();
            classByBase = new();
            classByBaseName = new();

            typeByAttribute = new();
            typeByInterface = new();

            typeByName = new();

            discoveredAssemblies = new();

            LoadAssemblies();
            Discover();
            Generate();
        }

        private static void LoadAssemblies()
        {
            var loadedAssemblies = new HashSet<string>();
            var currentAsssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic);
            foreach (var currentAssembly in currentAsssemblies)
            {
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
                    if (Config.DiscoveryAssemblyNameStartsWiths.Length > 0 && !Config.DiscoveryAssemblyNameStartsWiths.Any(x => assemblyFileName.StartsWith(x)))
                        continue;

                    var assemblyName = AssemblyName.GetAssemblyName(assemblyFilePath);

                    if (loadedAssemblies.Contains(assemblyName.FullName))
                        continue;

                    if (assemblyName.Name != null && assemblyName.Name.EndsWith(".Web.Views"))
                        continue;

                    try
                    {
                        var assembly = Assembly.Load(assemblyName);
                        if (!String.IsNullOrEmpty(assembly.FullName))
                            _ = loadedAssemblies.Add(assembly.FullName);
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        try
                        {
                            var assembly = Assembly.LoadFrom(assemblyFileName);
                            if (!String.IsNullOrEmpty(assembly.FullName))
                                _ = loadedAssemblies.Add(assembly.FullName);
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
        private static void Discover()
        {
            Assembly[] assemblies;
            if (Config.DiscoveryAssemblyNameStartsWiths.Length > 0)
                assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic && Config.DiscoveryAssemblyNameStartsWiths.Any(y => x.FullName != null && x.FullName.StartsWith(y))).ToArray();
            else
                assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic).ToArray();

            foreach (var assembly in assemblies)
            {
                if (assembly.FullName == null || discoveredAssemblies.Contains(assembly.FullName))
                    continue;

                discoveredAssemblies.Add(assembly.FullName);

                Type[]? typesInAssembly = null;
                try
                {
                    typesInAssembly = assembly.GetTypes();
                }
                catch { }
                if (typesInAssembly == null)
                    continue;

                foreach (var typeInAssembly in typesInAssembly.Where(x => !String.IsNullOrWhiteSpace(x.FullName)))
                {
                    DiscoverType(typeInAssembly);
                }
            }
        }
        private static void DiscoverType(Type typeInAssembly)
        {
            var debug = typeInAssembly.Name;
            var typeList1 = typeByName.GetOrAdd(typeInAssembly.Name, (key) => { return new(); });
            typeList1.Add(typeInAssembly);
            if (typeInAssembly.FullName != null && typeInAssembly.Name != typeInAssembly.FullName)
            {
                var typeList2 = typeByName.GetOrAdd(typeInAssembly.FullName, (key) => { return new(); });
                typeList2.Add(typeInAssembly);
            }

            var interfaceTypes = typeInAssembly.GetInterfaces();
            if (interfaceTypes.Length > 0)
            {
                var interfaceByTypeList = interfaceByType.GetOrAdd(typeInAssembly, (key) => { return new(); });

                foreach (var interfaceType in interfaceTypes)
                {
                    interfaceByTypeList.Add(interfaceType);

                    var typeByInterfaceList = typeByInterface.GetOrAdd(interfaceType, (key) => { return new(); });
                    typeByInterfaceList.Add(typeInAssembly);

                    string? interfaceTypeName = null;
                    if (interfaceType.ContainsGenericParameters)
                    {
                        interfaceTypeName = GetNiceFullName(interfaceType);
                        var typeByInterfaceNameList = typeByInterfaceName.GetOrAdd(interfaceTypeName, (key) => { return new(); });
                        typeByInterfaceNameList.Add(typeInAssembly);
                    }

                    if (!typeInAssembly.IsAbstract && typeInAssembly.IsClass)
                    {
                        var classByInterfaceList = classByInterface.GetOrAdd(interfaceType, (key) => { return new(); });
                        classByInterfaceList.Add(typeInAssembly);

                        if (interfaceType.ContainsGenericParameters)
                        {
                            var classByInterfaceNameList = classByInterfaceName.GetOrAdd(interfaceTypeName!, (key) => { return new(); });
                            classByInterfaceNameList.Add(typeInAssembly);
                        }
                    }
                }
            }

            Type? baseType = typeInAssembly.BaseType;
            while (baseType != null)
            {
                var classByBaseList = classByBase.GetOrAdd(baseType, (key) => { return new(); });
                classByBaseList.Add(typeInAssembly);

                if (baseType.ContainsGenericParameters)
                {
                    var baseTypeName = GetNiceFullName(baseType);
                    var classByBaseNameList = classByBaseName.GetOrAdd(baseTypeName, (key) => { return new(); });
                    classByBaseNameList.Add(typeInAssembly);
                }

                baseType = baseType.BaseType;
            }

            var attributeTypes = typeInAssembly.GetCustomAttributes().Select(x => x.GetType()).Distinct().ToArray();
            foreach (var attributeType in attributeTypes)
            {
                var thisAttributeType = attributeType;
                while (thisAttributeType != null && thisAttributeType != typeof(Attribute))
                {
                    var list = typeByAttribute.GetOrAdd(thisAttributeType, (key) => { return new(); });
                    list.Add(typeInAssembly);
                    thisAttributeType = thisAttributeType.BaseType;
                }
            }
        }
        private static void Generate()
        {
            var generationTypes = GetTypesFromAttribute(typeof(BaseGenerateAttribute));
            foreach (var generationType in generationTypes.Distinct())
            {
                var typeDetail = TypeAnalyzer.GetTypeDetail(generationType);
                foreach (var attribute in typeDetail.Attributes)
                {
                    if (attribute is BaseGenerateAttribute generateAttribute)
                    {
                        var newType = generateAttribute.Generate(generationType);
                        DiscoverType(newType);
                    }
                }
            }
        }

        public static bool HasTypeByInterface(Type interfaceType)
        {
            if (interfaceType == null)
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
        public static bool HasClassByBase(Type baseType)
        {
            if (baseType == null)
                throw new ArgumentNullException(nameof(baseType));

            if (baseType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(baseType);
                return classByBaseName.ContainsKey(name);
            }
            else
            {
                return classByBase.ContainsKey(baseType);
            }
        }
        public static bool HasClassByInterface(Type interfaceType)
        {
            if (interfaceType == null)
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
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");
            if (secondaryInterface == null)
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

                if (secondaryInterface == null || interfaceList.Contains(secondaryInterface))
                    return true;
            }

            return false;
        }
        public static unsafe bool HasClassByInterface(Type interfaceType, IReadOnlyList<Type?> secondaryInterfaces, int secondaryInterfaceStartIndex, Type ignoreInterface)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");
            if (secondaryInterfaces == null)
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
                    if (secondaryInterfaces[i] != null)
                    {
                        var found = false;
                        for (var k = 0; k < interfaceList.Count; k++)
                        {
                            if (interfaceList[k] == secondaryInterfaces[i])
                            {
                                found = true;
                            }
                            if (ignoreInterface != null && ignoreInterface == interfaceList[k])
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
                            if (ignoreInterface != null && ignoreInterface == interfaceList[k])
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
            if (interfaceType == null)
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
        public static Type? GetClassByBase(Type baseType, bool throwException = true)
        {
            if (baseType == null)
                throw new ArgumentNullException(nameof(baseType));

            List<Type>? classList;
            if (baseType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(baseType);
                if (!classByBaseName.TryGetValue(name, out classList))
                {
                    if (throwException)
                        throw new Exception($"No implementations found for {GetNiceName(baseType)}");
                    else
                        return null;
                }
            }
            else
            {
                if (!classByBase.TryGetValue(baseType, out classList))
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
            if (interfaceType == null)
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
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");
            if (secondaryInterface == null)
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
                    if (found == null)
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

            if (found == null)
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
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");
            if (secondaryInterfaces == null)
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
                    if (secondaryInterfaces[i] != null)
                    {
                        var found = false;
                        for (var k = 0; k < interfaceList.Count; k++)
                        {
                            if (interfaceList[k] == secondaryInterfaces[i])
                            {
                                found = true;
                            }
                            if (ignoreInterface != null && ignoreInterface == interfaceList[k])
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
                            if (ignoreInterface != null && ignoreInterface == interfaceList[k])
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
                    throw new Exception($"No classes found for {GetNiceName(interfaceType)} with secondary interfaces types {String.Join(", ", secondaryInterfaces.Select(x => x == null ? "null" : GetNiceName(x)))}");
                else
                    return null;
            }

            return classList[index];
        }

        public static IReadOnlyList<Type> GetTypesByInterface(Type interfaceType)
        {
            if (interfaceType == null)
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
        public static IReadOnlyList<Type> GetClassesByBase(Type baseType)
        {
            if (baseType == null)
                throw new ArgumentNullException(nameof(baseType));

            List<Type>? typeList;
            if (baseType.ContainsGenericParameters)
            {
                var name = GetNiceFullName(baseType);
                if (!classByBaseName.TryGetValue(name, out typeList))
                    return Type.EmptyTypes;
            }
            else
            {
                if (!classByBase.TryGetValue(baseType, out typeList))
                    return Type.EmptyTypes;
            }

            return typeList;
        }
        public static IReadOnlyList<Type> GetClassesByInterface(Type interfaceType)
        {
            if (interfaceType == null)
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
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");
            if (secondaryInterface == null)
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

                if (secondaryInterface == null || interfaceList.Contains(secondaryInterface))
                {
                    list.Add(classType);
                }
            }

            return list;
        }
        public static unsafe IReadOnlyCollection<Type> GetClassesByInterface(Type interfaceType, IReadOnlyList<Type> secondaryInterfaces, int secondaryInterfaceStartIndex, Type ignoreInterface)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");
            if (secondaryInterfaces == null)
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
                    if (secondaryInterfaces[i] != null)
                    {
                        var found = false;
                        for (var k = 0; k < interfaceList.Count; k++)
                        {
                            if (interfaceList[k] == secondaryInterfaces[i])
                            {
                                found = true;
                            }
                            if (ignoreInterface != null && ignoreInterface == interfaceList[k])
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
                            if (ignoreInterface != null && ignoreInterface == interfaceList[k])
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
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {GetNiceName(interfaceType)} is not an interface");
            if (implementationType == null)
                throw new ArgumentNullException(nameof(implementationType));
            if (!implementationType.IsClass || implementationType.IsAbstract)
                throw new ArgumentException($"Type {GetNiceName(implementationType)} is not a non-abstract class");
            if (!TypeAnalyzer.GetTypeDetail(implementationType).Interfaces.Contains(interfaceType))
                throw new ArgumentException($"Type {GetNiceName(implementationType)} does not implement {GetNiceName(interfaceType)}");

            _ = classByInterface.AddOrUpdate(interfaceType, (key) => { return new() { implementationType }; }, (key, old) => { return new() { implementationType }; });
        }

        public static IEnumerable<Type> GetTypesFromAttribute(Type attribute)
        {
            if (attribute == null)
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
                var type = ParseType(name);
                matches = typeByName.GetOrAdd(name, (key) => { return new ConcurrentReadWriteList<Type>(); });
                lock (matches)
                {
                    if (!matches.Contains(type))
                    {
                        matches.Add(type);
                    }
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
                throw new Exception($"More than one type matches {name} - {String.Join(", ", matches.Select(x => x.AssemblyQualifiedName).ToArray())}");
            }
        }

        private static unsafe Type ParseType(string name)
        {
            var index = 0;
            var chars = name.AsSpan();
            string? currentName = null;

            var current = stackalloc char[128];
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
                                currentName = new string(current, 0, i);
                                i = 0;
                            }
                            else if (openGeneric)
                            {
                                openGenericType = true;
                            }
                            else
                            {
                                openArray = true;

                                if (currentName == null)
                                {
                                    if (i == 0)
                                        throw new Exception($"{nameof(ParseType)} Unexpected '{c}' at position {index - 1}");
                                    currentName = new string(current, 0, i);
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
                                var genericArgumentName = new string(current, 0, 1);
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

            if (currentName == null)
            {
                currentName = new string(current, 0, i);
            }

            var type = GetTypeFromNameWithoutParse(currentName);
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

        private static Type GetTypeFromNameWithoutParse(string name)
        {
            if (!typeByName.TryGetValue(name, out var matches))
            {
                var type = Type.GetType(name);
                if (type == null)
                {
                    throw new Exception($"Could not find type {name}.  Remember discovery finds assemblies with the same first namespace segment.  Additional assemblies must be added with Config class.");
                }

                matches = typeByName.GetOrAdd(name, (key) => { return new ConcurrentReadWriteList<Type>(); });
                lock (matches)
                {
                    if (!matches.Contains(type))
                    {
                        matches.Add(type);
                    }
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
                throw new Exception($"More than one type matches {name} - {String.Join(", ", matches.Select(x => x.AssemblyQualifiedName).ToArray())}");
            }
        }

        public static string GetNiceName(Type it)
        {
            if (it == null)
                return "null";
            var niceName = niceNames.GetOrAdd(it, (it) =>
            {
                return GenerateNiceName(it, false);
            });
            return niceName;
        }
        public static string GetNiceFullName(Type it)
        {
            if (it == null)
                return "null";
            var niceFullName = niceFullNames.GetOrAdd(it, (it) =>
            {
                return GenerateNiceName(it, true);
            });
            return niceFullName;
        }

        //framework dependent if property exists
        private static Func<object, object?>? typeGetIsZSArrayGetter;
        private static bool loadedTypeGetIsZSArrayGetter = false;
        private static Func<object, object?>? GetTypeGetIsZSArrayGetter()
        {
            if (!loadedTypeGetIsZSArrayGetter)
            {
                lock (niceFullNames)
                {
                    if (!loadedTypeGetIsZSArrayGetter)
                    {
                        if (TypeAnalyzer.GetTypeDetail(typeof(Type)).TryGetMember("IsSZArray", out var member))
                            typeGetIsZSArrayGetter = member.GetterBoxed;
                        loadedTypeGetIsZSArrayGetter = true;
                    }
                }
            }
            return typeGetIsZSArrayGetter;
        }

        private static string GenerateNiceName(Type type, bool ns)
        {
            var typeDetails = TypeAnalyzer.GetTypeDetail(type);

            if (type.IsArray)
            {
                var sb = new StringBuilder();
                if (ns && !String.IsNullOrWhiteSpace(type.Namespace))
                    _ = sb.Append(type.Namespace).Append('.');

                var rank = type.GetArrayRank();
                var elementType = typeDetails.InnerTypes[0];
                var elementTypeName = GetNiceName(elementType);
                _ = sb.Append(elementTypeName);
                _ = sb.Append('[');
                var getter = GetTypeGetIsZSArrayGetter();

                var szArray = getter != null && (bool)getter(type)!;
                if (!szArray)
                {
                    if (rank == 1)
                    {
                        _ = sb.Append('*');
                    }
                    else if (rank > 1)
                    {
                        for (var i = 0; i < rank - 1; i++)
                            _ = sb.Append(',');
                    }
                }
                _ = sb.Append(']');

                return sb.ToString();
            }

            if (type.IsGenericType)
            {
                var sb = new StringBuilder();
                if (ns && !String.IsNullOrWhiteSpace(type.Namespace))
                    _ = sb.Append(type.Namespace).Append('.');

                _ = sb.Append(type.Name.Split('`')[0]);

                var genericTypes = typeDetails.InnerTypes;
                _ = sb.Append('<');
                var first = true;
                foreach (var genericType in genericTypes)
                {
                    if (!first)
                        _ = sb.Append(',');
                    first = false;
                    if (genericType.ContainsGenericParameters && genericType.IsGenericType)
                    {
                        if (ns)
                            _ = sb.Append(GetNiceFullName(genericType));
                        else
                            _ = sb.Append(GetNiceName(genericType));
                    }
                    else
                    {
                        _ = sb.Append('T');
                    }
                }
                _ = sb.Append('>');

                return sb.ToString();
            }

            if (ns && !String.IsNullOrWhiteSpace(type.Namespace))
            {
                return $"{type.Namespace}.{type.Name}";
            }

            return type.Name;
        }
    }
}