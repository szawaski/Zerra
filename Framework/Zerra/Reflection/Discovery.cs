// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Zerra.Collections;

namespace Zerra.Reflection
{
    public static class Discovery
    {
        private static readonly ConcurrentDictionary<Type, List<Type>> classByInterface;
        private static readonly ConcurrentDictionary<Type, List<Type>> typeByInterface;
        private static readonly ConcurrentDictionary<Type, List<Type>> interfaceByType;
        private static readonly ConcurrentDictionary<Type, List<Type>> typeByAttribute;
        private static readonly ConcurrentDictionary<string, ConcurrentReadWriteList<Type>> typeByName;
        private static readonly List<string> discoveredAssemblies;

        static Discovery()
        {
            Config.SetDiscoveryStarted();

            classByInterface = new();
            typeByInterface = new();
            interfaceByType = new();
            typeByAttribute = new();
            typeByName = new();
            discoveredAssemblies = new();

            LoadAssemblies();
            Discover();
            Generate();
        }

        private static readonly string[] pathSplits = new string[] { "\\", "/" };
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
                    var assemblyFileName = assemblyFilePath.Split(pathSplits, StringSplitOptions.RemoveEmptyEntries).Last();
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

        private static void Generate()
        {
            var generationTypes = GetTypesFromAttribute(typeof(BaseGenerateAttribute));
            foreach (var generationType in generationTypes)
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
                var interfaceList = interfaceByType.GetOrAdd(typeInAssembly, (key) => { return new(); });

                foreach (var interfaceType in interfaceTypes)
                {
                    if (interfaceType.FullName == null)
                        continue;

                    interfaceList.Add(interfaceType);

                    var typeList = typeByInterface.GetOrAdd(interfaceType, (key) => { return new(); });
                    typeList.Add(typeInAssembly);

                    if (!typeInAssembly.IsAbstract && typeInAssembly.IsClass)
                    {
                        var classList = classByInterface.GetOrAdd(interfaceType, (key) => { return new(); });
                        classList.Add(typeInAssembly);
                    }
                }
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

        public static bool HasImplementationType(Type interfaceType)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            return typeByInterface.ContainsKey(interfaceType);
        }
        public static bool HasImplementationClass(Type interfaceType)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            return classByInterface.ContainsKey(interfaceType);
        }
        public static bool HasImplementationClass(Type interfaceType, Type secondaryInterface)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");
            if (secondaryInterface == null)
                throw new ArgumentNullException(nameof(secondaryInterface));

            if (!classByInterface.TryGetValue(interfaceType, out var classList))
                return false;

            foreach (var classType in classList)
            {
                if (!interfaceByType.TryGetValue(classType, out var interfaceList))
                    continue;

                if (secondaryInterface == null || interfaceList.Contains(secondaryInterface))
                    return true;
            }

