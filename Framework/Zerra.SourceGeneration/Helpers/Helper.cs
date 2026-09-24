// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Globalization;
using System.Text;

namespace Zerra.SourceGeneration
{
    internal static class Helper
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
            //if (!IsPublic(typeSymbol))
            //    return "null";

            if (typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeArguments.Length > 0)
            {
                var sb = new StringBuilder();
                _ = sb.Append("typeof(");
                if (String.IsNullOrEmpty(typeSymbol.Name))
                {
                    _ = sb.Append(GetFullName(typeSymbol));
                }
                else
                {
                    _ = sb.Append("global::");
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

            return $"typeof({GetFullName(typeSymbol)}){(isByRef ? ".MakeByRefType()" : null)}";
        }
        private static void GetTypeOfNameRecursive(ITypeSymbol typeSymbol, StringBuilder sb)
        {
            if (String.IsNullOrEmpty(typeSymbol.Name))
            {
                _ = sb.Append(typeSymbol.ToString().Replace("?", String.Empty));
                return;
            }

            _ = sb.Append("global::");
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
            var fullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            //if (!typeSymbol.IsValueType && fullTypeName.EndsWith("?"))
            //    fullTypeName = fullTypeName.Substring(0, fullTypeName.Length - 1);
            return fullTypeName;
        }

        //keeps nullable reference annotations such as string?, for member signatures that must match the declared nullability, not for typeof or casts
        public static string GetFullNameWithNullability(ITypeSymbol typeSymbol)
            => typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));

        public static bool IsNullable(ITypeSymbol typeSymbol)
            => typeSymbol.NullableAnnotation == NullableAnnotation.Annotated || typeSymbol.OriginalDefinition.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Nullable_T;

        public static string GetClassSafeName(ITypeSymbol typeSymbol)
        {
            var ns = typeSymbol.ContainingNamespace is null || typeSymbol.ContainingNamespace.ToString().Contains("<global namespace>") ? null : typeSymbol.ContainingNamespace.ToString();

            var name = typeSymbol.ToString();
            if (ns is not null && name.StartsWith(ns))
                name = name.Substring(ns.Length + 1);
            name = name.Replace('<', '_').Replace('>', '_').Replace('.', '_').Replace(",", "_").Replace(" ", String.Empty).Replace("[]", "Array");
            if (name.EndsWith("?") && !typeSymbol.IsValueType)
                name = name.Substring(0, name.Length - 1);
            name = name.Replace("?", "Nullable");
            return name;
        }

        public static string AdjustName(string name, bool removeNamespace)
        {
            Span<char> chars = name.ToCharArray();
            var nameStart = 0;
            var nameEnd = -1;
            var openCount = 0;
            var typeCount = 0;
            for (var i = 0; i < chars.Length; i++)
            {
                switch (chars[i])
                {
                    case '.':
                        if (removeNamespace && openCount == 0)
                        {
                            nameStart = i + 1;
                            nameEnd = -1;
                            typeCount = 0;
                        }
                        break;
                    case '<':
                        if (nameEnd == -1)
                        {
                            typeCount++;
                            nameEnd = i;
                        }
                        openCount++;
                        break;
                    case '>':
                        openCount--;
                        break;
                    case ',':
                        if (openCount == 1)
                        {
                            typeCount++;
                        }
                        break;
                }
            }
            if (nameEnd == -1)
                return name;
            var result = $"{chars.Slice(nameStart, nameEnd - nameStart).ToString()}`{typeCount}";
            return result;
        }

        //written as code, so every value is a valid literal of its exact type: a boxed or overloaded argument depends on the type,
        //and numbers are formatted invariant so a culture using a decimal comma does not break the code
        public static void TypedConstantToString(TypedConstant constant, StringBuilder sb)
        {
            switch (constant.Kind)
            {
                case TypedConstantKind.Primitive:
                    switch (constant.Value)
                    {
                        case null:
                            _ = sb.Append("null");
                            break;
                        case string stringValue:
                            _ = sb.Append(SymbolDisplay.FormatLiteral(stringValue, true));
                            break;
                        case char charValue:
                            _ = sb.Append(SymbolDisplay.FormatLiteral(charValue, true));
                            break;
                        case bool boolValue:
                            _ = sb.Append(BoolString(boolValue));
                            break;
                        case float floatValue:
                            if (Single.IsNaN(floatValue))
                                _ = sb.Append("float.NaN");
                            else if (Single.IsPositiveInfinity(floatValue))
                                _ = sb.Append("float.PositiveInfinity");
                            else if (Single.IsNegativeInfinity(floatValue))
                                _ = sb.Append("float.NegativeInfinity");
                            else
                                _ = sb.Append(floatValue.ToString("R", CultureInfo.InvariantCulture)).Append('F');
                            break;
                        case double doubleValue:
                            if (Double.IsNaN(doubleValue))
                                _ = sb.Append("double.NaN");
                            else if (Double.IsPositiveInfinity(doubleValue))
                                _ = sb.Append("double.PositiveInfinity");
                            else if (Double.IsNegativeInfinity(doubleValue))
                                _ = sb.Append("double.NegativeInfinity");
                            else
                                _ = sb.Append(doubleValue.ToString("R", CultureInfo.InvariantCulture)).Append('D');
                            break;
                        default:
                            //integers, cast so a byte stays a byte when boxed to object
                            _ = sb.Append('(').Append(GetFullName(constant.Type!)).Append(")(").Append(((IFormattable)constant.Value).ToString(null, CultureInfo.InvariantCulture)).Append(')');
                            break;
                    }
                    break;
                case TypedConstantKind.Enum:
                    _ = sb.Append('(').Append(GetFullName(constant.Type!)).Append(")(").Append(((IFormattable)constant.Value!).ToString(null, CultureInfo.InvariantCulture)).Append(')');
                    break;
                case TypedConstantKind.Type:
                    if (constant.Value is ITypeSymbol typeValue)
                        _ = sb.Append("typeof(").Append(GetFullName(typeValue)).Append(')');
                    else
                        _ = sb.Append("null");
                    break;
                case TypedConstantKind.Array:
                    if (constant.IsNull)
                    {
                        _ = sb.Append("null");
                        break;
                    }
                    _ = sb.Append("new ").Append(GetFullName(((IArrayTypeSymbol)constant.Type!).ElementType)).Append("[] { ");
                    var pastFirstValue = false;
                    foreach (var value in constant.Values)
                    {
                        if (pastFirstValue)
                            _ = sb.Append(", ");
                        else
                            pastFirstValue = true;
                        TypedConstantToString(value, sb);
                    }
                    _ = sb.Append(" }");
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool CanBeReferencedAsCode(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.DeclaredAccessibility != Accessibility.Public && typeSymbol.DeclaredAccessibility != Accessibility.NotApplicable)
                return false;
            if (typeSymbol.IsRefLikeType)
                return false;
            if (typeSymbol.Kind == SymbolKind.FunctionPointerType)
                return false;

            if (typeSymbol.Kind == SymbolKind.NamedType && typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                foreach (var argument in namedTypeSymbol.TypeArguments)
                {
                    if (!CanBeReferencedAsCode(argument))
                        return false;
                }
            }
            if (typeSymbol.Kind == SymbolKind.ArrayType && typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                if (!CanBeReferencedAsCode(arrayTypeSymbol.ElementType))
                    return false;
            }
            return true;
        }

        public static string BoolString(bool value)
        {
            return value ? "true" : "false";
        }

        public static bool IsUnclosedGeneric(ITypeSymbol type)
        {
            switch (type)
            {
                case ITypeParameterSymbol:
                    return true;

                case IArrayTypeSymbol arrayType:
                    return IsUnclosedGeneric(arrayType.ElementType);

                case IPointerTypeSymbol pointerType:
                    return IsUnclosedGeneric(pointerType.PointedAtType);

                case INamedTypeSymbol namedType:
                    if (namedType.IsUnboundGenericType)
                        return true;

                    foreach (var arg in namedType.TypeArguments)
                        if (IsUnclosedGeneric(arg))
                            return true;

                    return false;

                default:
                    return false;
            }
        }

        public static INamedTypeSymbol? FindBase(string ns, string name, ITypeSymbol typeSymbol)
        {
            var baseType = typeSymbol.BaseType;
            while (baseType is not null)
            {
                if (baseType.ContainingNamespace.ToString() == ns && baseType.Name == name)
                    return baseType;
                baseType = baseType.BaseType;
            }
            return null;
        }
        public static bool IsTypeOrBase(string ns, string name, ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                return false;
            if (namedTypeSymbol != null && (Helper.FindBase(ns, name, namedTypeSymbol) != null || (namedTypeSymbol.Name == "MapDefinition" && namedTypeSymbol.ContainingNamespace.ToString() == "Zerra.Map")))
                return true;
            return false;
        }
    }
}
