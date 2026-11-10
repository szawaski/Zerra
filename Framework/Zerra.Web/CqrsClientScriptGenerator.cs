// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;
using Zerra.CQRS;
using Zerra.Reflection;
using Zerra.Serialization.Json;

namespace Zerra.Web
{
    //Writes the models, query functions, and commands for Bus.js and Bus.ts from the types added to the bus
    internal static class CqrsClientScriptGenerator
    {
        private const string spacing = "    ";

        public static string GenerateTypeScript(IBus bus)
        {
            var (queryInterfaces, commands, models) = GetQueriesCommandsModels(bus);

            var sb = new StringBuilder();

            _ = sb.Append("import { Bus, type ICommand } from \"./Bus\";").Append(Environment.NewLine).Append(Environment.NewLine);
            foreach (var model in models)
            {
                var members = GetMembers(model.Key);
                //a command used as a model, such as a member of another command, is declared once by its command class below
                if (!commands.Exists(x => x.Type == model.Key))
                {
                    _ = sb.Append("export class ").Append(model.Value).Append(" {").Append(Environment.NewLine);
                    foreach (var member in members)
                    {
                        var type = GetJavaScriptType(member.Type, models);
                        _ = sb.Append(spacing).Append(member.Key).Append("!: ").Append(type.Name).Append(type.Nullable ? " | null" : null).Append(";").Append(Environment.NewLine);
                    }
                    _ = sb.Append("}").Append(Environment.NewLine).Append(Environment.NewLine);
                }

                //the members the server writes, nameless JSON has a value for each in this order
                _ = sb.Append("const ").Append(model.Value).Append("Type =").Append(Environment.NewLine);
                _ = sb.Append("{").Append(Environment.NewLine);
                foreach (var member in members)
                {
                    if (!member.IsWritten)
                        continue;
                    var type = GetJavaScriptType(member.Type, models);
                    _ = sb.Append(spacing).Append(member.Key).Append(": \"").Append(type.Name).Append("\",").Append(Environment.NewLine);
                }
                _ = sb.Append("}").Append(Environment.NewLine).Append(Environment.NewLine);
            }

            //Bus reads nested models by name
            _ = sb.Append("Bus.addModels({").Append(Environment.NewLine);
            foreach (var model in models)
            {
                _ = sb.Append(spacing).Append(model.Value).Append(": ").Append(model.Value).Append("Type,").Append(Environment.NewLine);
            }
            _ = sb.Append("});").Append(Environment.NewLine).Append(Environment.NewLine);

            foreach (var query in queryInterfaces)
            {
                _ = sb.Append("export class ").Append(query.Type.Name).Append(" {").Append(Environment.NewLine);
                foreach (var method in query.Methods)
                {
                    if (method.IsStatic || IsHandlerMethod(method))
                        continue;
                    var returnType = GetReturnType(method.ReturnType);
                    var type = returnType is null ? null : GetJavaScriptType(returnType, models);
                    var clientParameterCount = GetClientParameterCount(method);
                    _ = sb.Append(spacing).Append("public static ").Append(method.Name).Append("(");
                    for (var i = 0; i < clientParameterCount; i++)
                    {
                        if (i > 0)
                            _ = sb.Append(", ");
                        var parameterType = GetJavaScriptType(method.Parameters[i].Type, models);
                        _ = sb.Append(GetParameterName(method.Parameters[i])).Append(": ").Append(parameterType.Name).Append(parameterType.Nullable ? " | null" : null);
                    }
                    _ = sb.Append("): Promise<").Append(type is null ? "void" : type.Name).Append(type is not null && type.Nullable ? " | null" : null).Append("> {").Append(Environment.NewLine);
                    _ = sb.Append(spacing).Append(spacing).Append("return Bus.call(\"").Append(query.Type.Name).Append("\", \"").Append(method.Name).Append("\", [");
                    AppendArguments(sb, method, clientParameterCount);
                    _ = sb.Append("], ").Append(GetModelTypeArgument(type)).Append(", ").Append(type is not null && type.HasMany ? "true" : "false").Append(");").Append(Environment.NewLine);
                    _ = sb.Append(spacing).Append("}").Append(Environment.NewLine);
                }
                _ = sb.Append("}").Append(Environment.NewLine).Append(Environment.NewLine);
            }

            foreach (var command in commands)
            {
                _ = sb.Append("export class ").Append(command.Type.Name).Append(" implements ICommand {").Append(Environment.NewLine);
                _ = sb.Append(spacing).Append("constructor(properties: ").Append(command.Type.Name).Append(") {").Append(Environment.NewLine);
                _ = sb.Append(spacing).Append(spacing).Append("const self: any = this;").Append(Environment.NewLine);
                _ = sb.Append(spacing).Append(spacing).Append("const props: any = properties;").Append(Environment.NewLine);
                _ = sb.Append(spacing).Append(spacing).Append("Object.keys(props).forEach(key => self[key] = props[key]);").Append(Environment.NewLine);
                _ = sb.Append(spacing).Append(spacing).Append("self[\"CommandType\"] = \"").Append(command.Type.Name).Append("\";").Append(Environment.NewLine);
                var resultType = GetCommandResultType(command);
                _ = sb.Append(spacing).Append(spacing).Append("self[\"CommandWithResult\"] = ").Append(resultType is not null ? "true" : "false").Append(";").Append(Environment.NewLine);
                if (resultType is not null)
                {
                    var type = GetJavaScriptType(resultType, models);
                    _ = sb.Append(spacing).Append(spacing).Append("self[\"ResultType\"] = ").Append(GetModelTypeArgument(type)).Append(";").Append(Environment.NewLine);
                    _ = sb.Append(spacing).Append(spacing).Append("self[\"ResultTypeHasMany\"] = ").Append(type.HasMany ? "true" : "false").Append(";").Append(Environment.NewLine);
                }
                else
                {
                    _ = sb.Append(spacing).Append(spacing).Append("self[\"ResultType\"] = null;").Append(Environment.NewLine);
                    _ = sb.Append(spacing).Append(spacing).Append("self[\"ResultTypeHasMany\"] = false;").Append(Environment.NewLine);
                }
                _ = sb.Append(spacing).Append("}").Append(Environment.NewLine);
                foreach (var member in GetMembers(command.Type))
                {
                    var type = GetJavaScriptType(member.Type, models);
                    _ = sb.Append(spacing).Append(member.Key).Append("!: ").Append(type.Name).Append(type.Nullable ? " | null" : null).Append(";").Append(Environment.NewLine);
                }
                _ = sb.Append("}").Append(Environment.NewLine).Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        public static string GenerateJavaScript(IBus bus)
        {
            var (queryInterfaces, commands, models) = GetQueriesCommandsModels(bus);

            var sb = new StringBuilder();

            foreach (var model in models)
            {
                _ = sb.Append("const ").Append(model.Value).Append("Type =").Append(Environment.NewLine);
                _ = sb.Append("{").Append(Environment.NewLine);
                foreach (var member in GetMembers(model.Key))
                {
                    //the members the server writes, nameless JSON has a value for each in this order
                    if (!member.IsWritten)
                        continue;
                    var type = GetJavaScriptType(member.Type, models);
                    _ = sb.Append(spacing).Append(member.Key).Append(": \"").Append(type.Name).Append("\",").Append(Environment.NewLine);
                }
                _ = sb.Append("}").Append(Environment.NewLine).Append(Environment.NewLine);
            }

            //Bus reads nested models by name
            _ = sb.Append("Bus.addModels({").Append(Environment.NewLine);
            foreach (var model in models)
            {
                _ = sb.Append(spacing).Append(model.Value).Append(": ").Append(model.Value).Append("Type,").Append(Environment.NewLine);
            }
            _ = sb.Append("});").Append(Environment.NewLine).Append(Environment.NewLine);

            foreach (var query in queryInterfaces)
            {
                _ = sb.Append("const ").Append(query.Type.Name).Append(" = {").Append(Environment.NewLine);
                foreach (var method in query.Methods)
                {
                    if (method.IsStatic || IsHandlerMethod(method))
                        continue;
                    var returnType = GetReturnType(method.ReturnType);
                    var type = returnType is null ? null : GetJavaScriptType(returnType, models);
                    var clientParameterCount = GetClientParameterCount(method);
                    _ = sb.Append(spacing).Append(method.Name).Append(": function(");
                    for (var i = 0; i < clientParameterCount; i++)
                        _ = sb.Append(GetParameterName(method.Parameters[i])).Append(", ");
                    _ = sb.Append("onComplete, onFail) {").Append(Environment.NewLine);
                    _ = sb.Append(spacing).Append(spacing).Append("Bus.call(\"").Append(query.Type.Name).Append("\", \"").Append(method.Name).Append("\", [");
                    AppendArguments(sb, method, clientParameterCount);
                    _ = sb.Append("], ").Append(GetModelTypeArgument(type)).Append(", ").Append(type is not null && type.HasMany ? "true" : "false").Append(", onComplete, onFail);").Append(Environment.NewLine);
                    _ = sb.Append(spacing).Append("},").Append(Environment.NewLine);
                }
                _ = sb.Append("}").Append(Environment.NewLine).Append(Environment.NewLine);
            }

            foreach (var command in commands)
            {
                _ = sb.Append("const ").Append(command.Type.Name).Append(" = function(properties) {").Append(Environment.NewLine);
                foreach (var member in GetMembers(command.Type))
                {
                    _ = sb.Append(spacing).Append("this").Append(member.Access).Append(" = (properties === undefined || properties").Append(member.Access).Append(" === undefined) ? null : properties").Append(member.Access).Append(";").Append(Environment.NewLine);
                }
                _ = sb.Append(spacing).Append("this.CommandType = \"").Append(command.Type.Name).Append("\";").Append(Environment.NewLine);
                var resultType = GetCommandResultType(command);
                _ = sb.Append(spacing).Append("this.CommandWithResult = ").Append(resultType is not null ? "true" : "false").Append(";").Append(Environment.NewLine);
                if (resultType is not null)
                {
                    var type = GetJavaScriptType(resultType, models);
                    _ = sb.Append(spacing).Append("this.ResultType = ").Append(GetModelTypeArgument(type)).Append(";").Append(Environment.NewLine);
                    _ = sb.Append(spacing).Append("this.ResultTypeHasMany = ").Append(type.HasMany ? "true" : "false").Append(";").Append(Environment.NewLine);
                }
                else
                {
                    _ = sb.Append(spacing).Append("this.ResultType = null;").Append(Environment.NewLine);
                    _ = sb.Append(spacing).Append("this.ResultTypeHasMany = false;").Append(Environment.NewLine);
                }
                _ = sb.Append("}").Append(Environment.NewLine).Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        private static string GetModelTypeArgument(JavaScriptType? type)
        {
            if (type is null)
                return "null";
            if (type.ElementName == "Blob")
                return "\"Blob\"";
            //Bus reads a date from its JSON text
            if (type.ElementName == "Date")
                return "\"Date\"";
            if (type.IsJavaScriptType)
                return "null";
            return type.ElementName + "Type";
        }

        //a query interface's methods include the ones every handler has, such as Initialize, which aren't queries
        private static readonly System.Reflection.MethodInfo[] handlerMethods = typeof(IHandler).GetMethods();
        private static bool IsHandlerMethod(MethodDetail method)
        {
            foreach (var handlerMethod in handlerMethods)
            {
                if (handlerMethod.Name != method.Name)
                    continue;
                var parameters = handlerMethod.GetParameters();
                if (parameters.Length != method.Parameters.Count)
                    continue;
                var match = true;
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType != method.Parameters[i].Type)
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                    return true;
            }
            return false;
        }

        //a CancellationToken as the last parameter is supplied by the server, so the client functions leave it out
        private static int GetClientParameterCount(MethodDetail method)
        {
            var count = method.Parameters.Count;
            if (count > 0 && method.Parameters[count - 1].Type == typeof(CancellationToken))
                count--;
            return count;
        }

        //the server expects every argument, null holds the place of a CancellationToken it replaces with its own
        private static void AppendArguments(StringBuilder sb, MethodDetail method, int clientParameterCount)
        {
            for (var i = 0; i < method.Parameters.Count; i++)
            {
                if (i > 0)
                    _ = sb.Append(", ");
                _ = sb.Append(i < clientParameterCount ? GetParameterName(method.Parameters[i]) : "null");
            }
        }

        //the arguments are sent in order, so a name JavaScript can't use as a parameter is changed without the caller knowing
        private static readonly HashSet<string> reservedParameterNames =
        [
            "arguments", "await", "break", "case", "catch", "class", "const", "continue", "debugger", "default", "delete", "do",
            "else", "enum", "eval", "export", "extends", "false", "finally", "for", "function", "if", "implements", "import",
            "in", "instanceof", "interface", "let", "new", "null", "package", "private", "protected", "public", "return",
            "static", "super", "switch", "this", "throw", "true", "try", "typeof", "var", "void", "while", "with", "yield",
            "onComplete", "onFail"
        ];
        private static string GetParameterName(ParameterDetail parameter)
        {
            var name = parameter.Name;
            return reservedParameterNames.Contains(name) ? name + "_" : name;
        }

        //the result of a query method, null when it has none
        private static Type? GetReturnType(Type type)
        {
            if (type == typeof(void) || type == typeof(Task) || type == typeof(ValueTask))
                return null;
            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(Task<>) || genericTypeDefinition == typeof(ValueTask<>))
                    return type.GetGenericArguments()[0];
            }
            return type;
        }

        private static Type? GetCommandResultType(TypeDetail command)
        {
            foreach (var interfaceType in command.Interfaces)
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICommand<>))
                    return interfaceType.GetGenericArguments()[0];
            }
            return null;
        }

        private static (List<TypeDetail>, List<TypeDetail>, SortedDictionary<Type, string>) GetQueriesCommandsModels(IBus bus)
        {
            var queryInterfaces = new List<TypeDetail>();
            var commands = new List<TypeDetail>();
            foreach (var type in bus.GetTypes())
            {
                var typeDetail = TypeAnalyzer.GetTypeDetail(type);
                if (type.IsInterface)
                {
                    if (typeDetail.Interfaces.Contains(typeof(IQueryHandler)))
                        queryInterfaces.Add(typeDetail);
                }
                else if (typeDetail.Interfaces.Contains(typeof(ICommand)))
                {
                    commands.Add(typeDetail);
                }
            }
            queryInterfaces.Sort((x, y) => String.CompareOrdinal(x.Type.Name, y.Type.Name));
            commands.Sort((x, y) => String.CompareOrdinal(x.Type.Name, y.Type.Name));

            var modelTypes = new HashSet<Type>();
            foreach (var query in queryInterfaces)
            {
                foreach (var method in query.Methods)
                {
                    if (method.IsStatic || IsHandlerMethod(method))
                        continue;
                    var returnType = GetReturnType(method.ReturnType);
                    if (returnType is not null)
                        AddModels(returnType, modelTypes);
                    foreach (var parameter in method.Parameters)
                        AddModels(parameter.Type, modelTypes);
                }
            }
            foreach (var command in commands)
            {
                foreach (var member in GetMembers(command.Type))
                    AddModels(member.Type, modelTypes);
                var resultType = GetCommandResultType(command);
                if (resultType is not null)
                    AddModels(resultType, modelTypes);
            }

            //the browser knows a model, query, or command only by its name, so two with the same name would replace each other
            var names = new Dictionary<string, Type>();
            foreach (var query in queryInterfaces)
                AddName(names, query.Type.Name, query.Type);
            foreach (var command in commands)
                AddName(names, command.Type.Name, command.Type);
            var models = new SortedDictionary<Type, string>(Comparer<Type>.Create((x, y) => String.CompareOrdinal(GetModelName(x), GetModelName(y))));
            foreach (var modelType in modelTypes)
            {
                var name = GetModelName(modelType);
                AddName(names, name, modelType);
                models.Add(modelType, name);
            }
            //each model also has a type map named with Type on the end
            foreach (var model in models)
            {
                if (names.TryGetValue(model.Value + "Type", out var existingType))
                    throw new InvalidOperationException($"{existingType.FullName} is named {model.Value}Type, which the client scripts use for the model {model.Key.FullName}, rename one of them");
            }

            return (queryInterfaces, commands, models);
        }

        //the names Bus and the browser use, a query, command, or model with one would replace it
        private static readonly HashSet<string> reservedNames =
        [
            "Bus", "ICommand", "Date", "Blob", "string", "number", "boolean", "any",
            "Object", "Array", "String", "Math", "JSON", "Promise", "TextEncoder", "DataView", "Uint8Array", "fetch", "btoa", "console"
        ];

        private static void AddName(Dictionary<string, Type> names, string name, Type type)
        {
            if (reservedNames.Contains(name))
                throw new InvalidOperationException($"{type.FullName} is named {name}, which the client scripts already use, rename it");
            if (names.TryGetValue(name, out var existingType))
            {
                //a command can also be a model, such as a member of another command
                if (existingType == type)
                    return;
                throw new InvalidOperationException($"{type.FullName} and {existingType.FullName} are both named {name} in the client scripts, rename one of them");
            }
            names.Add(name, type);
        }

        //a generic model is named with its type arguments, PagedResult<Order> is PagedResultOfOrder
        private static string GetModelName(Type type)
        {
            //an array type argument, PagedResult<Order[]> is PagedResultOfArrayOfOrder
            if (type.IsArray)
                return "ArrayOf" + GetModelName(type.GetElementType()!);
            if (!type.IsGenericType)
                return type.Name;
            var name = type.Name;
            var index = name.IndexOf('`');
            if (index >= 0)
                name = name.Substring(0, index);
            var sb = new StringBuilder(name).Append("Of");
            var genericArguments = type.GetGenericArguments();
            for (var i = 0; i < genericArguments.Length; i++)
            {
                if (i > 0)
                    _ = sb.Append("And");
                _ = sb.Append(GetModelName(genericArguments[i]));
            }
            return sb.ToString();
        }

        private static void AddModels(Type type, HashSet<Type> models)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType is not null)
                type = underlyingType;

            if (type.IsEnum || type == typeof(byte[]) || type == typeof(object) || type == typeof(CancellationToken) || typeof(Stream).IsAssignableFrom(type))
                return;

            if (type.IsArray)
            {
                AddModels(type.GetElementType()!, models);
                return;
            }

            if (TypeLookup.GetCoreType(type, out _))
                return;

            //only the types the source generator writes details for are analyzed, so this also works where dynamic code isn't supported
            if (type.IsGenericType)
            {
                var typeDetail = TypeAnalyzer.GetTypeDetail(type);
                if (typeDetail.HasIDictionaryGeneric || typeDetail.HasIReadOnlyDictionaryGeneric)
                {
                    //written as JSON objects the client doesn't convert, the models in them are still written for the code that uses them
                    foreach (var genericArgument in type.GetGenericArguments())
                        AddModels(genericArgument, models);
                    return;
                }
                if (typeDetail.HasIEnumerableGeneric)
                {
                    AddModels(typeDetail.IEnumerableGenericInnerType!, models);
                    return;
                }
            }

            if (type.IsInterface || type.IsAbstract || type.IsPointer || typeof(Delegate).IsAssignableFrom(type) || TypeLookup.GetSpecialType(type, out _))
                return;

            if (!models.Add(type))
                return;

            foreach (var member in GetMembers(type))
                AddModels(member.Type, models);
        }

        //the values of a collection, null if the type isn't one, strings and byte arrays are single values in JSON
        private static Type? GetElementType(Type type)
        {
            if (type == typeof(byte[]))
                return null;
            if (type.IsArray)
                return type.GetElementType();
            if (!type.IsGenericType)
                return null;
            var typeDetail = TypeAnalyzer.GetTypeDetail(type);
            if (typeDetail.HasIDictionaryGeneric || typeDetail.HasIReadOnlyDictionaryGeneric)
                return null;
            if (typeDetail.HasIEnumerableGeneric)
                return typeDetail.IEnumerableGenericInnerType;
            return null;
        }

        //the members as the JSON serializer reads and writes them, in its order, which nameless JSON depends on
        //Key is the name as a property in a class or object, Access reads it from an object, IsWritten is false for a member the server only reads
        private static List<(string Key, string Access, Type Type, bool IsWritten)> GetMembers(Type type)
        {
            var typeDetail = TypeAnalyzer.GetTypeDetail(type);
            var members = new List<(string, string, Type, bool)>();
            foreach (var member in typeDetail.SerializableMembers)
            {
                string? name = null;
                var ignoreCondition = JsonIgnoreCondition.Never;
                foreach (var attribute in member.Attributes)
                {
                    //the same conditions as the serializer
                    if (attribute is JsonIgnoreAttribute jsonIgnore)
                    {
                        if (ignoreCondition != JsonIgnoreCondition.Always)
                            ignoreCondition = jsonIgnore.Condition;
                    }
                    else if (attribute is System.Text.Json.Serialization.JsonIgnoreAttribute jsonIgnore2)
                    {
                        if (ignoreCondition != JsonIgnoreCondition.Always)
                        {
                            ignoreCondition = jsonIgnore2.Condition switch
                            {
                                System.Text.Json.Serialization.JsonIgnoreCondition.Never => JsonIgnoreCondition.Never,
                                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault => JsonIgnoreCondition.WhenWritingDefault,
                                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull => JsonIgnoreCondition.WhenWritingNull,
                                _ => JsonIgnoreCondition.Always,
                            };
                        }
                    }
                    else if (attribute is NonSerializedAttribute)
                    {
                        ignoreCondition = JsonIgnoreCondition.Always;
                    }
                    else if (attribute is JsonPropertyNameAttribute jsonPropertyName)
                    {
                        name ??= jsonPropertyName.Name;
                    }
                    else if (attribute is System.Text.Json.Serialization.JsonPropertyNameAttribute jsonPropertyName2)
                    {
                        name ??= jsonPropertyName2.Name;
                    }
                }
                if (ignoreCondition == JsonIgnoreCondition.Always)
                    continue;

                name ??= member.Name;
                var isWritten = ignoreCondition != JsonIgnoreCondition.WhenWriting;
                if (IsIdentifier(name))
                {
                    members.Add((name, "." + name, member.Type, isWritten));
                }
                else
                {
                    //a JSON name such as my-name is quoted
                    var quoted = JsonSerializer.Serialize(name);
                    members.Add((quoted, "[" + quoted + "]", member.Type, isWritten));
                }
            }
            return members;
        }

        private static bool IsIdentifier(string name)
        {
            if (name.Length == 0)
                return false;
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (c == '_' || c == '$' || Char.IsLetter(c))
                    continue;
                if (i > 0 && Char.IsDigit(c))
                    continue;
                return false;
            }
            return true;
        }

        private static JavaScriptType GetJavaScriptType(Type type, SortedDictionary<Type, string> models)
        {
            var nullable = false;
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType is not null)
            {
                nullable = true;
                type = underlyingType;
            }

            var hasMany = false;
            var elementType = GetElementType(type);
            if (elementType is not null)
            {
                hasMany = true;
                type = Nullable.GetUnderlyingType(elementType) ?? elementType;
            }

            string name;
            bool isJavaScriptType;
            if (typeof(Stream).IsAssignableFrom(type))
            {
                name = "Blob";
                isJavaScriptType = true;
                if (!hasMany)
                    nullable = true;
            }
            else if (type == typeof(byte[]))
            {
                //JSON carries bytes as base64
                name = "string";
                isJavaScriptType = true;
                if (!hasMany)
                    nullable = true;
            }
            else if (type.IsEnum)
            {
                //enums go over the wire by name
                name = "string";
                isJavaScriptType = true;
            }
            else if (models.TryGetValue(type, out var modelName))
            {
                name = modelName;
                isJavaScriptType = false;
                if (!hasMany && !type.IsValueType)
                    nullable = true;
            }
            else
            {
                CoreType? coreType = TypeLookup.GetCoreType(type, out var foundCoreType) ? foundCoreType : null;
                name = coreType switch
                {
                    CoreType.Boolean => "boolean",
                    CoreType.Byte or CoreType.SByte or CoreType.Int16 or CoreType.UInt16 or CoreType.Int32 or CoreType.UInt32 or CoreType.Int64 or CoreType.UInt64 or CoreType.Single or CoreType.Double or CoreType.Decimal => "number",
                    CoreType.DateTime or CoreType.DateTimeOffset => "Date",
                    //TimeSpan is written as a duration, and a DateOnly read as a Date would shift with the time zone
                    CoreType.Char or CoreType.Guid or CoreType.TimeSpan or CoreType.String => "string",
#if !NETSTANDARD2_0
                    CoreType.DateOnly or CoreType.TimeOnly => "string",
#endif
                    _ => "any",
                };
                isJavaScriptType = true;
                if (coreType == CoreType.String && !hasMany)
                    nullable = true;
            }

            return new JavaScriptType(isJavaScriptType, hasMany ? name + "[]" : name, name, hasMany, nullable);
        }

        private sealed class JavaScriptType
        {
            public readonly bool IsJavaScriptType;
            public readonly string Name;
            public readonly string ElementName;
            public readonly bool HasMany;
            public readonly bool Nullable;
            public JavaScriptType(bool isJavaScriptType, string name, string elementName, bool hasMany, bool nullable)
            {
                this.IsJavaScriptType = isJavaScriptType;
                this.Name = name;
                this.ElementName = elementName;
                this.HasMany = hasMany;
                this.Nullable = nullable;
            }
        }
    }
}
