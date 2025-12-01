// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Zerra.SourceGeneration.Types;

namespace Zerra.SourceGeneration.Reflection
{
    [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
    internal static class TypeDetailGenerator
    {
        private static readonly string nullaleTypeName = typeof(Nullable<>).Name;

        private static readonly string enumberableTypeName = nameof(IEnumerable);
        private static readonly string enumberableGenericTypeName = typeof(IEnumerable<>).Name;

        private static readonly string collectionTypeName = nameof(ICollection);
        private static readonly string collectionGenericTypeName = typeof(ICollection<>).Name;
        private static readonly string readOnlyCollectionGenericTypeName = typeof(IReadOnlyCollection<>).Name;
        private static readonly string listTypeName = nameof(IList);
        private static readonly string listGenericTypeName = typeof(IList<>).Name;
        private static readonly string readOnlyListTypeName = typeof(IReadOnlyList<>).Name;
        private static readonly string setGenericTypeName = typeof(ISet<>).Name;
#if NET5_0_OR_GREATER
        private static readonly string readOnlySetGenericTypeName = typeof(IReadOnlySet<>).Name;
#else
        private static readonly string readOnlySetGenericTypeName = "IReadOnlySet`1";
#endif
        private static readonly string dictionaryTypeName = typeof(IDictionary).Name;
        private static readonly string dictionaryGenericTypeName = typeof(IDictionary<,>).Name;
        private static readonly string readOnlyDictionaryGenericTypeName = typeof(IReadOnlyDictionary<,>).Name;

        private static readonly Type keyValuePairType = typeof(KeyValuePair<,>);
        private static readonly Type dictionaryEntryType = typeof(DictionaryEntry);
        private static readonly Type memberDetailType = typeof(MemberDetail<>);
        private static readonly Type methodDetailType = typeof(MethodDetail<>);

        private static readonly MethodInfo generateTypeDetailGeneric = typeof(TypeDetailGenerator).GetMethod(nameof(GenerateTypeDetailGeneric), BindingFlags.NonPublic | BindingFlags.Static)!;
        public static TypeDetail GenerateTypeDetail(Type type)
        {
            var method = generateTypeDetailGeneric.MakeGenericMethod(type);
            return (TypeDetail)method.Invoke(null, [type])!;
        }

        private static TypeDetail<T> GenerateTypeDetailGeneric<T>(Type type)
        {
            var constructors = GenerateConstructors<T>(type);
            var innerTypes = GenerateInnerTypes(type);
            var baseTypes = GenerateBaseTypes(type);
            var interfaces = GenerateInterfaces(type);
            var attributes = GenerateAttributes(type);
            var members = GenerateMembers(type, interfaces);
            var methods = GetMethodDetails(type, interfaces);

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
            if (TypeLookup.CoreTypeLookup(type, out var coreTypeLookup))
                coreType = coreTypeLookup;

            SpecialType? specialType = null;
            if (TypeLookup.SpecialTypeLookup(type, out var specialTypeLookup))
                specialType = specialTypeLookup;

            CoreEnumType? enumType = null;
            if (type.IsEnum)
            {
                var enumEnderlyingType = Enum.GetUnderlyingType(type);
                if (!TypeLookup.CoreEnumTypeLookup(enumEnderlyingType, out var enumCoreTypeLookup))
                    throw new NotImplementedException("Should not happen");
                enumType = enumCoreTypeLookup;
            }
            else if (isNullable && innerTypes[0].IsEnum)
            {
                var enumEnderlyingType = Enum.GetUnderlyingType(innerTypes[0]);
                if (!TypeLookup.CoreEnumTypeLookup(enumEnderlyingType, out var enumCoreTypeLookup))
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

            var hasIEnumerable = type.IsArray || type.Name == enumberableTypeName || interfaceNames.Contains(enumberableTypeName);
            var hasIEnumerableGeneric = type.IsArray || type.Name == enumberableGenericTypeName || interfaceNames.Contains(enumberableGenericTypeName);
            var hasICollection = type.Name == collectionTypeName || interfaceNames.Contains(collectionTypeName);
            var hasICollectionGeneric = type.Name == collectionGenericTypeName || interfaceNames.Contains(collectionGenericTypeName);
            var hasIReadOnlyCollectionGeneric = type.Name == readOnlyCollectionGenericTypeName || interfaceNames.Contains(readOnlyCollectionGenericTypeName);
            var hasIList = type.Name == listTypeName || interfaceNames.Contains(listTypeName);
            var hasIListGeneric = type.Name == listGenericTypeName || interfaceNames.Contains(listGenericTypeName);
            var hasIReadOnlyListGeneric = type.Name == readOnlyListTypeName || interfaceNames.Contains(readOnlyListTypeName);
            var hasISetGeneric = type.Name == setGenericTypeName || interfaceNames.Contains(setGenericTypeName);
            var hasIReadOnlySetGeneric = type.Name == readOnlySetGenericTypeName || interfaceNames.Contains(readOnlySetGenericTypeName);
            var hasIDictionary = type.Name == dictionaryTypeName || interfaceNames.Contains(dictionaryTypeName);
            var hasIDictionaryGeneric = type.Name == dictionaryGenericTypeName || interfaceNames.Contains(dictionaryGenericTypeName);
            var hasIReadOnlyDictionaryGeneric = type.Name == readOnlyDictionaryGenericTypeName || interfaceNames.Contains(readOnlyDictionaryGenericTypeName);

            var isIEnumerable = type.Name == enumberableTypeName;
            var isIEnumerableGeneric = type.Name == enumberableGenericTypeName;
            var isICollection = type.Name == collectionTypeName;
            var isICollectionGeneric = type.Name == collectionGenericTypeName;
            var isIReadOnlyCollectionGeneric = type.Name == readOnlyCollectionGenericTypeName;
            var isIList = type.Name == listTypeName;
            var isIListGeneric = type.Name == listGenericTypeName;
            var isIReadOnlyListGeneric = type.Name == readOnlyListTypeName;
            var isISetGeneric = type.Name == setGenericTypeName;
            var isIReadOnlySetGeneric = type.Name == readOnlySetGenericTypeName;
            var isIDictionary = type.Name == dictionaryTypeName;
            var isIDictionaryGeneric = type.Name == dictionaryGenericTypeName;
            var isIReadOnlyDictionaryGeneric = type.Name == readOnlyDictionaryGenericTypeName;

            Type? iEnumerableGenericInnerType = null;
            if (isIEnumerableGeneric)
            {
                iEnumerableGenericInnerType = innerTypes[0];
            }
            else
            {
                var interfaceFound = interfaces.Where(x => x.Name == enumberableGenericTypeName).ToArray();
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
                var interfaceFound = interfaces.Where(x => x.Name == dictionaryGenericTypeName).ToArray();
                if (interfaceFound.Length == 1)
                {
                    dictionaryInnerType = keyValuePairType.MakeGenericType(interfaceFound[0].GetGenericArguments());
                }
            }
            else if (hasIReadOnlyDictionaryGeneric)
            {
                var interfaceFound = interfaces.Where(x => x.Name == readOnlyDictionaryGenericTypeName).ToArray();
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
                members,
                constructors,
                methods,
                creator,
                creatorBoxed,
                isNullable,
                coreType,
                specialType,
                enumType,
                hasIEnumerable,
                hasIEnumerableGeneric,
                hasICollection,
                hasICollectionGeneric,
                hasIReadOnlyCollectionGeneric,
                hasIList,
                hasIListGeneric,
                hasIReadOnlyListGeneric,
                hasISetGeneric,
                hasIReadOnlySetGeneric,
                hasIDictionary,
                hasIDictionaryGeneric,
                hasIReadOnlyDictionaryGeneric,
                isIEnumerable,
                isIEnumerableGeneric,
                isICollection,
                isICollectionGeneric,
                isIReadOnlyCollectionGeneric,
                isIList,
                isIListGeneric,
                isIReadOnlyListGeneric,
                isISetGeneric,
                isIReadOnlySetGeneric,
                isIDictionary,
                isIDictionaryGeneric,
                isIReadOnlyDictionaryGeneric,
                innerType,
                iEnumerableGenericInnerType,
                dictionaryInnerType,
                innerTypes,
                baseTypes,
                interfaces,
                attributes
            );
            return typeDetail;
        }

        private static List<MemberDetail> GenerateMembers(Type type, Type[] interfaces)
        {
            var items = new List<MemberDetail>();

            if (type.IsGenericTypeDefinition)
                return items;

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).ToList();

            var hasInterfaces = interfaces.Length > 0;
            var names = hasInterfaces ? new HashSet<string>() : null; //explicit declarations can create duplicates with interfaces

            foreach (var property in properties)
            {
                if (property.GetIndexParameters().Length > 0)
                    continue;

                //<{property.Name}>k__BackingField
                //<{property.Name}>i__Field
                var backingName = $"<{property.Name}>";
                var backingField = fields.FirstOrDefault(x => x.Name.StartsWith(backingName));

                Delegate? getter = null;
                Func<object, object?>? getterBoxed = null;

                Delegate? setter = null;
                Action<object, object?>? setterBoxed = null;

                if (!property.PropertyType.IsPointer)
                {
                    if (backingField != null)
                    {
                        _ = fields.Remove(backingField);

                        if (!backingField.IsLiteral)
                        {
                            getter = AccessorGenerator.GenerateGetter(backingField, backingField.FieldType);
                            getterBoxed = AccessorGenerator.GenerateGetter(backingField);

                            setter = AccessorGenerator.GenerateSetter(backingField, backingField.FieldType);
                            setterBoxed = AccessorGenerator.GenerateSetter(backingField);
                        }
                    }
                    else
                    {
                        if (property.GetMethod != null)
                        {
                            getter = AccessorGenerator.GenerateGetter(property, property.PropertyType);
                            getterBoxed = AccessorGenerator.GenerateGetter(property);
                        }
                        if (property.SetMethod != null)
                        {
                            setter = AccessorGenerator.GenerateSetter(property, property.PropertyType);
                            setterBoxed = AccessorGenerator.GenerateSetter(property);
                        }
                    }
                }

                var attributes = property.GetCustomAttributes(true).Cast<Attribute>().ToArray();

                var isStatic = property.GetMethod?.IsStatic ?? property.SetMethod?.IsStatic ?? false;

                var memberDetailGenericType = memberDetailType.MakeGenericType(property.PropertyType);
                var constructor = memberDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                var member = (MemberDetail)constructor.Invoke([property.PropertyType, property.Name, getter, getterBoxed, setter, setterBoxed, attributes, backingField != null, isStatic, false]);
                items.Add(member);

                if (hasInterfaces)
                    _ = names!.Add(property.Name);
            }

            foreach (var @field in fields)
            {
                Delegate? getter = null;
                Func<object, object?>? getterBoxed = null;

                Delegate? setter = null;
                Action<object, object?>? setterBoxed = null;

                if (!@field.IsLiteral)
                {
                    getter = AccessorGenerator.GenerateGetter(@field, @field.FieldType);
                    getterBoxed = AccessorGenerator.GenerateGetter(@field);

                    setter = AccessorGenerator.GenerateSetter(@field, @field.FieldType);
                    setterBoxed = AccessorGenerator.GenerateSetter(@field);
                }

                var attributes = @field.GetCustomAttributes(true).Cast<Attribute>().ToArray();

                var memberDetailGenericType = memberDetailType.MakeGenericType(@field.FieldType);
                var constructor = memberDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                var member = (MemberDetail)constructor.Invoke([@field.FieldType, @field.Name, getter, getterBoxed, setter, setterBoxed, attributes, true, field.IsStatic, false]);
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

                            if (!property.PropertyType.IsPointer)
                            {
                                getter = AccessorGenerator.GenerateGetter(property, property.PropertyType);
                                getterBoxed = AccessorGenerator.GenerateGetter(property);

                                setter = AccessorGenerator.GenerateSetter(property, property.PropertyType);
                                setterBoxed = AccessorGenerator.GenerateSetter(property);
                            }

                            var attributes = property.GetCustomAttributes(true).Cast<Attribute>().ToArray();

                            var isStatic = property.GetMethod?.IsStatic ?? property.SetMethod?.IsStatic ?? false;

                            var memberDetailGenericType = memberDetailType.MakeGenericType(property.PropertyType);
                            var constructor = memberDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                            var member = (MemberDetail)constructor.Invoke([property.PropertyType, name, getter, getterBoxed, setter, setterBoxed, attributes, false, isStatic, isExplicitFromInterface]);
                            items.Add(member);
                        }
                    }
                }
            }

