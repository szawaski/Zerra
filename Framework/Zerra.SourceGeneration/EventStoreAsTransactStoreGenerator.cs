// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Text;

namespace Zerra.SourceGeneration.Discovery
{
    public static class EventStoreAsTransactStoreGenerator
    {
        public static void Generate(SourceProductionContext context, string ns, StringBuilder sbInitializer, ITypeSymbol symbol)
        {
            if (symbol.TypeKind != TypeKind.Class)
                return;
            if (symbol is not INamedTypeSymbol namedTypeSymbol)
                return;

            var baseSymbol = symbol.BaseType;
            while (baseSymbol is not null && baseSymbol.Name != "DataContext")
                baseSymbol = baseSymbol?.BaseType;
            if (baseSymbol?.Name != "DataContext")
                return;

            foreach (var attribute in namedTypeSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.Name != "EventStoreEntityAttribute")
                    return;

                var modelSymbol = attribute.AttributeClass.TypeArguments[0];
                var typeNameForClass = Helpers.GetNameForClass(modelSymbol);
                var className = $"EventStoreAsTransactStoreProvider_{typeNameForClass}";

                var code = $$"""
                //Zerra Generated File
                #if NET5_0_OR_GREATER

                namespace {{ns}}.SourceGeneration
                {
                    public class {{className}} : Zerra.Repository.EventStoreAsTransactStoreProvider<{{symbol.ToString()}}, {{modelSymbol.ToString()}}>
                    {
                        public {{className}}() { }
                    }
                }

                #endif
            
                """;

                context.AddSource($"{className}.cs", SourceText.From(code, Encoding.UTF8));

                var classFullTypeOf = $"typeof({ns}.SourceGeneration.{className})";
                var interface1FullTypeOf = $"typeof(Zerra.Repository.ITransactStoreProvider<{modelSymbol.ToString()}>)";
                var interface2FullTypeOf = $"typeof(Zerra.Repository.IProviderRelation<{modelSymbol.ToString()}>)";
                _ = sbInitializer.Append(Environment.NewLine).Append("            ");
                _ = sbInitializer.Append("Zerra.Reflection.SourceGenerationRegistration.DiscoverType(").Append(classFullTypeOf).Append(", [");
                _ = sbInitializer.Append(interface1FullTypeOf).Append(", ").Append(interface2FullTypeOf);
                _ = sbInitializer.Append("]);");
            }
        }
    }
}
