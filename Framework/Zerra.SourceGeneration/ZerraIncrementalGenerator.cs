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
    /// <summary>
    /// An incremental Roslyn source generator that discovers types in the compilation and emits
    /// registration and initialization code for the Zerra framework at compile time.
    /// </summary>
    [Generator]
    public class ZerraIncrementalGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// Initializes the incremental generator by registering syntax and source output providers.
        /// </summary>
        /// <param name="context">The <see cref="IncrementalGeneratorInitializationContext"/> used to register providers and output actions.</param>
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

            //each declaration of a partial type is its own syntax node, the type must only be generated once
            var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var symbol in symbols)
            {
                if (!seen.Add(symbol))
                    continue;
                //filter
                discoverySymbols.Add(symbol);
            }

            //types in the global namespace have no name to build on
            var ns = symbols.Where(x => x.ContainingNamespace is not null && !x.ContainingNamespace.IsGlobalNamespace).Select(x => x.ContainingNamespace.ToString()).OrderBy(x => x.Length).FirstOrDefault() ?? "Unknown";

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

            GenerateInitializer(context, ns, sbInitializer, GetSuppressedDiagnosticIds(typesToGenerate));
        }

        //The initializer touches members of every model type, including types from other libraries marked [Obsolete(DiagnosticId = ...)]
        //or [Experimental(...)]. Those diagnostics carry their own IDs and fail projects built with TreatWarningsAsErrors, so each ID
        //found on the models, their members and the types those members use is suppressed in the generated file.
        private static string[] GetSuppressedDiagnosticIds(Dictionary<string, TypeToGenerate> models)
        {
            var ids = new HashSet<string>();
            var seenTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var model in models.Values)
            {
                for (var type = model.TypeSymbol; type is not null; type = type.BaseType)
                {
                    AddTypeDiagnosticIds(type, ids, seenTypes);
                    foreach (var member in type.GetMembers())
                    {
                        AddDiagnosticIds(member, ids);
                        switch (member)
                        {
                            case IPropertySymbol property:
                                AddTypeDiagnosticIds(property.Type, ids, seenTypes);
                                break;
                            case IFieldSymbol field:
                                AddTypeDiagnosticIds(field.Type, ids, seenTypes);
                                break;
                            case IMethodSymbol method:
                                AddTypeDiagnosticIds(method.ReturnType, ids, seenTypes);
                                foreach (var parameter in method.Parameters)
                                    AddTypeDiagnosticIds(parameter.Type, ids, seenTypes);
                                break;
                        }
                    }
                }
            }
            return ids.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        }

        private static void AddTypeDiagnosticIds(ITypeSymbol type, HashSet<string> ids, HashSet<ITypeSymbol> seenTypes)
        {
            if (!seenTypes.Add(type))
                return;
            AddDiagnosticIds(type, ids);
            AddDiagnosticIds(type.ContainingAssembly, ids);
            if (type is IArrayTypeSymbol arrayType)
                AddTypeDiagnosticIds(arrayType.ElementType, ids, seenTypes);
            else if (type is INamedTypeSymbol namedType)
            {
                foreach (var typeArgument in namedType.TypeArguments)
                    AddTypeDiagnosticIds(typeArgument, ids, seenTypes);
            }
        }

        private static void AddDiagnosticIds(ISymbol? symbol, HashSet<string> ids)
        {
            if (symbol is null)
                return;
            foreach (var attribute in symbol.GetAttributes())
            {
                var attributeClass = attribute.AttributeClass;
                if (attributeClass is null)
                    continue;
                var attributeNamespace = attributeClass.ContainingNamespace?.ToString();
                if (attributeClass.Name == "ObsoleteAttribute" && attributeNamespace == "System")
                {
                    foreach (var namedArgument in attribute.NamedArguments)
                    {
                        if (namedArgument.Key == "DiagnosticId" && namedArgument.Value.Value is string obsoleteId && obsoleteId.Length > 0)
                            _ = ids.Add(obsoleteId);
                    }
                }
                else if (attributeClass.Name == "ExperimentalAttribute" && attributeNamespace == "System.Diagnostics.CodeAnalysis")
                {
                    if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string experimentalId && experimentalId.Length > 0)
                        _ = ids.Add(experimentalId);
                }
            }
        }

        private static void GenerateInitializer(SourceProductionContext context, string ns, StringBuilder sbInitializer, string[] suppressedDiagnosticIds)
        {
            var lines = sbInitializer.ToString();
            var splits = lines.Split([EnvironmentHelper.NewLine], StringSplitOptions.RemoveEmptyEntries);

            var sbCallers = new StringBuilder();
            var sbMethods = new StringBuilder();
            var methodNumber = 1;
            var i = 0;
            while (i < splits.Length)
            {
                var split = splits[i++];
                
                _ = sbCallers.Append("            ").Append("M").Append(methodNumber).Append("();").Append(EnvironmentHelper.NewLine);
                _ = sbMethods.Append("        ").Append("private static void M").Append(methodNumber).Append("()");
                if (!split.StartsWith(" { "))
                    _ = sbMethods.Append(" => ");
                _ = sbMethods.Append(split).Append(EnvironmentHelper.NewLine);

                if (!split.EndsWith(";") && !split.EndsWith(" }"))
                {
                    while (i < splits.Length)
                    {
                        split = splits[i++];
                        _ = sbMethods.Append(split).Append(EnvironmentHelper.NewLine);
                        if (split.EndsWith(";"))
                            break;
                    }
                }

                methodNumber++;
            }

            //CS0436: a referenced assembly can expose the same generated type (such as Empty_*) to this one through InternalsVisibleTo; the local one is the one wanted
            var suppressions = "#pragma warning disable CS0436";
            if (suppressedDiagnosticIds.Length > 0)
                suppressions += $"{EnvironmentHelper.NewLine}#pragma warning disable {String.Join(", ", suppressedDiagnosticIds)}";

            var code = $$"""
                // <auto-generated/>
                #nullable enable
                #nullable disable warnings

                #pragma warning disable CS0612, CS0618
                {{suppressions}}

                namespace {{ns}}.SourceGeneration
                {
                    internal static class SourceGenerationInitializer
                    {
                #pragma warning disable CA2255
                        [System.Runtime.CompilerServices.ModuleInitializer]
                #pragma warning restore CA2255
                        public static void Initialize()
                        {
                            var timer = global::System.Diagnostics.Stopwatch.StartNew();
                {{sbCallers.ToString()}}
                            global::System.Console.WriteLine($"Source Generation Startup - {System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}: {timer.ElapsedMilliseconds} ms");
                        }

                {{sbMethods.ToString()}}
                    }
                }

                #if !NET5_0_OR_GREATER
                namespace System.Runtime.CompilerServices
                {
                    [global::System.AttributeUsage(global::System.AttributeTargets.Method, Inherited = false)]
                    internal sealed class ModuleInitializerAttribute : global::System.Attribute { }
                }
                #endif
                """;

            context.AddSource("ZerraSourceGenerationInitializer.cs", SourceText.From(code, Encoding.UTF8));
        }
    }
}