            return items;
        }
        private static List<ConstructorDetail<T>> GenerateConstructors<T>(Type type)
        {
            var items = new List<ConstructorDetail<T>>();
            if (type.IsGenericTypeDefinition)
                return items;

            foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length == 0)
                    continue;

                Func<object?[], T>? creator = AccessorGenerator.GenerateCreator<T>(constructor);
                if (creator == null)
                    continue;

                Func<object?[], object>? creatorBoxed = AccessorGenerator.GenerateCreator(constructor);
                if (creatorBoxed == null)
                    continue;

                var parameterTypes = parameters.Select(x => new ParameterDetail(x.ParameterType, x.Name!)).ToArray();

                var constructorDetail = new ConstructorDetail<T>(parameterTypes, creator, creatorBoxed);
                items.Add(constructorDetail);
            }
            return items;
        }
        private static List<MethodDetail> GetMethodDetails(Type type, Type[] interfaces)
        {
            var items = new List<MethodDetail>();
            var hasInterfaces = interfaces.Length > 0;
            var names = hasInterfaces ? new HashSet<string>() : null; //explicit declarations can create duplicates with interfaces
            if (!type.IsGenericTypeDefinition)
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    var parameterTypes = parameters.Select(x => new ParameterDetail(x.ParameterType, x.Name!)).ToArray();

                    var attributes = method.GetCustomAttributes(true).Cast<Attribute>().ToArray();

                    Delegate? caller = AccessorGenerator.GenerateCaller(method, type, method.ReturnType);
                    Func<object, object?[], object?>? callerBoxed = AccessorGenerator.GenerateCaller(method);

                    var methodDetailGenericType = methodDetailType.MakeGenericType(method.ReturnType);
                    var constructor = methodDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                    var methodDetail = (MethodDetail)constructor.Invoke([parameterTypes, caller, callerBoxed, attributes, method.IsStatic, false]);
                    items.Add(methodDetail);
                    if (hasInterfaces)
                        names!.Add(method.Name);
                }

                if (hasInterfaces)
                {
                    foreach (var i in interfaces)
                    {
                        var iMethods = i.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); //don't get static interface methods
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

                                Delegate? caller = AccessorGenerator.GenerateCaller(method, type, method.ReturnType);
                                Func<object, object?[], object?>? callerBoxed = AccessorGenerator.GenerateCaller(method);

                                var methodDetailGenericType = methodDetailType.MakeGenericType(method.ReturnType);
                                var constructor = methodDetailGenericType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0]!;
                                var methodDetail = (MethodDetail)constructor.Invoke([parameterTypes, caller, callerBoxed, attributes, method.IsStatic, isExplicitFromInterface]);
                                items.Add(methodDetail);
                                if (hasInterfaces)
                                    names!.Add(method.Name);
                            }
                        }
                    }
                }
            }
            return items;
        }
        private static Type[] GenerateInnerTypes(Type type)
        {
            if (type.IsGenericType)
                return type.GetGenericArguments();
            else if (type.IsArray)
                return [type.GetElementType()!];
            else
                return Array.Empty<Type>();
        }
        private static List<Type> GenerateBaseTypes(Type type)
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
        private static Type[] GenerateInterfaces(Type type)
        {
            return type.GetInterfaces();
        }
        private static Attribute[] GenerateAttributes(Type type)
        {
            return type.GetCustomAttributes(true).Cast<Attribute>().ToArray();
        }
    }
}
