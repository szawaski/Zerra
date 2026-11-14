// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using System.Collections;
using System.Collections.Immutable;
using System.Text;

namespace Zerra.SourceGeneration
{
    /// <summary>
    /// Generates source code for registering serializer and map configurations for discovered model types.
    /// </summary>
    public static class SerializerAndMapGenerator
    {
        private static readonly string enumberableGenericTypeName = typeof(IEnumerable<>).Name;
        private static readonly string dictionaryTypeName = typeof(IDictionary).Name;
        private static readonly string dictionaryGenericTypeName = typeof(IDictionary<,>).Name;
        private static readonly string readOnlyDictionaryGenericTypeName = typeof(IReadOnlyDictionary<,>).Name;

        /// <summary>
        /// Appends source generation code for serializer and map registrations to the provided <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to append the generated source code to.</param>
        /// <param name="models">The dictionary of model types to generate registrations for, keyed by full type name.</param>
        public static void Generate(StringBuilder sb, Dictionary<string, TypeToGenerate> models)
        {
            foreach (var model in models.Values)
            {
                if (model.TypeSymbol.IsStatic)
                    continue;

                var namedTypeSymbol = model.TypeSymbol as INamedTypeSymbol;

                //a class can implement IMapDefinition for several type pairs, and each one is registered
                INamedTypeSymbol[] mapBasesOrInterfaces = [];
                if (namedTypeSymbol != null)
                {
                    mapBasesOrInterfaces = namedTypeSymbol.AllInterfaces.Where(x => x.Name == "IMapDefinition" && x.ContainingNamespace.ToString() == "Zerra.Map").ToArray();
                    if (mapBasesOrInterfaces.Length == 0)
                    {
                        var mapBase = Helper.FindBase("Zerra.Map", "MapDefinition", model.TypeSymbol);
                        if (mapBase != null)
                            mapBasesOrInterfaces = [mapBase];
                    }
                }

                if (namedTypeSymbol != null && mapBasesOrInterfaces.Length > 0)
                {
                    foreach (var mapBaseOrInterface in mapBasesOrInterfaces)
                    {
                        var sourceType = mapBaseOrInterface.TypeArguments[0];
                        var targetType = mapBaseOrInterface.TypeArguments[1];
                        var (sourceTypeName, sourceEnumerableTypeName, sourceDictionaryKeyTypeName, sourceDictionaryValueTypeName) = GetTypeParameters(sourceType);
                        var (targetTypeName, targetEnumerableTypeName, targetDictionaryKeyTypeName, targetDictionaryValueTypeName) = GetTypeParameters(targetType);

                        _ = sb.Append(EnvironmentHelper.NewLine);
                        _ = sb.Append("global::Zerra.Reflection.Register.CustomMap<")
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
                }
                else
                {
                    if (Helper.IsTypeOrBase("Zerra.Map", "MapDefinition", model.TypeSymbol) || model.TypeSymbol.AllInterfaces.Any(x => x.Name == "IMapDefinition" && x.ContainingNamespace.ToString() == "Zerra.Map"))
                        continue;

                    var (enumerableType, dictionaryKeyType, dictionaryValueType) = GetTypeParameterSymbols(model.TypeSymbol);

                    //the serializers and map look up the detail of these types when registered, and the finder never generates the special types it skips, such as Type and Task
                    if ((enumerableType is not null && TypeLookup.IsNotGeneratedSpecialType(enumerableType))
                        || (dictionaryKeyType is not null && TypeLookup.IsNotGeneratedSpecialType(dictionaryKeyType))
                        || (dictionaryValueType is not null && TypeLookup.IsNotGeneratedSpecialType(dictionaryValueType)))
                        continue;

                    var typeName = Helper.GetFullName(model.TypeSymbol);
                    var enumerableTypeName = GetTypeParameterName(enumerableType);
                    var dictionaryKeyTypeName = GetTypeParameterName(dictionaryKeyType);
                    var dictionaryValueTypeName = GetTypeParameterName(dictionaryValueType);

                    _ = sb.Append(EnvironmentHelper.NewLine);
                    _ = sb.Append("global::Zerra.Reflection.Register.SerializersAndMap<")
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
            var (enumerableType, dictionaryKeyType, dictionaryValueType) = GetTypeParameterSymbols(typeSymbol);
            return (Helper.GetFullName(typeSymbol), GetTypeParameterName(enumerableType), GetTypeParameterName(dictionaryKeyType), GetTypeParameterName(dictionaryValueType));
        }

        private static string GetTypeParameterName(ITypeSymbol? typeSymbol) => typeSymbol is null ? "object" : Helper.GetFullName(typeSymbol);

        //null where the type is not that kind of collection, which is registered as object
        private static (ITypeSymbol?, ITypeSymbol?, ITypeSymbol?) GetTypeParameterSymbols(ITypeSymbol typeSymbol)
        {
            var namedTypeSymbol = typeSymbol as INamedTypeSymbol;

            ITypeSymbol? innerType = null;
            if (namedTypeSymbol is not null)
            {
                if (namedTypeSymbol.TypeArguments.Length == 1)
                    innerType = namedTypeSymbol.TypeArguments[0];
            }
            else if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                innerType = arrayTypeSymbol.ElementType;
            }

            ITypeSymbol? enumerableType = null;
            var isArray = typeSymbol.Kind == SymbolKind.ArrayType;
            var interfaceNames = typeSymbol.AllInterfaces.Select(x => x.MetadataName).ToImmutableHashSet();
            var hasIEnumerableGeneric = isArray || typeSymbol.MetadataName == enumberableGenericTypeName || interfaceNames.Contains(enumberableGenericTypeName);
            var isIEnumerableGeneric = typeSymbol.MetadataName == enumberableGenericTypeName;
            if (isIEnumerableGeneric || typeSymbol.TypeKind == TypeKind.Array)
            {
                enumerableType = innerType;
            }
            else if (hasIEnumerableGeneric)
            {
                var interfaceSymbol = typeSymbol.AllInterfaces.FirstOrDefault(x => x.MetadataName == enumberableGenericTypeName);
                if (interfaceSymbol is not null)
                    enumerableType = interfaceSymbol.TypeArguments[0];
            }

            ITypeSymbol? dictionaryKeyType = null;
            ITypeSymbol? dictionaryValueType = null;
            var hasIDictionary = typeSymbol.MetadataName == dictionaryTypeName || interfaceNames.Contains(dictionaryTypeName);
            var hasIDictionaryGeneric = typeSymbol.MetadataName == dictionaryGenericTypeName || interfaceNames.Contains(dictionaryGenericTypeName);
            var hasIReadOnlyDictionaryGeneric = typeSymbol.MetadataName == readOnlyDictionaryGenericTypeName || interfaceNames.Contains(readOnlyDictionaryGenericTypeName);
            var isIDictionaryGeneric = typeSymbol.MetadataName == dictionaryGenericTypeName;
            var isIReadOnlyDictionaryGeneric = typeSymbol.MetadataName == readOnlyDictionaryGenericTypeName;
            if (isIDictionaryGeneric || isIReadOnlyDictionaryGeneric)
            {
                if (namedTypeSymbol != null)
                {
                    dictionaryKeyType = namedTypeSymbol.TypeArguments[0];
                    dictionaryValueType = namedTypeSymbol.TypeArguments[1];
                }
            }
            else if (hasIDictionaryGeneric)
            {
                var interfaceFound = typeSymbol.AllInterfaces.Where(x => x.MetadataName == dictionaryGenericTypeName).ToArray();
                if (interfaceFound.Length == 1)
                {
                    var i = interfaceFound[0];
                    dictionaryKeyType = i.TypeArguments[0];
                    dictionaryValueType = i.TypeArguments[1];
                }
            }
            else if (hasIReadOnlyDictionaryGeneric)
            {
                var interfaceFound = typeSymbol.AllInterfaces.Where(x => x.MetadataName == readOnlyDictionaryGenericTypeName).ToArray();
                if (interfaceFound.Length == 1)
                {
                    var i = interfaceFound[0];
                    dictionaryKeyType = i.TypeArguments[0];
                    dictionaryValueType = i.TypeArguments[1];
                }
            }
            else if (hasIDictionary)
            {
                //dictionaryInnerTypeOf = "typeof(DictionaryEntry)";
            }

            return (enumerableType, dictionaryKeyType, dictionaryValueType);
        }  
    }
}

