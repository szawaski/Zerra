// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Zerra.T4.CSharp
{
    public static class CSharpParser
    {
        public static CSharpSolution ParseAllFiles(string rootDirectory)
        {
            var files = new List<string>();
            AddFilesRecursive(new DirectoryInfo(rootDirectory), files);

            var solution = new CSharpSolution();
            foreach (var file in files)
            {
                using (var sr = File.OpenText(file))
                {
                    var context = new CSharpFileContext(file.Substring(rootDirectory.Length));
                    try
                    {
                        ParseText(solution, context, sr.ReadToEnd());
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            return solution;
        }
        private static void AddFilesRecursive(DirectoryInfo directory, List<string> files)
        {
            foreach (var subDirectory in directory.GetDirectories())
                AddFilesRecursive(subDirectory, files);
            var directoryFiles = Directory.GetFiles(directory.FullName, "*.cs");
            files.AddRange(directoryFiles);
        }

        private static readonly string[] modifierKeywords = new string[] {
            "public",
            "private",
            "internal",
            "protected",
            "static",
            "partial",
            "virtual",
            "abstract",
            "override",
            "sealed",
            "new",
            "readonly",
            "const",
            "unsafe",
            "explicit",
            "operator",
            "async",
            "volatile",
            "record"
        };

        private static void ParseText(CSharpSolution solution, CSharpFileContext context, string text)
        {
            var chars = text.ToCharArray();
            var index = 0;
            ParseNamespaceBody(solution, context, chars, ref index);
        }

        private static void ParseNamespaceBody(CSharpSolution solution, CSharpFileContext context, char[] chars, ref int index)
        {
            SkipWhiteSpace(context, chars, ref index);

            var currentKeywords = new List<string>();
            while (index < chars.Length)
            {
                var keyword = ReadKeywordOrToken(context, chars, ref index);
                if (keyword == "}" || keyword == String.Empty)
                {
                    break;
                }
                if (keyword.Length <= 1)
                {
                    if (currentKeywords.Count == 0)
                        break;
                    throw new Exception($"Invalid token {keyword} at {index} in {context.FileName}");
                }
                switch (keyword)
                {
                    case "using":
                        ParseUsing(context, chars, ref index, currentKeywords);
                        currentKeywords.Clear();
                        break;
                    case "namespace":
                        ParseNamespace(solution, context, chars, ref index, currentKeywords);
                        currentKeywords.Clear();
                        break;
                    case "class":
                        var csClass = ParseObject(solution, context, chars, ref index, CSharpObjectType.Class, currentKeywords);
                        solution.Classes.Add(csClass);
                        currentKeywords.Clear();
                        break;
                    case "struct":
                        var csStruct = ParseObject(solution, context, chars, ref index, CSharpObjectType.Struct, currentKeywords);
                        solution.Structs.Add(csStruct);
                        currentKeywords.Clear();
                        break;
                    case "enum":
                        var csEnum = ParseEnum(solution, context, chars, ref index, currentKeywords);
                        solution.Enums.Add(csEnum);
                        currentKeywords.Clear();
                        break;
                    case "interface":
                        var csInterface = ParseObject(solution, context, chars, ref index, CSharpObjectType.Interface, currentKeywords);
                        solution.Interfaces.Add(csInterface);
                        currentKeywords.Clear();
                        break;
                    case "delegate":
                        var csDelegate = ParseDelegate(solution, context, chars, ref index, currentKeywords);
                        solution.Delegates.Add(csDelegate);
                        currentKeywords.Clear();
                        break;
                    case "record":
                        var csRecord = ParseObject(solution, context, chars, ref index, CSharpObjectType.Record, currentKeywords);
                        solution.Structs.Add(csRecord);
                        currentKeywords.Clear();
                        break;
                    default:
                        if (modifierKeywords.Contains(keyword) || keyword.StartsWith("["))
                            currentKeywords.Add(keyword);
                        break;
                }
            }
        }

        private static void ParseUsing(CSharpFileContext context, char[] chars, ref int index, IList<string> modifiers)
        {
            if (modifiers.Count > 1)
                throw new Exception($"Invalid keywords before \"using\" at {index} in {context.FileName}");

            var ns = ReadNamespace(context, chars, ref index);
            context.Usings.Add(new CSharpNamespace(ns));
            ExpectToken(context, chars, ref index, ';');
        }

        private static void ParseNamespace(CSharpSolution solution, CSharpFileContext context, char[] chars, ref int index, IList<string> modifiers)
        {
            var ns = ReadNamespace(context, chars, ref index);
            var lastNameSpace = context.Namespaces.Count > 0 ? context.Namespaces.Peek() : null;
            var csNamespace = new CSharpNamespace(lastNameSpace, ns);
            if (!solution.Namespaces.Any(x => x.ToString() == csNamespace.ToString()))
                solution.Namespaces.Add(csNamespace);
            context.Namespaces.Push(csNamespace);
            ExpectToken(context, chars, ref index, '{');
            ParseNamespaceBody(solution, context, chars, ref index);
            _ = context.Namespaces.Pop();
        }

        private static CSharpObject ParseObject(CSharpSolution solution, CSharpFileContext context, char[] chars, ref int index, CSharpObjectType objectType, IList<string> modifiers)
        {
            string name = null;
            var isPublic = modifiers.Contains("public");
            var isStatic = modifiers.Contains("static");
            var isAbstract = modifiers.Contains("abstract");
            var isPartial = modifiers.Contains("partial");
            var attributes = AttributesFromKeywords(context, modifiers);
            var implements = new List<CSharpUnresolvedType>();

            if (index < chars.Length)
            {
                name = ReadKeywordOrToken(context, chars, ref index);
            }

            SkipWhiteSpace(context, chars, ref index);
            var c = chars[index];

            if (c == ':')
            {
                index++;
                var implementsName = ReadKeywordOrToken(context, chars, ref index);
                implements.Add(new CSharpUnresolvedType(solution, context.CurrentNamespace, context.Usings, implementsName));
                SkipWhiteSpace(context, chars, ref index);
                c = chars[index];
                while (c == ',')
                {
                    index++;
                    implementsName = ReadKeywordOrToken(context, chars, ref index);
                    implements.Add(new CSharpUnresolvedType(solution, context.CurrentNamespace, context.Usings, implementsName));
                    SkipWhiteSpace(context, chars, ref index);
                    c = chars[index];
                }
            }

            if (c == 'w')
            {
                var keyword = ReadKeywordOrToken(context, chars, ref index);
                if (keyword != "where")
                    throw new Exception($"Unexpected keyword {keyword} at {index} in {context.FileName}");
                SkipToToken(context, chars, ref index, '{');
            }

            var classes = new List<CSharpObject>();
            var structs = new List<CSharpObject>();
            var enums = new List<CSharpEnum>();
            var interfaces = new List<CSharpObject>();
            var delegates = new List<CSharpDelegate>();
            var fields = new List<CSharpField>();
            var properties = new List<CSharpProperty>();
            var methods = new List<CSharpMethod>();

            if (HasToken(context, chars, ref index, '{'))
            {
                var currentKeywords = new List<string>();
                var statementType = (string)null;
                var statementName = (string)null;
                while (index < chars.Length)
                {
                    var keyword = ReadKeywordOrToken(context, chars, ref index);
                    if (keyword == "}")
                    {
                        break;
                    }
                    switch (keyword)
                    {
                        case "class":
                            var csClass = ParseObject(solution, context, chars, ref index, CSharpObjectType.Class, currentKeywords);
                            classes.Add(csClass);
                            currentKeywords.Clear();
                            statementType = null;
                            statementName = null;
                            break;
                        case "struct":
                            var csStruct = ParseObject(solution, context, chars, ref index, CSharpObjectType.Struct, currentKeywords);
                            structs.Add(csStruct);
                            currentKeywords.Clear();
                            statementType = null;
                            statementName = null;
                            break;
                        case "enum":
                            var csEnum = ParseEnum(solution, context, chars, ref index, currentKeywords);
                            enums.Add(csEnum);
                            currentKeywords.Clear();
                            statementType = null;
                            statementName = null;
                            break;
                        case "interface":
                            var csInterface = ParseObject(solution, context, chars, ref index, CSharpObjectType.Interface, currentKeywords);
                            interfaces.Add(csInterface);
                            currentKeywords.Clear();
                            statementType = null;
                            statementName = null;
                            break;
                        case "delegate":
                            var csDelegate = ParseDelegate(solution, context, chars, ref index, currentKeywords);
                            delegates.Add(csDelegate);
                            currentKeywords.Clear();
                            statementType = null;
                            statementName = null;
                            break;
                        case ";":
                        case "=":
                            var csField = ParseField(solution, context, chars, ref index, statementType, statementName, currentKeywords);
                            fields.Add(csField);
                            currentKeywords.Clear();
                            statementType = null;
                            statementName = null;
                            break;
                        case "{":
                            //property;
                            var csProperty = ParseProperty(solution, context, chars, ref index, statementType, statementName, currentKeywords);
                            properties.Add(csProperty);
                            currentKeywords.Clear();
                            statementType = null;
                            statementName = null;
                            break;
                        case "(":
                            //method;
                            if (statementType == null)
                            {
                                SkipToToken(context, chars, ref index, ')');
                                index++;
                                statementType = "object";
                            }
                            else
                            {
                                if (statementName == null)
                                {
                                    statementName = statementType;
                                    statementType = "void";
                                }
                                var csMethod = ParseMethod(solution, context, chars, ref index, statementType, statementName, currentKeywords);
                                methods.Add(csMethod);
                                currentKeywords.Clear();
                                statementType = null;
                                statementName = null;
                            }
                            break;
                        default:
                            if (modifierKeywords.Contains(keyword) || keyword.StartsWith("["))
                            {
                                currentKeywords.Add(keyword);
                                break;
                            }
                            if (statementType == null)
                                statementType = keyword;
                            else if (statementName == null)
                                statementName = keyword;
                            else
                                throw new Exception($"Invalid token {keyword} at {index} in {context.FileName}");
                            if (statementName != null && (statementName == "this" || statementName.EndsWith(".this")))
                            {
                                //property;
                                var csIndexProperty = ParseIndexProperty(solution, context, chars, ref index, statementType, currentKeywords);
                                properties.Add(csIndexProperty);
                                currentKeywords.Clear();
                                statementType = null;
                                statementName = null;
                            }
                            break;
                    }
                }
            }

            var csObject = new CSharpObject(context.CurrentNamespace, context.Usings, name, objectType, implements, isPublic, isStatic, isAbstract, isPartial, classes, structs, interfaces, enums, delegates, properties, methods, attributes);
            return csObject;
        }

        private static CSharpEnum ParseEnum(CSharpSolution solution, CSharpFileContext context, char[] chars, ref int index, IList<string> modifiers)
        {
            var ns = context.Namespaces.Count > 0 ? context.Namespaces.Peek() : null;
            var type = typeof(int);
            var isPublic = modifiers.Contains("public");
            var attributes = AttributesFromKeywords(context, modifiers);

            var name = ReadKeywordOrToken(context, chars, ref index);

            SkipWhiteSpace(context, chars, ref index);
            var c = chars[index];
            if (c == ':')
            {
                index++;
                var typeName = ReadKeywordOrToken(context, chars, ref index);
                switch (typeName)
                {
                    case "sbyte": type = typeof(sbyte); break;
                    case "byte": type = typeof(byte); break;
                    case "short": type = typeof(short); break;
                    case "ushort": type = typeof(ushort); break;
                    case "int": type = typeof(int); break;
                    case "uint": type = typeof(uint); break;
                    case "long": type = typeof(long); break;
                    case "ulong": type = typeof(ulong); break;
                }
            }

            ExpectToken(context, chars, ref index, '{');

            long largestValue = 0;
            var enumValues = new List<CSharpEnumValue>();
            while (index < chars.Length)
            {
                SkipWhiteSpace(context, chars, ref index);
                c = chars[index];
                if (c == ',')
                {
                    index++;
                    continue;
                }
                else if (c == '}')
                {
                    index++;
                    break;
                }

                var valueAttributes = new List<CSharpAttribute>();
                string valueName = null;
                while (index < chars.Length)
                {
                    var keyword = ReadKeywordOrToken(context, chars, ref index);
                    if (keyword.StartsWith("["))
                    {
                        valueAttributes.Add(ParseAttribute(context, keyword));
                        continue;
                    }
                    valueName = keyword;
                    break;
                }

                SkipWhiteSpace(context, chars, ref index);

                long? value = null;
                c = chars[index];
                if (c == '=')
                {
                    index++;
                    var valueKeyword = ReadKeywordOrToken(context, chars, ref index);
                    if (!Int64.TryParse(valueKeyword, out var parsedValue))
                        throw new Exception($"Invalid keyword {valueKeyword} at {index} in {context.FileName}");
                    value = parsedValue;
                    if (value.Value > largestValue)
                        largestValue = value.Value;
                }

                if (!value.HasValue)
                {
                    largestValue++;
                    value = largestValue;
                }

                var enumValue = new CSharpEnumValue(valueName, value.Value, valueAttributes);
                enumValues.Add(enumValue);
            }

            var csEnum = new CSharpEnum(solution, ns, context.Usings, name, type, isPublic, enumValues, attributes);
            return csEnum;
        }

        private static CSharpDelegate ParseDelegate(CSharpSolution solution, CSharpFileContext context, char[] chars, ref int index, IList<string> modifiers)
        {
            var ns = context.Namespaces.Count > 0 ? context.Namespaces.Peek() : null;
            var isPublic = modifiers.Contains("public");
            var attributes = AttributesFromKeywords(context, modifiers);


            var returnType = ReadKeywordOrToken(context, chars, ref index);
            var name = ReadKeywordOrToken(context, chars, ref index);

            ExpectToken(context, chars, ref index, '(');

            var arguments = ParseParameters(solution, context, chars, ref index);

            ExpectToken(context, chars, ref index, ';');

            var unresolvedReturnType = new CSharpUnresolvedType(solution, ns, context.Usings, returnType);
            var csDelegate = new CSharpDelegate(ns, context.Usings, name, unresolvedReturnType, isPublic, arguments, attributes);
            return csDelegate;
        }

        private static CSharpField ParseField(CSharpSolution solution, CSharpFileContext context, char[] chars, ref int index, string type, string name, IList<string> modifiers)
        {
            var isPublic = modifiers.Contains("public");
            var isStatic = modifiers.Contains("static");
            var isReadOnly = modifiers.Contains("readonly") || modifiers.Contains("const");
            var attributes = AttributesFromKeywords(context, modifiers);

            var c = chars[index - 1];
            if (c != ';')
            {
                SkipToToken(context, chars, ref index, ';');
                index++;
            }

            var unresolvedType = new CSharpUnresolvedType(solution, context.CurrentNamespace, context.Usings, type);
            var csField = new CSharpField(name, unresolvedType, isPublic, isStatic, isReadOnly, attributes);
            return csField;
        }

        private static CSharpProperty ParseProperty(CSharpSolution solution, CSharpFileContext context, char[] chars, ref int index, string type, string name, IList<string> modifiers)
        {
            var isPublic = modifiers.Contains("public");
            var isStatic = modifiers.Contains("static");
            var isVirtual = modifiers.Contains("virtual");
            var isAbstract = modifiers.Contains("abstract");
            var hasGet = false;
            var hasSet = false;
            var isGetPublic = false;
            var isSetPublic = false;
            var attributes = AttributesFromKeywords(context, modifiers);

            index++;
            string firstKeyword = null;
            while (index < chars.Length)
            {
                var keyword = ReadKeywordOrToken(context, chars, ref index);
                if (keyword == "}")
                {
                    break;
                }
                switch (keyword)
                {
                    case "public":
                    case "internal":
                    case "private":
                        firstKeyword = keyword;
                        break;
                    case "get":
                        hasGet = true;
                        isGetPublic = firstKeyword == null || firstKeyword == "public";
                        firstKeyword = null;
                        var afterGetToken = ReadKeywordOrToken(context, chars, ref index);
                        switch (afterGetToken)
                        {
                            case ";": break;
                            case "=": SkipToToken(context, chars, ref index, ';'); break;
                            case "{": SkipBlock(context, chars, ref index); break;
                            default: throw new Exception($"Invalid token {afterGetToken} at {index} in {context.FileName}");
                        }
                        break;
                    case "set":
                        hasSet = true;
                        isSetPublic = firstKeyword == null || firstKeyword == "public";
                        firstKeyword = null;
                        var afterSetToken = ReadKeywordOrToken(context, chars, ref index);
                        switch (afterSetToken)
                        {
                            case ";": break;
                            case "=": SkipToToken(context, chars, ref index, ';'); break;
                            case "{": SkipBlock(context, chars, ref index); break;
                            default: throw new Exception($"Invalid token {afterSetToken} at {index} in {context.FileName}");
                        }
                        break;
                }
            }

            SkipWhiteSpace(context, chars, ref index);
            var c = chars[index];
            if (c == '=')
            {
                SkipToToken(context, chars, ref index, ';');
                index++;
            }

            var unresolvedType = new CSharpUnresolvedType(solution, context.CurrentNamespace, context.Usings, type);
            var csProperty = new CSharpProperty(name, unresolvedType, isPublic, isStatic, isVirtual, isAbstract, hasGet, hasSet, isGetPublic, isSetPublic, attributes);
            return csProperty;
        }

        private static CSharpProperty ParseIndexProperty(CSharpSolution solution, CSharpFileContext context, char[] chars, ref int index, string type, IList<string> modifiers)
        {
            ExpectToken(context, chars, ref index, '[');

            var arguments = ParseParameters(solution, context, chars, ref index);

            ExpectToken(context, chars, ref index, '{');

            var csProperty = ParseProperty(solution, context, chars, ref index, type, "this", modifiers);
            return csProperty;
        }

        private static List<CSharpParameter> ParseParameters(CSharpSolution solution, CSharpFileContext context, char[] chars, ref int index)
        {
            var parameters = new List<CSharpParameter>();
            var parameterKeywords = new List<string>();
            while (index < chars.Length)
            {
                var parameterKeyword = ReadKeywordOrToken(context, chars, ref index);
                if (parameterKeyword == ")" || parameterKeyword == "]" || parameterKeyword == "," || parameterKeyword == "=")
                {
                    if (parameterKeywords.Count > 0)
                    {
                        if (parameterKeywords.Count < 2)
                            throw new Exception($"Invalid Syntax at {index} in {context.FileName}");
                        var pName = parameterKeywords[parameterKeywords.Count - 1];
                        var pType = parameterKeywords[parameterKeywords.Count - 2];
                        var pIsIn = parameterKeywords.Contains("in");
                        var pIsOut = parameterKeywords.Contains("out");
                        var pIsRef = parameterKeywords.Contains("ref");
                        var pDefaultValue = (string)null;
                        if (parameterKeyword == "=")
                        {
                            var methodLevel = 0;
                            for (; index < chars.Length; index++)
                            {
                                var c = chars[index];
                                if (c == '(')
                                {
                                    methodLevel++;
                                }
                                else if (c == ')')
                                {
                                    if (methodLevel == 0)
                                        break;
                                    methodLevel--;
                                }
                                else if (c == ',' && methodLevel == 0)
                                {
                                    break;
                                }
                            }
                        }
                        parameterKeywords.Clear();
                        var unresolvedType = new CSharpUnresolvedType(solution, context.CurrentNamespace, context.Usings, pType);
                        var csParameter = new CSharpParameter(pName, unresolvedType, pIsIn, pIsOut, pIsRef, pDefaultValue);
                        parameters.Add(csParameter);
                    }
                    if (parameterKeyword == ")" || parameterKeyword == "]")
                        break;
                }
                else
                {
                    parameterKeywords.Add(parameterKeyword);
                }
            }
            return parameters;
        }

        private static CSharpMethod ParseMethod(CSharpSolution solution, CSharpFileContext context, char[] chars, ref int index, string returnType, string name, IList<string> modifiers)
        {
            var isPublic = modifiers.Contains("public");
            var isStatic = modifiers.Contains("static");
            var isVirtual = modifiers.Contains("virtual");
            var isAbstract = modifiers.Contains("abstract");
            var isImplemented = false;
            var attributes = AttributesFromKeywords(context, modifiers);

            var parameters = ParseParameters(solution, context, chars, ref index);

            SkipWhiteSpace(context, chars, ref index);
            var c = chars[index];
            if (c == ':')
            {
                index++;
                var keyword = ReadKeywordOrToken(context, chars, ref index);
                if (keyword != "base" && keyword != "this")
                    throw new Exception($"Unexpected keyword {keyword} at {index} in {context.FileName}");
                SkipToToken(context, chars, ref index, '(');
                index++;
                SkipParenthesis(context, chars, ref index);
                SkipWhiteSpace(context, chars, ref index);
                c = chars[index];
            }
            if (c == 'w')
            {
                var keyword = ReadKeywordOrToken(context, chars, ref index);
                if (keyword != "where")
                    throw new Exception($"Unexpected keyword {keyword} at {index} in {context.FileName}");
                SkipToToken(context, chars, ref index, '{', ';');
                c = chars[index];
            }
            if (c == '{')
            {
                index++;
                isImplemented = true;
                SkipBlock(context, chars, ref index);
            }
            else if (c == ';')
            {
                index++;
            }
            else if (c == '=')
            {
                index++;
                SkipToToken(context, chars, ref index, ';');
                index++;
            }
            else
            {
                throw new Exception($"Unexpected token {c} at {index} in {context.FileName}");
            }

            var unresolvedReturnType = new CSharpUnresolvedType(solution, context.CurrentNamespace, context.Usings, returnType);
            var csMethod = new CSharpMethod(name, unresolvedReturnType, isPublic, isStatic, isVirtual, isAbstract, isImplemented, parameters, attributes);
            return csMethod;
        }

        private static CSharpAttribute[] AttributesFromKeywords(CSharpFileContext context, IList<string> keywords)
        {
            var attributes = new List<CSharpAttribute>();
            foreach (var keyword in keywords)
            {
                if (!keyword.StartsWith("["))
                    break;
                var attribute = ParseAttribute(context, keyword);
                attributes.Add(attribute);
            }
            return attributes.ToArray();
        }
        private static CSharpAttribute ParseAttribute(CSharpFileContext context, string text)
        {
            var index = 0;
            var chars = text.ToCharArray();

            ExpectTokenSubset(context, chars, ref index, '[');
            var name = ReadKeywordOrToken(context, chars, ref index);
            var arguments = new List<string>();


            if (index < chars.Length)
            {
                var c = chars[index];
                if (c == '(')
                {
                    var parenthesis = 1;
                    var buffer = new List<char>();
                    index++;
                    for (; index < chars.Length; index++)
                    {
                        c = chars[index];
                        if (c == ',')
                        {
                            arguments.Add(new string(buffer.ToArray()));
                            buffer.Clear();
                        }
                        else if (c == '(')
                        {
                            parenthesis++;
                        }
                        else if (c == ')')
                        {
                            parenthesis--;
                            if (parenthesis == 0)
                            {
                                arguments.Add(new string(buffer.ToArray()));
                                index++;
                                break;
                            }
                            else
                            {
                                buffer.Add(c);
                            }
                        }
                        else
                        {
                            buffer.Add(c);
                        }
                    }
                }
            }

            ExpectTokenSubset(context, chars, ref index, ']');

            var csAttribute = new CSharpAttribute(name, arguments);
            return csAttribute;
        }

        private static void SkipWhiteSpace(CSharpFileContext context, char[] chars, ref int index)
        {
            byte comment = 0;
            /* 
            1=First Char Comment
            2=// Comment
            3=/* Comment
            4=First Char End /* Comment 
            5=Region
            */
            for (; index < chars.Length; index++)
            {
                var c = chars[index];
                if (c == '\n')
                    context.Line++;
                switch (comment)
                {
                    case 0:
                        if (Char.IsWhiteSpace(c))
                            continue;
                        else if (c == '/')
                            comment = 1;
                        else if (c == '#')
                            comment = 5;
                        else
                            return;
                        break;
                    case 1:
                        if (c == '/')
                            comment = 2;
                        else if (c == '*')
                            comment = 3;
                        else
                            comment = 0;
                        break;
                    case 2:
                        if (c == '\r' || c == '\n')
                            comment = 0;
                        break;
                    case 3:
                        if (c == '*')
                            comment = 4;
                        break;
                    case 4:
                        if (c == '/')
                            comment = 0;
                        else
                            comment = 3;
                        break;
                    case 5:
                        if (c == '\r' || c == '\n')
                            comment = 0;
                        break;
                }
            }
            if (comment == 1)
                index--;
        }

        private static string ReadKeywordOrToken(CSharpFileContext context, char[] chars, ref int index)
        {
            SkipWhiteSpace(context, chars, ref index);

            var genericLevel = 0;
            var attribute = 0;
            var buffer = new List<char>();
            for (; index < chars.Length; index++)
            {
                var c = chars[index];
                if (Char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '?')
                {
                    buffer.Add(c);
                }
                else if (c == ':' && buffer.Count == 6 || buffer.Count == 7 && new string(buffer.Take(6).ToArray()) == "global")
                {
                    buffer.Add(c);
                }
                else if (buffer.Count == 0)
                {
                    if (c == '[')
                    {
                        attribute++;
                        buffer.Add(c);
                    }
                    else if (c == '~')
                    {
                        buffer.Add(c);
                    }
                    else
                    {
                        index++;
                        return new string(new char[] { c });
                    }
                }
                else if (c == '<' && attribute == 0)
                {
                    genericLevel++;
                    buffer.Add(c);
                }
                else if (c == '>' && attribute == 0)
                {
                    genericLevel--;
                    buffer.Add(c);
                }
                else if (genericLevel == 0)
                {
                    if (attribute > 0)
                    {
                        buffer.Add(c);
                        if (c == '[')
                        {
                            attribute++;
                        }
                        if (c == ']')
                        {
                            attribute--;
                            if (attribute == 0)
                            {
                                index++;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (c == '[' && index + 1 < chars.Length && chars[index + 1] == ']')
                        {
                            buffer.Add(c);
                            buffer.Add(']');
                            index += 2;
                            c = chars[index];
                        }
                        if (c == '?')
                        {
                            buffer.Add(c);
                            index += 1;
                        }
                        break;
                    }
                }
                else if (genericLevel > 0)
                {
                    buffer.Add(c);
                }
            }
            return new string(buffer.ToArray());
        }

        private static string ReadNamespace(CSharpFileContext context, char[] chars, ref int index)
        {
            SkipWhiteSpace(context, chars, ref index);

            var buffer = new List<char>();
            for (; index < chars.Length; index++)
            {
                var c = chars[index];
                if (Char.IsLetterOrDigit(c) || c == '.' || c == '_')
                {
                    buffer.Add(c);
                }
                else if (c == ':' && buffer.Count == 6 || buffer.Count == 7 && new string(buffer.Take(6).ToArray()) == "global")
                {
                    buffer.Add(c);
                }
                else
                {
                    break;
                }
            }
            return new string(buffer.ToArray());
        }

        private static void ExpectToken(CSharpFileContext context, char[] chars, ref int index, char token)
        {
            SkipWhiteSpace(context, chars, ref index);
            var c = chars[index];
            if (c != token)
                throw new Exception($"Expected {token} at {index} in {context.FileName}");
            index++;
        }
        private static void ExpectTokenSubset(CSharpFileContext context, char[] chars, ref int index, char token)
        {
            SkipWhiteSpace(context, chars, ref index);
            var c = chars[index];
            if (c != token)
                throw new Exception($"Expected {token} at {index} in {new string(chars)}");
            index++;
        }

        private static bool HasToken(CSharpFileContext context, char[] chars, ref int index, char token)
        {
            SkipWhiteSpace(context, chars, ref index);
            var c = chars[index++];
            return c == token;
        }

        private static void SkipToToken(CSharpFileContext context, char[] chars, ref int index, params char[] tokens)
        {
            byte comment = 0;
            /* 
            1=Pre-Comment
            2=Regular Comment
            3=Multiline Comment
            4=Pre-End Multiline Comment 
            5=Region
            */
            byte quote = 0;
            /* 
            1=Double Quote
            2=Double Quote Multiline
            3=Single Quote
            */
            for (; index < chars.Length; index++)
            {
                var c = chars[index];
                switch (c)
                {
                    case '/':
                        if (quote == 0)
                        {
                            if (comment == 0)
                                comment = 1; //pre-comment
                            else if (comment == 1)
                                comment = 2; //regular comment
                            else if (comment == 4)
                                comment = 0; //end multiline comment
                        }
                        break;
                    case '*':
                        if (quote == 0)
                        {
                            if (comment == 1)
                                comment = 3; //multiline comment
                            else if (comment == 3)
                                comment = 4; // pre-end multiline comment
                        }
                        break;
                    case '#':
                        if (quote == 0)
                        {
                            if (comment == 0)
                                comment = 5; //region
                        }
                        break;
                    case '\n':
                        if (comment == 1)
                            comment = 0;
                        if (quote == 0)
                        {
                            if (comment == 2)
                                comment = 0; //end regular comment
                            else if (comment == 5)
                                comment = 0; //end region
                        }
                        context.Line++;
                        break;
                    case '"':
                        if (comment == 1)
                            comment = 0;
                        if (comment < 2)
                        {
                            if (quote == 0)
                            {
                                quote = 1;
                                if (chars[index - 1] == '@' || (chars[index - 1] == '$' && chars[index - 2] == '@'))
                                    quote = 2;
                            }
                            else if (quote == 1)
                            {
                                if (chars[index - 1] != '\\')
                                    quote = 0;
                            }
                            else if (quote == 2)
                            {
                                if (index + 1 < chars.Length && chars[index + 1] != '"')
                                {
                                    quote = 0;
                                }
                                else
                                {
                                    index++;
                                }
                            }
                        }
                        break;
                    case '\'':
                        if (comment == 1)
                            comment = 0;
                        if (comment < 2)
                        {
                            if (quote == 0)
                            {
                                quote = 3;
                            }
                            else if (quote == 3)
                            {
                                if (chars[index - 1] != '\'' || chars[index - 2] != '\\')
                                    quote = 0;
                            }
                        }
                        break;
                    default:
                        if (comment == 1)
                            comment = 0;
                        break;
                }
                if (comment < 2 && quote == 0 && tokens.Contains(c))
                    break;
            }
        }
        private static void SkipBlock(CSharpFileContext context, char[] chars, ref int index)
        {
            var blockLevel = 1;
            byte comment = 0;
            /* 
            1=Pre-Comment
            2=Regular Comment
            3=Multiline Comment
            4=Pre-End Multiline Comment 
            5=Region
            */
            byte quote = 0;
            /* 
            1=Double Quote
            2=Double Quote Multiline
            3=Single Quote
            */
            for (; index < chars.Length; index++)
            {
                var c = chars[index];
                switch (c)
                {
                    case '/':
                        if (quote == 0)
                        {
                            if (comment == 0)
                                comment = 1; //pre-comment
                            else if (comment == 1)
                                comment = 2; //regular comment
                            else if (comment == 4)
                                comment = 0; //end multiline comment
                        }
                        break;
                    case '*':
                        if (quote == 0)
                        {
                            if (comment == 1)
                                comment = 3; //multiline comment
                            else if (comment == 3)
                                comment = 4; // pre-end multiline comment
                        }
                        break;
                    case '#':
                        if (quote == 0)
                        {
                            if (comment == 0)
                                comment = 5; //region
                        }
                        break;
                    case '\n':
                        if (comment == 1)
                            comment = 0;
                        if (quote == 0)
                        {
                            if (comment == 2)
                                comment = 0; //end regular comment
                            else if (comment == 5)
                                comment = 0; //end region
                        }
                        context.Line++;
                        break;
                    case '"':
                        if (comment == 1)
                            comment = 0;
                        if (comment < 2)
                        {
                            if (quote == 0)
                            {
                                quote = 1;
                                if (chars[index - 1] == '@' || (chars[index - 1] == '$' && chars[index - 2] == '@'))
                                    quote = 2;
                            }
                            else if (quote == 1)
                            {
                                if (chars[index - 1] != '\\')
                                    quote = 0;
                            }
                            else if (quote == 2)
                            {
                                if (index + 1 < chars.Length && chars[index + 1] != '"')
                                {
                                    quote = 0;
                                }
                                else
                                {
                                    index++;
                                }
                            }
                        }
                        break;
                    case '\'':
                        if (comment == 1)
                            comment = 0;
                        if (comment < 2)
                        {
                            if (quote == 0)
                            {
                                quote = 3;
                            }
                            else if (quote == 3)
                            {
                                if (chars[index - 1] != '\'' || chars[index - 2] != '\\')
                                    quote = 0;
                            }
                        }
                        break;
                    case '{':
                        if (comment == 1)
                            comment = 0;
                        if (comment < 2 && quote == 0)
                            blockLevel++;
                        break;
                    case '}':
                        if (comment == 1)
                            comment = 0;
                        if (comment < 2 && quote == 0)
                        {
                            blockLevel--;
                            if (blockLevel == 0)
                            {
                                index++;
                                return;
                            }
                        }
                        break;
                    default:
                        if (comment == 1)
                            comment = 0;
                        break;
                }
            }
        }
        private static void SkipParenthesis(CSharpFileContext context, char[] chars, ref int index)
        {
            var blockLevel = 1;
            byte comment = 0;
            /* 
            1=Pre-Comment
            2=Regular Comment
            3=Multiline Comment
            4=Pre-End Multiline Comment 
            5=Region
            */
            byte quote = 0;
            /* 
            1=Double Quote
            2=Double Quote Multiline
            3=Single Quote
            */
            for (; index < chars.Length; index++)
            {
                var c = chars[index];
                switch (c)
                {
                    case '/':
                        if (quote == 0)
                        {
                            if (comment == 0)
                                comment = 1; //pre-comment
                            else if (comment == 1)
                                comment = 2; //regular comment
                            else if (comment == 4)
                                comment = 0; //end multiline comment
                        }
                        break;
                    case '*':
                        if (quote == 0)
                        {
                            if (comment == 1)
                                comment = 3; //multiline comment
                            else if (comment == 3)
                                comment = 4; // pre-end multiline comment
                        }
                        break;
                    case '#':
                        if (quote == 0)
                        {
                            if (comment == 0)
                                comment = 5; //region
                        }
                        break;
                    case '\n':
                        if (comment == 1)
                            comment = 0;
                        if (quote == 0)
                        {
                            if (comment == 2)
                                comment = 0; //end regular comment
                            else if (comment == 5)
                                comment = 0; //end region
                        }
                        context.Line++;
                        break;
                    case '"':
                        if (comment == 1)
                            comment = 0;
                        if (comment < 2)
                        {
                            if (quote == 0)
                            {
                                quote = 1;
                                if (chars[index - 1] == '@' || (chars[index - 1] == '$' && chars[index - 2] == '@'))
                                    quote = 2;
                            }
                            else if (quote == 1)
                            {
                                if (chars[index - 1] != '\\')
                                    quote = 0;
                            }
                            else if (quote == 2)
                            {
                                if (index + 1 < chars.Length && chars[index + 1] != '"')
                                {
                                    quote = 0;
                                }
                                else
                                {
                                    index++;
                                }
                            }
                        }
                        break;
                    case '\'':
                        if (comment == 1)
                            comment = 0;
                        if (comment < 2)
                        {
                            if (quote == 0)
                            {
                                quote = 3;
                            }
                            else if (quote == 3)
                            {
                                if (chars[index - 1] != '\'' || chars[index - 2] != '\\')
                                    quote = 0;
                            }
                        }
                        break;
                    case '(':
                        if (comment == 1)
                            comment = 0;
                        if (comment < 2 && quote == 0)
                            blockLevel++;
                        break;
                    case ')':
                        if (comment == 1)
                            comment = 0;
                        if (comment < 2 && quote == 0)
                        {
                            blockLevel--;
                            if (blockLevel == 0)
                            {
                                index++;
                                return;
                            }
                        }
                        break;
                    default:
                        if (comment == 1)
                            comment = 0;
                        break;
                }
            }
        }
    }
}