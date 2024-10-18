// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Zerra.SourceGeneration.Discovery
{
    [Generator]
    public class ZerraIncrementalGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(
                (node, cancellationToken) => node is BaseTypeDeclarationSyntax || node is InterfaceDeclarationSyntax,
                (context, cancellationToken) => (ITypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node)!
            ).Collect();

            context.RegisterSourceOutput(syntaxProvider, (a, b) => SourceOutput(a, b));
        }

        private static void SourceOutput(SourceProductionContext context, ImmutableArray<ITypeSymbol> symbols)
        {
            var discoverySymbols = new List<ITypeSymbol>();

            foreach (var symbol in symbols)
            {
                if (symbol.GetAttributes().Any(IsDiscoveryAttribute))
                {
                    discoverySymbols.Add(symbol);
                }
                else if (symbol.TypeKind == TypeKind.Interface || symbol.Interfaces.Length > 0)
                {
                    discoverySymbols.Add(symbol);
                }
            }

            var ns = symbols.Where(x => x.ContainingNamespace is not null).Select(x => x.ContainingNamespace.ToString()).OrderBy(x => x.Length).FirstOrDefault() ?? "Unknown";

            var sbInitializer = new StringBuilder();
            var firstPass = false;
            foreach (var symbol in discoverySymbols)
            {
                if (!firstPass)
                {
                    var fullTypeOf = Helpers.GetTypeOfName(symbol);
                    _ = sbInitializer.Append(Environment.NewLine).Append("            ");
                    _ = sbInitializer.Append("Zerra.Reflection.SourceGenerationRegistration.MarkAssemblyAsDiscovered(").Append(fullTypeOf).Append(".Assembly);");
                    firstPass = true;
                }

                DiscoveryGenerator.Generate(context, ns, sbInitializer, symbol);
                EmptyImplementationGenerator.Generate(context, ns, sbInitializer, symbol);
                BusRouterCallerGenerator.Generate(context, ns, sbInitializer, symbol);
                BusRouterDispatcherGenerator.Generate(context, ns, sbInitializer, symbol);
                TransactStoreGenerator.Generate(context, ns, sbInitializer, symbol);
                EventStoreAsTransactStoreGenerator.Generate(context, ns, sbInitializer, symbol);
                EventStoreGenerator.Generate(context, ns, sbInitializer, symbol);
            }

            _ = sbInitializer.Append(Environment.NewLine).Append("            ");
            _ = sbInitializer.Append("Zerra.Reflection.SourceGenerationRegistration.RunGenerationsFromAttributes();");

            GenerateInitializer(context, ns, sbInitializer);
        }

        private static bool IsDiscoveryAttribute(AttributeData attribute)
        {
            if (attribute.AttributeClass is null)
                return false;
            if (attribute.AttributeClass.Name == "DiscoverAttribute" && attribute.AttributeClass.ContainingNamespace.ToString() == "Zerra.Reflection")
                return true;
            if (attribute.AttributeClass.Name == "ServiceExposedAttribute" && attribute.AttributeClass.ContainingNamespace.ToString() == "Zerra.CQRS")
                return true;
            if (attribute.AttributeClass.Name == "EntityAttribute" && attribute.AttributeClass.ContainingNamespace.ToString() == "Zerra.Repository")
                return true;
            if (attribute.AttributeClass.Name == "EventStoreAggregateAttribute" && attribute.AttributeClass.ContainingNamespace.ToString() == "Zerra.Repository")
                return true;
            if (attribute.AttributeClass.Name == "EventStoreEntityAttribute" && attribute.AttributeClass.ContainingNamespace.ToString() == "Zerra.Repository")
                return true;
            if (attribute.AttributeClass.Name == "TransactStoreEntityAttribute" && attribute.AttributeClass.ContainingNamespace.ToString() == "Zerra.Repository")
                return true;
            return false;
        }

        private static void GenerateInitializer(SourceProductionContext context, string ns, StringBuilder sbInitializer)
        {
            var lines = sbInitializer.ToString();

            var code = $$"""
                //Zerra Generated File
                #if NET5_0_OR_GREATER

                namespace {{ns}}.SourceGeneration
                {
                    internal static class DiscoveryInitializer
                    {
                #pragma warning disable CA2255
                        [System.Runtime.CompilerServices.ModuleInitializer]
                #pragma warning restore CA2255
                        public static void Initialize()
                        {{{lines}}
                        }
                    }
                }

                #endif
            
                """;

            context.AddSource("DiscoveryInitializer.cs", SourceText.From(code, Encoding.UTF8));
        }
    }
}
