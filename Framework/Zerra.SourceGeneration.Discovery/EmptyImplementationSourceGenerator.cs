// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zerra.SourceGeneration.Discovery
{
    public static class EmptyImplementationSourceGenerator
    {
        public static void Generate(SourceProductionContext context, string ns, StringBuilder sbInitializer, List<ITypeSymbol> discoverySymbols)
        {
            var sb = new StringBuilder();
            foreach (var symbol in discoverySymbols)
            {
                if (symbol.TypeKind != TypeKind.Interface)
                    continue;
                if (symbol is not INamedTypeSymbol namedTypeSymbol)
                    continue;

                var typeNameForClass = Helpers.GetNameForClass(symbol);
                var className = $"Empty_{typeNameForClass}";

                WriteMembers(namedTypeSymbol, sb);

                foreach (var i in namedTypeSymbol.AllInterfaces)
                    WriteMembers(i, sb);

                var membersLines = sb.ToString();
                sb.Clear();

                var code = $$"""
                //Zerra Generated File
                #if NET5_0_OR_GREATER

                namespace {{ns}}.SourceGeneration
                {
                    public class {{className}} : {{symbol.ToString()}}
                    {
                        {{membersLines}}
                    }
                }

                #endif
            
                """;

                context.AddSource($"{className}.cs", SourceText.From(code, Encoding.UTF8));

                var interfacefullTypeOf = Helpers.GetTypeOfName(symbol);
                var classFullTypeOf = $"typeof({className})";
                _ = sbInitializer.Append(Environment.NewLine).Append("            ");
                _ = sbInitializer.Append("Zerra.Reflection.SourceGenerationRegistration.RegisterEmptyImplementation(").Append(interfacefullTypeOf).Append(", ").Append(classFullTypeOf).Append(");");
            }
        }

        private static void WriteMembers(INamedTypeSymbol namedTypeSymbol, StringBuilder sb)
        {
            var members = namedTypeSymbol.GetMembers();

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
                _ = sb.Append(')');
                if (method.ReturnsVoid)
                {
                    sb.Append(" { }");
                }
                else
                {
                    string returnValue;
                    if (method.ReturnType.Name == "Task")
                    {
                        var namedReturnType = (INamedTypeSymbol)method.ReturnType;
                        if (namedReturnType.TypeParameters.Length > 0)
                            returnValue = $"System.Threading.Tasks.Task.FromResult(default({namedReturnType.TypeArguments[0].ToString()}))";
                        else
                            returnValue = "System.Threading.Tasks.Task.CompletedTask";
                    }
                    else
                    {
                        returnValue = $"default({method.ReturnType.ToString()})";
                    }

                    sb.Append(" => ").Append(returnValue).Append("!;");
                }
            }

            foreach (IPropertySymbol property in members.Where(x => x.Kind == SymbolKind.Property))
            {
                if (sb.Length > 0)
                    _ = sb.Append(Environment.NewLine).Append("        ");

                _ = sb.Append("public ").Append(property.Type.ToString()).Append(" @").Append(property.Name).Append(" {");
                if (property.GetMethod is not null)
                    _ = sb.Append(" get;");
                if (property.SetMethod is not null)
                    _ = sb.Append(" set;");
                _ = sb.Append(" }");
            }
        }
    }
}
