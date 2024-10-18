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
    public static class BusRouterDispatcherGenerator
    {
        public static void Generate(SourceProductionContext context, string ns, StringBuilder sbInitializer, ITypeSymbol symbol)
        {
            if (symbol.TypeKind != TypeKind.Interface)
                return;
            if (symbol is not INamedTypeSymbol namedTypeSymbol)
                return;
            if (!namedTypeSymbol.AllInterfaces.Any(x => x.Name == "ICommandHandler" || x.Name == "IEventHandler"))
                return;

            var typeNameForClass = Helpers.GetNameForClass(symbol);
            var className = $"Dispatcher_{typeNameForClass}";

            var sb = new StringBuilder();

            WriteMembers(namedTypeSymbol, namedTypeSymbol, sb);

            foreach (var i in namedTypeSymbol.AllInterfaces)
                WriteMembers(namedTypeSymbol, i, sb);

            var membersLines = sb.ToString();

            var code = $$"""
                //Zerra Generated File
                #if NET5_0_OR_GREATER

                namespace {{ns}}.SourceGeneration
                {
                    public class {{className}} : {{symbol.ToString()}}
                    {
                        private readonly bool requireAffirmation;
                        private readonly Zerra.CQRS.NetworkType networkType;
                        private readonly string source;
                        private readonly Zerra.CQRS.BusLogging busLogging;
                        public {{className}}(bool requireAffirmation, Zerra.CQRS.NetworkType networkType, string source, Zerra.CQRS.BusLogging busLogging)
                        {
                            this.requireAffirmation = requireAffirmation;
                            this.networkType = networkType;
                            this.source = source;
                            this.busLogging = busLogging;
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
            _ = sbInitializer.Append("Zerra.Reflection.SourceGenerationRegistration.RegisterDispatcher(").Append(interfacefullTypeOf).Append(", ").Append(classFullTypeOf).Append(");");
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

                if (method.ContainingType.Name == "ICommandHandler")
                {
                    if (method.ContainingType.TypeArguments.Length == 1)
                    {
                        var parameter = method.Parameters[0];
                        var messageTypeOf = Helpers.GetTypeOfName(parameter.Type);
                        _ = sb.Append("Zerra.CQRS.Bus._DispatchCommandInternalAsync(@").Append(parameter.Name).Append(", ").Append(messageTypeOf).Append(", this.requireAffirmation, this.networkType, this.source, this.busLogging);");
                    }
                    else
                    {
                        var parameter = method.Parameters[0];
                        var messageTypeOf = Helpers.GetTypeOfName(parameter.Type);
                        _ = sb.Append("Zerra.CQRS.Bus._DispatchCommandWithResultInternalAsync(@").Append(parameter.Name).Append(", ").Append(messageTypeOf).Append(", this.networkType, this.source, this.busLogging)!;");
                    }
                }
                else if (method.ContainingType.Name == "IEventHandler")
                {
                    var parameter = method.Parameters[0];
                    var messageTypeOf = Helpers.GetTypeOfName(parameter.Type);
                    _ = sb.Append("Zerra.CQRS.Bus._DispatchEventInternalAsync(@").Append(parameter.Name).Append(", ").Append(messageTypeOf).Append(", this.networkType, this.source, this.busLogging);");
                }
                else
                {
                    _ = sb.Append("throw new System.NotSupportedException();");
                }
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
