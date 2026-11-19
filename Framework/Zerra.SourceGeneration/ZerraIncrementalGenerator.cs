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
        private static readonly DiagnosticDescriptor languageVersionDescriptor = new(
            "ZERRA001",
            "Zerra requires C# 9 or later",
            "Zerra source generation requires C# 9 or later but this project uses C# {0}; set <LangVersion>9.0</LangVersion> or higher in the project file",
            "Zerra",
            DiagnosticSeverity.Error,
            true);

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

            var expressionInitializerProvider = context.SyntaxProvider.CreateSyntaxProvider(
                (node, cancellationToken) => node is BaseObjectCreationExpressionSyntax { Initializer: not null } creation && creation.Initializer.IsKind(SyntaxKind.ObjectInitializerExpression),
                FindExpressionInitializer
            )
            .Where(x => x is not null)
            .Select((x, cancellationToken) => x!.Value)
            .Collect();

            var usedTypeProvider = context.SyntaxProvider.CreateSyntaxProvider(
                (node, cancellationToken) => node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Map" or "MapTo" or "Copy" or "Serialize" or "SerializeAsync" or "SerializeToUtf8Bytes" or "SerializeBytes" or "Deserialize" or "DeserializeAsync" or "DeserializePatch" } },
                FindUsedTypes
            )
            .SelectMany((x, cancellationToken) => x)
            .Collect();

            var languageVersionProvider = context.ParseOptionsProvider.Select((options, cancellationToken) => ((CSharpParseOptions)options).LanguageVersion);

            context.RegisterSourceOutput(syntaxProvider.Combine(expressionInitializerProvider).Combine(usedTypeProvider).Combine(languageVersionProvider), (a, b) =>
            {
                //the initializer is a module initializer which needs C# 9
                if (b.Right < LanguageVersion.CSharp9)
                {
                    a.ReportDiagnostic(Diagnostic.Create(languageVersionDescriptor, Location.None, b.Right.ToDisplayString()));
                    return;
                }
                SourceOutput(a, b.Left.Left.Left, b.Left.Left.Right, b.Left.Right);
            });
        }

        //a type only used with the mapper or a serializer, such as the target of source.Map<Target>() or Deserialize<Target>(bytes), is found nowhere else
        private static ImmutableArray<ITypeSymbol> FindUsedTypes(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (context.SemanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method)
                return ImmutableArray<ITypeSymbol>.Empty;
            var declared = method.ReducedFrom ?? method;
            var containingType = declared.ContainingType;
            if (containingType is null)
                return ImmutableArray<ITypeSymbol>.Empty;
            var containingNamespace = containingType.ContainingNamespace?.ToString();

            var isMapper = containingType.Name == "Mapper" && containingNamespace == "Zerra.Map";
            var isSerializer = (containingType.Name == "ByteSerializer" && containingNamespace == "Zerra.Serialization.Bytes")
                || (containingType.Name == "JsonSerializer" && containingNamespace == "Zerra.Serialization.Json")
                || (containingType.Name == "INetworkSerializer" && containingNamespace == "Zerra.CQRS.Network")
                || containingType.AllInterfaces.Any(x => x.Name == "INetworkSerializer" && x.ContainingNamespace?.ToString() == "Zerra.CQRS.Network");
            if (!isMapper && !isSerializer)
                return ImmutableArray<ITypeSymbol>.Empty;

            var types = ImmutableArray.CreateBuilder<ITypeSymbol>();
            foreach (var typeArgument in method.TypeArguments)
                AddUsedType(types, typeArgument);

            if (isMapper)
            {
                //the source of Map<TTarget>(this object) is only known by the type it's called on
                AddUsedType(types, context.SemanticModel.GetTypeInfo(((MemberAccessExpressionSyntax)invocation.Expression).Expression, cancellationToken).Type);
            }
            else
            {
                //Serialize(object) only knows the value's declared type, Deserialize(bytes, Type) only a typeof written at the call
                var arguments = invocation.ArgumentList.Arguments;
                for (var i = 0; i < arguments.Count; i++)
                {
                    var argument = arguments[i];
                    var parameter = argument.NameColon is not null
                        ? method.Parameters.FirstOrDefault(x => x.Name == argument.NameColon.Name.Identifier.ValueText)
                        : i < method.Parameters.Length ? method.Parameters[i] : null;
                    if (parameter is null)
                        continue;
                    if (parameter.Type.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Object)
                        AddUsedType(types, context.SemanticModel.GetTypeInfo(argument.Expression, cancellationToken).Type);
                    else if (argument.Expression is TypeOfExpressionSyntax typeOf && parameter.Type.Name == "Type" && parameter.Type.ContainingNamespace?.ToString() == "System")
                        AddUsedType(types, context.SemanticModel.GetTypeInfo(typeOf.Type, cancellationToken).Type);
                }
            }
            return types.ToImmutable();
        }

        private static void AddUsedType(ImmutableArray<ITypeSymbol>.Builder types, ITypeSymbol? type)
        {
            if (type is not null && type is not ITypeParameterSymbol && type.TypeKind != TypeKind.Error && type.SpecialType != Microsoft.CodeAnalysis.SpecialType.System_Object)
                types.Add(type);
        }

        //Expression.Bind(MethodInfo, ...) from an initializer in an expression tree is flagged IL2026 by native AOT, generating the type keeps its members
        private static (ITypeSymbol Type, string MemberID)? FindExpressionInitializer(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            var node = (BaseObjectCreationExpressionSyntax)context.Node;

            var inExpression = false;
            foreach (var ancestor in node.Ancestors())
            {
                if (ancestor is MemberDeclarationSyntax)
                    break;
                if (ancestor is AnonymousFunctionExpressionSyntax lambda
                    && context.SemanticModel.GetTypeInfo(lambda, cancellationToken).ConvertedType is INamedTypeSymbol { MetadataName: "Expression`1" } convertedType
                    && convertedType.ContainingNamespace.ToString() == "System.Linq.Expressions")
                {
                    inExpression = true;
                    break;
                }
            }
            if (!inExpression)
                return null;

            var type = context.SemanticModel.GetTypeInfo(node, cancellationToken).Type;
            if (type is null || type.TypeKind == TypeKind.Error)
                return null;

            var member = context.SemanticModel.GetEnclosingSymbol(node.SpanStart, cancellationToken);
            while (member is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction })
                member = member.ContainingSymbol;
            if (member is not IMethodSymbol)
                return null;
            var memberID = member.GetDocumentationCommentId();
            if (memberID is null)
                return null;

            return (type, memberID);
        }

        private static void SourceOutput(SourceProductionContext context, ImmutableArray<ITypeSymbol> symbols, ImmutableArray<(ITypeSymbol Type, string MemberID)> expressionInitializers, ImmutableArray<ITypeSymbol> usedTypes)
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
            foreach (var expressionInitializer in expressionInitializers)
                TypeFinder.FindExpressionInitializerModel(expressionInitializer.Type, expressionInitializer.MemberID, typesToGenerate);
            foreach (var usedType in usedTypes)
                TypeFinder.FindUsedModel(usedType, typesToGenerate);
            TypesGenerator.Generate(sbInitializer, typesToGenerate);
            EmptyImplementationGenerator.Generate(context, ns, sbInitializer, typesToGenerate);
            SerializerAndMapGenerator.Generate(sbInitializer, typesToGenerate);

            var expressionInitializerMemberIDs = typesToGenerate.Values.SelectMany(x => x.ExpressionInitializerMemberIDs).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();
            GenerateInitializer(context, ns, sbInitializer, GetSuppressedDiagnosticIds(typesToGenerate), expressionInitializerMemberIDs);
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

        private static void GenerateInitializer(SourceProductionContext context, string ns, StringBuilder sbInitializer, string[] suppressedDiagnosticIds, string[] expressionInitializerMemberIDs)
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

            //the targets use the ID form native AOT matches, "M:Namespace.Type.Method(Parameters)" without a return type
            var trimmingSuppressions = String.Empty;
            if (expressionInitializerMemberIDs.Length > 0)
            {
                var sbTrimmingSuppressions = new StringBuilder();
                _ = sbTrimmingSuppressions.Append("#if NET5_0_OR_GREATER").Append(EnvironmentHelper.NewLine);
                foreach (var memberID in expressionInitializerMemberIDs)
                    _ = sbTrimmingSuppressions.Append("[module: global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"IL2026\", Justification = \"An object initializer in an expression tree, its type is generated so its members are kept.\", Scope = \"member\", Target = \"").Append(memberID).Append("\")]").Append(EnvironmentHelper.NewLine);
                _ = sbTrimmingSuppressions.Append("#endif").Append(EnvironmentHelper.NewLine);
                trimmingSuppressions = sbTrimmingSuppressions.ToString();
            }

            var code = $$"""
                // <auto-generated/>
                #nullable enable
                #nullable disable warnings

                #pragma warning disable CS0612, CS0618
                {{suppressions}}

                {{trimmingSuppressions}}
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
