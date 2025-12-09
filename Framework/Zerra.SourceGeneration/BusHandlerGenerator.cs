// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using System.Text;

namespace Zerra.SourceGeneration
{
    public static class BusHandlerGenerator
    {
        public static void Generate(StringBuilder sb, ITypeSymbol typeSymbol)
        {
            if (typeSymbol.TypeKind != TypeKind.Interface)
                return;
            if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                return;
            if (!namedTypeSymbol.AllInterfaces.Any(x => x.Name == "IQueryHandler" || x.Name == "ICommandHandler" || x.Name == "IEventHandler"))
                return;

            var typeNameForInterface = Helper.GetFullName(namedTypeSymbol);
            var typeOfForInterface = Helper.GetTypeOfName(namedTypeSymbol);
            Generate(namedTypeSymbol, typeNameForInterface, typeOfForInterface, sb);
            foreach (var i in namedTypeSymbol.AllInterfaces)
                Generate(i, typeNameForInterface, typeOfForInterface, sb);
        }

        private static void Generate(INamedTypeSymbol namedTypeSymbol, string typeNameForInterface, string typeOfForInterface, StringBuilder sb)
        {
            var members = namedTypeSymbol.GetMembers();
            foreach (IMethodSymbol method in members.Where(x => x.Kind == SymbolKind.Method))
            {
                if (method.MethodKind != MethodKind.Ordinary)
                    continue;
                if (method.IsGenericMethod)
                    continue;

                string methodName;
                if (namedTypeSymbol.Name == "ICommandHandler" || namedTypeSymbol.Name == "IEventHandler")
                    methodName = $"{method.Name}-{method.Parameters[0].Type.Name}";
                else
                    methodName = method.Name;

                var isTask = method.ReturnType.Name == "Task";
                var typeOfTaskInnerType = "null";
                if (isTask)
                {
                    var returnType = (INamedTypeSymbol)method.ReturnType;
                    if (returnType.TypeArguments.Length == 1)
                        typeOfTaskInnerType = Helper.GetTypeOfName(returnType.TypeArguments[0]);
                }

                _ = sb.Append(Environment.NewLine).Append("            ");
                _ = sb.Append("global::Zerra.Reflection.Register.Handler(");

                _ = sb.Append(typeOfForInterface).Append(", ");
                _ = sb.Append("\"").Append(methodName).Append("\", ");
                _ = sb.Append(isTask ? "true" : "false").Append(", ");
                _ = sb.Append(typeOfTaskInnerType).Append(", ");

                _ = sb.Append("[");
                var i = 0;
                foreach (var parameter in method.Parameters)
                {
                    if (i > 0)
                        _ = sb.Append(", ");
                    var parameterTypeOf = Helper.GetTypeOfName(parameter.Type);
                    _ = sb.Append(parameterTypeOf);
                    i++;
                }
                _ = sb.Append("], ");

                _ = sb.Append("static (object instance, object?[]? args) => ");
                if (method.ReturnsVoid)
                    _ = sb.Append("{");
                _ = sb.Append("((").Append(typeNameForInterface).Append(")instance).").Append(method.Name).Append("(");
                i = 0;
                foreach (var parameter in method.Parameters)
                {
                    if (i > 0)
                        _ = sb.Append(", ");
                    var parameterTypeName = Helper.GetFullName(parameter.Type);
                    _ = sb.Append("(").Append(parameterTypeName).Append(")args![").Append(i++).Append("]!");
                }
                _ = sb.Append(")");
                if (method.ReturnsVoid)
                    _ = sb.Append("; return null;}");
                _ = sb.Append(", ");

                if (isTask && typeOfTaskInnerType != "null")
                {
                    _ = sb.Append("static (object task) => ");
                    var returnTypeName = Helper.GetFullName(method.ReturnType);
                    _ = sb.Append("((").Append(returnTypeName).Append(")task).Result");
                }
                else
                {
                    _ = sb.Append("null");
                }

                _ = sb.Append(");");
            }
        }
    }
}

