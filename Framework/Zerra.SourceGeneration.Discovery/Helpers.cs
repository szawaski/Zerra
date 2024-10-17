// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using System;
using System.Text;

namespace Zerra.SourceGeneration.Discovery
{
    internal static class Helpers
    {
        public static string GetTypeOfName(ITypeSymbol typeSymbol, bool isByRef = false)
        {
            if (typeSymbol.Kind == SymbolKind.ErrorType)
                return "null";
            if (typeSymbol.Kind == SymbolKind.TypeParameter)
                return "null";
            if (typeSymbol.Kind == SymbolKind.PointerType)
                return "typeof(nint)";
            if (typeSymbol.Name == "Void")
                return "typeof(void)";
            if (!IsPublic(typeSymbol))
                return "null";

            if (typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeArguments.Length > 0)
            {
                var sb = new StringBuilder();
                _ = sb.Append("typeof(");
                if (String.IsNullOrEmpty(typeSymbol.Name))
                {
                    sb.Append(GetFullName(typeSymbol));
                }
                else
                {
                    if (typeSymbol.ContainingType is not null)
                        _ = sb.Append(typeSymbol.ContainingType).Append('.');
                    else if (typeSymbol.ContainingNamespace is not null)
                        _ = sb.Append(typeSymbol.ContainingNamespace).Append('.');
                    _ = sb.Append(typeSymbol.Name);
                }
                _ = sb.Append('<');

                var constructed = IsGenericDefined(namedTypeSymbol);
                for (var i = 0; i < namedTypeSymbol.TypeArguments.Length; i++)
                {
                    if (i > 0)
                        _ = sb.Append(',');
                    if (constructed)
                        GetTypeOfNameRecursive(namedTypeSymbol.TypeArguments[i], sb);
                }

                _ = sb.Append(">)");
                if (isByRef)
                    _ = sb.Append(".MakeByRefType()");
                return sb.ToString();
            }

            if (String.IsNullOrEmpty(typeSymbol.Name))
                return $"typeof({GetFullName(typeSymbol)}){(isByRef ? ".MakeByRefType()" : null)}";
            else if (typeSymbol.ContainingType is not null)
                return $"typeof({typeSymbol.ContainingType}.{typeSymbol.Name}){(isByRef ? ".MakeByRefType()" : null)}";
            else if (typeSymbol.ContainingNamespace is not null)
                return $"typeof({typeSymbol.ContainingNamespace}.{typeSymbol.Name}){(isByRef ? ".MakeByRefType()" : null)}";
            else
                return $"typeof({typeSymbol.Name}){(isByRef ? ".MakeByRefType()" : null)}";
        }
        private static void GetTypeOfNameRecursive(ITypeSymbol typeSymbol, StringBuilder sb)
        {
            if (String.IsNullOrEmpty(typeSymbol.Name))
            {
                sb.Append(typeSymbol.ToString().Replace("?", String.Empty));
                return;
            }

            if (typeSymbol.ContainingType is not null)
                _ = sb.Append(typeSymbol.ContainingType).Append('.');
            else if (typeSymbol.ContainingNamespace is not null)
                _ = sb.Append(typeSymbol.ContainingNamespace).Append('.');
            _ = sb.Append(typeSymbol.Name);

            if (typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeArguments.Length > 0)
            {
                var constructed = IsGenericDefined(namedTypeSymbol);
                _ = sb.Append('<');
                for (var i = 0; i < namedTypeSymbol.TypeArguments.Length; i++)
                {
                    if (i > 0)
                        _ = sb.Append(',');
                    if (constructed)
                        GetTypeOfNameRecursive(namedTypeSymbol.TypeArguments[i], sb);
                }

                _ = sb.Append('>');
            }
        }

        public static bool IsGenericDefined(INamedTypeSymbol namedTypeSymbol)
        {
            foreach (var argument in namedTypeSymbol.TypeArguments)
            {
                if (argument.Kind == SymbolKind.ErrorType)
                    return false;
                if (argument.Kind == SymbolKind.TypeParameter)
                    return false;
                if (argument is not INamedTypeSymbol argumemtNamedTypeSymbol)
                    return false;
                if (!IsGenericDefined(argumemtNamedTypeSymbol))
                    return false;
            }

            return true;
        }
        public static bool IsPublic(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.DeclaredAccessibility != Accessibility.Public && typeSymbol.DeclaredAccessibility != Accessibility.NotApplicable)
                return false;
            if (typeSymbol.Kind == SymbolKind.NamedType && typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                foreach (var argument in namedTypeSymbol.TypeArguments)
                {
                    if (!IsPublic(argument))
                        return false;
                }
            }
            if (typeSymbol.Kind == SymbolKind.ArrayType && typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                if (!IsPublic(arrayTypeSymbol.ElementType))
                    return false;
            }
            return true;
        }

        public static string GetFullName(ITypeSymbol typeSymbol)
        {
            var fullTypeName = typeSymbol.ToString();
            if (!typeSymbol.IsValueType && fullTypeName.EndsWith("?"))
                fullTypeName = fullTypeName.Substring(0, fullTypeName.Length - 1);
            return fullTypeName;
        }

        public static string GetNameForClass(ITypeSymbol typeSymbol)
        {
            var ns = typeSymbol.ContainingNamespace is null || typeSymbol.ContainingNamespace.ToString().Contains("<global namespace>") ? null : typeSymbol.ContainingNamespace.ToString();

            var name = typeSymbol.ToString();
            if (ns is not null && name.StartsWith(ns))
                name = name.Substring(ns.Length + 1);
            name = name.Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace('.', '_').Replace("[]", "Array");
            if (name.EndsWith("?") && !typeSymbol.IsValueType)
                name = name.Substring(0, name.Length - 1);
            name = name.Replace("?", "Nullable");
            return name;
        }
    }
}
