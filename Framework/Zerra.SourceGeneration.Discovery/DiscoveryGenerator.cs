// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Zerra.SourceGeneration.Discovery
{
    public static class DiscoveryGenerator
    {
        public static void Generate(SourceProductionContext context, string ns, StringBuilder sbInitializer, List<ITypeSymbol> discoverySymbols)
        {
            var passFirst = false;
            foreach (var symbol in discoverySymbols)
            {
                var fullTypeOf = Helpers.GetTypeOfName(symbol);
                if (!passFirst)
                {
                    _ = sbInitializer.Append(Environment.NewLine).Append("            ");
                    _ = sbInitializer.Append("Zerra.Reflection.SourceGenerationRegistration.MarkAssemblyAsDiscovered(").Append(fullTypeOf).Append(".Assembly);");
                    passFirst = true;
                }
                _ = sbInitializer.Append(Environment.NewLine).Append("            ");
                _ = sbInitializer.Append("Zerra.Reflection.SourceGenerationRegistration.DiscoverType(").Append(fullTypeOf).Append(", [");
                var firstPassed = false;
                foreach(var i in symbol.AllInterfaces)
                {
                    if (firstPassed)
                        _ = sbInitializer.Append(", ");
                    else
                        firstPassed = true;
                    var iFullTypeOf = Helpers.GetTypeOfName(i);
                    _ = sbInitializer.Append(iFullTypeOf);
                }

                _ = sbInitializer.Append("]);");
            }
        }
    }
}
