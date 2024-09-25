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

            context.RegisterSourceOutput(compilationAndClasses, (a, b) => Generate(a, b.Left, b.Right));
        }

        private static void Generate(SourceProductionContext context, ImmutableArray<INamedTypeSymbol> symbols, Compilation compilation)
        {
            var classList = new List<Tuple<string, string>>();
            foreach (var symbol in symbols.GroupBy(x => x.ToString()))
                TypeDetailSourceGenerator.GenerateType(context, symbol.First(), symbols, classList, true);
            TypeDetailSourceGenerator.GenerateInitializer(context, classList);
        }
    }
}
