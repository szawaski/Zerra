// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using Zerra.SourceGeneration.Discovery;

namespace Zerra.SourceGeneration
{
    [Generator]
    public class ZerraIncrementalGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(
                (node, cancellationToken) => node is BaseTypeDeclarationSyntax || node is InterfaceDeclarationSyntax,
                (context, cancellationToken) => (ITypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node)!
            )
            .Where(x => x != null)
            .Collect();

            context.RegisterSourceOutput(syntaxProvider, (a, b) => SourceOutput(a, b));
        }

        private static void SourceOutput(SourceProductionContext context, ImmutableArray<ITypeSymbol> symbols)
        {
            var discoverySymbols = new List<ITypeSymbol>();

            foreach (var symbol in symbols)
            {
                //filter
                discoverySymbols.Add(symbol);
            }

            var ns = symbols.Where(x => x.ContainingNamespace is not null).Select(x => x.ContainingNamespace.ToString()).OrderBy(x => x.Length).FirstOrDefault() ?? "Unknown";

            var sbInitializer = new StringBuilder();
            var typesToGenerate = new Dictionary<string, TypeToGenerate>();
            foreach (var symbol in discoverySymbols)
            {
                TypeFinder.FindModels(symbol, typesToGenerate);
                BusRouterGenerator.Generate(context, ns, sbInitializer, symbol);
                BusHandlerGenerator.Generate(sbInitializer, symbol);
                BusCommandOrEventInfoGenerator.Generate(sbInitializer, symbol);
                TypeFinderGenerator.Generate(sbInitializer, symbol);
                EnumGenerator.Generate(sbInitializer, symbol);
            }
            TypesGenerator.Generate(sbInitializer, typesToGenerate);
            EmptyImplementationGenerator.Generate(context, ns, sbInitializer, typesToGenerate);
            SerializerAndMapGenerator.Generate(sbInitializer, typesToGenerate);

            GenerateInitializer(context, ns, sbInitializer);
        }


        private static void GenerateInitializer(SourceProductionContext context, string ns, StringBuilder sbInitializer)
        {
            var lines = sbInitializer.ToString();

            var code = $$"""
                //Zerra Generated File

                namespace {{ns}}.SourceGeneration
                {
                    internal static class SourceGenerationInitializer
                    {
                #pragma warning disable CA2255
                        [System.Runtime.CompilerServices.ModuleInitializer]
                #pragma warning restore CA2255
                        public static void Initialize()
                        {
                            var timer = global::System.Diagnostics.Stopwatch.StartNew();{{lines}}
                            global::System.Console.WriteLine($"Source Generation Startup - {System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}: {timer.ElapsedMilliseconds} ms");
                        }
                    }
                }
                """;

            context.AddSource("ZerraSourceGenerationInitializer.cs", SourceText.From(code, Encoding.UTF8));
        }
    }
}
