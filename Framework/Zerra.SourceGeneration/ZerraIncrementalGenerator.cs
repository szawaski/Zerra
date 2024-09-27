// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Zerra.SourceGeneration
{
    [Generator]
    public class ZerraIncrementalGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classProvider = context.SyntaxProvider.CreateSyntaxProvider(
                (node, cancellationToken) => node is BaseTypeDeclarationSyntax || node is InterfaceDeclarationSyntax,
                (context, cancellationToken) => (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node)!
            ).Where(x => x.DeclaredAccessibility == Accessibility.Public && !x.IsStatic)
            .Collect();

            var compilationAndClasses = classProvider.Combine(context.CompilationProvider);

            context.RegisterSourceOutput(compilationAndClasses, (a, b) => GenerateCoreTypes(a, b.Right));
            context.RegisterSourceOutput(compilationAndClasses, (a, b) => Generate(a, b.Left, b.Right));
        }

        private static void Generate(SourceProductionContext context, ImmutableArray<INamedTypeSymbol> symbols, Compilation compilation)
        {
            if (compilation.Assembly.Name == "Zerra")
                return;

            var classList = new List<Tuple<string, string>>();
            foreach (var symbol in symbols.GroupBy(x => x.ToString()))
            {
                TypeDetailSourceGenerator.GenerateType(context, symbol.First(), symbols, classList, true);
            }
            if (classList.Count > 0)
            {
                TypeDetailSourceGenerator.GenerateInitializer(context, compilation, classList);
            }
        }

        private static void GenerateCoreTypes(SourceProductionContext context, Compilation compilation)
        {
            if (compilation.Assembly.Name != "Zerra")
                return;

            var assembly = compilation.SourceModule.ReferencedAssemblySymbols.Where(x => x.MetadataName == "System.Runtime").FirstOrDefault();
            var allSymbols = assembly.GetForwardedTypes();
            var symbols = allSymbols.Where(x => TypeLookup.GetCoreTypeNames.Contains(x.Name)).ToImmutableArray();

            var classList = new List<Tuple<string, string>>();
            foreach (var symbol in symbols)
            {
                TypeDetailSourceGenerator.GenerateType(context, symbol, symbols, classList, false);
            }
            if (classList.Count > 0)
            {
                TypeDetailSourceGenerator.GenerateInitializer(context, compilation, classList);
            }
        }
    }
}
