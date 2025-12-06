// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace Zerra.SourceGeneration
{
    public static class TypesGenerator
    {
        private static readonly string nullaleTypeName = typeof(Nullable<>).Name;
        private static readonly string iEnumberableTypeName = nameof(IEnumerable);
        private static readonly string iEnumberableGenericTypeName = typeof(IEnumerable<>).Name;

        private static readonly string iCollectionTypeName = nameof(ICollection);
        private static readonly string iCollectionGenericTypeName = typeof(ICollection<>).Name;
        private static readonly string iReadOnlyCollectionGenericTypeName = typeof(IReadOnlyCollection<>).Name;
        private static readonly string iListTypeName = nameof(IList);
        private static readonly string iListGenericTypeName = typeof(IList<>).Name;
        private static readonly string listGenericTypeName = typeof(List<>).Name;
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
        private static readonly string dictionaryGenericTypeName = typeof(Dictionary<,>).Name;

        public static void FindModels(ITypeSymbol typeSymbol, Dictionary<string, TypeToGenerate> models)
        {
            if (typeSymbol.TypeKind != TypeKind.Class && typeSymbol.TypeKind != TypeKind.Interface)
                return;
            if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                return;

            var stack = new Stack<string>();
            if ((namedTypeSymbol.AllInterfaces.Any(x => x.Name == "ICommand" || x.Name == "IEvent" || x.Name == "IQueryHandler" || x.Name == "IMapDefinition")
                || namedTypeSymbol.GetAttributes().Any(x => x.AttributeClass?.Name == "GenerateTypeDetailAttribute"))
                && !namedTypeSymbol.GetAttributes().Any(x => x.AttributeClass?.Name == "IgnoreGenerateTypeDetailAttribute"))
            {
                if (namedTypeSymbol.TypeKind == TypeKind.Interface)
                    SearchInterface(namedTypeSymbol, models, stack);
                else if (namedTypeSymbol.TypeKind == TypeKind.Class)
                    CheckModel(namedTypeSymbol, models, stack);
            }
        }
        public static void Generate(StringBuilder sb, Dictionary<string, TypeToGenerate> models)
        {
            foreach (var model in models.Values)
            {
                var namedTypeSymbol = model.TypeSymbol as INamedTypeSymbol;

                var typeOfName = $"typeof({model.TypeName})";
                var symbolMembers = model.TypeSymbol.GetMembers();

                CoreType? coreType = null;
                var isCoreType = TypeLookup.CoreTypeLookup(model.TypeName, out var coreTypeParsed);
                if (isCoreType)
                    coreType = coreTypeParsed;

                _ = sb.Append(Environment.NewLine).Append("            ");
                _ = sb.Append("//").Append(Helper.GetFullName(model.TypeSymbol) + " - " + model.Source);

                _ = sb.Append(Environment.NewLine).Append("            ");
                _ = sb.Append("global::Zerra.SourceGeneration.Register.Type(new global::Zerra.SourceGeneration.Types.TypeDetail<").Append(model.TypeName).Append(">(");

                GenerateMembers(sb, isCoreType, model.TypeName, namedTypeSymbol, symbolMembers);

                _ = sb.Append(", ");

                GenerateConstructors(sb, isCoreType, namedTypeSymbol, symbolMembers);

                _ = sb.Append(", ");

                GenerateMethods(sb, isCoreType, model.TypeName, namedTypeSymbol, symbolMembers);

                _ = sb.Append(", ");

                GenerateCreators(sb, model.TypeName, namedTypeSymbol);

                _ = sb.Append(", ");

                GenerateTypeInfo(sb, coreType, model.TypeName, model.TypeSymbol, namedTypeSymbol);

                _ = sb.Append(", ");

                GenerateInnerTypes(sb, model.TypeSymbol, namedTypeSymbol);

                _ = sb.Append(", ");

                GenerateBaseTypes(sb, model.TypeSymbol);

                _ = sb.Append(", ");

                GenerateInterfaces(sb, model.TypeSymbol);

                _ = sb.Append(", ");

                GenerateAttributes(sb, model.TypeSymbol);

                _ = sb.Append("));");
            }
        }

        private static void GenerateMembers(StringBuilder sb, bool isCoreType, string typeName, INamedTypeSymbol? namedTypeSymbol, ImmutableArray<ISymbol> symbolMembers)
        {
            _ = sb.Append("[");

            if (!isCoreType && namedTypeSymbol != null)
            {
                (var properties, var fields) = GetPropertiesAndFields(namedTypeSymbol, symbolMembers);
                var hasFirst = false;
                foreach (var propertySets in properties)
                {
                    var property = propertySets.Item1;
                    var propertyName = propertySets.Item2;
                    var isExplicitFromInterface = propertySets.Item3;
                    if (property.IsIndexer)
                        continue;
                    if (property.DeclaredAccessibility != Accessibility.Public)
                        continue;
                    if (isExplicitFromInterface)
                        continue;

                    //<{property.Name}>k__BackingField
                    //<{property.Name}>i__Field
                    var backingName = $"<{property.Name}>";
                    var backingField = fields.FirstOrDefault(x => x.Name.StartsWith(backingName));
                    var isBacked = backingField != null;

                    if (hasFirst)
                        _ = sb.Append(", ");
                    else
                        hasFirst = true;

                    var propertyTypeName = Helper.GetFullName(property.Type);

                    _ = sb.Append(Environment.NewLine).Append("                ");

                    _ = sb.Append("new global::Zerra.SourceGeneration.Types.MemberDetail<").Append(propertyTypeName).Append(">(");
                    _ = sb.Append(Helper.GetTypeOfName(namedTypeSymbol)).Append(", ");
                    _ = sb.Append("\"").Append(propertyName).Append("\", false, ");
                    if (property.GetMethod != null && property.GetMethod.DeclaredAccessibility == Accessibility.Public)
                    {
                        if (property.IsStatic)
                        {
                            _ = sb.Append("static (object x) => ").Append(typeName).Append(".").Append(property.Name).Append(", ");
                            _ = sb.Append("static (object x) => ").Append(typeName).Append(".").Append(property.Name).Append(", ");
                        }
                        else
                        {
                            _ = sb.Append("static (object x) => ((").Append(typeName).Append(")x)").Append(".").Append(property.Name).Append(", ");
                            _ = sb.Append("static (object x) => ((").Append(typeName).Append(")x)").Append(".").Append(property.Name).Append(", ");
                        }
                    }
                    else
                    {
                        _ = sb.Append("null, null, ");
                    }
                    if (property.SetMethod != null && property.SetMethod.DeclaredAccessibility == Accessibility.Public && !property.SetMethod.IsInitOnly)
                    {
                        if (property.IsStatic)
                        {
                            _ = sb.Append("static (object x, ").Append(propertyTypeName).Append(property.Type.IsValueType ? null : '?').Append(" value) => ").Append(typeName).Append(".").Append(property.Name).Append(" = value!, ");
                            _ = sb.Append("static (object x, object? value) => ((").Append(typeName).Append(")x).").Append(property.Name).Append(" = (").Append(propertyTypeName).Append(")value!, ");
                        }
                        else
                        {
                            _ = sb.Append("static (object x, ").Append(propertyTypeName).Append(property.Type.IsValueType ? null : '?').Append(" value) => ((").Append(typeName).Append(")x).").Append(property.Name).Append(" = value!, ");
                            _ = sb.Append("static (object x, object? value) => ((").Append(typeName).Append(")x).").Append(property.Name).Append(" = (").Append(propertyTypeName).Append(")value!, ");
                        }
                    }
                    else
                    {
                        _ = sb.Append("null, null, ");
                    }

                    GenerateAttributes(sb, property);

                    _ = sb.Append(", ").Append(Helper.BoolString(isBacked));
                    _ = sb.Append(", ").Append(Helper.BoolString(property.IsStatic));
                    _ = sb.Append(", ").Append(Helper.BoolString(isExplicitFromInterface)).Append(")");
                }
                foreach (var @field in fields)
                {
                    if (@field.DeclaredAccessibility != Accessibility.Public)
                        continue;

                    if (hasFirst)
                        _ = sb.Append(", ");
                    else
                        hasFirst = true;

                    var fieldTypeName = Helper.GetFullName(@field.Type);

                    _ = sb.Append(Environment.NewLine).Append("                ");

                    _ = sb.Append("new global::Zerra.SourceGeneration.Types.MemberDetail<").Append(fieldTypeName).Append(">(");
                    _ = sb.Append(Helper.GetTypeOfName(namedTypeSymbol)).Append(", ");
                    _ = sb.Append("\"").Append(@field.Name).Append("\", true, ");

                    if (@field.IsStatic)
                    {
                        _ = sb.Append("static (object x) => ").Append(typeName).Append(".").Append(@field.Name).Append(", ");
                        _ = sb.Append("static (object x) => ").Append(typeName).Append(".").Append(@field.Name).Append(", ");

                        _ = sb.Append("static (object x, ").Append(fieldTypeName).Append(@field.Type.IsValueType ? null : '?').Append(" value) => ").Append(typeName).Append(".").Append(@field.Name).Append(" = value!, ");
                        _ = sb.Append("static (object x, object? value) => ").Append(typeName).Append(".").Append(@field.Name).Append(" = (").Append(fieldTypeName).Append(")value!, ");
                    }
                    else
                    {
                        _ = sb.Append("static (object x) => ((").Append(typeName).Append(")x).").Append(@field.Name).Append(", ");
                        _ = sb.Append("static (object x) => ((").Append(typeName).Append(")x).").Append(@field.Name).Append(", ");

                        _ = sb.Append("static (object x, ").Append(fieldTypeName).Append(@field.Type.IsValueType ? null : '?').Append(" value) => ((").Append(typeName).Append(")x).").Append(@field.Name).Append(" = value!, ");
                        _ = sb.Append("static (object x, object? value) => ((").Append(typeName).Append(")x).").Append(@field.Name).Append(" = (").Append(fieldTypeName).Append(")value!, ");
                    }

                    GenerateAttributes(sb, @field);

                    _ = sb.Append(", true");
                    _ = sb.Append(", ").Append(Helper.BoolString(@field.IsStatic));
                    _ = sb.Append(", false)");
                }
            }

            _ = sb.Append("]");
        }
        private static void GenerateConstructors(StringBuilder sb, bool isCoreType, INamedTypeSymbol? namedTypeSymbol, ImmutableArray<ISymbol> symbolMembers)
        {
            _ = sb.Append("[");

            if (!isCoreType && namedTypeSymbol != null)
            {
                var constructors = GetConstructors(namedTypeSymbol, symbolMembers);
                foreach (var constructor in constructors)
                {
                    //TODO Constructors
                }
            }

            _ = sb.Append("]");
        }
        private static void GenerateMethods(StringBuilder sb, bool isCoreType, string typeName, INamedTypeSymbol? namedTypeSymbol, ImmutableArray<ISymbol> symbolMembers)
        {
            _ = sb.Append("[");

            if (!isCoreType && namedTypeSymbol != null)
            {
                var methods = GetMethods(namedTypeSymbol, symbolMembers);
                var hasFirst = false;
                foreach (var methodSet in methods)
                {
                    var method = methodSet.Item1;
                    var methodName = methodSet.Item2;
                    var isExplicitFromInterface = methodSet.Item3;

                    if (method.DeclaredAccessibility != Accessibility.Public)
                        continue;
                    if (method.MethodKind != MethodKind.Ordinary)
                        continue;
                    if (method.IsStatic) //the code can do this, just reducing size for now
                        continue;
                    if (method.IsGenericMethod)
                        continue;
                    if (isExplicitFromInterface)
                        continue;

                    if (hasFirst)
                        _ = sb.Append(", ");
                    else
                        hasFirst = true;

                    var isVoid = method.ReturnType.Name == "Void";
                    var methodReturnTypeName = isVoid ? "object?" : Helper.GetFullName(method.ReturnType);

                    _ = sb.Append(Environment.NewLine).Append("                ");

                    _ = sb.Append("new global::Zerra.SourceGeneration.Types.MethodDetail<").Append(methodReturnTypeName).Append(">(");
                    _ = sb.Append(Helper.GetTypeOfName(namedTypeSymbol)).Append(", ");
                    _ = sb.Append("\"").Append(methodName).Append("\", ");

                    GenerateParameters(sb, method.Parameters);
                    sb.Append(", ");

                    if (method.IsStatic)
                    {
                        _ = sb.Append("static (object? x, object?[]? args) => ");
                        if (isVoid)
                            _ = sb.Append("{ ");
                        _ = sb.Append(typeName).Append(".").Append(method.Name).Append("(");
                        foreach (var parameter in method.Parameters)
                        {
                            var parameterTypeName = Helper.GetFullName(parameter.Type);
                            if (parameter.Ordinal > 0)
                                _ = sb.Append(", ");
                            _ = sb.Append("(").Append(parameterTypeName).Append(")args![").Append(parameter.Ordinal).Append("]!");
                        }
                        _ = sb.Append(")");
                        if (isVoid)
                            _ = sb.Append("; return null; }");
                        _ = sb.Append(", ");

                        _ = sb.Append("static (object? x, object?[]? args) => ");
                        if (isVoid)
                            _ = sb.Append("{ ");
                        _ = sb.Append(typeName).Append(".").Append(method.Name).Append("(");
                        foreach (var parameter in method.Parameters)
                        {
                            var parameterTypeName = Helper.GetFullName(parameter.Type);
                            if (parameter.Ordinal > 0)
                                _ = sb.Append(", ");
                            _ = sb.Append("(").Append(parameterTypeName).Append(")args![").Append(parameter.Ordinal).Append("]!");
                        }
                        _ = sb.Append(")");
                        if (isVoid)
                            _ = sb.Append("; return null; }");
                        _ = sb.Append(", ");
                    }
                    else
                    {
                        _ = sb.Append("static (object? x, object?[]? args) => ");
                        if (isVoid)
                            _ = sb.Append("{ ");
                        _ = sb.Append("((").Append(typeName).Append(")x!)").Append(".").Append(method.Name).Append("(");
                        foreach (var parameter in method.Parameters)
                        {
                            var parameterTypeName = Helper.GetFullName(parameter.Type);
                            if (parameter.Ordinal > 0)
                                _ = sb.Append(", ");

                            switch (parameter.RefKind)
                            {
                                case RefKind.Ref:
                                    _ = sb.Append("ref (").Append(parameterTypeName).Append(")args![").Append(parameter.Ordinal).Append("]!");
                                    break;
                                case RefKind.In:
                                    _ = sb.Append("in (").Append(parameterTypeName).Append(")args![").Append(parameter.Ordinal).Append("]!");
                                    break;
                                case RefKind.Out:
                                    _ = sb.Append("out args![").Append(parameter.Ordinal).Append("]!");
                                    break;
                                case RefKind.None:
                                default:
                                    _ = sb.Append("(").Append(parameterTypeName).Append(")args![").Append(parameter.Ordinal).Append("]!");
                                    break;
                            }

                        }
                        _ = sb.Append(")");
                        if (isVoid)
                            _ = sb.Append("; return null; }");
                        _ = sb.Append(", ");

                        _ = sb.Append("static (object? x, object?[]? args) => ");
                        if (isVoid)
                            _ = sb.Append("{ ");
                        _ = sb.Append("((").Append(typeName).Append(")x!)").Append(".").Append(method.Name).Append("(");
                        foreach (var parameter in method.Parameters)
                        {
                            var parameterTypeName = Helper.GetFullName(parameter.Type);
                            if (parameter.Ordinal > 0)
                                _ = sb.Append(", ");
                            _ = sb.Append("(").Append(parameterTypeName).Append(")args![").Append(parameter.Ordinal).Append("]!");
                        }
                        _ = sb.Append(")");
                        if (isVoid)
                            _ = sb.Append("; return null; }");
                        _ = sb.Append(", ");
                    }

                    GenerateAttributes(sb, method);

                    _ = sb.Append(", ").Append(Helper.BoolString(method.IsStatic));
                    _ = sb.Append(", ").Append(Helper.BoolString(isExplicitFromInterface)).Append(")");
                }
            }

            _ = sb.Append("]");
        }
        private static void GenerateCreators(StringBuilder sb, string typeName, INamedTypeSymbol? namedTypeSymbol)
        {
            if (namedTypeSymbol != null && !namedTypeSymbol.IsAbstract && !namedTypeSymbol.IsUnboundGenericType && namedTypeSymbol.TypeKind != TypeKind.Interface)
            {
                if (namedTypeSymbol.Constructors.Any(x => x.Parameters.Length == 0))
                {
                    _ = sb.Append(Environment.NewLine).Append("                ");
                    _ = sb.Append("() => new ").Append(typeName).Append("(), ");
                    _ = sb.Append("() => new ").Append(typeName).Append("()");
                    return;
                }
                else if (namedTypeSymbol.IsValueType)
                {
                    _ = sb.Append(Environment.NewLine).Append("                ");
                    _ = sb.Append("() => default(").Append(typeName).Append(")!, ");
                    _ = sb.Append("() => default(").Append(typeName).Append(")!");
                    return;
                }
                else if (namedTypeSymbol.Name == "String")
                {
                    _ = sb.Append(Environment.NewLine).Append("                ");
                    _ = sb.Append("() => string.Empty, ");
                    _ = sb.Append("() => string.Empty");
                    return;
                }
            }

            _ = sb.Append("null, null");
        }
        private static void GenerateTypeInfo(StringBuilder sb, CoreType? coreType, string typeName, ITypeSymbol typeSymbol, INamedTypeSymbol? namedTypeSymbol)
        {
            var isNullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
            var isArray = typeSymbol.Kind == SymbolKind.ArrayType;
            var interfaceNames = typeSymbol.AllInterfaces.Select(x => x.MetadataName).ToImmutableHashSet();

            var baseTypeNames = new HashSet<string>();
            var baseTypeSymbol = typeSymbol.BaseType;
            while (baseTypeSymbol != null)
            {
                baseTypeNames.Add(baseTypeSymbol.MetadataName);
                baseTypeSymbol = baseTypeSymbol.BaseType;
            }

            SpecialType? specialType = null;
            if (TypeLookup.SpecialTypeLookup(typeName, out var specialTypeParsed))
                specialType = specialTypeParsed;

            CoreEnumType? enumType = null;
            if (namedTypeSymbol is not null)
            {
                if (isNullable && namedTypeSymbol.TypeArguments.Length == 1)
                {
                    var innerType = (INamedTypeSymbol)namedTypeSymbol.TypeArguments[0];
                    if (innerType.EnumUnderlyingType is not null)
                    {
                        if (!TypeLookup.CoreEnumTypeLookup(innerType.EnumUnderlyingType.Name, out var enumTypeParsed))
                            throw new InvalidOperationException($"Failed to get enum underlying type for {Helper.GetFullName(typeSymbol)} {innerType.EnumUnderlyingType.Name}");
                        enumType = enumTypeParsed;
                    }
                }
                else
                {
                    if (namedTypeSymbol.EnumUnderlyingType is not null)
                    {
                        if (!TypeLookup.CoreEnumTypeLookup(namedTypeSymbol.EnumUnderlyingType.Name, out var enumTypeParsed))
                            throw new InvalidOperationException($"Failed to get enum underlying type for {Helper.GetFullName(typeSymbol)} {namedTypeSymbol.EnumUnderlyingType.Name}");
                        enumType = enumTypeParsed;
                    }
                }
            }

            var hasIEnumerable = isArray || typeSymbol.MetadataName == iEnumberableTypeName || interfaceNames.Contains(iEnumberableTypeName);
            var hasIEnumerableGeneric = isArray || typeSymbol.MetadataName == iEnumberableGenericTypeName || interfaceNames.Contains(iEnumberableGenericTypeName);
            var hasICollection = typeSymbol.MetadataName == iCollectionTypeName || interfaceNames.Contains(iCollectionTypeName);
            var hasICollectionGeneric = typeSymbol.MetadataName == iCollectionGenericTypeName || interfaceNames.Contains(iCollectionGenericTypeName);
            var hasIReadOnlyCollectionGeneric = typeSymbol.MetadataName == iReadOnlyCollectionGenericTypeName || interfaceNames.Contains(iReadOnlyCollectionGenericTypeName);
            var hasIList = typeSymbol.MetadataName == iListTypeName || interfaceNames.Contains(iListTypeName);
            var hasIListGeneric = typeSymbol.MetadataName == iListGenericTypeName || interfaceNames.Contains(iListGenericTypeName);
            var hasListGeneric = typeSymbol.MetadataName == listGenericTypeName || baseTypeNames.Contains(listGenericTypeName);
            var hasIReadOnlyListGeneric = typeSymbol.MetadataName == iReadOnlyListTypeName || interfaceNames.Contains(iReadOnlyListTypeName);
            var hasISetGeneric = typeSymbol.MetadataName == iSetGenericTypeName || interfaceNames.Contains(iSetGenericTypeName);
            var hasIReadOnlySetGeneric = typeSymbol.MetadataName == iReadOnlySetGenericTypeName || interfaceNames.Contains(iReadOnlySetGenericTypeName);
            var hasHashSetGeneric = typeSymbol.MetadataName == hashSetGenericTypeName || baseTypeNames.Contains(hashSetGenericTypeName);
            var hasIDictionary = typeSymbol.MetadataName == iDictionaryTypeName || interfaceNames.Contains(iDictionaryTypeName);
            var hasIDictionaryGeneric = typeSymbol.MetadataName == iDictionaryGenericTypeName || interfaceNames.Contains(iDictionaryGenericTypeName);
            var hasIReadOnlyDictionaryGeneric = typeSymbol.MetadataName == iReadOnlyDictionaryGenericTypeName || interfaceNames.Contains(iReadOnlyDictionaryGenericTypeName);
            var hasDictionaryGeneric = typeSymbol.MetadataName == dictionaryGenericTypeName || baseTypeNames.Contains(dictionaryGenericTypeName);

            var isIEnumerable = typeSymbol.MetadataName == iEnumberableTypeName;
            var isIEnumerableGeneric = typeSymbol.MetadataName == iEnumberableGenericTypeName;
            var isICollection = typeSymbol.MetadataName == iCollectionTypeName;
            var isICollectionGeneric = typeSymbol.MetadataName == iCollectionGenericTypeName;
            var isIReadOnlyCollectionGeneric = typeSymbol.MetadataName == iReadOnlyCollectionGenericTypeName;
            var isIList = typeSymbol.MetadataName == iListTypeName;
            var isIListGeneric = typeSymbol.MetadataName == iListGenericTypeName;
            var isIReadOnlyListGeneric = typeSymbol.MetadataName == iReadOnlyListTypeName;
            var isListGeneric = typeSymbol.MetadataName == listGenericTypeName;
            var isISetGeneric = typeSymbol.MetadataName == iSetGenericTypeName;
            var isIReadOnlySetGeneric = typeSymbol.MetadataName == iReadOnlySetGenericTypeName;
            var isHashSetGeneric = typeSymbol.MetadataName == hashSetGenericTypeName;
            var isIDictionary = typeSymbol.MetadataName == iDictionaryTypeName;
            var isIDictionaryGeneric = typeSymbol.MetadataName == iDictionaryGenericTypeName;
            var isIReadOnlyDictionaryGeneric = typeSymbol.MetadataName == iReadOnlyDictionaryGenericTypeName;
            var isDictionaryGeneric = typeSymbol.MetadataName == dictionaryGenericTypeName;

            string innerTypeOf = "null";
            if (namedTypeSymbol is not null)
            {
                if (namedTypeSymbol.TypeArguments.Length == 1)
                    innerTypeOf = Helper.GetTypeOfName(namedTypeSymbol.TypeArguments[0]);
            }
            else if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                innerTypeOf = Helper.GetTypeOfName(arrayTypeSymbol.ElementType);
            }

            string iEnumerableInnerTypeOf = "null";
            if (isIEnumerableGeneric || typeSymbol.TypeKind == TypeKind.Array)
            {
                iEnumerableInnerTypeOf = innerTypeOf;
            }
            else if (hasIEnumerableGeneric)
            {
                var interfaceSymbol = typeSymbol.AllInterfaces.FirstOrDefault(x => x.MetadataName == iEnumberableGenericTypeName);
                if (interfaceSymbol is not null)
                    iEnumerableInnerTypeOf = Helper.GetTypeOfName(interfaceSymbol.TypeArguments[0]);
            }

            string dictionaryInnerTypeOf = "null";
            if (isIDictionaryGeneric || isIReadOnlyDictionaryGeneric)
            {
                if (namedTypeSymbol != null)
                    dictionaryInnerTypeOf = $"typeof(global::System.Collections.Generic.KeyValuePair<{Helper.GetFullName(namedTypeSymbol.TypeArguments[0])},{Helper.GetFullName(namedTypeSymbol.TypeArguments[1])}>)";
            }
            else if (hasIDictionaryGeneric)
            {
                var interfaceFound = typeSymbol.AllInterfaces.Where(x => x.MetadataName == iDictionaryGenericTypeName).ToArray();
                if (interfaceFound.Length == 1)
                {
                    var i = interfaceFound[0];
                    dictionaryInnerTypeOf = $"typeof(global::System.Collections.Generic.KeyValuePair<{Helper.GetFullName(i.TypeArguments[0])},{Helper.GetFullName(i.TypeArguments[1])}>)";
                }
            }
            else if (hasIReadOnlyDictionaryGeneric)
            {
                var interfaceFound = typeSymbol.AllInterfaces.Where(x => x.MetadataName == iReadOnlyDictionaryGenericTypeName).ToArray();
                if (interfaceFound.Length == 1)
                {
                    var i = interfaceFound[0];
                    dictionaryInnerTypeOf = $"typeof(global::System.Collections.Generic.KeyValuePair<{Helper.GetFullName(i.TypeArguments[0])},{Helper.GetFullName(i.TypeArguments[1])}>)";
                }
            }
            else if (hasIDictionary)
            {
                dictionaryInnerTypeOf = "typeof(global::System.Collections.DictionaryEntry)";
            }

            _ = sb.Append(Environment.NewLine).Append("                ");

            _ = sb.Append(Helper.BoolString(isNullable)).Append(", ");
            _ = sb.Append(coreType != null ? "global::Zerra.SourceGeneration.Types.CoreType." + coreType.Value.ToString() : "null").Append(", ");
            _ = sb.Append(specialType != null ? "global::Zerra.SourceGeneration.Types.SpecialType." + specialType.Value.ToString() : "null").Append(", ");
            _ = sb.Append(enumType != null ? "global::Zerra.SourceGeneration.Types.CoreEnumType." + enumType.Value.ToString() : "null").Append(", ");

            _ = sb.Append(Helper.BoolString(hasIEnumerable)).Append(", ");
            _ = sb.Append(Helper.BoolString(hasIEnumerableGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(hasICollection)).Append(", ");
            _ = sb.Append(Helper.BoolString(hasICollectionGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(hasIReadOnlyCollectionGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(hasIList)).Append(", ");
            _ = sb.Append(Helper.BoolString(hasIListGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(hasIReadOnlyListGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(hasListGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(hasISetGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(hasIReadOnlySetGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(hasHashSetGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(hasIDictionary)).Append(", ");
            _ = sb.Append(Helper.BoolString(hasIDictionaryGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(hasIReadOnlyDictionaryGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(hasDictionaryGeneric)).Append(", ");

            _ = sb.Append(Helper.BoolString(isIEnumerable)).Append(", ");
            _ = sb.Append(Helper.BoolString(isIEnumerableGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(isICollection)).Append(", ");
            _ = sb.Append(Helper.BoolString(isICollectionGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(isIReadOnlyCollectionGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(isIList)).Append(", ");
            _ = sb.Append(Helper.BoolString(isIListGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(isIReadOnlyListGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(isListGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(isISetGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(isIReadOnlySetGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(isHashSetGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(isIDictionary)).Append(", ");
            _ = sb.Append(Helper.BoolString(isIDictionaryGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(isIReadOnlyDictionaryGeneric)).Append(", ");
            _ = sb.Append(Helper.BoolString(isDictionaryGeneric)).Append(", ");

            _ = sb.Append(Environment.NewLine).Append("                ");

            _ = sb.Append(innerTypeOf).Append(", ");
            _ = sb.Append(iEnumerableInnerTypeOf).Append(", ");
            _ = sb.Append(dictionaryInnerTypeOf);
        }
        private static void GenerateInnerTypes(StringBuilder sb, ITypeSymbol typeSymbol, INamedTypeSymbol? namedTypeSymbol)
        {
            _ = sb.Append("[");

            if (namedTypeSymbol != null)
            {
                if (Helper.IsGenericDefined(namedTypeSymbol))
                {
                    if (namedTypeSymbol.TypeArguments.Length > 0)
                        _ = sb.Append(Environment.NewLine).Append("                ");

                    var hasFirst = false;
                    foreach (var type in namedTypeSymbol.TypeArguments)
                    {
                        if (hasFirst)
                            _ = sb.Append(", ");
                        else
                            hasFirst = true;

                        _ = sb.Append(Helper.GetTypeOfName(type));
                    }
                }
            }
            else if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                _ = sb.Append(Environment.NewLine).Append("                ");
                _ = sb.Append(Helper.GetTypeOfName(arrayTypeSymbol.ElementType));
            }

            _ = sb.Append("]");
        }
        private static void GenerateBaseTypes(StringBuilder sb, ITypeSymbol typeSymbol)
        {
            _ = sb.Append("[");

            if (typeSymbol.BaseType != null)
                _ = sb.Append(Environment.NewLine).Append("                ");

            var baseTypeSymbol = typeSymbol.BaseType;
            var hasFirst = false;
            while (baseTypeSymbol != null)
            {
                if (!Helper.CanBeReferencedAsCode(baseTypeSymbol))
                    break;
                if (hasFirst)
                    _ = sb.Append(", ");
                else
                    hasFirst = true;

                _ = sb.Append(Helper.GetTypeOfName(baseTypeSymbol));
                baseTypeSymbol = baseTypeSymbol.BaseType;
            }

            _ = sb.Append("]");
        }
        private static void GenerateInterfaces(StringBuilder sb, ITypeSymbol typeSymbol)
        {
            _ = sb.Append("[");

            if (typeSymbol.AllInterfaces.Length > 0)
                _ = sb.Append(Environment.NewLine).Append("                ");

            var hasFirst = false;
            foreach (var i in typeSymbol.AllInterfaces)
            {
                if (!Helper.CanBeReferencedAsCode(i))
                    break;
                if (hasFirst)
                    _ = sb.Append(", ");
                else
                    hasFirst = true;

                _ = sb.Append(Helper.GetTypeOfName(i));
            }

            _ = sb.Append("]");
        }
        private static void GenerateAttributes(StringBuilder sb, ISymbol symbol)
        {
            _ = sb.Append("[");

            var attributeSymbols = symbol.GetAttributes();

            //if (attributeSymbols.Length > 0)
            //    _ = sb.Append(Environment.NewLine).Append("                ");

            var hasFirst = false;
            foreach (var attributeSymbol in attributeSymbols)
            {
                if (attributeSymbol.AttributeClass is null)
                    continue;
                if (attributeSymbol.AttributeClass.ContainingNamespace.ToString().StartsWith("System.Runtime.CompilerServices"))
                    continue;
                if (attributeSymbol.AttributeClass.ContainingNamespace.ToString().StartsWith("System.Diagnostics"))
                    continue;
                if (!Helper.CanBeReferencedAsCode(attributeSymbol.AttributeClass))
                    continue;
                if (attributeSymbol.ConstructorArguments.Any(x => x.Type is null || !Helper.CanBeReferencedAsCode(x.Type)))
                    continue;

                if (hasFirst)
                    _ = sb.Append(", ");
                else
                    hasFirst = true;

                _ = sb.Append("new ").Append(attributeSymbol.AttributeClass.ToString()).Append('(');
                var hasFirst2 = false;
                //if (attributeSymbol.NamedArguments.Length > 0)
                //{
                //    foreach (var arg in attributeSymbol.NamedArguments)
                //    {
                //        if (usedFirst)
                //            _ = sbAttributes.Append(", ");
                //        else
                //            usedFirst = true;
                //        _ = sbAttributes.Append(arg.Key).Append(": ");
                //        TypedConstantToString(arg.Value, sbAttributes);
                //    }
                //}
                //else
                //{
                foreach (var arg in attributeSymbol.ConstructorArguments)
                {
                    if (hasFirst2)
                        _ = sb.Append(", ");
                    else
                        hasFirst2 = true;
                    Helper.TypedConstantToString(arg, sb);
                }
                //}
                _ = sb.Append(')');
            }

            _ = sb.Append("]");
        }
        private static void GenerateParameters(StringBuilder sb, ImmutableArray<IParameterSymbol> parameterSymbols)
        {
            _ = sb.Append("[");

            var hasFirst = false;
            foreach (var parameterSymbol in parameterSymbols)
            {
                if (hasFirst)
                    _ = sb.Append(", ");
                else
                    hasFirst = true;
                var typeOfName = Helper.GetTypeOfName(parameterSymbol.Type);

                _ = sb.Append("new global::Zerra.SourceGeneration.Types.ParameterDetail(").Append(typeOfName).Append(", \"").Append(parameterSymbol.Name).Append("\")");
            }

            _ = sb.Append("]");
        }

        private static void CheckModel(ITypeSymbol typeSymbol, Dictionary<string, TypeToGenerate> models, Stack<string> stack)
        {
            var name = Helper.GetFullName(typeSymbol);
            if (models.ContainsKey(name))
                return;
            var namedTypeSymbol = typeSymbol as INamedTypeSymbol;
            var arrayTypeSymbol = typeSymbol as IArrayTypeSymbol;
            if (namedTypeSymbol == null && arrayTypeSymbol == null)
                return;
            if (namedTypeSymbol != null && namedTypeSymbol.EnumUnderlyingType is not null)
                return;

            if (typeSymbol.TypeKind != TypeKind.Class && typeSymbol.TypeKind != TypeKind.Struct
                && arrayTypeSymbol == null
                && (namedTypeSymbol == null || !namedTypeSymbol.IsGenericType))
            {
                return;
            }

            if (TypeLookup.SpecialTypeLookup(name, out var specialType))
            {
                if (specialType == SpecialType.Task || specialType == SpecialType.Dictionary)
                {
                    if (namedTypeSymbol != null)
                    {
                        foreach (var typeArg in namedTypeSymbol.TypeArguments)
                            CheckModel(typeArg, models, stack);
                    }
                }
                if (specialType != SpecialType.Dictionary && specialType != SpecialType.Object)
                    return;
            }
            else
            {
                SearchModel(typeSymbol, models, stack);
            }

            if (Helper.IsUnclosedGeneric(typeSymbol))
                return;
            if (models.ContainsKey(name))
                return;
            var stackString = String.Join(" - ", stack);
            models.Add(name, new TypeToGenerate(typeSymbol, name, stackString));
        }

        private static void SearchInterface(ITypeSymbol typeSymbol, Dictionary<string, TypeToGenerate> models, Stack<string> stack)
        {
            var name = Helper.GetFullName(typeSymbol);
            if (stack.Contains(name))
                return;

            var symbolMembers = typeSymbol.GetMembers();
            (var properties, _) = GetPropertiesAndFields(typeSymbol, symbolMembers);

            foreach (var property in properties)
            {
                stack.Push(name);
                CheckModel(property.Item1.Type, models, stack);
                _ = stack.Pop();
            }

            var methods = GetMethods(typeSymbol, symbolMembers);
            foreach (var method in methods)
            {
                stack.Push(name);
                CheckModel(method.Item1.ReturnType, models, stack);
                _ = stack.Pop();
                foreach (var parameter in method.Item1.Parameters)
                {
                    stack.Push(name);
                    CheckModel(parameter.Type, models, stack);
                    _ = stack.Pop();
                }
            }

            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                foreach (var typeArg in namedTypeSymbol.TypeArguments)
                {
                    stack.Push(name);
                    CheckModel(typeArg, models, stack);
                    _ = stack.Pop();
                }
            }
            else if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                stack.Push(name);
                CheckModel(arrayTypeSymbol.ElementType, models, stack);
                _ = stack.Pop();
            }
        }
        private static void SearchModel(ITypeSymbol typeSymbol, Dictionary<string, TypeToGenerate> models, Stack<string> stack)
        {
            var name = Helper.GetFullName(typeSymbol);
            if (TypeLookup.CoreTypeLookup(name, out var coreType))
                return;
            if (stack.Contains(name))
                return;

            if (typeSymbol.Kind != SymbolKind.ArrayType)
            {
                var symbolMembers = typeSymbol.GetMembers();
                (var properties, var fields) = GetPropertiesAndFields(typeSymbol, symbolMembers);

                foreach (var @field in fields)
                {
                    stack.Push(name);
                    CheckModel(@field.Type, models, stack);
                    _ = stack.Pop();
                }
                foreach (var property in properties)
                {
                    stack.Push(name);
                    CheckModel(property.Item1.Type, models, stack);
                    _ = stack.Pop();
                }
            }

            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                foreach (var typeArg in namedTypeSymbol.TypeArguments)
                {
                    stack.Push(name);
                    CheckModel(typeArg, models, stack);
                    _ = stack.Pop();
                }
            }
            else if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                stack.Push(name);
                CheckModel(arrayTypeSymbol.ElementType, models, stack);
                _ = stack.Pop();
            }

            var baseType = typeSymbol.BaseType;
            while (baseType != null)
            {
                stack.Push(name);
                CheckModel(baseType, models, stack);
                _ = stack.Pop();
                baseType = baseType.BaseType;
            }
        }

        private static (List<Tuple<IPropertySymbol, string, bool>>, List<IFieldSymbol>) GetPropertiesAndFields(ITypeSymbol typeSymbol, ImmutableArray<ISymbol> symbolMembers)
        {
            var properties = symbolMembers.Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Where(x => x.ExplicitInterfaceImplementations.Length == 0 && !x.IsIndexer).Select(x => new Tuple<IPropertySymbol, string, bool>(x, x.Name, false)).ToList();
            var fields = symbolMembers.Where(x => x.Kind == SymbolKind.Field).Cast<IFieldSymbol>().ToList();

            if (typeSymbol.AllInterfaces.Length > 0)
            {
                var memberNames = new HashSet<string>();
                if (typeSymbol.TypeKind == TypeKind.Interface)
                {
                    foreach (var property in properties)
                        _ = memberNames.Add(property.Item1.Name);
                    foreach (var field in fields)
                        _ = memberNames.Add(field.Name);
                }

                foreach (var i in typeSymbol.AllInterfaces)
                {
                    var iMembers = i.GetMembers();
                    foreach (var iProperty in iMembers.Where(x => x.Kind == SymbolKind.Property && !x.IsStatic).Cast<IPropertySymbol>())
                    {
                        string memberName;
                        bool isExplicitFromInterface;
                        if (typeSymbol.TypeKind == TypeKind.Interface && !memberNames.Contains(iProperty.Name))
                        {
                            memberName = iProperty.Name;
                            isExplicitFromInterface = false;
                        }
                        else
                        {
                            memberName = $"{iProperty.ContainingType}.{iProperty.Name}";
                            isExplicitFromInterface = true;
                        }

                        if (!memberNames.Contains(memberName))
                        {
                            properties.Add(new Tuple<IPropertySymbol, string, bool>(iProperty, memberName, isExplicitFromInterface));
                            _ = memberNames.Add(memberName);
                        }
                    }
                }
            }

            return (properties, fields);
        }
        private static List<Tuple<IMethodSymbol, string, bool>> GetMethods(ITypeSymbol typeSymbol, ImmutableArray<ISymbol> symbolMembers)
        {
            var methods = symbolMembers.Where(x => x.Kind == SymbolKind.Method && x.DeclaredAccessibility == Accessibility.Public).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Ordinary || x.MethodKind == MethodKind.Destructor || x.MethodKind == MethodKind.PropertyGet || x.MethodKind == MethodKind.PropertySet || x.MethodKind == MethodKind.ExplicitInterfaceImplementation).Select(x => new Tuple<IMethodSymbol, string, bool>(x, x.Name, false)).ToList();

            if (typeSymbol.AllInterfaces.Length > 0)
            {
                var methodNames = new HashSet<string>();
                foreach (var method in methods)
                    _ = methodNames.Add(method.Item1.Name);

                foreach (var i in typeSymbol.AllInterfaces)
                {
                    var iMethods = i.GetMembers().Where(x => x.Kind == SymbolKind.Method && !x.IsStatic).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Ordinary || x.MethodKind == MethodKind.Destructor || x.MethodKind == MethodKind.PropertyGet || x.MethodKind == MethodKind.PropertySet || x.MethodKind == MethodKind.ExplicitInterfaceImplementation).ToArray();
                    foreach (var iMethod in iMethods)
                    {
                        string methodName;
                        bool isExplicitFromInterface;
                        if (typeSymbol.TypeKind == TypeKind.Interface && !methodNames.Contains(iMethod.Name))
                        {
                            methodName = iMethod.Name;
                            isExplicitFromInterface = false;
                        }
                        else
                        {
                            methodName = $"{Helper.AdjustName(iMethod.ContainingType.ToString(), false)}.{Helper.AdjustName(iMethod.Name, true)}";
                            isExplicitFromInterface = true;
                        }

                        if (!methodNames.Contains(methodName))
                        {
                            methods.Add(new Tuple<IMethodSymbol, string, bool>(iMethod, methodName, isExplicitFromInterface));
                            _ = methodNames.Add(methodName);
                        }
                    }
                }
            }

            return methods;
        }
        private static IMethodSymbol[] GetConstructors(ITypeSymbol typeSymbol, ImmutableArray<ISymbol> symbolMembers)
        {
            var constructors = symbolMembers.Where(x => x.Kind == SymbolKind.Method && x.DeclaredAccessibility == Accessibility.Public).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Constructor && x.Parameters.Length > 0).ToArray();
            return constructors;
        }
    }
}

