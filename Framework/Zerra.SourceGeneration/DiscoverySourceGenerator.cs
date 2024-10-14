// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace Zerra.SourceGeneration
{
    public static class DiscoverySourceGenerator
    {
        public static void GenerateInitializer(SourceProductionContext context, Compilation compilation, List<string> discoverTypeOfList)
        {
            var sb = new StringBuilder();
            foreach (var fullTypeOf in discoverTypeOfList)
            {
                if (sb.Length > 0)
                    sb.Append(Environment.NewLine).Append("            ");
                sb.Append("Zerra.Reflection.Discovery.DiscoverType(").Append(fullTypeOf).Append(");");
            }
            var lines = sb.ToString();

            var code = $$"""
                //Zerra Generated File
                #if NET5_0_OR_GREATER

                namespace {{compilation.AssemblyName}}.SourceGeneration
                {
                    internal static class DiscoveryInitializer
                    {
                #pragma warning disable CA2255
                        [System.Runtime.CompilerServices.ModuleInitializer]
                #pragma warning restore CA2255
                        public static void Initialize()
                        {
                            {{lines}}
                        }
                    }
                }

                #endif
            
                """;

            context.AddSource("DiscoveryInitializer.cs", SourceText.From(code, Encoding.UTF8));
        }
    }
}
