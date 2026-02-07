// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Zerra.Reflection.Dynamic
{
    [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
    internal static class TypeDetailGenerator
    {
        private static readonly string nullaleTypeName = typeof(Nullable<>).Name;

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
#if NET5_0_OR_GREATER
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

        private static readonly MethodInfo generateTypeDetailGeneric = typeof(TypeDetailGenerator).GetMethod(nameof(Generate), BindingFlags.NonPublic | BindingFlags.Static)!;
        public static TypeDetail GenerateTypeDetail(Type type)
        {
            if (type.ContainsGenericParameters)
            {
                return GenerateIncompleteGeneric(type);
            }
            else
            {
                var method = generateTypeDetailGeneric.MakeGenericMethod(type);
                return (TypeDetail)method.Invoke(null, [type])!;
            }
        }

        private static TypeDetail GenerateIncompleteGeneric(Type type)
        {
            var constructors = new List<ConstructorDetail>(0);
            var innerTypes = GenerateInnerTypes(type);
            var baseTypes = GenerateBaseTypes(type);
            var interfaces = GenerateInterfaces(type);
            var attributes = GenerateAttributes(type);
            var members = GenerateMembers(type, interfaces);
            var methods = GenerateMethods(type, interfaces);

            Delegate? creator = null;
            Func<object>? creatorBoxed = null;

            Type? innerType = null;
            if (innerTypes.Length == 1)
                innerType = innerTypes[0];

            var isNullable = type.Name == nullaleTypeName;

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
                members: members,
                constructors: constructors,
                methods: methods,
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
                attributes: attributes
            );
            return typeDetail;
        }
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

            if (!type.IsAbstract && !type.IsGenericTypeDefinition && !type.IsInterface)
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

            var isNullable = type.Name == nullaleTypeName;

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

        public static List<MemberDetail> GenerateMembers(Type type, IReadOnlyCollection<Type> interfaces)
        {
            var items = new List<MemberDetail>();

            if (type.IsGenericTypeDefinition)
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

                //try backing field pattern <{property.Name}>k__BackingField or <{property.Name}>i__Field
                var backingName = $"<{property.Name}>";
                var backingField = fields.FirstOrDefault(x => x.Name.StartsWith(backingName) && x.FieldType == property.PropertyType);
                //try same name without underscores case insensitive
                backingField ??= fields.FirstOrDefault(x => MemberAndParameterNameComparer.Instance.Equals(x.Name, property.Name) && x.FieldType == property.PropertyType);

                Delegate? getter = null;
                Func<object, object?>? getterBoxed = null;

                Delegate? setter = null;
                Action<object, object?>? setterBoxed = null;

                if (backingField != null && !backingField.IsLiteral)
                {
                    _ = fields.Remove(backingField);

                    getter = AccessorGenerator.GenerateGetter(backingField, backingField.FieldType);
                    getterBoxed = AccessorGenerator.GenerateGetter(backingField);

                    setter = AccessorGenerator.GenerateSetter(backingField, backingField.FieldType);
                    setterBoxed = AccessorGenerator.GenerateSetter(backingField);
                }
                else
                {
                    if (property.GetMethod != null && property.GetMethod.IsPublic)
                    {
                        getter = AccessorGenerator.GenerateGetter(property, property.PropertyType);
                        getterBoxed = AccessorGenerator.GenerateGetter(property);
                    }
                    if (property.SetMethod != null && property.SetMethod.IsPublic)
                    {
                        setter = AccessorGenerator.GenerateSetter(property, property.PropertyType);
                        setterBoxed = AccessorGenerator.GenerateSetter(property);
                    }
                }

                var attributes = property.GetCustomAttributes(true).Cast<Attribute>().ToArray();

                var isStatic = property.GetMethod?.IsStatic ?? property.SetMethod?.IsStatic ?? false;

                MemberDetail member;
                if (property.PropertyType.ContainsGenericParameters || property.PropertyType.IsPointer || property.PropertyType.IsByRef || property.PropertyType.IsByRefLike)
                {
                    member = new MemberDetail(type, property.PropertyType, property.Name, false, getter, getterBoxed, setter, setterBoxed, attributes, backingField != null, isStatic, false);
                }
                else
                {
                    var memberDetailGenericType = memberDetailType.MakeGenericType(property.PropertyType);
                    var constructor = memberDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                    member = (MemberDetail)constructor.Invoke([type, property.Name, false, getter, getterBoxed, setter, setterBoxed, attributes, backingField != null, isStatic, false]);
                }
                items.Add(member);

                if (hasInterfaces)
                    _ = names!.Add(property.Name);
            }

            foreach (var @field in fields)
            {
                if (@field.IsLiteral)
                    continue;
                if (field.IsPrivate)
                    continue;

                Delegate? getter = null;
                Func<object, object?>? getterBoxed = null;

                Delegate? setter = null;
                Action<object, object?>? setterBoxed = null;

                getter = AccessorGenerator.GenerateGetter(@field, @field.FieldType);
                getterBoxed = AccessorGenerator.GenerateGetter(@field);

                setter = AccessorGenerator.GenerateSetter(@field, @field.FieldType);
                setterBoxed = AccessorGenerator.GenerateSetter(@field);

                var attributes = @field.GetCustomAttributes(true).Cast<Attribute>().ToArray();

                MemberDetail member;
                if (@field.FieldType.ContainsGenericParameters || @field.FieldType.IsPointer || @field.FieldType.IsByRef || @field.FieldType.IsByRefLike)
                {
                    member = new MemberDetail(type, field.FieldType, field.Name, true, getter, getterBoxed, setter, setterBoxed, attributes, true, field.IsStatic, false);
                }
                else
                {
                    var memberDetailGenericType = memberDetailType.MakeGenericType(@field.FieldType);
                    var constructor = memberDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                    member = (MemberDetail)constructor.Invoke([type, field.Name, true, getter, getterBoxed, setter, setterBoxed, attributes, true, field.IsStatic, false]);
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
                        if (property.GetIndexParameters().Length > 0)
                            continue;
                        if (property.PropertyType.IsPointer)
                            continue;
                        if (property.GetMethod?.IsPublic != true && property.SetMethod?.IsPublic != true)
                            continue;

                        string name;
                        bool isExplicitFromInterface;
                        if (type.IsInterface && !names!.Contains(property.Name))
                        {
                            name = property.Name;
                            isExplicitFromInterface = false;
                        }
                        else
                        {
                            name = $"{property.DeclaringType?.Namespace}.{property.DeclaringType?.Name}.{property.Name.Split('.').Last()}";
                            isExplicitFromInterface = true;
                        }

                        if (names!.Add(name))
                        {
                            Delegate? getter = null;
                            Func<object, object?>? getterBoxed = null;

                            Delegate? setter = null;
                            Action<object, object?>? setterBoxed = null;

                            if (property.GetMethod != null && property.GetMethod.IsPublic)
                            {
                                getter = AccessorGenerator.GenerateGetter(property, property.PropertyType);
                                getterBoxed = AccessorGenerator.GenerateGetter(property);
                            }
                            if (property.SetMethod != null && property.SetMethod.IsPublic)
                            {
                                setter = AccessorGenerator.GenerateSetter(property, property.PropertyType);
                                setterBoxed = AccessorGenerator.GenerateSetter(property);
                            }

                            var attributes = property.GetCustomAttributes(true).Cast<Attribute>().ToArray();

                            var isStatic = property.GetMethod?.IsStatic ?? property.SetMethod?.IsStatic ?? false;
                            MemberDetail member;
                            if (property.PropertyType.ContainsGenericParameters || property.PropertyType.IsPointer || property.PropertyType.IsByRef || property.PropertyType.IsByRefLike)
                            {
                                member = new MemberDetail(type, property.PropertyType, name, false, getter, getterBoxed, setter, setterBoxed, attributes, false, isStatic, isExplicitFromInterface);
                            }
                            else
                            {
                                var memberDetailGenericType = memberDetailType.MakeGenericType(property.PropertyType);
                                var constructor = memberDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                                member = (MemberDetail)constructor.Invoke([type, name, false, getter, getterBoxed, setter, setterBoxed, attributes, false, isStatic, isExplicitFromInterface]);
                            }
                            items.Add(member);
                        }
                    }
                }
            }

            return items;
        }
        public static List<ConstructorDetail> GenerateConstructors(Type type)
        {
            return (List<ConstructorDetail>)generateConstructorMethod.MakeGenericMethod(type).Invoke(null, [type])!;
        }
        private static MethodInfo generateConstructorMethod = typeof(TypeDetailGenerator).GetMethod(nameof(GenerateConstructorsGeneric), BindingFlags.Public | BindingFlags.Static)!;
        public static List<ConstructorDetail> GenerateConstructorsGeneric<T>(Type type)
        {
            var items = new List<ConstructorDetail>();
            if (type.IsGenericTypeDefinition)
                return items;

            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            foreach (var constructor in constructors)
            {
                if (!constructor.IsPublic)
                    continue;

                Func<object?[]?, T>? creator = AccessorGenerator.GenerateCreator<T>(constructor);
                if (creator == null)
                    continue;

                Func<object?[]?, object>? creatorBoxed = AccessorGenerator.GenerateCreator(constructor);
                if (creatorBoxed == null)
                    continue;

                var parameters = constructor.GetParameters();
                var parameterTypes = parameters.Select(x => new ParameterDetail(x.ParameterType, x.Name!)).ToArray();

                var constructorDetail = new ConstructorDetail<T>(parameterTypes, creator, creatorBoxed);
                items.Add(constructorDetail);
            }
            return items;
        }
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

                    var parameters = method.GetParameters();
                    var parameterTypes = parameters.Select(x => new ParameterDetail(x.ParameterType, x.Name!)).ToArray();

                    var attributes = method.GetCustomAttributes(true).Cast<Attribute>().ToArray();

                    Delegate? caller = AccessorGenerator.GenerateCaller(method, method.ReturnType);

                    Func<object?, object?[]?, object?>? callerBoxed = AccessorGenerator.GenerateCaller(method);

                    MethodDetail methodDetail;
                    if (method.ReturnType.ContainsGenericParameters || method.ReturnType.IsPointer || method.ReturnType.IsByRef || method.ReturnType.IsByRefLike)
                    {
                        methodDetail = new MethodDetail(type, method.Name, method.ReturnType, method.GetGenericArguments().Length, parameterTypes, caller, callerBoxed, attributes, method.IsStatic, false);
                    }
                    else
                    {
                        var methodDetailGenericType = methodDetailType.MakeGenericType(method.ReturnType.Name == "Void" ? typeof(object) : method.ReturnType);
                        var constructor = methodDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                        methodDetail = (MethodDetail)constructor.Invoke([type, method.Name, method.GetGenericArguments().Length, parameterTypes, caller, callerBoxed, attributes, method.IsStatic, false]);
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
                            string name;
                            bool isExplicitFromInterface;
                            if (type.IsInterface && !names!.Contains(method.Name))
                            {
                                name = method.Name;
                                isExplicitFromInterface = false;
                            }
                            else
                            {
                                name = $"{method.DeclaringType?.Namespace}.{method.DeclaringType?.Name}.{method.Name.Split('.').Last()}";
                                isExplicitFromInterface = true;
                            }

                            if (!names!.Contains(name))
                            {
                                var parameters = method.GetParameters();
                                var parameterTypes = parameters.Select(x => new ParameterDetail(x.ParameterType, x.Name!)).ToArray();

                                var attributes = method.GetCustomAttributes(true).Cast<Attribute>().ToArray();

                                Delegate? caller = AccessorGenerator.GenerateCaller(method, method.ReturnType);
                                if (caller == null)
                                    continue;

                                Func<object?, object?[]?, object?>? callerBoxed = AccessorGenerator.GenerateCaller(method);
                                if (callerBoxed == null)
                                    continue;

                                MethodDetail methodDetail;
                                if (method.ReturnType.ContainsGenericParameters || method.ReturnType.IsPointer || method.ReturnType.IsByRef || method.ReturnType.IsByRefLike)
                                {
                                    methodDetail = new MethodDetail(type, name, method.ReturnType, method.GetGenericArguments().Length, parameterTypes, caller, callerBoxed, attributes, method.IsStatic, false);
                                }
                                else
                                {
                                    var methodDetailGenericType = methodDetailType.MakeGenericType(method.ReturnType.Name == "Void" ? typeof(object) : method.ReturnType);
                                    var constructor = methodDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                                    methodDetail = (MethodDetail)constructor.Invoke([type, name, method.GetGenericArguments().Length, parameterTypes, caller, callerBoxed, attributes, method.IsStatic, isExplicitFromInterface]);
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
        public static Type[] GenerateInterfaces(Type type)
        {
            return type.GetInterfaces();
        }
        public static Attribute[] GenerateAttributes(Type type)
        {
            return type.GetCustomAttributes(true).Cast<Attribute>().ToArray();
        }
    }
}
