// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using System.Text;

namespace Zerra.SourceGeneration
{
    public static class TypeFinderGenerator
    {
        public static void Generate(StringBuilder sbInitializer, ITypeSymbol typeSymbol)
        {
            if (typeSymbol.TypeKind != TypeKind.Class && typeSymbol.TypeKind != TypeKind.Interface)
                return;
            if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                return;
            if (!namedTypeSymbol.AllInterfaces.Any(x => x.Name == "ICommand" || x.Name == "IEvent" || x.Name == "IQueryHandler" || x.Name == "ICommandHandler"))
                return;

            var fullName = Helper.GetFullName(namedTypeSymbol);

            var firstBracket = fullName.IndexOf('<');
            if (firstBracket == -1)
                firstBracket = fullName.Length - 1;
            var index = fullName.LastIndexOf('.', firstBracket) + 1;
            var shortName = fullName.Substring(index);

            var typeOf = Helper.GetTypeOfName(namedTypeSymbol);
            _ = sbInitializer.Append(Environment.NewLine).Append("            ");
            //_ = sbInitializer.Append("global::Zerra.Reflection.Register.NiceName(").Append(typeOf).Append(", \"").Append(shortName).Append("\", \"").Append(fullName).Append("\");");
            _ = sbInitializer.Append("global::Zerra.Reflection.Register.Finder(").Append(typeOf).Append(");");
        }
    }
}

