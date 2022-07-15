// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zerra.T4.CSharp;

namespace Zerra.T4
{
    public static class CQRSClientDomain
    {
        private static readonly string spacing = "    ";
        public static string GenerateTypeScript(string directory)
        {
            var (queryInterfaces, commands, models) = GetQueriesCommandsModels(directory);

            var modelsFiltered = models.Where(x => x.ObjectType == CSharpObjectType.Class || x.ObjectType == CSharpObjectType.Struct).ToArray();

            var sb = new StringBuilder();

            _ = sb.Append("import { Bus, ICommand } from \"./Bus\";").Append(Environment.NewLine).Append(Environment.NewLine);
            foreach (var model in modelsFiltered)
            {
                _ = sb.Append("export class ").Append(model.Name).Append(" {").Append(Environment.NewLine);
                foreach (var property in model.Properties)
                {
                    var (isJavaScriptType, type, hasMany, nullable) = GetJavaScriptPropertyType(property.Type.Resolved, models);
                    _ = sb.Append(spacing).Append(property.Name).Append("!: ").Append(type).Append(nullable ? " | null" : null).Append(";").Append(Environment.NewLine);
                }
                _ = sb.Append("}").Append(Environment.NewLine).Append(Environment.NewLine);

                _ = sb.Append("const ").Append(model.Name).Append("Type =").Append(Environment.NewLine);
                _ = sb.Append("{").Append(Environment.NewLine);
                foreach (var property in model.Properties)
                {
                    var (isJavaScriptType, type, hasMany, nullable) = GetJavaScriptPropertyType(property.Type.Resolved, models);
                    _ = sb.Append(spacing).Append(property.Name).Append(": \"").Append(type).Append("\",").Append(Environment.NewLine);
                }
                _ = sb.Append("}").Append(Environment.NewLine).Append(Environment.NewLine);
            }

            _ = sb.Append("export const ").Append("ModelTypeDictionary: any[string] =").Append(Environment.NewLine);
            _ = sb.Append("{").Append(Environment.NewLine);
            foreach (var model in modelsFiltered)
            {
                _ = sb.Append(spacing).Append(model.Name).Append(": ").Append(model.Name).Append("Type,").Append(Environment.NewLine);
            }
            _ = sb.Append("}").Append(Environment.NewLine).Append(Environment.NewLine);

            foreach (var query in queryInterfaces)
            {
                _ = sb.Append("export class ").Append(query.Name).Append(" {").Append(Environment.NewLine);
                foreach (var method in query.Methods)
                {
                    var (isJavaScriptType, type, hasMany, nullable) = GetJavaScriptPropertyType(method.ReturnType.Resolved, models);
                    _ = sb.Append(spacing).Append("public static ").Append(method.Name).Append("(");
                    for (var i = 0; i < method.Parameters.Count; i++)
                    {
                        if (i > 0)
                            _ = sb.Append(", ");
                        var (parameterIsJavaScriptType, parameterType, parameterHasMany, parameterNullable) = GetJavaScriptPropertyType(method.Parameters[i].Type.Resolved, models);
                        _ = sb.Append(method.Parameters[i].Name).Append(": ").Append(parameterType).Append(parameterNullable ? " | null" : null);
                    }
                    _ = sb.Append("): Promise<").Append(type).Append(nullable ? " | null" : null).Append("> {").Append(Environment.NewLine);
                    _ = sb.Append(spacing).Append(spacing).Append("return Bus.Call(\"").Append(query.Name).Append("\", \"").Append(method.Name).Append("\", [");
                    for (var i = 0; i < method.Parameters.Count; i++)
                    {
                        if (i > 0)
                            _ = sb.Append(", ");
                        _ = sb.Append(method.Parameters[i].Name);
                    }
                    _ = sb.Append("], ").Append(type == null || isJavaScriptType ? "null" : (hasMany ? type.Remove(type.Length - 2) : type) + "Type").Append(", ").Append(hasMany ? "true" : "false").Append(");").Append(Environment.NewLine);
                    _ = sb.Append(spacing).Append("}").Append(Environment.NewLine);
                }
                _ = sb.Append("}").Append(Environment.NewLine).Append(Environment.NewLine);

            }

            foreach (var command in commands)
            {
                _ = sb.Append("export class ").Append(command.Name).Append(" implements ICommand {").Append(Environment.NewLine);
                _ = sb.Append(spacing).Append("constructor(properties: ").Append(command.Name).Append(") {").Append(Environment.NewLine);
                _ = sb.Append(spacing).Append(spacing).Append("const self: any = this;").Append(Environment.NewLine);
                _ = sb.Append(spacing).Append(spacing).Append("const props: any = properties;").Append(Environment.NewLine);
                _ = sb.Append(spacing).Append(spacing).Append("Object.keys(props).forEach(key => self[key] = props[key]);").Append(Environment.NewLine);
                _ = sb.Append(spacing).Append(spacing).Append("self[\"CommandType\"] = \"").Append(command.Name).Append("\";").Append(Environment.NewLine);
                _ = sb.Append(spacing).Append("}").Append(Environment.NewLine);
                foreach (var property in command.Properties)
                {
                    var (isJavaScriptType, type, hasMany, nullable) = GetJavaScriptPropertyType(property.Type.Resolved, models);
                    _ = sb.Append(spacing).Append(property.Name).Append("!: ").Append(type).Append(nullable ? " | null" : null).Append(";").Append(Environment.NewLine);
                }
                _ = sb.Append("}").Append(Environment.NewLine).Append(Environment.NewLine);
            }

            return sb.ToString();
        }
        public static string GenerateJavaScript(string directory)
        {
            var (queryInterfaces, commands, models) = GetQueriesCommandsModels(directory);

            var modelsFiltered = models.Where(x => x.ObjectType == CSharpObjectType.Class || x.ObjectType == CSharpObjectType.Struct).ToArray();

            var sb = new StringBuilder();

            foreach (var model in modelsFiltered)
            {
                _ = sb.Append("const ").Append(model.Name).Append("Type =").Append(Environment.NewLine);
                _ = sb.Append("{").Append(Environment.NewLine);
                foreach (var property in model.Properties)
                {
                    var (isJavaScriptType, type, hasMany, nullable) = GetJavaScriptPropertyType(property.Type.Resolved, models);
                    _ = sb.Append(spacing).Append(property.Name).Append(": \"").Append(type).Append("\",").Append(Environment.NewLine);
                }
                _ = sb.Append("}").Append(Environment.NewLine).Append(Environment.NewLine);
            }

            _ = sb.Append("const ").Append("ModelTypeDictionary =").Append(Environment.NewLine);
            _ = sb.Append("{").Append(Environment.NewLine);
            foreach (var model in modelsFiltered)
            {
                _ = sb.Append(spacing).Append(model.Name).Append(": ").Append(model.Name).Append("Type,").Append(Environment.NewLine);
            }
            _ = sb.Append("}").Append(Environment.NewLine).Append(Environment.NewLine);

            foreach (var query in queryInterfaces)
            {
                _ = sb.Append("const ").Append(query.Name).Append(" = {").Append(Environment.NewLine);
                foreach (var method in query.Methods)
                {
                    var (isJavaScriptType, type, hasMany, nullable) = GetJavaScriptPropertyType(method.ReturnType.Resolved, models);
                    _ = sb.Append(spacing).Append(method.Name).Append(": function(");
                    int i;
                    for (i = 0; i < method.Parameters.Count; i++)
                    {
                        if (i > 0)
                            _ = sb.Append(", ");
                        _ = sb.Append(method.Parameters[i].Name);
                    }
                    if (i > 0)
                        _ = sb.Append(", ");
                    _ = sb.Append("onComplete, onFail) {").Append(Environment.NewLine);
                    _ = sb.Append(spacing).Append(spacing).Append("Bus.Call(\"").Append(query.Name).Append("\", \"").Append(method.Name).Append("\", [");
                    for (i = 0; i < method.Parameters.Count; i++)
                    {
                        if (i > 0)
                            _ = sb.Append(", ");
                        _ = sb.Append(method.Parameters[i].Name);
                    }
                    _ = sb.Append("], ").Append(type == null || isJavaScriptType ? "null" : (hasMany ? type.Remove(type.Length - 2) : type) + "Type").Append(", ").Append(hasMany ? "true" : "false").Append(", onComplete, onFail);").Append(Environment.NewLine);
                    _ = sb.Append(spacing).Append("},").Append(Environment.NewLine);
                }
                _ = sb.Append("}").Append(Environment.NewLine).Append(Environment.NewLine);

            }

            foreach (var command in commands)
            {
                _ = sb.Append("const ").Append(command.Name).Append(" = function(properties) {").Append(Environment.NewLine);
                foreach (var property in command.Properties)
                {
                    _ = sb.Append(spacing).Append("this.").Append(property.Name).Append(" = (properties === undefined || properties.").Append(property.Name).Append(" === undefined) ? null : properties.").Append(property.Name).Append(";").Append(Environment.NewLine);
                }
                _ = sb.Append(spacing).Append("this.CommandType = \"").Append(command.Name).Append("\";").Append(Environment.NewLine);
                _ = sb.Append("}").Append(Environment.NewLine).Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        private static (List<CSharpObject>, List<CSharpObject>, List<CSharpObject>) GetQueriesCommandsModels(string directory)
        {
            var solution = CSharpParser.ParseAllFiles(directory);

            foreach (var item in solution.Interfaces)
            {
                foreach (var m in item.Methods)
                {
                    var t = m.ReturnType.Resolved;
                }
            }

            var queryInterfaces = new List<CSharpObject>();
            foreach (var csInterface in solution.Interfaces)
            {
                var apiExposed = false;
                foreach (var attribute in csInterface.Attributes)
                {
                    if (attribute.Name == "ServiceBlocked" && (attribute.Arguments.Count == 0 || attribute.Arguments.Any(x => x.EndsWith("NetworkType.Api"))))
                    {
                        apiExposed = false;
                        break;
                    }
                    if (attribute.Name == "ServiceExposed" && (attribute.Arguments.Count == 0 || attribute.Arguments.Any(x => x.EndsWith("NetworkType.Api"))))
                    {
                        apiExposed = true;
                    }
                }
                if (apiExposed)
                    queryInterfaces.Add(csInterface);
            }

            var commands = new List<CSharpObject>();
            foreach (var csClass in solution.Classes)
            {
                if (csClass.Implements.Any(x => x.Name == "ICommand"))
                {
                    var apiExposed = false;
                    foreach (var attribute in csClass.Attributes)
                    {
                        if (attribute.Name == "ServiceBlocked" && (attribute.Arguments.Count == 0 || attribute.Arguments.Any(x => x.EndsWith("NetworkType.Api"))))
                        {
                            apiExposed = false;
                            break;
                        }
                        if (attribute.Name == "ServiceExposed" && (attribute.Arguments.Count == 0 || attribute.Arguments.Any(x => x.EndsWith("NetworkType.Api"))))
                        {
                            apiExposed = true;
                        }
                    }
                    if (apiExposed)
                        commands.Add(csClass);
                }
            }

            var models = new List<CSharpObject>();
            foreach (var queryInterface in queryInterfaces)
            {
                foreach (var method in queryInterface.Methods)
                {
                    var returnType = method.ReturnType.Resolved;
                    while (returnType.GenericArguments.Count == 1)
                    {
                        returnType = returnType.GenericArguments[0];
                    }
                    if (returnType.SolutionType != null)
                    {
                        if (!models.Contains(returnType.SolutionType))
                        {
                            models.Add(returnType.SolutionType);
                            AddModels(returnType.SolutionType, models);
                        }
                    }

                    foreach (var parameter in method.Parameters)
                    {
                        var parameterType = parameter.Type.Resolved;
                        if (parameterType.SolutionType != null)
                        {
                            if (!models.Contains(parameterType.SolutionType))
                            {
                                models.Add(parameterType.SolutionType);
                                AddModels(parameterType.SolutionType, models);
                            }
                        }
                    }
                }
            }
            foreach (var command in commands)
            {
                AddModels(command, models);
            }

            return (queryInterfaces, commands, models);
        }

        private static void AddModels(CSharpObject model, List<CSharpObject> models)
        {
            foreach (var property in model.Properties)
            {
                var propertyType = property.Type.Resolved;
                AddModels(propertyType, models);
            }
        }
        private static void AddModels(CSharpType type, List<CSharpObject> models)
        {
            if (type.SolutionType != null)
            {
                if (!models.Contains(type.SolutionType))
                {
                    models.Add(type.SolutionType);
                    AddModels(type.SolutionType, models);
                }
            }
            foreach (var generic in type.GenericArguments)
            {
                if (generic.SolutionType != null)
                {
                    if (!models.Contains(generic.SolutionType))
                    {
                        models.Add(generic.SolutionType);
                        AddModels(generic.SolutionType, models);
                    }
                }
            }
        }

        private static (bool, string, bool, bool) GetJavaScriptPropertyType(CSharpType csharpType, IEnumerable<CSharpObject> references)
        {
            bool isJavaScriptType;
            var hasMany = false;
            var nullable = false;
            while (csharpType.GenericArguments.Count == 1)
            {
                if (csharpType.Name == "Array`1" || csharpType.NativeType != null && (csharpType.NativeType.Name == "IEnumerable`1" || csharpType.NativeType.GetInterface(typeof(IEnumerable<>).Name) != null))
                    hasMany = true;
                if (csharpType.Name == "Nullable`1")
                    nullable = true;
                csharpType = csharpType.GenericArguments[0];
            }

            string type;
            var coreType = csharpType.NativeType != null && IsCoreType(csharpType.NativeType);
            if (coreType)
            {
                type = ConvertCoreTypeToJavaScriptType(csharpType.NativeType);
                isJavaScriptType = true;
                if (csharpType.NativeType.IsClass)
                    nullable = true;
            }
            else
            {
                var modelReference = references.FirstOrDefault(x => x.Name == csharpType.Name);
                if (modelReference != null)
                {
                    if (modelReference.ObjectType == CSharpObjectType.Enum)
                    {
                        type = "string";
                        isJavaScriptType = true;
                    }
                    else
                    {
                        type = modelReference.Name;
                        isJavaScriptType = false;
                    }
                    if (modelReference.ObjectType == CSharpObjectType.Class || modelReference.ObjectType == CSharpObjectType.Interface)
                        nullable = true;
                }
                else
                {
                    type = "any";
                    isJavaScriptType = true;
                }
            }

            if (hasMany)
                type += "[]";

            return (isJavaScriptType, type, hasMany, nullable);
        }
        private static bool IsCoreType(Type type)
        {
            if (type == typeof(bool))
                return true;
            if (type == typeof(char))
                return true;
            if (type == typeof(DateTime))
                return true;
            if (type == typeof(DateTimeOffset))
                return true;
            if (type == typeof(TimeSpan))
                return true;
            if (type == typeof(Guid))
                return true;
            if (type == typeof(byte))
                return true;
            if (type == typeof(sbyte))
                return true;
            if (type == typeof(short))
                return true;
            if (type == typeof(ushort))
                return true;
            if (type == typeof(int))
                return true;
            if (type == typeof(uint))
                return true;
            if (type == typeof(long))
                return true;
            if (type == typeof(ulong))
                return true;
            if (type == typeof(float))
                return true;
            if (type == typeof(double))
                return true;
            if (type == typeof(decimal))
                return true;
            if (type == typeof(string))
                return true;

            if (type == typeof(bool?))
                return true;
            if (type == typeof(char?))
                return true;
            if (type == typeof(DateTime?))
                return true;
            if (type == typeof(DateTimeOffset?))
                return true;
            if (type == typeof(TimeSpan?))
                return true;
            if (type == typeof(Guid?))
                return true;
            if (type == typeof(byte?))
                return true;
            if (type == typeof(sbyte?))
                return true;
            if (type == typeof(short?))
                return true;
            if (type == typeof(ushort?))
                return true;
            if (type == typeof(int?))
                return true;
            if (type == typeof(uint?))
                return true;
            if (type == typeof(long?))
                return true;
            if (type == typeof(ulong?))
                return true;
            if (type == typeof(float?))
                return true;
            if (type == typeof(double?))
                return true;
            if (type == typeof(decimal?))
                return true;

            if (type == typeof(byte[]))
                return true;

            return false;
        }
        private static string ConvertCoreTypeToJavaScriptType(Type type)
        {
            if (type == typeof(bool))
                return "boolean";
            if (type == typeof(char))
                return "string";
            if (type == typeof(DateTime))
                return "Date";
            if (type == typeof(DateTimeOffset))
                return "Date";
            if (type == typeof(TimeSpan))
                return "Date";
            if (type == typeof(Guid))
                return "string";
            if (type == typeof(byte))
                return "number";
            if (type == typeof(sbyte))
                return "number";
            if (type == typeof(short))
                return "number";
            if (type == typeof(ushort))
                return "number";
            if (type == typeof(int))
                return "number";
            if (type == typeof(uint))
                return "number";
            if (type == typeof(long))
                return "number";
            if (type == typeof(ulong))
                return "number";
            if (type == typeof(float))
                return "number";
            if (type == typeof(double))
                return "number";
            if (type == typeof(decimal))
                return "number";
            if (type == typeof(string))
                return "string";

            if (type == typeof(bool?))
                return "boolean";
            if (type == typeof(char?))
                return "string";
            if (type == typeof(DateTime?))
                return "Date";
            if (type == typeof(DateTimeOffset?))
                return "Date";
            if (type == typeof(TimeSpan?))
                return "Date";
            if (type == typeof(Guid?))
                return "string";
            if (type == typeof(byte?))
                return "number";
            if (type == typeof(sbyte?))
                return "number";
            if (type == typeof(short?))
                return "number";
            if (type == typeof(ushort?))
                return "number";
            if (type == typeof(int?))
                return "number";
            if (type == typeof(uint?))
                return "number";
            if (type == typeof(long?))
                return "number";
            if (type == typeof(ulong?))
                return "number";
            if (type == typeof(float?))
                return "number";
            if (type == typeof(double?))
                return "number";
            if (type == typeof(decimal?))
                return "number";

            if (type == typeof(byte[]))
                return "Array<number>";

            throw new NotImplementedException();
        }
    }
}