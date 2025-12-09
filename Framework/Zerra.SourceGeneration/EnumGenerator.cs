// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using System.Text;

namespace Zerra.SourceGeneration
{
    public static class EnumGenerator
    {
        public static void Generate(StringBuilder sb, ITypeSymbol typeSymbol)
        {
            if (typeSymbol.TypeKind != TypeKind.Enum)
                return;
            if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                return;
            if (namedTypeSymbol.EnumUnderlyingType == null)
                return;
            if (namedTypeSymbol.DeclaredAccessibility != Accessibility.Public)
                return;

            var typeName = Helper.GetFullName(typeSymbol);
            var typeOfName = Helper.GetTypeOfName(typeSymbol);
            _ = TypeLookup.CoreEnumTypeLookup(namedTypeSymbol.EnumUnderlyingType.Name, out CoreEnumType enumType);
            var hasFlagsAttribute = namedTypeSymbol.GetAttributes().Any(x => x.AttributeClass?.Name == "FlagsAttribute");

            _ = sb.Append(Environment.NewLine).Append("            ");
            _ = sb.Append("global::Zerra.Reflection.Register.Enum(").Append(typeOfName).Append(", ");
            _ = sb.Append("global::Zerra.Reflection.CoreEnumType." + enumType.ToString()).Append(", ");
            _ = sb.Append(Helper.BoolString(hasFlagsAttribute)).Append(", ");
            GenerateFields(sb, namedTypeSymbol);
            _ = sb.Append(", ");
            _ = sb.Append("() => default(").Append(typeName).Append("), ");

            _ = sb.Append("(object value1, object value2) => ");
            switch (enumType)
            {
                case CoreEnumType.Byte: _ = sb.Append("(").Append(typeName).Append(")(object)(byte)((byte)value1 | (byte)value2)"); break;
                case CoreEnumType.SByte: _ = sb.Append("(").Append(typeName).Append(")(object)(sbyte)((sbyte)value1 | (sbyte)value2)"); break;
                case CoreEnumType.Int16: _ = sb.Append("(").Append(typeName).Append(")(object)(short)((short)value1 | (short)value2)"); break;
                case CoreEnumType.UInt16: _ = sb.Append("(").Append(typeName).Append(")(object)(ushort)((ushort)value1 | (ushort)value2)"); break;
                case CoreEnumType.Int32: _ = sb.Append("(").Append(typeName).Append(")(object)(int)((int)value1 | (int)value2)"); break;
                case CoreEnumType.UInt32: _ = sb.Append("(").Append(typeName).Append(")(object)(uint)((uint)value1 | (uint)value2)"); break;
                case CoreEnumType.Int64: _ = sb.Append("(").Append(typeName).Append(")(object)(long)((long)value1 | (long)value2)"); break;
                case CoreEnumType.UInt64: _ = sb.Append("(").Append(typeName).Append(")(object)(ulong)((ulong)value1 | (ulong)value2)"); break;
            }
            _ = sb.Append(");");
        }

        private static void GenerateFields(StringBuilder sb, INamedTypeSymbol namedTypeSymbol)
        {
            _ = sb.Append("[");

            var typeName = Helper.GetFullName(namedTypeSymbol);
            var fields = namedTypeSymbol.GetMembers().Where(x => x.Kind == SymbolKind.Field && x.DeclaredAccessibility == Accessibility.Public).Cast<IFieldSymbol>().ToList();

            var hasFirst = false;
            foreach (var @field in fields)
            {
                if (hasFirst)
                    _ = sb.Append(", ");
                else
                    hasFirst = true;

                var text = @field.Name;
                var enumNameAttribute = @field.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "EnumName");
                if (enumNameAttribute != null)
                {
                    var textArg = enumNameAttribute.ConstructorArguments.FirstOrDefault();
                    if (textArg.Value is string enumNameValue)
                        text = enumNameValue;
                }

                _ = sb.Append(Environment.NewLine).Append("                ");

                _ = sb.Append("new global::EnumName.EnumFieldInfo(");
                _ = sb.Append("\"").Append(@field.Name).Append("\", ");
                _ = sb.Append("\"").Append(text).Append("\", ");
                _ = sb.Append(typeName).Append(".").Append(@field.Name);
                _ = sb.Append(")");
            }

            _ = sb.Append("]");
        }
    }
}

