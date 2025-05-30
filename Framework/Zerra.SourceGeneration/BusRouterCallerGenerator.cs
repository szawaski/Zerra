﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;

namespace Zerra.SourceGeneration.Discovery
{
    public static class BusRouterCallerGenerator
    {
        public static void Generate(SourceProductionContext context, string ns, StringBuilder sbInitializer, ITypeSymbol symbol)
        {
            if (symbol.TypeKind != TypeKind.Interface)
                return;
            if (symbol is not INamedTypeSymbol namedTypeSymbol)
                return;
            if (namedTypeSymbol.AllInterfaces.Any(x => x.Name == "ICommandHandler" || x.Name == "IEventHandler"))
                return;
            var typeNameForClass = Helpers.GetNameForClass(symbol);
            var className = $"Caller_{typeNameForClass}";

            var sb = new StringBuilder();

            WriteMembers(namedTypeSymbol, namedTypeSymbol, sb);

            foreach (var i in namedTypeSymbol.AllInterfaces)
                WriteMembers(namedTypeSymbol, i, sb);

            var membersLines = sb.ToString();
            sb.Clear();

            var code = $$"""
                //Zerra Generated File
                #if NET5_0_OR_GREATER

                namespace {{ns}}.SourceGeneration
                {
                    public class {{className}} : {{symbol.ToString()}}
                    {
                        private readonly Zerra.CQRS.NetworkType networkType;
                        private readonly bool isFinalLayer;
                        private readonly string source;
                        public {{className}}(Zerra.CQRS.NetworkType networkType, bool isFinalLayer, string source)
                        {
                            this.networkType = networkType;
                            this.isFinalLayer = isFinalLayer;
                            this.source = source;
                        }

                        {{membersLines}}
                    }
                }

                #endif
            
                """;

            context.AddSource($"{className}.cs", SourceText.From(code, Encoding.UTF8));

            var interfacefullTypeOf = Helpers.GetTypeOfName(symbol);
            var classFullTypeOf = $"typeof({ns}.SourceGeneration.{className})";
            _ = sbInitializer.Append(Environment.NewLine).Append("            ");
            _ = sbInitializer.Append("Zerra.Reflection.SourceGenerationRegistration.RegisterCaller(").Append(interfacefullTypeOf).Append(", ").Append(classFullTypeOf).Append(");");
        }

        private static void WriteMembers(INamedTypeSymbol parent, INamedTypeSymbol namedTypeSymbol, StringBuilder sb)
        {
            var members = namedTypeSymbol.GetMembers();

            var parentTypeOf = Helpers.GetTypeOfName(parent);
            foreach (IMethodSymbol method in members.Where(x => x.Kind == SymbolKind.Method))
            {
                if (method.MethodKind != MethodKind.Ordinary)
                    continue;

                if (sb.Length > 0)
                    _ = sb.Append(Environment.NewLine).Append("        ");

                _ = sb.Append("public ").Append(method.ReturnsVoid ? "void" : method.ReturnType.ToString()).Append(' ').Append(method.Name).Append('(');
                var firstPassed = false;
                foreach (var parameter in method.Parameters)
                {
                    if (firstPassed)
                        _ = sb.Append(", ");
                    else
                        firstPassed = true;
                    _ = sb.Append(parameter.Type.ToString()).Append(" @").Append(parameter.Name);
                }
                _ = sb.Append(") => ");


                _ = sb.Append("Zerra.CQRS.Bus._CallMethod<").Append(method.ReturnType.ToString()).Append(">(")
                    .Append(parentTypeOf).Append(", \"").Append(method.Name).Append("\", [");
                firstPassed = false;
                foreach (var parameter in method.Parameters)
                {
                    if (firstPassed)
                        _ = sb.Append(", ");
                    else
                        firstPassed = true;
                    sb.Append('@').Append(parameter.Name);
                }
                _ = sb.Append("], this.networkType, this.isFinalLayer, this.source);");
            }

            foreach (IPropertySymbol property in members.Where(x => x.Kind == SymbolKind.Property))
            {
                if (sb.Length > 0)
                    _ = sb.Append(Environment.NewLine).Append("        ");

                _ = sb.Append("public ").Append(property.Type.ToString()).Append(" @").Append(property.Name).Append(" {");
                if (property.GetMethod is not null)
                    _ = sb.Append(" get => throw new System.NotSupportedException();");
                if (property.SetMethod is not null)
                    _ = sb.Append(" set => throw new System.NotSupportedException();");
                _ = sb.Append(" }");
            }
        }
    }
}
