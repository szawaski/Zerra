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
using System.Reflection;

namespace Zerra.SourceGeneration
{
    [Generator]
    public class ZerraIncrementalGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var compilationProvider = context.CompilationProvider
                .Select((compilation, cancellationToken) =>
                {
                    var optionsField = typeof(CSharpCompilation).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic);
                    var options = (CSharpCompilationOptions)optionsField.GetValue(compilation);

                    var metadataImportOptionsProperty = typeof(CSharpCompilationOptions).GetProperty(nameof(CSharpCompilationOptions.MetadataImportOptions), BindingFlags.Instance | BindingFlags.Public);
                    metadataImportOptionsProperty.SetValue(options, MetadataImportOptions.All);

                    return compilation;
                });

            var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(
                (node, cancellationToken) => node is BaseTypeDeclarationSyntax || node is InterfaceDeclarationSyntax,
                (context, cancellationToken) => (ITypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node)!
            )
            .Collect();

            //compilationProvider must be first in Combine for changes in compilation to work in syntaxProvider
            var compilationAndClasses = compilationProvider.Combine(syntaxProvider);

            context.RegisterSourceOutput(compilationAndClasses, (a, b) => SourceOutput(a, b.Right, b.Left));
        }

        private static void SourceOutput(SourceProductionContext context, ImmutableArray<ITypeSymbol> symbols, Compilation compilation)
        {
            if (compilation.Assembly.Name == "Zerra"
#if DEBUG
                || compilation.Assembly.Name == "TestProject"
#endif
                )
                GenerateForZerra(context, symbols, compilation);
            else
                GenerateForRegular(context, symbols, compilation);
        }

        private static void GenerateForRegular(SourceProductionContext context, ImmutableArray<ITypeSymbol> symbols, Compilation compilation)
        {
            var symbolsToGenerate = new List<ITypeSymbol>();
            foreach (var symbol in symbols)
            {
                if (symbol.Kind != SymbolKind.NamedType || symbol is not INamedTypeSymbol namedTypeSymbol)
                    continue;
                if (namedTypeSymbol.TypeKind != TypeKind.Class && namedTypeSymbol.TypeKind != TypeKind.Struct && namedTypeSymbol.TypeKind != TypeKind.Enum)
                    continue;

                var members = namedTypeSymbol.GetMembers();
                var methods = members.Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().ToArray();
                var constructors = methods.Where(x => x.MethodKind == MethodKind.Constructor).ToArray();

                //only types, properties, fields, methods
                if (members.Count(x => x.Kind != SymbolKind.NamedType || x.Kind != SymbolKind.Property || x.Kind != SymbolKind.Field || x.Kind != SymbolKind.Method) > 0)
                    continue;
                //only one constructor method
                if (methods.Length != constructors.Length && constructors.Length != 1)
                    continue;
                //only one parameterless constructor method
                if (((IMethodSymbol)constructors[0]).Parameters.Length > 0)
                    continue;

                symbolsToGenerate.Add(symbol);
            }

            var classList = new List<Tuple<string, string>>();
            foreach (var symbol in symbols.GroupBy(x => x.ToString()))
                TypeDetailSourceGenerator.GenerateType(context, symbol.First(), symbols, classList, true);
            if (classList.Count > 0)
                TypeDetailSourceGenerator.GenerateInitializer(context, compilation, classList);
        }

        private static void GenerateForZerra(SourceProductionContext context, ImmutableArray<ITypeSymbol> symbols, Compilation compilation)
        {
            var symbolsToGenerate = new List<ITypeSymbol>();

            var helperClass = symbols.First(x => x.Name == "TypesToGenerate");
            foreach (var item in helperClass.GetMembers().Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>())
            {
                if (item.Name.EndsWith("MakeUnbounded"))
                    symbolsToGenerate.Add(((INamedTypeSymbol)item.Type).ConstructUnboundGenericType());
                else
                    symbolsToGenerate.Add(item.Type);
            }

            foreach (var symbol in symbols)
            {
                var s = symbol;
                while (s is not null)
                {
                    if (s.GetAttributes().Any(x => x.AttributeClass?.Name == "GenerateTypeDetailAttribute"))
                    {
                        symbolsToGenerate.Add(symbol);
                        break;
                    }
                    s = s.BaseType;
                }
            }

            var classList = new List<Tuple<string, string>>();
            foreach (var symbol in symbolsToGenerate)
                TypeDetailSourceGenerator.GenerateType(context, symbol, symbols, classList, false);
            if (classList.Count > 0)
                TypeDetailSourceGenerator.GenerateInitializer(context, compilation, classList);
        }
    }
}
