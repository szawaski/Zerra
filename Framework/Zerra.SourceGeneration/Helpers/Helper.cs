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
            if (HasPointer(typeSymbol))
                return "typeof(nint)";
            if (typeSymbol.TypeKind == TypeKind.Dynamic)
                return "typeof(object)";
            if (typeSymbol.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Void)
                return "typeof(void)";
            if (typeSymbol is IArrayTypeSymbol && !IsUnclosedGeneric(typeSymbol))
            {
                var sbArray = new StringBuilder();
                _ = sbArray.Append("typeof(");
                GetTypeOfNameRecursive(typeSymbol, sbArray);
                _ = sbArray.Append(')');
                if (isByRef)
                    _ = sbArray.Append(".MakeByRefType()");
                return sbArray.ToString();
            }
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
                    AppendQualifiedName(typeSymbol, sb);
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
            if (typeSymbol.TypeKind == TypeKind.Dynamic)
            {
                _ = sb.Append("global::System.Object");
                return;
            }
            if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                //int[][,] is an array of int[,], the outermost rank is written first
                var ranks = new List<int>();
                ITypeSymbol elementType = arrayTypeSymbol;
                while (elementType is IArrayTypeSymbol elementArrayTypeSymbol)
                {
                    ranks.Add(elementArrayTypeSymbol.Rank);
                    elementType = elementArrayTypeSymbol.ElementType;
                }
                GetTypeOfNameRecursive(elementType, sb);
                foreach (var rank in ranks)
                    _ = sb.Append('[').Append(',', rank - 1).Append(']');
                return;
            }
            if (String.IsNullOrEmpty(typeSymbol.Name))
            {
                _ = sb.Append(GetFullName(typeSymbol));
                return;
            }

            AppendQualifiedName(typeSymbol, sb);

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

        private static void AppendQualifiedName(ITypeSymbol typeSymbol, StringBuilder sb)
        {
            if (typeSymbol.ContainingType is not null)
                _ = sb.Append(typeSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append('.');
            else if (typeSymbol.ContainingNamespace is { IsGlobalNamespace: false })
                _ = sb.Append(typeSymbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append('.');
            else
                _ = sb.Append("global::");
            if (SyntaxFacts.GetKeywordKind(typeSymbol.Name) != SyntaxKind.None)
                _ = sb.Append('@');
            _ = sb.Append(typeSymbol.Name);
        }

        public static bool IsGenericDefined(INamedTypeSymbol namedTypeSymbol)
        {
            foreach (var typeArgument in namedTypeSymbol.TypeArguments)
            {
                var argument = typeArgument;
                while (argument is IArrayTypeSymbol arrayTypeSymbol)
                    argument = arrayTypeSymbol.ElementType;
                if (argument.Kind == SymbolKind.ErrorType)
                    return false;
                if (argument.Kind == SymbolKind.TypeParameter)
                    return false;
                if (argument.TypeKind == TypeKind.Dynamic)
                    continue;
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
            //a tuple type such as (Guid ID, string Name) brings parentheses
            name = name.Replace('<', '_').Replace('>', '_').Replace('(', '_').Replace(')', '_').Replace('.', '_').Replace(",", "_").Replace(" ", String.Empty).Replace("[]", "Array");
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

        //generated code is in its own class, so it can only name types any code in the assembly can
        public static bool IsWritable(ITypeSymbol typeSymbol, IAssemblySymbol assembly)
        {
            if (typeSymbol.IsAnonymousType)
                return false;
            switch (typeSymbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                case Accessibility.NotApplicable:
                    break;
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                    if (!SymbolEqualityComparer.Default.Equals(typeSymbol.ContainingAssembly, assembly) && (typeSymbol.ContainingAssembly is null || !typeSymbol.ContainingAssembly.GivesAccessTo(assembly)))
                        return false;
                    break;
                default:
                    return false;
            }
            if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
                return IsWritable(arrayTypeSymbol.ElementType, assembly);
            if (typeSymbol is IPointerTypeSymbol pointerTypeSymbol)
                return IsWritable(pointerTypeSymbol.PointedAtType, assembly);
            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.IsFileLocal)
                    return false;
                foreach (var typeArgument in namedTypeSymbol.TypeArguments)
                {
                    if (!IsWritable(typeArgument, assembly))
                        return false;
                }
                if (namedTypeSymbol.ContainingType is not null)
                    return IsWritable(namedTypeSymbol.ContainingType, assembly);
            }
            return true;
        }

        public static bool CanBeTypeArgument(ITypeSymbol typeSymbol)
            => !typeSymbol.IsRefLikeType && !HasPointer(typeSymbol) && !HasStaticAbstract(typeSymbol);

        public static bool HasStaticAbstract(ITypeSymbol typeSymbol)
            => typeSymbol.TypeKind == TypeKind.Interface && typeSymbol.GetMembers().Concat(typeSymbol.AllInterfaces.SelectMany(x => x.GetMembers())).Any(x => x.IsStatic && x.IsAbstract);

        public static bool IsAccessorVisible(IMethodSymbol accessor, IAssemblySymbol assembly)
        {
            switch (accessor.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return true;
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                    return SymbolEqualityComparer.Default.Equals(accessor.ContainingAssembly, assembly) || (accessor.ContainingAssembly is not null && accessor.ContainingAssembly.GivesAccessTo(assembly));
                default:
                    return false;
            }
        }

        public static bool IsAccessorUsed(IMethodSymbol accessor)
            => accessor.DeclaredAccessibility == Accessibility.Public || accessor.DeclaredAccessibility == Accessibility.Internal || accessor.DeclaredAccessibility == Accessibility.ProtectedOrInternal;

        //a nested type such as Dictionary<TKey, TValue>.KeyCollection takes its containing type's type arguments first, as reflection lists them
        public static List<ITypeSymbol> GetAllTypeArguments(INamedTypeSymbol namedTypeSymbol)
        {
            var typeArguments = new List<ITypeSymbol>();
            for (var typeSymbol = namedTypeSymbol; typeSymbol is not null; typeSymbol = typeSymbol.ContainingType)
                typeArguments.InsertRange(0, typeSymbol.TypeArguments);
            return typeArguments;
        }

        public static bool IsInGenericType(INamedTypeSymbol namedTypeSymbol)
        {
            for (var typeSymbol = namedTypeSymbol; typeSymbol is not null; typeSymbol = typeSymbol.ContainingType)
            {
                if (typeSymbol.IsGenericType)
                    return true;
            }
            return false;
        }

        public static bool HasPointer(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.Kind == SymbolKind.PointerType || typeSymbol.Kind == SymbolKind.FunctionPointerType)
                return true;
            if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
                return HasPointer(arrayTypeSymbol.ElementType);
            return false;
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
                if (namedTypeSymbol.IsFileLocal)
                    return false;
                for (var containingType = namedTypeSymbol.ContainingType; containingType is not null; containingType = containingType.ContainingType)
                {
                    if (containingType.DeclaredAccessibility != Accessibility.Public || containingType.IsFileLocal)
                        return false;
                }
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

                    //a nested type such as SortedDictionary<TKey, int>.KeyCollection has no type arguments of its own
                    if (namedType.ContainingType is not null)
                        return IsUnclosedGeneric(namedType.ContainingType);

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
