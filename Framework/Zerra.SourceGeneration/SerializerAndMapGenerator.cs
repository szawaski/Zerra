// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using System.Collections;
using System.Collections.Immutable;
using System.Text;

namespace Zerra.SourceGeneration
{
    public static class SerializerAndMapGenerator
    {
        private static readonly string enumberableGenericTypeName = typeof(IEnumerable<>).Name;
        private static readonly string dictionaryTypeName = typeof(IDictionary).Name;
        private static readonly string dictionaryGenericTypeName = typeof(IDictionary<,>).Name;
        private static readonly string readOnlyDictionaryGenericTypeName = typeof(IReadOnlyDictionary<,>).Name;

        public static void Generate(StringBuilder sb, Dictionary<string, TypeToGenerate> models)
        {
            foreach (var model in models.Values)
            {
                var namedTypeSymbol = model.TypeSymbol as INamedTypeSymbol;

                INamedTypeSymbol? mapBaseOrInterface = null;
                if (namedTypeSymbol != null)
                {
                    mapBaseOrInterface = namedTypeSymbol.AllInterfaces.FirstOrDefault(x => x.Name == "IMapDefinition" && x.ContainingNamespace.ToString() == "Zerra.Map");
                    mapBaseOrInterface ??= Helper.FindBase("Zerra.Map", "MapDefinition", model.TypeSymbol);
                }

                if (namedTypeSymbol != null && mapBaseOrInterface != null)
                {
                    var sourceType = mapBaseOrInterface.TypeArguments[0];
                    var targetType = mapBaseOrInterface.TypeArguments[1];
                    var (sourceTypeName, sourceEnumerableTypeName, sourceDictionaryKeyTypeName, sourceDictionaryValueTypeName) = GetTypeParameters(sourceType);
                    var (targetTypeName, targetEnumerableTypeName, targetDictionaryKeyTypeName, targetDictionaryValueTypeName) = GetTypeParameters(targetType);

                    _ = sb.Append(Environment.NewLine).Append("            ");
                    _ = sb.Append("global::Zerra.SourceGeneration.Register.CustomMap<")
                        .Append(sourceTypeName).Append(",")
                        .Append(targetTypeName).Append(",")
                        .Append(sourceEnumerableTypeName).Append(",")
                        .Append(targetEnumerableTypeName).Append(",")
                        .Append(sourceDictionaryKeyTypeName).Append(",")
                        .Append(sourceDictionaryValueTypeName).Append(",")
                        .Append(targetDictionaryKeyTypeName).Append(",")
                        .Append(targetDictionaryValueTypeName)
                        .Append(">(new ").Append(Helper.GetFullName(namedTypeSymbol)).Append("());");
                }
                else
                {
                    if (Helper.IsTypeOrBase("Zerra.Map", "MapDefinition", model.TypeSymbol) || model.TypeSymbol.AllInterfaces.Any(x => x.Name == "IMapDefinition" && x.ContainingNamespace.ToString() == "Zerra.Map"))
                        continue;

                    var (typeName, enumerableTypeName, dictionaryKeyTypeName, dictionaryValueTypeName) = GetTypeParameters(model.TypeSymbol);

                    _ = sb.Append(Environment.NewLine).Append("            ");
                    _ = sb.Append("global::Zerra.SourceGeneration.Register.SerializersAndMap<")
                        .Append(typeName).Append(",")
                        .Append(enumerableTypeName).Append(",")
                        .Append(dictionaryKeyTypeName).Append(",")
                        .Append(dictionaryValueTypeName)
                        .Append(">();");
                }
            }
        }

        private static (string, string, string, string) GetTypeParameters(ITypeSymbol typeSymbol)
        {
            var namedTypeSymbol = typeSymbol as INamedTypeSymbol;

            var innerTypeName = "object";
            if (namedTypeSymbol is not null)
            {
                if (namedTypeSymbol.TypeArguments.Length == 1)
                    innerTypeName = Helper.GetFullName(namedTypeSymbol.TypeArguments[0]);
            }
            else if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                innerTypeName = Helper.GetFullName(arrayTypeSymbol.ElementType);
            }

            var enumerableTypeName = "object";
            var isArray = typeSymbol.Kind == SymbolKind.ArrayType;
            var interfaceNames = typeSymbol.AllInterfaces.Select(x => x.MetadataName).ToImmutableHashSet();
            var hasIEnumerableGeneric = isArray || typeSymbol.MetadataName == enumberableGenericTypeName || interfaceNames.Contains(enumberableGenericTypeName);
            var isIEnumerableGeneric = typeSymbol.MetadataName == enumberableGenericTypeName;
            if (isIEnumerableGeneric || typeSymbol.TypeKind == TypeKind.Array)
            {
                enumerableTypeName = innerTypeName;
            }
            else if (hasIEnumerableGeneric)
            {
                var interfaceSymbol = typeSymbol.AllInterfaces.FirstOrDefault(x => x.MetadataName == enumberableGenericTypeName);
                if (interfaceSymbol is not null)
                    enumerableTypeName = Helper.GetFullName(interfaceSymbol.TypeArguments[0]);
            }

            var dictionaryKeyTypeName = "object";
            var dictionaryValueTypeName = "object";
            var hasIDictionary = typeSymbol.MetadataName == dictionaryTypeName || interfaceNames.Contains(dictionaryTypeName);
            var hasIDictionaryGeneric = typeSymbol.MetadataName == dictionaryGenericTypeName || interfaceNames.Contains(dictionaryGenericTypeName);
            var hasIReadOnlyDictionaryGeneric = typeSymbol.MetadataName == readOnlyDictionaryGenericTypeName || interfaceNames.Contains(readOnlyDictionaryGenericTypeName);
            var isIDictionaryGeneric = typeSymbol.MetadataName == dictionaryGenericTypeName;
            var isIReadOnlyDictionaryGeneric = typeSymbol.MetadataName == readOnlyDictionaryGenericTypeName;
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
                var interfaceFound = typeSymbol.AllInterfaces.Where(x => x.MetadataName == dictionaryGenericTypeName).ToArray();
                if (interfaceFound.Length == 1)
                {
                    var i = interfaceFound[0];
                    dictionaryKeyTypeName = Helper.GetFullName(i.TypeArguments[0]);
                    dictionaryValueTypeName = Helper.GetFullName(i.TypeArguments[1]);
                }
            }
            else if (hasIReadOnlyDictionaryGeneric)
            {
                var interfaceFound = typeSymbol.AllInterfaces.Where(x => x.MetadataName == readOnlyDictionaryGenericTypeName).ToArray();
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

            return (Helper.GetFullName(typeSymbol), enumerableTypeName, dictionaryKeyTypeName, dictionaryValueTypeName);
        }  
    }
}

