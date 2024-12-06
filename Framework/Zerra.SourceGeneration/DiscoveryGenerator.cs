// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using System;
using System.Text;

namespace Zerra.SourceGeneration.Discovery
{
    public static class DiscoveryGenerator
    {
        public static void Generate(SourceProductionContext context, string ns, StringBuilder sbInitializer, ITypeSymbol symbol)
        {
            var fullTypeOf = Helpers.GetTypeOfName(symbol);

            _ = sbInitializer.Append(Environment.NewLine).Append("            ");
            _ = sbInitializer.Append("Zerra.Reflection.SourceGenerationRegistration.DiscoverType(").Append(fullTypeOf).Append(", [");
            var firstPassed = false;
            foreach (var i in symbol.AllInterfaces)
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
