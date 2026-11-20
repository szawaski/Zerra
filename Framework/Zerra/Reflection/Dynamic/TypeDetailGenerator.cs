// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Zerra.Reflection.Dynamic
{
    internal static class TypeDetailGenerator
    {
        private static readonly string nullableTypeName = typeof(Nullable<>).Name;

        private static readonly string iEnumberableTypeName = nameof(IEnumerable);
        private static readonly string iEnumberableGenericTypeName = typeof(IEnumerable<>).Name;

        private static readonly string iCollectionTypeName = nameof(ICollection);
        private static readonly string iCollectionGenericTypeName = typeof(ICollection<>).Name;
        private static readonly string iReadOnlyCollectionGenericTypeName = typeof(IReadOnlyCollection<>).Name;
        private static readonly string iListTypeName = nameof(IList);
        private static readonly string iListGenericTypeName = typeof(IList<>).Name;
        private static readonly string ListGenericTypeName = typeof(List<>).Name;
        private static readonly string iReadOnlyListTypeName = typeof(IReadOnlyList<>).Name;
        private static readonly string iSetGenericTypeName = typeof(ISet<>).Name;
#if !NETSTANDARD2_0
        private static readonly string iReadOnlySetGenericTypeName = typeof(IReadOnlySet<>).Name;
#else
        private static readonly string iReadOnlySetGenericTypeName = "IReadOnlySet`1";
#endif
        private static readonly string hashSetGenericTypeName = typeof(HashSet<>).Name;
        private static readonly string iDictionaryTypeName = typeof(IDictionary).Name;
        private static readonly string iDictionaryGenericTypeName = typeof(IDictionary<,>).Name;
        private static readonly string iReadOnlyDictionaryGenericTypeName = typeof(IReadOnlyDictionary<,>).Name;
        private readonly static string dictionaryGenericTypeName = typeof(Dictionary<,>).Name;

        private static readonly Type keyValuePairType = typeof(KeyValuePair<,>);
        private static readonly Type dictionaryEntryType = typeof(DictionaryEntry);
        private static readonly Type memberDetailType = typeof(MemberDetail<>);

        private static readonly Type methodDetailType = typeof(MethodDetail<>);

        //These should be fine since we are being explicit with the type
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
        private static readonly MethodInfo generateTypeDetailGeneric = typeof(TypeDetailGenerator).GetMethod(nameof(Generate), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo generateConstructorMethod = typeof(TypeDetailGenerator).GetMethod(nameof(GenerateConstructorsGeneric), BindingFlags.Public | BindingFlags.Static)!;
        private static readonly MethodInfo generateFullAccessConstructorMethod = typeof(TypeDetailGenerator).GetMethod(nameof(GenerateFullAccessConstructorsGeneric), BindingFlags.Public | BindingFlags.Static)!;
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

#if !NETSTANDARD2_0
        [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
        [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
#endif
        public static TypeDetail GenerateTypeDetail(Type type)
        {
            if (type.ContainsGenericParameters || type.IsByRefLike)
            {
                return GenerateIncomplete(type);
            }
            else
            {
                var method = generateTypeDetailGeneric.MakeGenericMethod(type);
                return (TypeDetail)method.Invoke(null, [type])!;
            }
        }

#if !NETSTANDARD2_0
        [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
        [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
#endif
        private static TypeDetail GenerateIncomplete(Type type)
        {
            var constructors = new List<ConstructorDetail>(0);
            var innerTypes = GenerateInnerTypes(type);
            var baseTypes = GenerateBaseTypes(type);
            var interfaces = GenerateInterfaces(type);
            //var attributes = GenerateAttributes(type);
            //var members = GenerateMembers(type, interfaces);
            //var methods = GenerateMethods(type, interfaces);

            Delegate? creator = null;
            Func<object>? creatorBoxed = null;

            Type? innerType = null;
            if (innerTypes.Length == 1)
                innerType = innerTypes[0];

            var isNullable = type.Name == nullableTypeName;

            CoreType? coreType = null;
            if (TypeLookup.GetCoreType(type, out var coreTypeLookup))
                coreType = coreTypeLookup;

            SpecialType? specialType = null;
            if (TypeLookup.GetSpecialType(type, out var specialTypeLookup))
                specialType = specialTypeLookup;

            CoreEnumType? enumType = null;
            if (type.IsEnum)
            {
                var enumEnderlyingType = Enum.GetUnderlyingType(type);
                if (!TypeLookup.GetCoreEnumType(enumEnderlyingType, out var enumCoreTypeLookup))
                    throw new NotImplementedException("Should not happen");
                enumType = enumCoreTypeLookup;
            }
            else if (isNullable && innerTypes[0].IsEnum)
            {
                var enumEnderlyingType = Enum.GetUnderlyingType(innerTypes[0]);
                if (!TypeLookup.GetCoreEnumType(enumEnderlyingType, out var enumCoreTypeLookup))
                    throw new NotImplementedException("Should not happen");
                enumCoreTypeLookup = enumCoreTypeLookup switch
                {
                    CoreEnumType.Byte => CoreEnumType.ByteNullable,
                    CoreEnumType.SByte => CoreEnumType.SByteNullable,
                    CoreEnumType.Int16 => CoreEnumType.Int16Nullable,
                    CoreEnumType.UInt16 => CoreEnumType.UInt16Nullable,
                    CoreEnumType.Int32 => CoreEnumType.Int32Nullable,
                    CoreEnumType.UInt32 => CoreEnumType.UInt32Nullable,
                    CoreEnumType.Int64 => CoreEnumType.Int64Nullable,
                    CoreEnumType.UInt64 => CoreEnumType.UInt64Nullable,
                    _ => throw new NotImplementedException(),
                };
                enumType = enumCoreTypeLookup;
            }

            var interfaceNames = new HashSet<string>(interfaces.Select(x => x.Name));
            var baseTypeNames = new HashSet<string>(baseTypes.Select(x => x.Name));

            var hasIEnumerable = type.IsArray || type.Name == iEnumberableTypeName || interfaceNames.Contains(iEnumberableTypeName);
            var hasIEnumerableGeneric = type.IsArray || type.Name == iEnumberableGenericTypeName || interfaceNames.Contains(iEnumberableGenericTypeName);
            var hasICollection = type.Name == iCollectionTypeName || interfaceNames.Contains(iCollectionTypeName);
            var hasICollectionGeneric = type.Name == iCollectionGenericTypeName || interfaceNames.Contains(iCollectionGenericTypeName);
            var hasIReadOnlyCollectionGeneric = type.Name == iReadOnlyCollectionGenericTypeName || interfaceNames.Contains(iReadOnlyCollectionGenericTypeName);
            var hasIList = type.Name == iListTypeName || interfaceNames.Contains(iListTypeName);
            var hasIListGeneric = type.Name == iListGenericTypeName || interfaceNames.Contains(iListGenericTypeName);
            var hasListGeneric = type.Name == ListGenericTypeName || baseTypeNames.Contains(ListGenericTypeName);
            var hasIReadOnlyListGeneric = type.Name == iReadOnlyListTypeName || interfaceNames.Contains(iReadOnlyListTypeName);
            var hasISetGeneric = type.Name == iSetGenericTypeName || interfaceNames.Contains(iSetGenericTypeName);
            var hasIReadOnlySetGeneric = type.Name == iReadOnlySetGenericTypeName || interfaceNames.Contains(iReadOnlySetGenericTypeName);
            var hasHashSetGeneric = type.Name == hashSetGenericTypeName || baseTypeNames.Contains(hashSetGenericTypeName);
            var hasIDictionary = type.Name == iDictionaryTypeName || interfaceNames.Contains(iDictionaryTypeName);
            var hasIDictionaryGeneric = type.Name == iDictionaryGenericTypeName || interfaceNames.Contains(iDictionaryGenericTypeName);
            var hasIReadOnlyDictionaryGeneric = type.Name == iReadOnlyDictionaryGenericTypeName || interfaceNames.Contains(iReadOnlyDictionaryGenericTypeName);
            var hasDictionaryGeneric = type.Name == dictionaryGenericTypeName || baseTypeNames.Contains(dictionaryGenericTypeName);

            var isIEnumerable = type.Name == iEnumberableTypeName;
            var isIEnumerableGeneric = type.Name == iEnumberableGenericTypeName;
            var isICollection = type.Name == iCollectionTypeName;
            var isICollectionGeneric = type.Name == iCollectionGenericTypeName;
            var isIReadOnlyCollectionGeneric = type.Name == iReadOnlyCollectionGenericTypeName;
            var isIList = type.Name == iListTypeName;
            var isIListGeneric = type.Name == iListGenericTypeName;
            var isListGeneric = type.Name == ListGenericTypeName;
            var isIReadOnlyListGeneric = type.Name == iReadOnlyListTypeName;
            var isISetGeneric = type.Name == iSetGenericTypeName;
            var isIReadOnlySetGeneric = type.Name == iReadOnlySetGenericTypeName;
            var isHashSetGeneric = type.Name == hashSetGenericTypeName;
            var isIDictionary = type.Name == iDictionaryTypeName;
            var isIDictionaryGeneric = type.Name == iDictionaryGenericTypeName;
            var isIReadOnlyDictionaryGeneric = type.Name == iReadOnlyDictionaryGenericTypeName;
            var isDictionaryGeneric = type.Name == dictionaryGenericTypeName;

            Type? iEnumerableGenericInnerType = null;
            if (isIEnumerableGeneric)
            {
                iEnumerableGenericInnerType = innerTypes[0];
            }
            else
            {
                var interfaceFound = interfaces.Where(x => x.Name == iEnumberableGenericTypeName).ToArray();
                if (interfaceFound.Length == 1)
                {
                    iEnumerableGenericInnerType = interfaceFound[0].GetGenericArguments()[0];
                }
            }

            Type? dictionaryInnerType = null;
            if (isIDictionaryGeneric || isIReadOnlyDictionaryGeneric)
            {
                dictionaryInnerType = keyValuePairType.MakeGenericType(innerTypes);
            }
            else if (hasIDictionaryGeneric)
            {
                var interfaceFound = interfaces.Where(x => x.Name == iDictionaryGenericTypeName).ToArray();
                if (interfaceFound.Length == 1)
                {
                    dictionaryInnerType = keyValuePairType.MakeGenericType(interfaceFound[0].GetGenericArguments());
                }
            }
            else if (hasIReadOnlyDictionaryGeneric)
            {
                var interfaceFound = interfaces.Where(x => x.Name == iReadOnlyDictionaryGenericTypeName).ToArray();
                if (interfaceFound.Length == 1)
                {
                    dictionaryInnerType = keyValuePairType.MakeGenericType(interfaceFound[0].GetGenericArguments());
                }
            }
            else if (hasIDictionary)
            {
                dictionaryInnerType = dictionaryEntryType;
            }

            var typeDetail = new TypeDetail(
                type: type,
                members: null,
                constructors: constructors,
                methods: null,
                creator: creator,
                creatorBoxed: creatorBoxed,
                isNullable: isNullable,
                coreType: coreType,
                specialType: specialType,
                enumUnderlyingType: enumType,
                hasIEnumerable: hasIEnumerable,
                hasIEnumerableGeneric: hasIEnumerableGeneric,
                hasICollection: hasICollection,
                hasICollectionGeneric: hasICollectionGeneric,
                hasIReadOnlyCollectionGeneric: hasIReadOnlyCollectionGeneric,
                hasIList: hasIList,
                hasIListGeneric: hasIListGeneric,
                hasIReadOnlyListGeneric: hasIReadOnlyListGeneric,
                hasListGeneric: hasListGeneric,
                hasISetGeneric: hasISetGeneric,
                hasIReadOnlySetGeneric: hasIReadOnlySetGeneric,
                hasHashSetGeneric: hasHashSetGeneric,
                hasIDictionary: hasIDictionary,
                hasIDictionaryGeneric: hasIDictionaryGeneric,
                hasIReadOnlyDictionaryGeneric: hasIReadOnlyDictionaryGeneric,
                hasDictionaryGeneric: hasDictionaryGeneric,
                isIEnumerable: isIEnumerable,
                isIEnumerableGeneric: isIEnumerableGeneric,
                isICollection: isICollection,
                isICollectionGeneric: isICollectionGeneric,
                isIReadOnlyCollectionGeneric: isIReadOnlyCollectionGeneric,
                isIList: isIList,
                isIListGeneric: isIListGeneric,
                isIReadOnlyListGeneric: isIReadOnlyListGeneric,
                isListGeneric: isListGeneric,
                isISetGeneric: isISetGeneric,
                isIReadOnlySetGeneric: isIReadOnlySetGeneric,
                isHashSetGeneric: isHashSetGeneric,
                isIDictionary: isIDictionary,
                isIDictionaryGeneric: isIDictionaryGeneric,
                isIReadOnlyDictionaryGeneric: isIReadOnlyDictionaryGeneric,
                isDictionaryGeneric: isDictionaryGeneric,
                innerType: innerType,
                iEnumerableGenericInnerType: iEnumerableGenericInnerType,
                dictionaryInnerType: dictionaryInnerType,
                innerTypes: innerTypes,
                baseTypes: baseTypes,
                interfaces: interfaces,
                attributes: null
            );
            return typeDetail;
        }

#if !NETSTANDARD2_0
        [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
        [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
#endif
        private static TypeDetail<T> Generate<T>(Type type)
        {
            //var constructors = GenerateConstructors<T>(type);
            var innerTypes = GenerateInnerTypes(type);
            var baseTypes = GenerateBaseTypes(type);
            var interfaces = GenerateInterfaces(type);
            //var attributes = GenerateAttributes(type);
            //var members = GenerateMembers(type, interfaces);
            //var methods = GetMethods(type, interfaces);

            Func<T>? creator = null;
            Func<object>? creatorBoxed = null;

            //a type with required members is created through a constructor that takes them, like generated code
            if (!type.IsAbstract && !type.IsGenericTypeDefinition && !type.IsInterface && !HasRequiredMembers(type))
            {
                var emptyConstructor = type.GetConstructor(Type.EmptyTypes);
                if (emptyConstructor != null)
                {
                    creator = AccessorGenerator.GenerateCreatorNoArgs<T>(emptyConstructor);
                    creatorBoxed = AccessorGenerator.GenerateCreatorNoArgs(emptyConstructor);
                }
                else if (type.IsValueType && type.Name != "Void")
                {
                    creator = static () => default!;
                    creatorBoxed = static () => default(T)!;
                }
                else if (type == typeof(string))
                {
                    creator = static () => (T)(object)String.Empty;
                    creatorBoxed = static () => String.Empty;
                }
            }

            Type? innerType = null;
            if (innerTypes.Length == 1)
                innerType = innerTypes[0];

            var isNullable = type.Name == nullableTypeName;

            CoreType? coreType = null;
            if (TypeLookup.GetCoreType(type, out var coreTypeLookup))
                coreType = coreTypeLookup;

            SpecialType? specialType = null;
            if (TypeLookup.GetSpecialType(type, out var specialTypeLookup))
                specialType = specialTypeLookup;

            CoreEnumType? enumType = null;
            if (type.IsEnum)
            {
                var enumEnderlyingType = Enum.GetUnderlyingType(type);
                if (!TypeLookup.GetCoreEnumType(enumEnderlyingType, out var enumCoreTypeLookup))
                    throw new NotImplementedException("Should not happen");
                enumType = enumCoreTypeLookup;
            }
            else if (isNullable && innerTypes[0].IsEnum)
            {
                var enumEnderlyingType = Enum.GetUnderlyingType(innerTypes[0]);
                if (!TypeLookup.GetCoreEnumType(enumEnderlyingType, out var enumCoreTypeLookup))
                    throw new NotImplementedException("Should not happen");
                enumCoreTypeLookup = enumCoreTypeLookup switch
                {
                    CoreEnumType.Byte => CoreEnumType.ByteNullable,
                    CoreEnumType.SByte => CoreEnumType.SByteNullable,
                    CoreEnumType.Int16 => CoreEnumType.Int16Nullable,
                    CoreEnumType.UInt16 => CoreEnumType.UInt16Nullable,
                    CoreEnumType.Int32 => CoreEnumType.Int32Nullable,
                    CoreEnumType.UInt32 => CoreEnumType.UInt32Nullable,
                    CoreEnumType.Int64 => CoreEnumType.Int64Nullable,
                    CoreEnumType.UInt64 => CoreEnumType.UInt64Nullable,
                    _ => throw new NotImplementedException(),
                };
                enumType = enumCoreTypeLookup;
            }

            var interfaceNames = new HashSet<string>(interfaces.Select(x => x.Name));
            var baseTypeNames = new HashSet<string>(baseTypes.Select(x => x.Name));

            var hasIEnumerable = type.IsArray || type.Name == iEnumberableTypeName || interfaceNames.Contains(iEnumberableTypeName);
            var hasIEnumerableGeneric = type.IsArray || type.Name == iEnumberableGenericTypeName || interfaceNames.Contains(iEnumberableGenericTypeName);
            var hasICollection = type.Name == iCollectionTypeName || interfaceNames.Contains(iCollectionTypeName);
            var hasICollectionGeneric = type.Name == iCollectionGenericTypeName || interfaceNames.Contains(iCollectionGenericTypeName);
            var hasIReadOnlyCollectionGeneric = type.Name == iReadOnlyCollectionGenericTypeName || interfaceNames.Contains(iReadOnlyCollectionGenericTypeName);
            var hasIList = type.Name == iListTypeName || interfaceNames.Contains(iListTypeName);
            var hasIListGeneric = type.Name == iListGenericTypeName || interfaceNames.Contains(iListGenericTypeName);
            var hasListGeneric = type.Name == ListGenericTypeName || baseTypeNames.Contains(ListGenericTypeName);
            var hasIReadOnlyListGeneric = type.Name == iReadOnlyListTypeName || interfaceNames.Contains(iReadOnlyListTypeName);
            var hasISetGeneric = type.Name == iSetGenericTypeName || interfaceNames.Contains(iSetGenericTypeName);
            var hasIReadOnlySetGeneric = type.Name == iReadOnlySetGenericTypeName || interfaceNames.Contains(iReadOnlySetGenericTypeName);
            var hasHashSetGeneric = type.Name == hashSetGenericTypeName || baseTypeNames.Contains(hashSetGenericTypeName);
            var hasIDictionary = type.Name == iDictionaryTypeName || interfaceNames.Contains(iDictionaryTypeName);
            var hasIDictionaryGeneric = type.Name == iDictionaryGenericTypeName || interfaceNames.Contains(iDictionaryGenericTypeName);
            var hasIReadOnlyDictionaryGeneric = type.Name == iReadOnlyDictionaryGenericTypeName || interfaceNames.Contains(iReadOnlyDictionaryGenericTypeName);
            var hasDictionaryGeneric = type.Name == dictionaryGenericTypeName || baseTypeNames.Contains(dictionaryGenericTypeName);

            var isIEnumerable = type.Name == iEnumberableTypeName;
            var isIEnumerableGeneric = type.Name == iEnumberableGenericTypeName;
            var isICollection = type.Name == iCollectionTypeName;
            var isICollectionGeneric = type.Name == iCollectionGenericTypeName;
            var isIReadOnlyCollectionGeneric = type.Name == iReadOnlyCollectionGenericTypeName;
            var isIList = type.Name == iListTypeName;
            var isIListGeneric = type.Name == iListGenericTypeName;
            var isListGeneric = type.Name == ListGenericTypeName;
            var isIReadOnlyListGeneric = type.Name == iReadOnlyListTypeName;
            var isISetGeneric = type.Name == iSetGenericTypeName;
            var isIReadOnlySetGeneric = type.Name == iReadOnlySetGenericTypeName;
            var isHashSetGeneric = type.Name == hashSetGenericTypeName;
            var isIDictionary = type.Name == iDictionaryTypeName;
            var isIDictionaryGeneric = type.Name == iDictionaryGenericTypeName;
            var isIReadOnlyDictionaryGeneric = type.Name == iReadOnlyDictionaryGenericTypeName;
            var isDictionaryGeneric = type.Name == dictionaryGenericTypeName;

            Type? iEnumerableGenericInnerType = null;
            if (isIEnumerableGeneric)
            {
                iEnumerableGenericInnerType = innerTypes[0];
            }
            else
            {
                var interfaceFound = interfaces.Where(x => x.Name == iEnumberableGenericTypeName).ToArray();
                if (interfaceFound.Length == 1)
                {
                    iEnumerableGenericInnerType = interfaceFound[0].GetGenericArguments()[0];
                }
            }

            Type? dictionaryInnerType = null;
            if (isIDictionaryGeneric || isIReadOnlyDictionaryGeneric)
            {
                dictionaryInnerType = keyValuePairType.MakeGenericType(innerTypes);
            }
            else if (hasIDictionaryGeneric)
            {
                var interfaceFound = interfaces.Where(x => x.Name == iDictionaryGenericTypeName).ToArray();
                if (interfaceFound.Length == 1)
                {
                    dictionaryInnerType = keyValuePairType.MakeGenericType(interfaceFound[0].GetGenericArguments());
                }
            }
            else if (hasIReadOnlyDictionaryGeneric)
            {
                var interfaceFound = interfaces.Where(x => x.Name == iReadOnlyDictionaryGenericTypeName).ToArray();
                if (interfaceFound.Length == 1)
                {
                    dictionaryInnerType = keyValuePairType.MakeGenericType(interfaceFound[0].GetGenericArguments());
                }
            }
            else if (hasIDictionary)
            {
                dictionaryInnerType = dictionaryEntryType;
            }

            var typeDetail = new TypeDetail<T>(
                members: null,
                constructors: null,
                methods: null,
                creator: creator,
                creatorBoxed: creatorBoxed,
                isNullable: isNullable,
                coreType: coreType,
                specialType: specialType,
                enumUnderlyingType: enumType,
                hasIEnumerable: hasIEnumerable,
                hasIEnumerableGeneric: hasIEnumerableGeneric,
                hasICollection: hasICollection,
                hasICollectionGeneric: hasICollectionGeneric,
                hasIReadOnlyCollectionGeneric: hasIReadOnlyCollectionGeneric,
                hasIList: hasIList,
                hasIListGeneric: hasIListGeneric,
                hasIReadOnlyListGeneric: hasIReadOnlyListGeneric,
                hasListGeneric: hasListGeneric,
                hasISetGeneric: hasISetGeneric,
                hasIReadOnlySetGeneric: hasIReadOnlySetGeneric,
                hasHashSetGeneric: hasHashSetGeneric,
                hasIDictionary: hasIDictionary,
                hasIDictionaryGeneric: hasIDictionaryGeneric,
                hasIReadOnlyDictionaryGeneric: hasIReadOnlyDictionaryGeneric,
                hasDictionaryGeneric: hasDictionaryGeneric,
                isIEnumerable: isIEnumerable,
                isIEnumerableGeneric: isIEnumerableGeneric,
                isICollection: isICollection,
                isICollectionGeneric: isICollectionGeneric,
                isIReadOnlyCollectionGeneric: isIReadOnlyCollectionGeneric,
                isIList: isIList,
                isIListGeneric: isIListGeneric,
                isIReadOnlyListGeneric: isIReadOnlyListGeneric,
                isListGeneric: isListGeneric,
                isISetGeneric: isISetGeneric,
                isIReadOnlySetGeneric: isIReadOnlySetGeneric,
                isHashSetGeneric: isHashSetGeneric,
                isIDictionary: isIDictionary,
                isIDictionaryGeneric: isIDictionaryGeneric,
                isIReadOnlyDictionaryGeneric: isIReadOnlyDictionaryGeneric,
                isDictionaryGeneric: isDictionaryGeneric,
                innerType: innerType,
                iEnumerableGenericInnerType: iEnumerableGenericInnerType,
                dictionaryInnerType: dictionaryInnerType,
                innerTypes: innerTypes,
                baseTypes: baseTypes,
                interfaces: interfaces,
                attributes: null
            );
            return typeDetail;
        }

#if !NETSTANDARD2_0
        [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
        [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
#endif
        public static List<MemberDetail> GenerateMembers(Type type, IReadOnlyCollection<Type> interfaces)
        {
            var items = new List<MemberDetail>();

            //built-in types and arrays are handled by their own converters, generated code lists no members for them
            if (type.IsGenericTypeDefinition || type.IsArray || TypeLookup.GetCoreType(type, out _))
                return items;

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            //take private fields to find backing fields and allow advanced serialization scenarios
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).ToList();

            var hasInterfaces = interfaces.Count > 0;
            var names = hasInterfaces ? new HashSet<string>() : null; //explicit declarations can create duplicates with interfaces

            foreach (var property in properties)
            {
                if (property.GetIndexParameters().Length > 0)
                    continue;
                if (property.PropertyType.IsPointer)
                    continue;
                if (property.GetMethod?.IsPublic != true && property.SetMethod?.IsPublic != true)
                    continue;

                Delegate? getter = null;
                Func<object, object?>? getterBoxed = null;

                Delegate? setter = null;
                Action<object, object?>? setterBoxed = null;

                var hasGetter = property.GetMethod != null && (property.GetMethod.IsPublic || property.GetMethod.IsAssembly || property.GetMethod.IsFamilyOrAssembly);
                var hasSetter = property.SetMethod != null && (property.SetMethod.IsPublic || property.SetMethod.IsAssembly || property.SetMethod.IsFamilyOrAssembly);

                //try backing field pattern <{property.Name}>k__BackingField or <{property.Name}>i__Field
                var backingName = $"<{property.Name}>";
                var backingField = fields.FirstOrDefault(x => x.Name.StartsWith(backingName) && x.FieldType == property.PropertyType);

                if (backingField != null && !backingField.IsLiteral)
                {
                    _ = fields.Remove(backingField);

                    if (hasGetter)
                    {
                        getter = AccessorGenerator.GenerateGetter(backingField, backingField.FieldType);
                        getterBoxed = AccessorGenerator.GenerateGetter(backingField);
                    }
                    if (hasSetter)
                    {
                        setter = AccessorGenerator.GenerateSetter(backingField, backingField.FieldType);
                        setterBoxed = AccessorGenerator.GenerateSetter(backingField);
                    }
                }
                else
                {
                    if (hasGetter)
                    {
                        getter = AccessorGenerator.GenerateGetter(property, property.PropertyType);
                        getterBoxed = AccessorGenerator.GenerateGetter(property);
                    }
                    if (hasSetter)
                    {
                        setter = AccessorGenerator.GenerateSetter(property, property.PropertyType);
                        setterBoxed = AccessorGenerator.GenerateSetter(property);
                    }
                }

                var attributes = GenerateAttributes(property);

                var isStatic = property.GetMethod?.IsStatic ?? property.SetMethod?.IsStatic ?? false;

                var allowsNull = GetAllowsNull(property, property.PropertyType);

                MemberDetail member;
                if (property.PropertyType.ContainsGenericParameters || property.PropertyType.IsPointer || property.PropertyType.IsByRef || property.PropertyType.IsByRefLike)
                {
                    member = new MemberDetail(type, property.PropertyType, property.Name, false, getter, getterBoxed, setter, setterBoxed, attributes, isStatic, allowsNull);
                }
                else
                {
                    var memberDetailGenericType = memberDetailType.MakeGenericType(property.PropertyType);
                    var constructor = memberDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                    member = (MemberDetail)constructor.Invoke([type, property.Name, false, getter, getterBoxed, setter, setterBoxed, attributes, isStatic, allowsNull]);
                }
                items.Add(member);

                if (hasInterfaces)
                    _ = names!.Add(property.Name);
            }

            foreach (var @field in fields)
            {
                if (@field.IsLiteral)
                    continue;
                if (!field.IsPublic)
                    continue;
                //an enum's value__ holds its number, the source generator doesn't see it as a member
                if (field.IsSpecialName)
                    continue;

                Delegate? getter = null;
                Func<object, object?>? getterBoxed = null;

                Delegate? setter = null;
                Action<object, object?>? setterBoxed = null;


                if (!@field.FieldType.IsByRef && !@field.FieldType.IsByRefLike)
                {
                    getter = AccessorGenerator.GenerateGetter(@field, @field.FieldType);
                    getterBoxed = AccessorGenerator.GenerateGetter(@field);

                    if (!@field.IsInitOnly)
                    {
                        setter = AccessorGenerator.GenerateSetter(@field, @field.FieldType);
                        setterBoxed = AccessorGenerator.GenerateSetter(@field);
                    }
                }


                var attributes = GenerateAttributes(@field);

                var allowsNull = GetAllowsNull(@field, @field.FieldType);

                MemberDetail member;
                if (@field.FieldType.ContainsGenericParameters || @field.FieldType.IsPointer || @field.FieldType.IsByRef || @field.FieldType.IsByRefLike)
                {
                    member = new MemberDetail(type, field.FieldType, field.Name, true, getter, getterBoxed, setter, setterBoxed, attributes, field.IsStatic, allowsNull);
                }
                else
                {
                    var memberDetailGenericType = memberDetailType.MakeGenericType(@field.FieldType);
                    var constructor = memberDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                    member = (MemberDetail)constructor.Invoke([type, field.Name, true, getter, getterBoxed, setter, setterBoxed, attributes, field.IsStatic, allowsNull]);
                }
                items.Add(member);

                if (hasInterfaces)
                    _ = names!.Add(@field.Name);
            }

            if (hasInterfaces)
            {
                foreach (var i in interfaces)
                {
                    var iProperties = i.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);//don't get static interface members

                    foreach (var property in iProperties)
                    {
                        if (property.DeclaringType == typeof(Zerra.CQRS.IHandler))
                            continue;
                        if (property.GetIndexParameters().Length > 0)
                            continue;
                        if (property.PropertyType.IsPointer)
                            continue;
                        if (property.GetMethod?.IsPublic != true && property.SetMethod?.IsPublic != true)
                            continue;

                        //explicit implementations are on the interface's own detail
                        if (!type.IsInterface || names!.Contains(property.Name))
                            continue;
                        var name = property.Name;

                        if (names!.Add(name))
                        {
                            Delegate? getter = null;
                            Func<object, object?>? getterBoxed = null;

                            Delegate? setter = null;
                            Action<object, object?>? setterBoxed = null;

                            if (!property.PropertyType.IsByRef && !property.PropertyType.IsByRefLike)
                            {
                                if (property.GetMethod != null && (property.GetMethod.IsPublic || property.GetMethod.IsAssembly || property.GetMethod.IsFamilyOrAssembly))
                                {
                                    getter = AccessorGenerator.GenerateGetter(property, property.PropertyType);
                                    getterBoxed = AccessorGenerator.GenerateGetter(property);
                                }
                                if (property.SetMethod != null && (property.SetMethod.IsPublic || property.SetMethod.IsAssembly || property.SetMethod.IsFamilyOrAssembly))
                                {
                                    setter = AccessorGenerator.GenerateSetter(property, property.PropertyType);
                                    setterBoxed = AccessorGenerator.GenerateSetter(property);
                                }
                            }

                            var attributes = GenerateAttributes(property);

                            var isStatic = property.GetMethod?.IsStatic ?? property.SetMethod?.IsStatic ?? false;

                            var allowsNull = GetAllowsNull(property, property.PropertyType);
                            MemberDetail member;
                            if (property.PropertyType.ContainsGenericParameters || property.PropertyType.IsPointer || property.PropertyType.IsByRef || property.PropertyType.IsByRefLike)
                            {
                                member = new MemberDetail(type, property.PropertyType, name, false, getter, getterBoxed, setter, setterBoxed, attributes, isStatic, allowsNull);
                            }
                            else
                            {
                                var memberDetailGenericType = TypeDetailGenerator.memberDetailType.MakeGenericType(property.PropertyType);
                                var memberDetailType = memberDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                                member = (MemberDetail)memberDetailType.Invoke([type, name, false, getter, getterBoxed, setter, setterBoxed, attributes, isStatic, allowsNull]);
                            }
                            items.Add(member);
                        }
                    }
                }
            }

            return items;
        }

#if !NETSTANDARD2_0
        [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
        [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
#endif
        public static List<ConstructorDetail> GenerateConstructors(Type type)
        {
            return (List<ConstructorDetail>)generateConstructorMethod.MakeGenericMethod(type).Invoke(null, [type])!;
        }

#if !NETSTANDARD2_0
        [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
        [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
#endif
        public static List<ConstructorDetail> GenerateConstructorsGeneric<T>(Type type)
        {
            var items = new List<ConstructorDetail>();
            //an array is made by its converters, a delegate's constructor can't be called from C#, generated code lists none for them,
            //built-in types such as DateTime keep theirs so a query can evaluate new DateTime(...)
            if (type.IsGenericTypeDefinition || type.IsArray || typeof(Delegate).IsAssignableFrom(type))
                return items;

            //required members are set after the constructor, taken as extra arguments like generated code's object initializer
            var requiredMembers = GetRequiredMembers(type);

            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            foreach (var constructor in constructors)
            {
                if (!constructor.IsPublic)
                    continue;

                var parameters = constructor.GetParameters();
                if (parameters.Any(x => (x.ParameterType.IsByRef ? x.ParameterType.GetElementType()! : x.ParameterType) is { } elementType && (elementType.IsByRefLike || elementType.IsPointer)))
                    continue;

                Func<object?[]?, T>? creator = null;
                Func<object?[]?, object>? creatorBoxed = null;

                creator = AccessorGenerator.GenerateCreator<T>(constructor);
                if (creator == null)
                    continue;

                creatorBoxed = AccessorGenerator.GenerateCreator(constructor);
                if (creatorBoxed == null)
                    continue;

                var parameterTypes = parameters.Select(x => new ParameterDetail(x.ParameterType, x.Name!)).ToArray();

                if (requiredMembers.Length > 0)
                    items.Add(GenerateRequiredMembersConstructor<T>(parameterTypes, creatorBoxed, requiredMembers));
                else
                    items.Add(new ConstructorDetail<T>(parameterTypes, creator, creatorBoxed));
            }

            //a struct's parameterless constructor isn't declared, generated code calls it to set the required members
            if (requiredMembers.Length > 0 && type.IsValueType && !items.Any(x => x.Parameters.Count == requiredMembers.Length))
                items.Add(GenerateRequiredMembersConstructor<T>([], static (object?[]? args) => default(T)!, requiredMembers));

            return items;
        }

#if !NETSTANDARD2_0
        [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
        [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
#endif
        private static (string Name, Type Type, Action<object, object?> Setter, object? Default)[] GetRequiredMembers(Type type)
        {
            var requiredMembers = new List<(string, Type, Action<object, object?>, object?)>();
            if (!HasRequiredMembers(type))
                return [];
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.GetCustomAttributes(true).Any(x => x.GetType().FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute"))
                    continue;
                var setter = AccessorGenerator.GenerateSetter(property);
                if (setter is not null)
                    requiredMembers.Add((property.Name, property.PropertyType, setter, property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null));
            }
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!field.GetCustomAttributes(true).Any(x => x.GetType().FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute"))
                    continue;
                var setter = AccessorGenerator.GenerateSetter(field);
                if (setter is not null)
                    requiredMembers.Add((field.Name, field.FieldType, setter, field.FieldType.IsValueType ? Activator.CreateInstance(field.FieldType) : null));
            }
            return requiredMembers.ToArray();
        }

        //the compiler marks each type that declares required members, the attribute isn't inherited
        private static bool HasRequiredMembers(Type type)
        {
            for (var current = type; current is not null; current = current.BaseType)
            {
                if (current.GetCustomAttributes(false).Any(x => x.GetType().FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute"))
                    return true;
            }
            return false;
        }

        private static ConstructorDetail<T> GenerateRequiredMembersConstructor<T>(ParameterDetail[] parameterTypes, Func<object?[]?, object> constructorBoxed, (string Name, Type Type, Action<object, object?> Setter, object? Default)[] requiredMembers)
        {
            var parameterCount = parameterTypes.Length;
            object CreatorBoxed(object?[]? args)
            {
                var constructorArgs = args;
                if (args is not null && args.Length > parameterCount)
                {
                    constructorArgs = new object?[parameterCount];
                    Array.Copy(args, constructorArgs, parameterCount);
                }
                var instance = constructorBoxed(constructorArgs);
                //ref and out values were written into the copy
                if (constructorArgs != args)
                    Array.Copy(constructorArgs!, args!, parameterCount);
                for (var i = 0; i < requiredMembers.Length; i++)
                {
                    var index = parameterCount + i;
                    requiredMembers[i].Setter(instance, (args is not null && index < args.Length ? args[index] : null) ?? requiredMembers[i].Default);
                }
                return instance;
            }
            var allParameters = parameterTypes.Concat(requiredMembers.Select(x => new ParameterDetail(x.Type, x.Name))).ToArray();
            return new ConstructorDetail<T>(allParameters, args => (T)CreatorBoxed(args), CreatorBoxed);
        }

#if !NETSTANDARD2_0
        [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
        [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
#endif
        public static List<MethodDetail> GenerateMethods(Type type, IReadOnlyCollection<Type> interfaces)
        {
            var items = new List<MethodDetail>();
            var hasInterfaces = interfaces.Count > 0;
            var names = hasInterfaces ? new HashSet<string>() : null; //explicit declarations can create duplicates with interfaces
            if (!type.IsGenericTypeDefinition)
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    if (!method.IsPublic)
                        continue;
                    if (method.IsStatic || method.IsSpecialName)
                        continue;
                    //an object[] of arguments can't pass a ref or out back
                    if (method.ReturnType.IsByRefLike || method.ReturnType.IsPointer || method.GetParameters().Any(x => (x.ParameterType.IsByRef && !x.IsIn) || x.ParameterType.IsByRefLike || x.ParameterType.IsPointer))
                        continue;

                    var parameters = method.GetParameters();
                    var parameterTypes = parameters.Select(x => new ParameterDetail(x.ParameterType, x.Name!)).ToArray();

                    var attributes = GenerateAttributes(method);

                    Delegate? caller = null;
                    Func<object?, object?[]?, object?>? callerBoxed = null;

                    caller = AccessorGenerator.GenerateCaller(method, method.ReturnType);
                    callerBoxed = AccessorGenerator.GenerateCaller(method);

                    var genericArguments = method.GetGenericArguments();

                    MethodDetail methodDetail;
                    if (method.ReturnType.ContainsGenericParameters || method.ReturnType.IsPointer || method.ReturnType.IsByRef || method.ReturnType.IsByRefLike)
                    {
                        methodDetail = new MethodDetail(type, method.Name, method.ReturnType, genericArguments, parameterTypes, caller, callerBoxed, attributes, method.IsStatic);
                    }
                    else
                    {
                        var methodDetailGenericType = TypeDetailGenerator.methodDetailType.MakeGenericType(method.ReturnType == typeof(void) ? typeof(object) : method.ReturnType);
                        var methodDetailType = methodDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                        methodDetail = (MethodDetail)methodDetailType.Invoke([type, method.Name, genericArguments, parameterTypes, caller, callerBoxed, attributes, method.IsStatic]);
                    }

                    items.Add(methodDetail);
                    if (hasInterfaces)
                        _ = names!.Add(method.Name);
                }

                if (hasInterfaces)
                {
                    foreach (var i in interfaces)
                    {
                        var iMethods = i.GetMethods(BindingFlags.Public | BindingFlags.Instance); //don't get static interface methods
                        foreach (var method in iMethods)
                        {
                            if (method.IsSpecialName || method.DeclaringType == typeof(Zerra.CQRS.IHandler))
                                continue;
                            if (method.ReturnType.IsByRefLike || method.ReturnType.IsPointer || method.GetParameters().Any(x => (x.ParameterType.IsByRef && !x.IsIn) || x.ParameterType.IsByRefLike || x.ParameterType.IsPointer))
                                continue;
                            //explicit implementations are on the interface's own detail
                            if (!type.IsInterface || names!.Contains(method.Name))
                                continue;
                            var name = method.Name;

                            if (!names!.Contains(name))
                            {
                                var parameters = method.GetParameters();
                                var parameterTypes = parameters.Select(x => new ParameterDetail(x.ParameterType, x.Name!)).ToArray();

                                var attributes = GenerateAttributes(method);

                                Delegate? caller = AccessorGenerator.GenerateCaller(method, method.ReturnType);
                                if (caller == null)
                                    continue;

                                Func<object?, object?[]?, object?>? callerBoxed = AccessorGenerator.GenerateCaller(method);
                                if (callerBoxed == null)
                                    continue;

                                var genericArguments = method.GetGenericArguments();

                                MethodDetail methodDetail;
                                if (method.ReturnType.ContainsGenericParameters || method.ReturnType.IsPointer || method.ReturnType.IsByRef || method.ReturnType.IsByRefLike)
                                {
                                    methodDetail = new MethodDetail(type, name, method.ReturnType, genericArguments, parameterTypes, caller, callerBoxed, attributes, method.IsStatic);
                                }
                                else
                                {
                                    var methodDetailGenericType = methodDetailType.MakeGenericType(method.ReturnType == typeof(void) ? typeof(object) : method.ReturnType);
                                    var constructor = methodDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                                    methodDetail = (MethodDetail)constructor.Invoke([type, name, genericArguments, parameterTypes, caller, callerBoxed, attributes, method.IsStatic]);
                                }
                                items.Add(methodDetail);
                                if (hasInterfaces)
                                    _ = names!.Add(name);
                            }
                        }
                    }
                }
            }
            return items;
        }

#if !NETSTANDARD2_0
        [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
        [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
#endif
        public static List<MemberDetail> GenerateFullAccessMembers(Type type, IReadOnlyCollection<Type> interfaces, IReadOnlyList<MemberDetail> members)
        {
            var items = new List<MemberDetail>();
            var names = new HashSet<string>();

            if (type.IsGenericTypeDefinition)
                return items;

            //a base type's private members aren't listed, they're on the base type's own detail
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            foreach (var property in properties)
            {
                if (property.GetIndexParameters().Length > 0)
                    continue;
                if (property.PropertyType.IsPointer)
                    continue;
                if (!names.Add(property.Name))
                    continue;

                //a get-only auto-property is set through its backing field, a static readonly field can't be set reliably
                var backingField = property.SetMethod is null ? fields.FirstOrDefault(x => x.Name == $"<{property.Name}>k__BackingField" && !(x.IsInitOnly && x.IsStatic)) : null;
                var hasGetter = property.GetMethod is not null;
                var hasSetter = property.SetMethod is not null || backingField is not null;

                //the restricted detail is shared when it already reads and writes everything this property can
                var shared = members.FirstOrDefault(x => x.Name == property.Name && !x.IsField && x.HasGetter == hasGetter && x.HasSetter == hasSetter);
                if (shared is not null)
                {
                    items.Add(shared);
                    continue;
                }

                Delegate? getter = null;
                Func<object, object?>? getterBoxed = null;

                Delegate? setter = null;
                Action<object, object?>? setterBoxed = null;

                if (!property.PropertyType.IsByRef && !property.PropertyType.IsByRefLike)
                {
                    if (hasGetter)
                    {
                        getter = AccessorGenerator.GenerateGetter(property, property.PropertyType);
                        getterBoxed = AccessorGenerator.GenerateGetter(property);
                    }
                    if (property.SetMethod is not null)
                    {
                        setter = AccessorGenerator.GenerateSetter(property, property.PropertyType);
                        setterBoxed = AccessorGenerator.GenerateSetter(property);
                    }
                    else if (backingField is not null)
                    {
                        setter = AccessorGenerator.GenerateSetter(backingField, backingField.FieldType);
                        setterBoxed = AccessorGenerator.GenerateSetter(backingField);
                    }
                }

                var attributes = GenerateAttributes(property);

                var isStatic = property.GetMethod?.IsStatic ?? property.SetMethod?.IsStatic ?? false;

                var allowsNull = GetAllowsNull(property, property.PropertyType);

                MemberDetail member;
                if (property.PropertyType.ContainsGenericParameters || property.PropertyType.IsByRef || property.PropertyType.IsByRefLike)
                {
                    member = new MemberDetail(type, property.PropertyType, property.Name, false, getter, getterBoxed, setter, setterBoxed, attributes, isStatic, allowsNull);
                }
                else
                {
                    var memberDetailGenericType = memberDetailType.MakeGenericType(property.PropertyType);
                    var constructor = memberDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                    member = (MemberDetail)constructor.Invoke([type, property.Name, false, getter, getterBoxed, setter, setterBoxed, attributes, isStatic, allowsNull]);
                }
                items.Add(member);
            }

            foreach (var @field in fields)
            {
                if (@field.IsLiteral)
                    continue;
                if (@field.FieldType.IsPointer)
                    continue;
                //an enum's value__ and the compiler's backing fields aren't declared members
                if (@field.IsSpecialName || @field.Name.StartsWith("<", StringComparison.Ordinal))
                    continue;
                if (!names.Add(@field.Name))
                    continue;

                //a static readonly field is treated as a constant once its type is initialized, setting it isn't reliable
                var hasSetter = !(@field.IsInitOnly && @field.IsStatic);

                var shared = members.FirstOrDefault(x => x.Name == @field.Name && x.IsField && x.HasSetter == hasSetter);
                if (shared is not null)
                {
                    items.Add(shared);
                    continue;
                }

                Delegate? getter = null;
                Func<object, object?>? getterBoxed = null;

                Delegate? setter = null;
                Action<object, object?>? setterBoxed = null;

                if (!@field.FieldType.IsByRef && !@field.FieldType.IsByRefLike)
                {
                    getter = AccessorGenerator.GenerateGetter(@field, @field.FieldType);
                    getterBoxed = AccessorGenerator.GenerateGetter(@field);

                    if (hasSetter)
                    {
                        setter = AccessorGenerator.GenerateSetter(@field, @field.FieldType);
                        setterBoxed = AccessorGenerator.GenerateSetter(@field);
                    }
                }

                var attributes = GenerateAttributes(@field);

                var allowsNull = GetAllowsNull(@field, @field.FieldType);

                MemberDetail member;
                if (@field.FieldType.ContainsGenericParameters || @field.FieldType.IsByRef || @field.FieldType.IsByRefLike)
                {
                    member = new MemberDetail(type, @field.FieldType, @field.Name, true, getter, getterBoxed, setter, setterBoxed, attributes, @field.IsStatic, allowsNull);
                }
                else
                {
                    var memberDetailGenericType = memberDetailType.MakeGenericType(@field.FieldType);
                    var constructor = memberDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                    member = (MemberDetail)constructor.Invoke([type, @field.Name, true, getter, getterBoxed, setter, setterBoxed, attributes, @field.IsStatic, allowsNull]);
                }
                items.Add(member);
            }

            //an interface gets the members of the interfaces it extends
            if (type.IsInterface)
            {
                foreach (var i in interfaces)
                {
                    foreach (var property in i.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (property.GetIndexParameters().Length > 0)
                            continue;
                        if (property.PropertyType.IsPointer)
                            continue;
                        if (!names.Add(property.Name))
                            continue;

                        var shared = members.FirstOrDefault(x => x.Name == property.Name && !x.IsField && x.HasGetter == (property.GetMethod is not null) && x.HasSetter == (property.SetMethod is not null));
                        if (shared is not null)
                        {
                            items.Add(shared);
                            continue;
                        }

                        Delegate? getter = null;
                        Func<object, object?>? getterBoxed = null;

                        Delegate? setter = null;
                        Action<object, object?>? setterBoxed = null;

                        if (!property.PropertyType.IsByRef && !property.PropertyType.IsByRefLike)
                        {
                            if (property.GetMethod is not null)
                            {
                                getter = AccessorGenerator.GenerateGetter(property, property.PropertyType);
                                getterBoxed = AccessorGenerator.GenerateGetter(property);
                            }
                            if (property.SetMethod is not null)
                            {
                                setter = AccessorGenerator.GenerateSetter(property, property.PropertyType);
                                setterBoxed = AccessorGenerator.GenerateSetter(property);
                            }
                        }

                        var attributes = GenerateAttributes(property);

                        var allowsNull = GetAllowsNull(property, property.PropertyType);

                        MemberDetail member;
                        if (property.PropertyType.ContainsGenericParameters || property.PropertyType.IsByRef || property.PropertyType.IsByRefLike)
                        {
                            member = new MemberDetail(type, property.PropertyType, property.Name, false, getter, getterBoxed, setter, setterBoxed, attributes, false, allowsNull);
                        }
                        else
                        {
                            var memberDetailGenericType = memberDetailType.MakeGenericType(property.PropertyType);
                            var constructor = memberDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                            member = (MemberDetail)constructor.Invoke([type, property.Name, false, getter, getterBoxed, setter, setterBoxed, attributes, false, allowsNull]);
                        }
                        items.Add(member);
                    }
                }
            }

            return items;
        }

#if !NETSTANDARD2_0
        [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
        [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
#endif
        public static List<MethodDetail> GenerateFullAccessMethods(Type type, IReadOnlyCollection<Type> interfaces, IReadOnlyList<MethodDetail> methods)
        {
            var items = new List<MethodDetail>();

            if (type.IsGenericTypeDefinition)
                return items;

            //a base type's private methods aren't listed, they're on the base type's own detail
            var allMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy).AsEnumerable();
            //an interface gets the methods of the interfaces it extends
            if (type.IsInterface)
                allMethods = allMethods.Concat(interfaces.SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)));

            foreach (var method in allMethods)
            {
                //lambdas and local functions are the compiler's
                if (method.Name.StartsWith("<", StringComparison.Ordinal))
                    continue;

                var parameters = method.GetParameters();

                var shared = methods.FirstOrDefault(x => x.Name == method.Name && x.IsStatic == method.IsStatic && x.Parameters.Select(p => p.Type).SequenceEqual(parameters.Select(p => p.ParameterType)));
                if (shared is not null)
                {
                    items.Add(shared);
                    continue;
                }

                var parameterTypes = parameters.Select(x => new ParameterDetail(x.ParameterType, x.Name!)).ToArray();

                var attributes = GenerateAttributes(method);

                Delegate? caller = null;
                Func<object?, object?[]?, object?>? callerBoxed = null;

                var genericArguments = method.GetGenericArguments();

                MethodDetail methodDetail;
                if (method.ReturnType.ContainsGenericParameters || method.ReturnType.IsPointer || method.ReturnType.IsByRef || method.ReturnType.IsByRefLike)
                {
                    methodDetail = new MethodDetail(type, method.Name, method.ReturnType, genericArguments, parameterTypes, caller, callerBoxed, attributes, method.IsStatic);
                }
                else
                {
                    caller = AccessorGenerator.GenerateCaller(method, method.ReturnType);
                    callerBoxed = AccessorGenerator.GenerateCaller(method);

                    var methodDetailGenericType = methodDetailType.MakeGenericType(method.ReturnType == typeof(void) ? typeof(object) : method.ReturnType);
                    var methodDetailConstructor = methodDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                    methodDetail = (MethodDetail)methodDetailConstructor.Invoke([type, method.Name, genericArguments, parameterTypes, caller, callerBoxed, attributes, method.IsStatic]);
                }

                items.Add(methodDetail);
            }

            return items;
        }
#if !NETSTANDARD2_0
        [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
        [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
#endif
        public static List<ConstructorDetail> GenerateFullAccessConstructors(Type type, IReadOnlyList<ConstructorDetail> constructors)
        {
            if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters || type.IsByRefLike || type == typeof(void))
                return [];
            return (List<ConstructorDetail>)generateFullAccessConstructorMethod.MakeGenericMethod(type).Invoke(null, [type, constructors])!;
        }

#if !NETSTANDARD2_0
        [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
        [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
#endif
        public static List<ConstructorDetail> GenerateFullAccessConstructorsGeneric<T>(Type type, IReadOnlyList<ConstructorDetail> constructors)
        {
            var items = new List<ConstructorDetail>();

            foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var parameters = constructor.GetParameters();
                if (parameters.Any(x => x.ParameterType.IsPointer || x.ParameterType.IsByRefLike))
                    continue;

                var parameterTypes = parameters.Select(x => new ParameterDetail(x.ParameterType, x.Name!)).ToArray();

                var shared = constructors.FirstOrDefault(x => x.Parameters.Select(p => p.Type).SequenceEqual(parameterTypes.Select(p => p.Type)));
                if (shared is not null)
                {
                    items.Add(shared);
                    continue;
                }

                Func<object?[]?, T>? creator = null;
                Func<object?[]?, object>? creatorBoxed = null;

                creator = AccessorGenerator.GenerateCreator<T>(constructor);
                if (creator == null)
                    continue;

                creatorBoxed = AccessorGenerator.GenerateCreator(constructor);
                if (creatorBoxed == null)
                    continue;

                var constructorDetail = new ConstructorDetail<T>(parameterTypes, creator, creatorBoxed);
                items.Add(constructorDetail);
            }
            return items;
        }
        public static Type[] GenerateInnerTypes(Type type)
        {
            if (type.IsGenericType)
                return type.GetGenericArguments();
            else if (type.IsArray)
                return [type.GetElementType()!];
            else
                return Array.Empty<Type>();
        }

        public static List<Type> GenerateBaseTypes(Type type)
        {
            var items = new List<Type>();
            var baseType = type.BaseType;
            while (baseType is not null)
            {
                items.Add(baseType);
                baseType = baseType.BaseType;
            }
            return items;
        }

#if !NETSTANDARD2_0
        [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
#endif
        //an internal interface such as IUtfChar<byte> can't be named by generated code, a public one over a private model can
        public static Type[] GenerateInterfaces(Type type)
        {
            return type.GetInterfaces().Where(x => (x.IsGenericType ? x.GetGenericTypeDefinition() : x).IsVisible).ToArray();
        }

        //the attributes the source generator sees and can write: not the compiler's, the debugger's, COM interop's, or declarative security, which Roslyn doesn't read as attributes,
        //not the [Serializable] flag or the [DefaultMember] for an indexer, which it reports differently, and only public ones whose arguments are public types
        public static Attribute[] GenerateAttributes(MemberInfo member)
        {
            HashSet<Type>? hiddenArguments = null;
            foreach (var data in member.GetCustomAttributesData())
            {
                if (data.ConstructorArguments.Any(x => !x.ArgumentType.IsVisible) || data.NamedArguments.Any(x => !x.TypedValue.ArgumentType.IsVisible))
                    (hiddenArguments ??= new()).Add(data.AttributeType);
            }

            var attributes = new List<Attribute>();
            foreach (Attribute attribute in member.GetCustomAttributes(true))
            {
                var attributeType = attribute.GetType();
                var ns = attributeType.Namespace;
                if (!attributeType.IsVisible || attributeType == typeof(SerializableAttribute) || attributeType == typeof(DefaultMemberAttribute) || (hiddenArguments is not null && hiddenArguments.Contains(attributeType)))
                    continue;
                if (ns is not null && (ns.StartsWith("System.Runtime.CompilerServices") || ns.StartsWith("System.Diagnostics") || ns.StartsWith("System.Security.Permissions") || ns.StartsWith("System.Runtime.InteropServices")))
                    continue;
                attributes.Add(attribute);
            }
            return attributes.ToArray();
        }

        //nullable annotations compile to NullableAttribute on the member, or NullableContextAttribute on an enclosing type for members without one,
        //read by name since .NET Framework builds declare these attributes in their own assembly
        private static bool? GetAllowsNull(MemberInfo member, Type memberType)
        {
            if (memberType.IsValueType)
                return null;

            byte? flag = null;
            foreach (var data in member.GetCustomAttributesData())
            {
                if (data.AttributeType.FullName != "System.Runtime.CompilerServices.NullableAttribute" || data.ConstructorArguments.Count != 1)
                    continue;
                var value = data.ConstructorArguments[0].Value;
                if (value is byte single)
                    flag = single;
                else if (value is IReadOnlyCollection<CustomAttributeTypedArgument> many && many.Count > 0 && many.First().Value is byte first)
                    flag = first;
                break;
            }

            for (var type = member.DeclaringType; flag is null && type is not null; type = type.DeclaringType)
            {
                foreach (var data in type.GetCustomAttributesData())
                {
                    if (data.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute" && data.ConstructorArguments.Count == 1 && data.ConstructorArguments[0].Value is byte context)
                    {
                        flag = context;
                        break;
                    }
                }
            }

            return flag == 1 ? false : null;
        }
    }
}