            return false;
        }
        public static unsafe bool HasImplementationClass(Type interfaceType, IReadOnlyList<Type> secondaryInterfaces, int secondaryInterfaceStartIndex, Type ignoreInterface)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");
            if (secondaryInterfaces == null)
                throw new ArgumentNullException(nameof(secondaryInterfaces));
            if (secondaryInterfaceStartIndex < 0 || secondaryInterfaceStartIndex > secondaryInterfaces.Count - 1)
                throw new ArgumentOutOfRangeException(nameof(secondaryInterfaceStartIndex));

            if (!classByInterface.TryGetValue(interfaceType, out var classList))
                return false;

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

        public static Type? GetImplementationType(Type interfaceType, bool throwException = true)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            if (!typeByInterface.TryGetValue(interfaceType, out var typeList))
            {
                if (throwException)
                    throw new Exception($"No implementations found for {interfaceType.GetNiceName()}");
                else
                    return null;
            }

            if (typeList.Count > 1)
            {
                if (throwException)
                    throw new Exception($"Multiple classes found for {interfaceType.GetNiceName()}");
                else
                    return null;
            }

            return typeList[0];
        }
        public static Type? GetImplementationClass(Type interfaceType, bool throwException = true)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            if (!classByInterface.TryGetValue(interfaceType, out var classList))
            {
                if (throwException)
                    throw new Exception($"No implementations found for {interfaceType.GetNiceName()}");
                else
                    return null;
            }

            if (classList.Count > 1)
            {
                if (throwException)
                    throw new Exception($"Multiple classes found for {interfaceType.GetNiceName()}");
                else
                    return null;
            }

            return classList[0];
        }
        public static Type? GetImplementationClass(Type interfaceType, Type secondaryInterface, bool throwException = true)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");
            if (secondaryInterface == null)
                throw new ArgumentNullException(nameof(secondaryInterface));

            if (!classByInterface.TryGetValue(interfaceType, out var classList))
            {
                if (throwException)
                    throw new Exception($"No implementations found for {interfaceType.GetNiceName()}");
                else
                    return null;
            }

            Type? found = null;
            for (var j = 0; j < classList.Count; j++)
            {
                var classType = classList[j];
                if (!interfaceByType.TryGetValue(classType, out var interfaceList))
                    continue;

                if (secondaryInterface == null || interfaceList.Contains(secondaryInterface))
                {
                    if (found == null)
                    {
                        found = classType;
                    }
                    else
                    {
                        if (throwException)
                            throw new Exception($"Multiple classes found for {interfaceType.GetNiceName()} with secondary interface type {secondaryInterface.GetNiceName()}");
                        else
                            return null;
                    }
                }
            }

            if (found == null)
            {
                if (throwException)
                    throw new Exception($"No classes found for {interfaceType.GetNiceName()} with secondary interface type {secondaryInterface.GetNiceName()}");
                else
                    return null;
            }

            return found;
        }
        public static unsafe Type? GetImplementationClass(Type interfaceType, IReadOnlyList<Type> secondaryInterfaces, int secondaryInterfaceStartIndex, Type ignoreInterface, bool throwException = true)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");
            if (secondaryInterfaces == null)
                throw new ArgumentNullException(nameof(secondaryInterfaces));
            if (secondaryInterfaceStartIndex < 0 || secondaryInterfaceStartIndex > secondaryInterfaces.Count - 1)
                throw new ArgumentOutOfRangeException(nameof(secondaryInterfaceStartIndex));

            if (!classByInterface.TryGetValue(interfaceType, out var classList))
            {
                if (throwException)
                    throw new Exception($"No implementations found for {interfaceType.GetNiceName()}");
                else
                    return null;
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
                    throw new Exception($"Multiple classes found for {interfaceType.GetNiceName()} with secondary interfaces type {secondaryInterfaces[firstLevelFound].GetNiceName()}");
                else
                    return null;
            }
            if (index == -1)
            {
                if (throwException)
                    throw new Exception($"No classes found for {interfaceType.GetNiceName()} with secondary interfaces types {String.Join(", ", secondaryInterfaces.Select(x => x.GetNiceName()))}");
                else
                    return null;
            }

            return classList[index];
        }

        public static IReadOnlyCollection<Type> GetImplementationTypes(Type interfaceType)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            if (!typeByInterface.TryGetValue(interfaceType, out var typeList))
                return Type.EmptyTypes;

            return typeList;
        }
        public static IReadOnlyCollection<Type> GetImplementationClasses(Type interfaceType)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            if (!classByInterface.TryGetValue(interfaceType, out var classList))
                return Type.EmptyTypes;

            return classList;
        }
        public static IReadOnlyCollection<Type> GetImplementationClasses(Type interfaceType, Type secondaryInterface)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");
            if (secondaryInterface == null)
                throw new ArgumentNullException(nameof(secondaryInterface));

            if (!classByInterface.TryGetValue(interfaceType, out var classList))
                return Type.EmptyTypes;

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
        public static unsafe IReadOnlyCollection<Type> GetImplementationClasses(Type interfaceType, IReadOnlyList<Type> secondaryInterfaces, int secondaryInterfaceStartIndex, Type ignoreInterface)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");
            if (secondaryInterfaces == null)
                throw new ArgumentNullException(nameof(secondaryInterfaces));
            if (secondaryInterfaceStartIndex < 0 || secondaryInterfaceStartIndex > secondaryInterfaces.Count - 1)
                throw new ArgumentOutOfRangeException(nameof(secondaryInterfaceStartIndex));

            if (!classByInterface.TryGetValue(interfaceType, out var classList))
                return Array.Empty<Type>();

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

        public static void DefineImplementationClass<T>(Type implementationType) => DefineImplementationClass(typeof(T), implementationType);
        public static void DefineImplementationClass(Type interfaceType, Type implementationType)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");
            if (implementationType == null)
                throw new ArgumentNullException(nameof(implementationType));
            if (!implementationType.IsClass || implementationType.IsAbstract)
                throw new ArgumentException($"Type {implementationType.GetNiceName()} is not a non-abstract class");
            if (!TypeAnalyzer.GetTypeDetail(implementationType).Interfaces.Contains(interfaceType))
                throw new ArgumentException($"Type {implementationType.GetNiceName()} does not implement {interfaceType.GetNiceName()}");

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

        private static Type ParseType(string name)
        {
            var index = 0;
            var chars = name.AsSpan();
            string currentName = null;

            var current = new List<char>();
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
                var c = chars[index];
                index++;

                switch (c)
                {
                    case '`':
                        {
                            if (openGenericType)
                            {
                                current.Add(c);
                            }
                            else if (openArray || (openGeneric && !openGenericType))
                            {
                                throw new Exception($"{nameof(ParseType)} Unexpected '{c}' at position {index - 1}");
                            }
                            else
                            {
                                expectingGenericOpen = true;
                                current.Add(c);
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
                                current.Add(c);
                                openGenericTypeSubBrackets++;
                            }
                            else if (expectingGenericOpen)
                            {
                                expectingGenericOpen = false;
                                openGeneric = true;

                                if (current.Count == 0)
                                    throw new Exception($"{nameof(ParseType)} Unexpected '{c}' at position {index - 1}");
                                currentName = new string(current.ToArray());
                                current.Clear();
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
                                    if (current.Count == 0)
                                        throw new Exception($"{nameof(ParseType)} Unexpected '{c}' at position {index - 1}");
                                    currentName = new string(current.ToArray());
                                    current.Clear();
                                }
                            }

                            break;
                        }
                    case ',':
                        {

                            if (!openGeneric || openGenericType || (openArray && !openArrayOneDimension))
                            {
                                current.Add(c);
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
                                current.Add(c);
                            }
                            else if (openArray)
                            {
                                if (current.Count > 0)
                                    throw new Exception($"{nameof(ParseType)} Unexpected {c}");
                                openArrayOneDimension = true;
                                current.Add(c);
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
                                current.Add(c);
                                openGenericTypeSubBrackets--;
                            }
                            else if (openGenericType)
                            {
                                openGenericType = false;
                                var genericArgumentName = new string(current.ToArray());
                                current.Clear();
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
                                if (current.Count > 0)
                                {
                                    if (openArrayOneDimension)
                                        arrayDimensions.Add(1);
                                    else
                                        arrayDimensions.Add(current.Count + 1);
                                }
                                else
                                {
                                    arrayDimensions.Add(0);
                                }
                                openArrayOneDimension = false;
                                current.Clear();
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

                            current.Add(c);
                            break;
                        }
                }

                if (done)
                    break;
            }

            if (currentName == null)
            {
                currentName = new string(current.ToArray());
                current.Clear();
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
    }
}