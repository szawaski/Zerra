// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using System.Collections;
using System.Collections.Immutable;
using System.Text;

namespace Zerra.SourceGeneration
{
    public static class ConverterGenerator
    {
        private static readonly string enumberableGenericTypeName = typeof(IEnumerable<>).Name;
        private static readonly string dictionaryTypeName = typeof(IDictionary).Name;
        private static readonly string dictionaryGenericTypeName = typeof(IDictionary<,>).Name;
        private static readonly string readOnlyDictionaryGenericTypeName = typeof(IReadOnlyDictionary<,>).Name;

        public static void FindModels(ITypeSymbol typeSymbol, Dictionary<string, TypeToGenerate> models)
        {
            if (typeSymbol.TypeKind != TypeKind.Class && typeSymbol.TypeKind != TypeKind.Interface)
                return;
            if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                return;

            var stack = new Stack<string>();
            if ((namedTypeSymbol.Interfaces.Any(x => x.Name == "ICommand" || x.Name == "IEvent" || x.Name == "IQueryHandler" || x.Name == "ICommandHandler")
                || namedTypeSymbol.GetAttributes().Any(x => x.AttributeClass?.Name == "SourceGenerationTypeDetailAttribute"))
                && !namedTypeSymbol.GetAttributes().Any(x => x.AttributeClass?.Name == "IgnoreSourceGenerationTypeDetailAttribute"))
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

                var innerTypeName = "object";
                if (namedTypeSymbol is not null)
                {
                    if (namedTypeSymbol.TypeArguments.Length == 1)
                        innerTypeName = Helper.GetFullName(namedTypeSymbol.TypeArguments[0]);
                }
                else if (model.TypeSymbol is IArrayTypeSymbol arrayTypeSymbol)
                {
                    innerTypeName = Helper.GetFullName(arrayTypeSymbol.ElementType);
                }

                var enumerableTypeName = "object";
                var isArray = model.TypeSymbol.Kind == SymbolKind.ArrayType;
                var interfaceNames = model.TypeSymbol.AllInterfaces.Select(x => x.MetadataName).ToImmutableHashSet();
                var hasIEnumerableGeneric = isArray || model.TypeSymbol.MetadataName == enumberableGenericTypeName || interfaceNames.Contains(enumberableGenericTypeName);
                var isIEnumerableGeneric = model.TypeSymbol.MetadataName == enumberableGenericTypeName;
                if (isIEnumerableGeneric || model.TypeSymbol.TypeKind == TypeKind.Array)
                {
                    enumerableTypeName = innerTypeName;
                }
                else if (hasIEnumerableGeneric)
                {
                    var interfaceSymbol = model.TypeSymbol.AllInterfaces.FirstOrDefault(x => x.MetadataName == enumberableGenericTypeName);
                    if (interfaceSymbol is not null)
                        enumerableTypeName = Helper.GetFullName(interfaceSymbol.TypeArguments[0]);
                }

                var dictionaryKeyTypeName = "object";
                var dictionaryValueTypeName = "object";
                var hasIDictionary = model.TypeSymbol.MetadataName == dictionaryTypeName || interfaceNames.Contains(dictionaryTypeName);
                var hasIDictionaryGeneric = model.TypeSymbol.MetadataName == dictionaryGenericTypeName || interfaceNames.Contains(dictionaryGenericTypeName);
                var hasIReadOnlyDictionaryGeneric = model.TypeSymbol.MetadataName == readOnlyDictionaryGenericTypeName || interfaceNames.Contains(readOnlyDictionaryGenericTypeName);
                var isIDictionaryGeneric = model.TypeSymbol.MetadataName == dictionaryGenericTypeName;
                var isIReadOnlyDictionaryGeneric = model.TypeSymbol.MetadataName == readOnlyDictionaryGenericTypeName;
                if (isIDictionaryGeneric || isIReadOnlyDictionaryGeneric)
                {
                    if (namedTypeSymbol != null)
                    {
                        dictionaryKeyTypeName = Helper.GetFullName(namedTypeSymbol.TypeArguments[0]);
                        dictionaryValueTypeName = Helper.GetFullName(namedTypeSymbol.TypeArguments[1]);
                    }
                }
                else if (hasIDictionaryGeneric)
                {
                    var interfaceFound = model.TypeSymbol.AllInterfaces.Where(x => x.MetadataName == dictionaryGenericTypeName).ToArray();
                    if (interfaceFound.Length == 1)
                    {
                        var i = interfaceFound[0];
                        dictionaryKeyTypeName = Helper.GetFullName(i.TypeArguments[0]);
                        dictionaryValueTypeName = Helper.GetFullName(i.TypeArguments[1]);
                    }
                }
                else if (hasIReadOnlyDictionaryGeneric)
                {
                    var interfaceFound = model.TypeSymbol.AllInterfaces.Where(x => x.MetadataName == readOnlyDictionaryGenericTypeName).ToArray();
                    if (interfaceFound.Length == 1)
                    {
                        var i = interfaceFound[0];
                        dictionaryKeyTypeName = Helper.GetFullName(i.TypeArguments[0]);
                        dictionaryValueTypeName = Helper.GetFullName(i.TypeArguments[1]);
                    }
                }
                else if (hasIDictionary)
                {
                    //dictionaryInnerTypeOf = "typeof(DictionaryEntry)";
                }

                //_ = sb.Append(Environment.NewLine).Append("            ");
                //_ = sb.Append("//").Append(Helper.GetFullName(typeSymbol) + " - " + stackString);

                _ = sb.Append(Environment.NewLine).Append("            ");
                _ = sb.Append("global::Zerra.Serialization.Bytes.Converters.ByteConverterFactory.RegisterCreator<")
                    .Append(model.TypeName).Append(",")
                    .Append(enumerableTypeName).Append(",")
                    .Append(dictionaryKeyTypeName).Append(",")
                    .Append(dictionaryValueTypeName)
                    .Append(">(").Append(typeOfName).Append(");");

                _ = sb.Append(Environment.NewLine).Append("            ");
                _ = sb.Append("global::Zerra.Serialization.Json.Converters.JsonConverterFactory.RegisterCreator<")
                    .Append(model.TypeName).Append(",")
                    .Append(enumerableTypeName).Append(",")
                    .Append(dictionaryKeyTypeName).Append(",")
                    .Append(dictionaryValueTypeName)
                    .Append(">(").Append(typeOfName).Append(");");
            }
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
            var properties = symbolMembers.Where(x => x.Kind == SymbolKind.Property && x.DeclaredAccessibility == Accessibility.Public).Cast<IPropertySymbol>().Where(x => !x.IsStatic && x.ExplicitInterfaceImplementations.Length == 0 && !x.IsIndexer).Select(x => new Tuple<IPropertySymbol, string, bool>(x, x.Name, false)).ToList();
            var fields = symbolMembers.Where(x => x.Kind == SymbolKind.Field && x.DeclaredAccessibility == Accessibility.Public).Cast<IFieldSymbol>().ToList();

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
    }
}

