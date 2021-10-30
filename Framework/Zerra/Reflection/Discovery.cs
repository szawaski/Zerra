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
        private static readonly string[] excludeMappingTypePrefixes = new string[] { "System.", "Microsoft.", "netstandard" };

        private static readonly ArrayPool<short> flagPool = ArrayPool<short>.Shared;
        private static readonly ConcurrentDictionary<Type, List<Type>> classByInterface;
        private static readonly ConcurrentDictionary<Type, List<Type>> interfaceByType;
        private static readonly ConcurrentDictionary<Type, List<Type>> typeByAttribute;
        private static readonly ConcurrentDictionary<string, ConcurrentList<Type>> typeByName;
        private static readonly HashSet<string> initializedAssemblies;

        static Discovery()
        {
            classByInterface = new ConcurrentDictionary<Type, List<Type>>();
            interfaceByType = new ConcurrentDictionary<Type, List<Type>>();
            typeByAttribute = new ConcurrentDictionary<Type, List<Type>>();
            typeByName = new ConcurrentDictionary<string, ConcurrentList<Type>>();
            initializedAssemblies = new HashSet<string>();

            ForceLoadAssemblies();
            Discover();
            Generate();
        }

        private static void ForceLoadAssemblies()
        {
            var loadedAssemblies = new HashSet<string>();
            var currentAsssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic);
            foreach (var currentAssembly in currentAsssemblies)
                loadedAssemblies.Add(currentAssembly.FullName);

            var assemblyPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var assemblyFileNames = System.IO.Directory.GetFiles(assemblyPath, "*.dll");

            foreach (string assemblyFileName in assemblyFileNames)
            {
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(assemblyFileName);
                    if (!loadedAssemblies.Contains(assemblyName.FullName))
                    {
                        if (assemblyName.Name.EndsWith(".Web.Views"))
                            continue;

                        try
                        {
                            var assembly = Assembly.Load(assemblyName);
                            loadedAssemblies.Add(assembly.FullName);
                        }
                        catch (System.IO.FileNotFoundException)
                        {
                            try
                            {
                                var assembly = Assembly.LoadFrom(assemblyFileName);
                                loadedAssemblies.Add(assembly.FullName);
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
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic).ToList();

            foreach (Assembly assembly in assemblies.Where(x => !initializedAssemblies.Contains(x.FullName)))
            {
                initializedAssemblies.Add(assembly.FullName);

                Type[] typesInAssembly = null;
                try
                {
                    typesInAssembly = assembly.GetTypes();
                }
                catch { }

                if (typesInAssembly != null)
                {
                    foreach (var typeInAssembly in typesInAssembly.Where(x => !String.IsNullOrWhiteSpace(x.FullName)))
                    {
                        DiscoverType(typeInAssembly);
                    }
                }
            }
        }
        private static void DiscoverType(Type typeInAssembly)
        {
            var typeList1 = typeByName.GetOrAdd(typeInAssembly.Name, (key) => { return new ConcurrentList<Type>(); });
            typeList1.Add(typeInAssembly);
            if (typeInAssembly.Name != typeInAssembly.FullName)
            {
                var typeList2 = typeByName.GetOrAdd(typeInAssembly.FullName, (key) => { return new ConcurrentList<Type>(); });
                typeList2.Add(typeInAssembly);
            }

            if (!excludeMappingTypePrefixes.Any(x => typeInAssembly.FullName.StartsWith(x)))
            {
                if (!typeInAssembly.IsAbstract && typeInAssembly.IsClass)
                {
                    var interfaceTypes = typeInAssembly.GetInterfaces();
                    if (interfaceTypes.Length > 0)
                    {
                        var interfaceList = interfaceByType.GetOrAdd(typeInAssembly, (key) => { return new List<Type>(); });

                        foreach (var interfaceType in interfaceTypes)
                        {
                            if (interfaceType.FullName != null && !excludeMappingTypePrefixes.Any(x => interfaceType.FullName.StartsWith(x)))
                            {
                                interfaceList.Add(interfaceType);

                                var classList = classByInterface.GetOrAdd(interfaceType, (key) => { return new List<Type>(); });
                                classList.Add(typeInAssembly);
                            }
                        }
                    }
                }

                var attributeTypes = typeInAssembly.GetCustomAttributes().Select(x => x.GetType()).Distinct().ToArray();
                foreach (var attributeType in attributeTypes)
                {
                    var thisAttributeType = attributeType;
                    while (thisAttributeType != typeof(Attribute))
                    {
                        var list = typeByAttribute.GetOrAdd(thisAttributeType, (key) => { return new List<Type>(); });
                        list.Add(typeInAssembly);
                        thisAttributeType = thisAttributeType.BaseType;
                    }
                }
            }
        }

        public static bool HasImplementationType(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface) throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            return classByInterface.Keys.Contains(interfaceType);
        }
        public static bool HasImplementationType(Type interfaceType, Type secondaryInterface)
        {
            if (interfaceType == null) throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface) throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");
            if (secondaryInterface == null) throw new ArgumentNullException(nameof(secondaryInterface));

            if (!classByInterface.TryGetValue(interfaceType, out List<Type> classList))
                return false;

            foreach (var classType in classList)
            {
                if (!interfaceByType.TryGetValue(classType, out List<Type> interfaceList))
                    continue;

                if (interfaceList.Contains(secondaryInterface))
                    return true;
            }

            return false;
        }
        public static bool HasImplementationType(Type interfaceType, Type[] secondaryInterfaces, int secondaryInterfaceStartIndex)
        {
            if (interfaceType == null) throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface) throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");
            if (secondaryInterfaces == null) throw new ArgumentNullException(nameof(secondaryInterfaces));

            if (!classByInterface.TryGetValue(interfaceType, out List<Type> classList))
                return false;

            var levels = flagPool.Rent(classList.Count);

            for (var j = 0; j < classList.Count; j++)
                levels[j] = -2;

            int firstLevelFound = -1;
            for (var i = 0; i < secondaryInterfaces.Length; i++)
            {
                for (var j = 0; j < classList.Count; j++)
                {
                    if (levels[j] > -2)
                        continue;

                    var classType = classList[j];

                    if (!interfaceByType.TryGetValue(classType, out List<Type> interfaceList))
                    {
                        levels[j] = -1;
                        continue;
                    }

                    if (interfaceList.Contains(secondaryInterfaces[i]))
                    {
                        levels[j] = (short)j;
                        if (i >= secondaryInterfaceStartIndex && firstLevelFound == -1)
                            firstLevelFound = j;
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
                }
            }

            Array.Clear(levels, 0, levels.Length);
            flagPool.Return(levels);

            return index != -1;
        }

        public static Type GetImplementationType(Type interfaceType, bool throwException = true)
        {
            if (interfaceType == null) throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface) throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            if (!classByInterface.TryGetValue(interfaceType, out List<Type> classList))
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
        public static Type GetImplementationType(Type interfaceType, Type secondaryInterface, bool throwException = true)
        {
            if (interfaceType == null) throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface) throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");
            if (secondaryInterface == null) throw new ArgumentNullException(nameof(secondaryInterface));

            if (!classByInterface.TryGetValue(interfaceType, out List<Type> classList))
            {
                if (throwException)
                    throw new Exception($"No implementations found for {interfaceType.GetNiceName()}");
                else
                    return null;
            }

            var flags = flagPool.Rent(classList.Count);

            for (var j = 0; j < classList.Count; j++)
                flags[j] = -2;

            for (var j = 0; j < classList.Count; j++)
            {
                var classType = classList[j];
                if (!interfaceByType.TryGetValue(classType, out List<Type> interfaceList))
                    continue;

                if (interfaceList.Contains(secondaryInterface))
                    flags[j] = (short)j;
            }

            var index = -1;
            for (var j = 0; j < classList.Count; j++)
            {
                if (flags[j] >= 0)
                {
                    if (index == -1)
                    {
                        index = j;
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
            if (index != -1)
                return classList[index];

            Array.Clear(flags, 0, flags.Length);
            flagPool.Return(flags);

            if (throwException)
                throw new Exception($"No classes found for {interfaceType.GetNiceName()} with secondary interface type {secondaryInterface.GetNiceName()}");
            else
                return null;
        }
        public static Type GetImplementationType(Type interfaceType, Type[] secondaryInterfaces, int secondaryInterfaceStartIndex, bool throwException = true)
        {
            if (interfaceType == null) throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface) throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");
            if (secondaryInterfaces == null) throw new ArgumentNullException(nameof(secondaryInterfaces));

            if (!classByInterface.TryGetValue(interfaceType, out List<Type> classList))
            {
                if (throwException)
                    throw new Exception($"No implementations found for {interfaceType.GetNiceName()}");
                else
                    return null;
            }

            var levels = flagPool.Rent(classList.Count);

            for (var j = 0; j < classList.Count; j++)
                levels[j] = -2;

            int firstLevelFound = -1;
            for (var i = 0; i < secondaryInterfaces.Length; i++)
            {
                for (var j = 0; j < classList.Count; j++)
                {
                    if (levels[j] > -2)
                        continue;

                    var classType = classList[j];

                    if (!interfaceByType.TryGetValue(classType, out List<Type> interfaceList))
                    {
                        levels[j] = -1;
                        continue;
                    }

                    if (interfaceList.Contains(secondaryInterfaces[i]))
                    {
                        levels[j] = (short)j;
                        if (i >= secondaryInterfaceStartIndex && firstLevelFound == -1)
                            firstLevelFound = j;
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

            Array.Clear(levels, 0, levels.Length);
            flagPool.Return(levels);

            if (index == -2)
            {
                if (throwException)
                    throw new Exception($"Multiple classes found for {interfaceType.GetNiceName()} with secondary interfaces type {secondaryInterfaces[firstLevelFound].GetNiceName()}");
                else
                    return null;
            }
            if (index != -1)
                return classList[index];

            if (throwException)
                throw new Exception($"No classes found for {interfaceType.GetNiceName()} with secondary interfaces types {String.Join(", ", secondaryInterfaces.Select(x => x.GetNiceName()))}");
            else
                return null;
        }

        public static ICollection<Type> GetImplementationTypes(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface) throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");

            if (!classByInterface.TryGetValue(interfaceType, out List<Type> classList))
                return Type.EmptyTypes;

            return classList;
        }
        public static ICollection<Type> GetImplementationTypes(Type interfaceType, Type secondaryInterface)
        {
            if (interfaceType == null) throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface) throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");
            if (secondaryInterface == null) throw new ArgumentNullException(nameof(secondaryInterface));

            if (!classByInterface.TryGetValue(interfaceType, out List<Type> classList))
                return Type.EmptyTypes;

            var flags = flagPool.Rent(classList.Count);

            for (var j = 0; j < classList.Count; j++)
                flags[j] = -2;

            for (var j = 0; j < classList.Count; j++)
            {
                var classType = classList[j];
                if (!interfaceByType.TryGetValue(classType, out List<Type> interfaceList))
                    continue;

                if (interfaceList.Contains(secondaryInterface))
                    flags[j] = (short)j;
            }

            var list = new List<Type>();
            for (var j = 0; j < classList.Count; j++)
            {
                if (flags[j] >= 0)
                    list.Add(classList[j]);
            }

            Array.Clear(flags, 0, flags.Length);
            flagPool.Return(flags);

            return list;
        }
        public static ICollection<Type> GetImplementationTypes(Type interfaceType, Type[] secondaryInterfaces, int secondaryInterfaceStartIndex)
        {
            if (interfaceType == null) throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface) throw new ArgumentException($"Type {interfaceType.GetNiceName()} is not an interface");
            if (secondaryInterfaces == null) throw new ArgumentNullException(nameof(secondaryInterfaces));

            if (!classByInterface.TryGetValue(interfaceType, out List<Type> classList))
                return Type.EmptyTypes;

            var levels = flagPool.Rent(classList.Count);

            for (var j = 0; j < classList.Count; j++)
                levels[j] = -2;

            int firstLevelFound = -1;
            for (var i = 0; i < secondaryInterfaces.Length; i++)
            {
                for (var j = 0; j < classList.Count; j++)
                {
                    if (levels[j] > -2)
                        continue;

                    var classType = classList[j];

                    if (!interfaceByType.TryGetValue(classType, out List<Type> interfaceList))
                    {
                        levels[j] = -1;
                        continue;
                    }

                    if (interfaceList.Contains(secondaryInterfaces[i]))
                    {
                        levels[j] = (short)j;
                        if (i >= secondaryInterfaceStartIndex && firstLevelFound == -1)
                            firstLevelFound = j;
                    }
                }
            }

            var list = new List<Type>();
            for (var j = 0; j < classList.Count; j++)
            {
                if (levels[j] == firstLevelFound)
                    list.Add(classList[j]);
            }

            Array.Clear(levels, 0, levels.Length);
            flagPool.Return(levels);

            return list;
        }

        public static IEnumerable<Type> GetTypesFromAttribute(Type attribute)
        {
            if (attribute == null) throw new ArgumentNullException(nameof(attribute));
            if (!typeByAttribute.TryGetValue(attribute, out List<Type> classList))
                return Type.EmptyTypes;
            return classList.ToArray();
        }

        public static Type GetTypeFromName(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (!typeByName.TryGetValue(name, out ConcurrentList<Type> matches))
            {
                var type = ParseType(name);
                var typeList = typeByName.GetOrAdd(name, (key) => { return new ConcurrentList<Type>(); });
                lock (typeList)
                {
                    if (!typeList.Contains(type))
                    {
                        typeList.Add(type);
                    }
                }
                return type;
            }
            else if (matches.Count == 1)
            {
                Type type = matches[0];
                return type;
            }
            else
            {
                throw new Exception($"More than one type matches {name} - , {String.Join(", ", matches.Select(x => x.AssemblyQualifiedName).ToArray())}");
            }
        }

        private static Type ParseType(string name)
        {
            var nameArray = name.ToArray();
            var index = 0;
            var type = ParseType(nameArray, ref index);
            return type;
        }
        private static Type ParseType(char[] chars, ref int index)
        {
            string name = null;

            var current = new List<char>();
            var genericArguments = new List<Type>();
            var arrayDimensions = new List<int>();

            bool expectingGenericOpen = false;
            bool expectingGenericComma = true;
            bool openGeneric = false;
            bool openGenericType = false;
            bool openArray = false;
            bool openArrayOneDimension = false;
            int openGenericTypeSubBrackets = 0;

            bool done = false;
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
                                throw new Exception($"ParseType Unexpected '{c}' at position {index - 1}");
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
                                throw new Exception($"ParseType Unexpected '{c}' at position {index - 1}");
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
                                    throw new Exception($"ParseType Unexpected '{c}' at position {index - 1}");
                                name = new String(current.ToArray());
                                current.Clear();
                            }
                            else if (openGeneric)
                            {
                                openGenericType = true;
                            }
                            else
                            {
                                openArray = true;

                                if (name == null)
                                {
                                    if (current.Count == 0)
                                        throw new Exception($"ParseType Unexpected '{c}' at position {index - 1}");
                                    name = new String(current.ToArray());
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
                                throw new Exception($"ParseType Unexpected '{c}' at position {index - 1}");
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
                                    throw new Exception($"ParseType Unexpected {c}");
                                openArrayOneDimension = true;
                                current.Add(c);
                            }
                            else
                            {
                                throw new Exception($"ParseType Unexpected {c}");
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
                                var genericArgumentName = new String(current.ToArray());
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
                                throw new Exception($"ParseType Unexpected '{c}' at position {index - 1}");
                            }
                            break;
                        }
                    default:
                        {
                            if (openArray || (openGeneric && !openGenericType))
                            {
                                throw new Exception($"ParseType Unexpected {c}");
                            }

                            current.Add(c);
                            break;
                        }
                }

                if (done)
                    break;
            }

            if (name == null)
            {
                name = new String(current.ToArray());
                current.Clear();
            }

            var type = GetTypeFromNameWithoutParse(name);
            if (type == null)
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

        private static Type GetTypeFromNameWithoutParse(string name)
        {
            if (!typeByName.TryGetValue(name, out ConcurrentList<Type> matches))
            {
                var type = Type.GetType(name);
                return type;
            }
            else if (matches.Count == 1)
            {
                Type type = matches[0];
                return type;
            }
            else
            {
                throw new Exception($"More than one type matches {name} - , {String.Join(", ", matches.Select(x => x.AssemblyQualifiedName).ToArray())}");
            }
        }
    }
}