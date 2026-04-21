// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using System.Text;

namespace Zerra.SourceGeneration
{
    /// <summary>
    /// Generates source code for registering discoverable types with the Zerra type finder.
    /// </summary>
    public static class TypeFinderGenerator
    {
        /// <summary>
        /// Appends source generation code for type finder registration to the provided <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="sbInitializer">The <see cref="StringBuilder"/> to append the generated source code to.</param>
        /// <param name="typeSymbol">The type symbol to register with the type finder.</param>
        public static void Generate(StringBuilder sbInitializer, ITypeSymbol typeSymbol)
        {
            if (typeSymbol.TypeKind != TypeKind.Class && typeSymbol.TypeKind != TypeKind.Interface)
                return;
            if (typeSymbol.DeclaredAccessibility != Accessibility.Public)
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
            _ = sbInitializer.Append(Environment.NewLine);
            //_ = sbInitializer.Append("global::Zerra.Reflection.Register.NiceName(").Append(typeOf).Append(", \"").Append(shortName).Append("\", \"").Append(fullName).Append("\");");
            _ = sbInitializer.Append("global::Zerra.Reflection.Register.Finder(").Append(typeOf).Append(");");
        }
    }
}

