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
                (context, cancellationToken) => (ITypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node)!
            ).Where(x => x.DeclaredAccessibility == Accessibility.Public && !x.IsStatic)
            .Collect();

            var compilationAndClasses = classProvider.Combine(context.CompilationProvider);

            context.RegisterSourceOutput(compilationAndClasses, (a, b) => GenerateTypesForZerra(a, b.Left, b.Right));
            context.RegisterSourceOutput(compilationAndClasses, (a, b) => Generate(a, b.Left, b.Right));
        }

        private static void Generate(SourceProductionContext context, ImmutableArray<ITypeSymbol> symbols, Compilation compilation)
        {
            if (compilation.Assembly.Name == "Zerra")
                return;

            var classList = new List<Tuple<string, string>>();
            foreach (var symbol in symbols.GroupBy(x => x.ToString()))
                TypeDetailSourceGenerator.GenerateType(context, symbol.First(), symbols, classList, true);
            if (classList.Count > 0)
                TypeDetailSourceGenerator.GenerateInitializer(context, compilation, classList);
        }

        private static void GenerateTypesForZerra(SourceProductionContext context, ImmutableArray<ITypeSymbol> symbols, Compilation compilation)
        {
            if (compilation.Assembly.Name != "Zerra")
                return;

            var target = symbols.First(x => x.Name == "TypesToGenerate");
            var typesToGenerate = target.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Select(x => x.Type).ToArray();
      
            foreach(var coreTypeSymbol in typesToGenerate)
            {
                var arraySymbol = compilation.CreateArrayTypeSymbol(coreTypeSymbol);
                symbols.Add(arraySymbol);
            }

            var classList = new List<Tuple<string, string>>();
            foreach (var symbol in typesToGenerate)
                TypeDetailSourceGenerator.GenerateType(context, symbol, symbols, classList, false);
            if (classList.Count > 0)
                TypeDetailSourceGenerator.GenerateInitializer(context, compilation, classList);
        }
    }
}
