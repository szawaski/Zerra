// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using System.Text;

namespace Zerra.SourceGeneration
{
    /// <summary>
    /// Generates source code for registering command and event handler type information used by the bus infrastructure.
    /// </summary>
    public static class BusCommandOrEventInfoGenerator
    {
        /// <summary>
        /// Appends source generation code for command and event handler registration to the provided <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to append the generated source code to.</param>
        /// <param name="symbol">The type symbol representing the command or event handler interface to generate registration for.</param>
        public static void Generate(StringBuilder sb, ITypeSymbol symbol)
        {
            if (symbol.TypeKind != TypeKind.Interface)
                return;
            if (symbol is not INamedTypeSymbol namedTypeSymbol)
                return;
            if (!namedTypeSymbol.AllInterfaces.Any(x => x.Name == "ICommandHandler" || x.Name == "IEventHandler"))
                return;

            var niceName = namedTypeSymbol.MetadataName;
            var typeOfName = Helper.GetTypeOfName(namedTypeSymbol);
            var commandTypes = new List<ITypeSymbol>();
            var eventTypes = new List<ITypeSymbol>();
            foreach (var interfaceTypeSymbol in namedTypeSymbol.AllInterfaces)
            {
                if (interfaceTypeSymbol.Name == "ICommandHandler")
                {
                    var commandType = interfaceTypeSymbol.TypeArguments[0];
                    commandTypes.Add(commandType);
                }
                else if (interfaceTypeSymbol.Name == "IEventHandler")
                {
                    var eventType = interfaceTypeSymbol.TypeArguments[0];
                    eventTypes.Add(eventType);
                }
            }

            _ = sb.Append(EnvironmentHelper.NewLine);
            _ = sb.Append("global::Zerra.Reflection.Register.CommandOrEventInfo(").Append(typeOfName).Append(", \"").Append(niceName).Append("\", [");

            var hasFirst = false;
            foreach (var commandType in commandTypes)
            {
                if (hasFirst)
                    _ = sb.Append(", ");
                else
                    hasFirst = true;
                _ = sb.Append(Helper.GetTypeOfName(commandType));
            }

            _ = sb.Append("], [");

            hasFirst = false;
            foreach (var eventType in eventTypes)
            {
                if (hasFirst)
                    _ = sb.Append(", ");
                else
                    hasFirst = true;
                _ = sb.Append(Helper.GetTypeOfName(eventType));
            }


            _ = sb.Append("]);");
        }
    }
}
