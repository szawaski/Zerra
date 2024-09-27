// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Zerra.SourceGeneration
{
    public static class TypeDetailSourceGenerator
    {
        private static readonly string nullaleTypeName = typeof(Nullable<>).Name;
        private static readonly string enumberableTypeName = nameof(IEnumerable);
        private static readonly string enumberableGenericTypeName = typeof(IEnumerable<>).Name;

        private static readonly string collectionTypeName = nameof(ICollection);
        private static readonly string collectionGenericTypeName = typeof(ICollection<>).Name;
        private static readonly string readOnlyCollectionGenericTypeName = typeof(IReadOnlyCollection<>).Name;
        private static readonly string listTypeName = nameof(IList);
        private static readonly string listGenericTypeName = typeof(IList<>).Name;
        private static readonly string readOnlyListTypeName = typeof(IReadOnlyList<>).Name;
        private static readonly string setGenericTypeName = typeof(ISet<>).Name;
#if NET5_0_OR_GREATER
        private static readonly string readOnlySetGenericTypeName = typeof(IReadOnlySet<>).Name;
#else
        private static readonly string readOnlySetGenericTypeName = "IReadOnlySet`1";
#endif
        private static readonly string dictionaryTypeName = typeof(IDictionary).Name;
        private static readonly string dictionaryGenericTypeName = typeof(IDictionary<,>).Name;
        private static readonly string readOnlyDictionaryGenericTypeName = typeof(IReadOnlyDictionary<,>).Name;

        public static void GenerateInitializer(SourceProductionContext context, Compilation compilation, List<Tuple<string, string>> classList)
        {
            var sb = new StringBuilder();
            foreach (var item in classList)
            {
                if (sb.Length > 0)
                    sb.Append(Environment.NewLine).Append("            ");
                sb.Append("TypeAnalyzer.AddTypeDetailCreator(typeof(").Append(item.Item1).Append("), () => new ").Append(item.Item2).Append("());");
            }
            var lines = sb.ToString();

            var code = $$"""
                //Zerra Generated File
                #if NET5_0_OR_GREATER

                using System;
                using System.Runtime.CompilerServices;
                using Zerra.Reflection;

                namespace {{compilation.AssemblyName}}.SourceGeneration
                {
                    public static class TypeDetailInitializer
                    {
                #pragma warning disable CA2255
                        [ModuleInitializer]
                #pragma warning restore CA2255
                        public static void Initialize()
                        {
                            {{lines}}
                        }
                    }
                }

                #endif
            
                """;

            context.AddSource("TypeDetailInitializer.cs", SourceText.From(code, Encoding.UTF8));
        }

        private static bool ShouldGenerateType(INamedTypeSymbol typeSymbol, ImmutableArray<INamedTypeSymbol> allTypeSymbol)
        {
            //We only want types in this build or generics that contain the types in this build.
            if (allTypeSymbol.Contains(typeSymbol))
                return true;

            if (typeSymbol.TypeParameters.Length == 0)
                return false;

            var allTypeSymbolString = allTypeSymbol.Select(x => $"{x.ContainingNamespace}.{x.Name}");
            foreach (var typeArgument in typeSymbol.TypeParameters)
            {
                var typeArgumentString = $"{typeArgument.ContainingNamespace}.{typeArgument.Name}";
                if (allTypeSymbolString.Contains(typeArgumentString))
                    return true;
            }

            return false;
        }

        public static void GenerateType(SourceProductionContext context, INamedTypeSymbol typeSymbol, ImmutableArray<INamedTypeSymbol> allTypeSymbol, List<Tuple<string, string>> classList, bool recursive)
        {
            var namespaceRecursionCheck = typeSymbol;
            while (namespaceRecursionCheck is not null)
            {
                if (namespaceRecursionCheck.ContainingNamespace.ToString().StartsWith("Zerra.Reflection"))
                    return;
                namespaceRecursionCheck = namespaceRecursionCheck.BaseType;
            }

            var ns = typeSymbol.ContainingNamespace.ToString().Contains("<global namespace>") ? null : typeSymbol.ContainingNamespace.ToString();

            var fullTypeName = typeSymbol.ToString();
            if (!typeSymbol.IsValueType)
                fullTypeName = fullTypeName.Replace("?", String.Empty);

            var typeName = typeSymbol.ToString();
            if (ns != null && typeName.StartsWith(ns))
                typeName = typeName.Substring(ns.Length + 1);
            typeName = typeName.Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace('.', '_');
            if (typeName.EndsWith("?"))
            {
                if (typeSymbol.IsValueType)
                    typeName = typeName.Replace("?", String.Empty) + "Nullable";
                else
                    typeName = typeName.Replace("?", String.Empty);
            }

            var className = $"{typeName}TypeDetail";
            var fileName = ns == null ? $"{typeName}TypeDetail.cs" : $"{ns}.{typeName}TypeDetail.cs";

            var fullClassName = ns == null ? $"SourceGeneration.{className}" : $"{ns}.SourceGeneration.{className}";
            var classListItem = new Tuple<string, string>(fullTypeName, fullClassName);
            if (classList.Contains(classListItem))
                return;
            classList.Add(classListItem);

            string? typeConstraints = null;
            if (typeSymbol.TypeArguments.Length > 0)
            {
                var sbConstraints = new StringBuilder();
                for (var i = 0; i < typeSymbol.TypeArguments.Length; i++)
                {
                    var typeArgument = typeSymbol.TypeArguments[i];
                    if (typeArgument is ITypeParameterSymbol typeParameterSymbol)
                    {
                        if (typeParameterSymbol.HasUnmanagedTypeConstraint || typeParameterSymbol.HasValueTypeConstraint || typeParameterSymbol.HasReferenceTypeConstraint || typeParameterSymbol.HasNotNullConstraint || typeParameterSymbol.HasConstructorConstraint)
                        {
                            _ = sbConstraints.Append(" where ").Append(typeArgument.Name).Append(" : ");
                            var wroteConstraint = false;

                            if (typeParameterSymbol.HasUnmanagedTypeConstraint)
                            {
                                if (wroteConstraint)
                                    _ = sbConstraints.Append(", ");
                                else
                                    wroteConstraint = true;
                                _ = sbConstraints.Append("unmanaged");
                            }
                            if (typeParameterSymbol.HasValueTypeConstraint)
                            {
                                if (wroteConstraint)
                                    _ = sbConstraints.Append(", ");
                                else
                                    wroteConstraint = true;
                                _ = sbConstraints.Append("struct");
                            }
                            if (typeParameterSymbol.HasReferenceTypeConstraint)
                            {
                                if (wroteConstraint)
                                    _ = sbConstraints.Append(", ");
                                else
                                    wroteConstraint = true;
                                _ = sbConstraints.Append("class");
                            }
                            if (typeParameterSymbol.HasNotNullConstraint)
                            {
                                if (wroteConstraint)
                                    _ = sbConstraints.Append(", ");
                                else
                                    wroteConstraint = true;
                                _ = sbConstraints.Append("notnull");
                            }
                            if (typeParameterSymbol.HasConstructorConstraint)
                            {
                                if (wroteConstraint)
                                    _ = sbConstraints.Append(", ");
                                else
                                    wroteConstraint = true;
                                _ = sbConstraints.Append("new()");
                            }
                        }
                    }
                }

                typeConstraints = sbConstraints.ToString();
            }

            var isArray = typeSymbol.Kind == SymbolKind.ArrayType;
            var interfaceNames = typeSymbol.AllInterfaces.Select(x => x.Name).ToImmutableHashSet();

            var hasIEnumerable = isArray || typeSymbol.Name == enumberableTypeName || interfaceNames.Contains(enumberableTypeName);
            var hasIEnumerableGeneric = isArray || typeSymbol.Name == enumberableGenericTypeName || interfaceNames.Contains(enumberableGenericTypeName);
            var hasICollection = typeSymbol.Name == collectionTypeName || interfaceNames.Contains(collectionTypeName);
            var hasICollectionGeneric = typeSymbol.Name == collectionGenericTypeName || interfaceNames.Contains(collectionGenericTypeName);
            var hasIReadOnlyCollectionGeneric = typeSymbol.Name == readOnlyCollectionGenericTypeName || interfaceNames.Contains(readOnlyCollectionGenericTypeName);
            var hasIList = typeSymbol.Name == listTypeName || interfaceNames.Contains(listTypeName);
            var hasIListGeneric = typeSymbol.Name == listGenericTypeName || interfaceNames.Contains(listGenericTypeName);
            var hasIReadOnlyListGeneric = typeSymbol.Name == readOnlyListTypeName || interfaceNames.Contains(readOnlyListTypeName);
            var hasISetGeneric = typeSymbol.Name == setGenericTypeName || interfaceNames.Contains(setGenericTypeName);
            var hasIReadOnlySetGeneric = typeSymbol.Name == readOnlySetGenericTypeName || interfaceNames.Contains(readOnlySetGenericTypeName);
            var hasIDictionary = typeSymbol.Name == dictionaryTypeName || interfaceNames.Contains(dictionaryTypeName);
            var hasIDictionaryGeneric = typeSymbol.Name == dictionaryGenericTypeName || interfaceNames.Contains(dictionaryGenericTypeName);
            var hasIReadOnlyDictionaryGeneric = typeSymbol.Name == readOnlyDictionaryGenericTypeName || interfaceNames.Contains(readOnlyDictionaryGenericTypeName);

            var isIEnumerable = typeSymbol.Name == enumberableTypeName;
            var isIEnumerableGeneric = typeSymbol.Name == enumberableGenericTypeName;
            var isICollection = typeSymbol.Name == collectionTypeName;
            var isICollectionGeneric = typeSymbol.Name == collectionGenericTypeName;
            var isIReadOnlyCollectionGeneric = typeSymbol.Name == readOnlyCollectionGenericTypeName;
            var isIList = typeSymbol.Name == listTypeName;
            var isIListGeneric = typeSymbol.Name == listGenericTypeName;
            var isIReadOnlyListGeneric = typeSymbol.Name == readOnlyListTypeName;
            var isISetGeneric = typeSymbol.Name == setGenericTypeName;
            var isIReadOnlySetGeneric = typeSymbol.Name == readOnlySetGenericTypeName;
            var isIDictionary = typeSymbol.Name == dictionaryTypeName;
            var isIDictionaryGeneric = typeSymbol.Name == dictionaryGenericTypeName;
            var isIReadOnlyDictionaryGeneric = typeSymbol.Name == readOnlyDictionaryGenericTypeName;

            var hasCreator = typeSymbol.Constructors.Any(x => !x.IsStatic && x.Parameters.Length == 0);

            var isNullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;

            CoreType? coreType = null;
            if (TypeLookup.CoreTypeLookup(typeSymbol.Name, out var coreTypeParsed))
                coreType = coreTypeParsed;

            SpecialType? specialType = null;
            if (TypeLookup.SpecialTypeLookup(typeSymbol.Name, out var specialTypeParsed))
                specialType = specialTypeParsed;

            CoreEnumType? enumType = null;
            if (typeSymbol.EnumUnderlyingType is not null && TypeLookup.CoreEnumTypeLookup(typeSymbol.EnumUnderlyingType.Name, out var enumTypeParsed))
                enumType = enumTypeParsed;

            string? innerType;
            if (typeSymbol.TypeKind == TypeKind.Array)
                innerType = ((IArrayTypeSymbol)typeSymbol).ElementType.ToString();
            else
                innerType = typeSymbol.TypeArguments.Length == 1 ? typeSymbol.TypeArguments[0].ToString() : null;

            var isTask = specialType == SpecialType.Task;

            string? iEnumerableInnerType = null;
            if (isIEnumerableGeneric || typeSymbol.TypeKind == TypeKind.Array)
            {
                iEnumerableInnerType = innerType;
            }
            else if (hasIEnumerableGeneric)
            {
                var interfaceSymbol = typeSymbol.AllInterfaces.FirstOrDefault(x => x.Name == enumberableGenericTypeName);
                if (interfaceSymbol != null)
                {
                    iEnumerableInnerType = interfaceSymbol.TypeParameters[0].ToString();
                }
            }

            var baseTypes = GenerateBaseTypes(typeSymbol);

            var innerTypes = GenerateInnerTypes(typeSymbol);

            var attributes = GenerateAttributes(typeSymbol);

            var interfaces = GenerateInterfaces(typeSymbol);

            var sbChildClasses = new StringBuilder();

            var constructors = GenerateConstructors(context, typeSymbol, allTypeSymbol, classList, recursive, sbChildClasses);

            var methods = GenerateMethods(context, typeSymbol, allTypeSymbol, classList, recursive, sbChildClasses);

            var members = GenerateMembers(context, typeSymbol, allTypeSymbol, classList, recursive, sbChildClasses);

            var childClasses = sbChildClasses.ToString();

            var code = $$"""
                //Zerra Generated File

                using System;
                using System.Collections.Generic;
                using Zerra.Reflection;
                using Zerra.Reflection.Generation;
                {{(ns == null ? null : $"using {ns};")}}

                namespace {{(ns == null ? null : $"{ns}.")}}SourceGeneration
                {
                    public sealed class {{className}} : TypeDetailTGenerationBase<{{fullTypeName}}>{{typeConstraints}}
                    {               
                        public override bool HasIEnumerable => {{(hasIEnumerable ? "true" : "false")}};
                        public override bool HasIEnumerableGeneric => {{(hasIEnumerableGeneric ? "true" : "false")}};
                        public override bool HasICollection => {{(hasICollection ? "true" : "false")}};
                        public override bool HasICollectionGeneric => {{(hasICollectionGeneric ? "true" : "false")}};
                        public override bool HasIReadOnlyCollectionGeneric => {{(hasIReadOnlyCollectionGeneric ? "true" : "false")}};
                        public override bool HasIList => {{(hasIList ? "true" : "false")}};
                        public override bool HasIListGeneric => {{(hasIListGeneric ? "true" : "false")}};
                        public override bool HasIReadOnlyListGeneric => {{(hasIReadOnlyListGeneric ? "true" : "false")}};
                        public override bool HasISetGeneric => {{(hasISetGeneric ? "true" : "false")}};
                        public override bool HasIReadOnlySetGeneric => {{(hasIReadOnlySetGeneric ? "true" : "false")}};
                        public override bool HasIDictionary => {{(hasIDictionary ? "true" : "false")}};
                        public override bool HasIDictionaryGeneric => {{(hasIDictionaryGeneric ? "true" : "false")}};
                        public override bool HasIReadOnlyDictionaryGeneric => {{(hasIReadOnlyDictionaryGeneric ? "true" : "false")}};
                
                        public override bool IsIEnumerable => {{(isIEnumerable ? "true" : "false")}};
                        public override bool IsIEnumerableGeneric => {{(isIEnumerableGeneric ? "true" : "false")}};
                        public override bool IsICollection => {{(isICollection ? "true" : "false")}};
                        public override bool IsICollectionGeneric => {{(isICollectionGeneric ? "true" : "false")}};
                        public override bool IsIReadOnlyCollectionGeneric => {{(isIReadOnlyCollectionGeneric ? "true" : "false")}};
                        public override bool IsIList => {{(isIList ? "true" : "false")}};
                        public override bool IsIListGeneric => {{(isIListGeneric ? "true" : "false")}};
                        public override bool IsIReadOnlyListGeneric => {{(isIReadOnlyListGeneric ? "true" : "false")}};
                        public override bool IsISetGeneric => {{(isISetGeneric ? "true" : "false")}};
                        public override bool IsIReadOnlySetGeneric => {{(isIReadOnlySetGeneric ? "true" : "false")}};
                        public override bool IsIDictionary => {{(isIDictionary ? "true" : "false")}};
                        public override bool IsIDictionaryGeneric => {{(isIDictionaryGeneric ? "true" : "false")}};
                        public override bool IsIReadOnlyDictionaryGeneric => {{(isIReadOnlyDictionaryGeneric ? "true" : "false")}};

                        public override Func<{{fullTypeName}}> Creator => () => {{(hasCreator ? $"new {fullTypeName}()" : "throw new NotSupportedException()")}};
                        public override bool HasCreator => {{(hasCreator ? "true" : "false")}};

                        public override bool IsNullable => {{(isNullable ? "true" : "false")}};

                        public override CoreType? CoreType => {{(coreType.HasValue ? "Zerra.Reflection.CoreType." + coreType.Value.ToString() : "null")}};
                        public override SpecialType? SpecialType => {{(specialType.HasValue ? "Zerra.Reflection.SpecialType." + specialType.Value.ToString() : "null")}};
                        public override CoreEnumType? EnumUnderlyingType => {{(enumType.HasValue ? "Zerra.Reflection.CoreEnumType." + enumType.Value.ToString() : "null")}};

                        private readonly Type? innerType = {{(innerType is null ? "null" : $"typeof({innerType})")}};
                        public override Type InnerType => innerType ?? throw new NotSupportedException();

                        public override bool IsTask => {{(isTask ? "true" : "false")}};

                        public override IReadOnlyList<Type> BaseTypes => {{baseTypes}};

                        private readonly Type[] interfaces = {{interfaces}};
                        public override IReadOnlyList<Type> Interfaces => interfaces;

                        protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};

                        private readonly Type[] innerTypes = {{innerTypes}};
                        public override IReadOnlyList<Type> InnerTypes => innerTypes;

                        private readonly Type iEnumerableGenericInnerType = {{(iEnumerableInnerType is null ? "null" : $"typeof({iEnumerableInnerType})")}};
                        public override Type IEnumerableGenericInnerType => iEnumerableGenericInnerType ?? throw new NotSupportedException();

                        public override Func<object, object?> TaskResultGetter => (obj) => {{(isTask ? "((System.Threading.Tasks.Task)obj).Result" : "throw new NotSupportedException()")}};
                        public override bool HasTaskResultGetter => {{(isTask ? "true" : "false")}};

                        public override Func<object> CreatorBoxed => {{(hasCreator ? $"() => new {fullTypeName}()" : "throw new NotSupportedException()")}};
                        public override bool HasCreatorBoxed => {{(hasCreator ? "true" : "false")}};

                        protected override Func<MethodDetail<{{fullTypeName}}>[]> CreateMethodDetails => () => {{methods}};

                        protected override Func<ConstructorDetail<{{fullTypeName}}>[]> CreateConstructorDetails => () => {{constructors}};

                        protected override Func<MemberDetail[]> CreateMemberDetails => () => {{members}};

                {{childClasses}}
                    }
                }
                """;

            context.AddSource(fileName, SourceText.From(code, Encoding.UTF8));
        }

        private static string GenerateConstructors(SourceProductionContext context, INamedTypeSymbol typeSymbol, ImmutableArray<INamedTypeSymbol> allTypeSymbol, List<Tuple<string, string>> classList, bool recursive, StringBuilder sbChildClasses)
        {
            var symbolMembers = typeSymbol.GetMembers();
            var fullTypeName = typeSymbol.ToString();

            var membersToInitialize = new List<string>();

            var constructors = symbolMembers.Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Constructor).ToArray();
            var constructorNumber = 0;
            foreach (var constructor in constructors)
            {
                if (constructor.DeclaredAccessibility == Accessibility.Public)
                    GenerateConstructor(constructor, fullTypeName, ++constructorNumber, membersToInitialize, sbChildClasses);
                else
                    GeneratePrivateConstructor(constructor, fullTypeName, ++constructorNumber, membersToInitialize, sbChildClasses);

                if (recursive)
                {
                    foreach (var arg in constructor.Parameters)
                    {
                        if (arg.Type is INamedTypeSymbol argNamedType)
                        {
                            if (ShouldGenerateType(argNamedType, allTypeSymbol))
                                GenerateType(context, argNamedType, allTypeSymbol, classList, false);
                        }
                    }
                }
            }

            var sbMembers = new StringBuilder();
            _ = sbMembers.Append('[');
            foreach (var memberName in membersToInitialize)
            {
                if (sbMembers.Length > 1)
                    _ = sbMembers.Append(", ");
                sbMembers.Append("new ").Append(memberName).Append("(locker, LoadConstructorInfo)");
            }
            _ = sbMembers.Append(']');
            var members = sbMembers.ToString();
            return members;
        }
        private static void GenerateConstructor(IMethodSymbol methodSymbol, string parentTypeName, int constructorNumber, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var memberName = methodSymbol.Name;
            var className = $"ConstructorDetail_{constructorNumber}";

            var hasCreator = methodSymbol.Parameters.Length == 0;
            var hasCreatorWithArgs = methodSymbol.Parameters.All(x => x.RefKind == RefKind.None && !x.Type.IsRefLikeType && x.Type.Kind != SymbolKind.PointerType);

            string? creatorWithArgs = null;
            if (hasCreatorWithArgs)
            {
                var sbCreatorWithArgs = new StringBuilder();
                _ = sbCreatorWithArgs.Append("new ").Append(parentTypeName).Append('(');
                var creatorIndex = 0;
                foreach (var parameter in methodSymbol.Parameters)
                {
                    if (creatorIndex > 0)
                        _ = sbCreatorWithArgs.Append(", ");
                    _ = sbCreatorWithArgs.Append('(').Append(parameter.Type.ToString()).Append(")args[").Append(creatorIndex++).Append("]");
                }
                _ = sbCreatorWithArgs.Append(')');
                creatorWithArgs = sbCreatorWithArgs.ToString();
            }

            var attributes = GenerateAttributes(methodSymbol);

            var parameters = GenerateParameters(methodSymbol, $"c{constructorNumber}", sbChildClasses);

            var code = $$""""

                        public sealed class {{className}} : ConstructorDetailGenerationBase<{{parentTypeName}}>
                        {
                            public {{className}}(object locker, Action loadConstructorInfo) : base(locker, loadConstructorInfo) { }

                            public override Func<object?[]?, {{parentTypeName}}> CreatorWithArgs => {{(hasCreatorWithArgs ? $"(args) => {creatorWithArgs}" : "throw new NotSupportedException()")}};
                            public override bool HasCreatorWithArgs => {{(hasCreatorWithArgs ? "true" : "false")}};

                            public override Func<{{parentTypeName}}> Creator => {{(hasCreator ? $"() => new {parentTypeName}()" : "throw new NotSupportedException()")}};
                            public override bool HasCreator => {{(hasCreator ? "true" : "false")}};

                            public override string Name => "{{memberName}}";

                            protected override Func<ParameterDetail[]> CreateParameterDetails => () => {{parameters}};

                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};

                            public override Func<object?[]?, object> CreatorWithArgsBoxed => {{(hasCreatorWithArgs ? $"(args) => {creatorWithArgs}" : "throw new NotSupportedException()")}};
                            public override bool HasCreatorWithArgsBoxed => {{(hasCreatorWithArgs ? "true" : "false")}};

                            public override Func<object> CreatorBoxed => {{(hasCreator ? $"() => new {parentTypeName}()" : "throw new NotSupportedException()")}};
                            public override bool HasCreatorBoxed => {{(hasCreator ? "true" : "false")}};
                        }
                """";

            membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }
        private static void GeneratePrivateConstructor(IMethodSymbol methodSymbol, string parentTypeName, int constructorNumber, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var memberName = methodSymbol.Name;
            var className = $"ConstructorDetail_{constructorNumber}";

            var attributes = GenerateAttributes(methodSymbol);

            var parameters = GenerateParameters(methodSymbol, $"c{constructorNumber}", sbChildClasses);

            var code = $$""""

                        public sealed class {{className}} : PrivateConstructorDetailGenerationBase<{{parentTypeName}}>
                        {
                            public {{className}}(object locker, Action loadConstructorInfo) : base(locker, loadConstructorInfo) { }

                            public override string Name => "{{memberName}}";

                            protected override Func<ParameterDetail[]> CreateParameterDetails => () => {{parameters}};

                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};
                        }
                """";

            membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }

        private static string GenerateMethods(SourceProductionContext context, INamedTypeSymbol typeSymbol, ImmutableArray<INamedTypeSymbol> allTypeSymbol, List<Tuple<string, string>> classList, bool recursive, StringBuilder sbChildClasses)
        {
            var symbolMembers = typeSymbol.GetMembers();
            var fullTypeName = typeSymbol.ToString();

            var membersToInitialize = new List<string>();

            var methods = symbolMembers.Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(x => !x.IsStatic && x.MethodKind == MethodKind.Ordinary).ToArray();
            var methodNumber = 0;
            foreach (var method in methods)
            {
                if (method.DeclaredAccessibility == Accessibility.Public)
                    GenerateMethod(method, fullTypeName, ++methodNumber, membersToInitialize, sbChildClasses);
                else
                    GeneratePrivateMethod(method, fullTypeName, ++methodNumber, membersToInitialize, sbChildClasses);

                if (recursive)
                {
                    if (method.ReturnType is INamedTypeSymbol namedType)
                    {
                        if (ShouldGenerateType(namedType, allTypeSymbol))
                            GenerateType(context, namedType, allTypeSymbol, classList, false);
                    }

                    foreach (var arg in method.Parameters)
                    {
                        if (arg.Type is INamedTypeSymbol argNamedType)
                        {
                            if (ShouldGenerateType(argNamedType, allTypeSymbol))
                                GenerateType(context, argNamedType, allTypeSymbol, classList, false);
                        }
                    }
                }
            }

            var sbMembers = new StringBuilder();
            _ = sbMembers.Append('[');
            foreach (var memberName in membersToInitialize)
            {
                if (sbMembers.Length > 1)
                    _ = sbMembers.Append(", ");
                sbMembers.Append("new ").Append(memberName).Append("(locker, LoadMemberInfo)");
            }
            _ = sbMembers.Append(']');
            var members = sbMembers.ToString();
            return members;
        }
        private static void GenerateMethod(IMethodSymbol methodSymbol, string parentTypeName, int methodNumber, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var memberName = methodSymbol.Name;
            var className = $"MethodDetail_{methodNumber}";

            var isVoid = methodSymbol.ReturnType.ToString() == "void";

            var hasCaller = methodSymbol.Parameters.All(x => x.RefKind == RefKind.None && !x.Type.IsRefLikeType && x.Type.Kind != SymbolKind.PointerType);
            string? caller = null;
            string? callerBoxed = null;
            if (hasCaller)
            {
                var sbCaller = new StringBuilder();
                var sbCallerBoxed = new StringBuilder();
                _ = sbCaller.Append("obj.").Append(memberName).Append('(');
                _ = sbCallerBoxed.Append("((").Append(parentTypeName).Append(')').Append("obj).").Append(memberName).Append('(');
                var creatorIndex = 0;
                foreach (var parameter in methodSymbol.Parameters)
                {
                    if (creatorIndex > 0)
                    {
                        _ = sbCaller.Append(", ");
                        _ = sbCallerBoxed.Append(", ");
                    }
                    _ = sbCaller.Append('(').Append(parameter.Type.ToString()).Append(")args[").Append(creatorIndex++).Append("]");
                    _ = sbCallerBoxed.Append('(').Append(parameter.Type.ToString()).Append(")args[").Append(creatorIndex++).Append("]");
                }
                _ = sbCaller.Append(')');
                _ = sbCallerBoxed.Append(')');
                caller = sbCaller.ToString();
                callerBoxed = sbCallerBoxed.ToString();
            }

            var attributes = GenerateAttributes(methodSymbol);

            var parameters = GenerateParameters(methodSymbol, $"m{methodNumber}", sbChildClasses);

            var code = $$""""

                        public sealed class {{className}} : MethodDetailGenerationBase<{{parentTypeName}}>
                        {
                            public {{className}}(object locker, Action loadMethodInfo) : base(locker, loadMethodInfo) { }

                            private Type? returnType = typeof({{methodSymbol.ReturnType.ToString()}});
                            public override Type ReturnType => returnType;

                            public override Func<{{parentTypeName}}, object?[]?, object?> Caller => {{(hasCaller ? $"(obj, args) => {(isVoid ? $"{{{caller};return null;}}" : caller)}" : "throw new NotSupportedException()")}};
                            public override bool HasCaller => {{(hasCaller ? "true" : "false")}};

                            public override string Name => "{{memberName}}";

                            protected override Func<ParameterDetail[]> CreateParameterDetails => () => {{parameters}};

                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};

                            public override Func<object?, object?[]?, object?> CallerBoxed => {{(hasCaller ? $"(obj, args) => {(isVoid ? $"{{{callerBoxed};return null;}}" : $"{callerBoxed}")}" : "throw new NotSupportedException()")}};
                            public override bool HasCallerBoxed => {{(hasCaller ? "true" : "false")}};
                        }
                """";

            membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }
        private static void GeneratePrivateMethod(IMethodSymbol methodSymbol, string parentTypeName, int methodNumber, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var memberName = methodSymbol.Name;
            var className = $"MethodDetail_{methodNumber}";

            var attributes = GenerateAttributes(methodSymbol);

            var parameters = GenerateParameters(methodSymbol, $"m{methodNumber}", sbChildClasses);

            var code = $$""""

                        public sealed class {{className}} : PrivateMethodDetailGenerationBase<{{parentTypeName}}>
                        {
                            public {{className}}(object locker, Action loadMethodInfo) : base(locker, loadMethodInfo) { }
                
                            private Type? returnType = typeof({{methodSymbol.ReturnType.ToString()}});
                            public override Type ReturnType => returnType;
                
                            public override string Name => "{{memberName}}";
                
                            protected override Func<ParameterDetail[]> CreateParameterDetails => () => {{parameters}};
                
                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};
                        }
                """";

            membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }

        private static string GenerateMembers(SourceProductionContext context, INamedTypeSymbol typeSymbol, ImmutableArray<INamedTypeSymbol> allTypeSymbol, List<Tuple<string, string>> classList, bool recursive, StringBuilder sbChildClasses)
        {
            var symbolMembers = typeSymbol.GetMembers();
            var fullTypeName = typeSymbol.ToString();

            var properties = symbolMembers.Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().ToArray();
            var fields = symbolMembers.Where(x => x.Kind == SymbolKind.Field).Cast<IFieldSymbol>().ToArray();

            var membersToInitialize = new List<string>();

#pragma warning disable RS1024 // Comparing instance so no need for special comparer
            var backingFields = new HashSet<IFieldSymbol>();
#pragma warning restore RS1024 // Comparing instance so no need for special comparer
            var memberNumber = 0;
            foreach (var property in properties)
            {
                if (property.DeclaredAccessibility == Accessibility.Public)
                    GeneratePublicMember(property, properties.Length, fields, backingFields, fullTypeName, ++memberNumber, membersToInitialize, sbChildClasses);
                else
                    GeneratePrivateMember(property, properties.Length, fields, backingFields, fullTypeName, ++memberNumber, membersToInitialize, sbChildClasses);

                if (recursive)
                {
                    if (property.Type is INamedTypeSymbol namedType)
                    {
                        if (ShouldGenerateType(namedType, allTypeSymbol))
                            GenerateType(context, namedType, allTypeSymbol, classList, false);
                    }
                }
            }
            foreach (var field in fields)
            {
                if (field.DeclaredAccessibility == Accessibility.Public)
                    GeneratePublicMember(field, fullTypeName, backingFields, ++memberNumber, membersToInitialize, sbChildClasses);
                else
                    GeneratePrivateMember(field, fullTypeName, backingFields, ++memberNumber, membersToInitialize, sbChildClasses);

                if (recursive)
                {
                    if (field.Type is INamedTypeSymbol namedType)
                    {
                        if (ShouldGenerateType(namedType, allTypeSymbol))
                            GenerateType(context, namedType, allTypeSymbol, classList, false);
                    }
                }
            }

            var sbMembers = new StringBuilder();
            _ = sbMembers.Append('[');
            foreach (var memberName in membersToInitialize)
            {
                if (sbMembers.Length > 1)
                    _ = sbMembers.Append(", ");
                sbMembers.Append("new ").Append(memberName).Append("(locker, LoadMemberInfo)");
            }
            _ = sbMembers.Append(']');
            var members = sbMembers.ToString();
            return members;
        }
        private static void GeneratePublicMember(IPropertySymbol propertySymbol, int propertyTotal, IFieldSymbol[] fieldSymbols, HashSet<IFieldSymbol> backingFields, string parentTypeName, int methodNumber, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var memberName = propertySymbol.Name;
            var className = $"MemberDetail_{methodNumber}";
            var typeName = propertySymbol.Type.ToString();

            //<{property.Name}>k__BackingField
            //<{property.Name}>i__Field
            var backingName = $"<{propertySymbol.Name}>";
            IFieldSymbol? fieldSymbol = null;
            var fieldMemberNumber = propertyTotal;
            foreach (var f in fieldSymbols)
            {
                fieldMemberNumber++;
                if (f.Name.StartsWith(backingName))
                {
                    fieldSymbol = f;
                    break;
                }
            }
            var isBacked = false;
            string? fieldClassName = null;
            if (fieldSymbol is not null)
            {
                backingFields.Add(fieldSymbol);
                isBacked = true;
                fieldClassName = $"MemberDetail_{fieldMemberNumber}";
            }

            var isStatic = propertySymbol.IsStatic;

            var hasGetter = propertySymbol.GetMethod is not null && !propertySymbol.IsIndexer;
            var hasSetter = propertySymbol.SetMethod is not null && !propertySymbol.IsIndexer;

            var attributes = GenerateAttributes(propertySymbol);

            var code = $$""""

                        public sealed class {{className}} : MemberDetailGenerationBase<{{parentTypeName}}, {{typeName}}>
                        {
                            public {{className}}(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

                            public override string Name => "{{memberName}}";

                            private readonly Type type = typeof({{typeName.Replace("?", "")}});
                            public override Type Type => type;

                            public override bool IsBacked => {{(isBacked ? "true" : "false")}};

                            public override Func<{{parentTypeName}}, {{typeName}}> Getter => (x) => {{(hasGetter ? $"{(isStatic ? propertySymbol.ContainingType.Name : "x")}.{memberName}" : "throw new NotSupportedException()")}};
                            public override bool HasGetter => {{(hasGetter ? "true" : "false")}};

                            public override Action<{{parentTypeName}}, {{typeName}}> Setter => {{(hasSetter ? $"(x, value) => {(isStatic ? propertySymbol.ContainingType.Name : "x")}.{memberName} = value" : "throw new NotSupportedException()")}};
                            public override bool HasSetter => {{(hasSetter ? "true" : "false")}};

                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};

                            public override Func<object, object?> GetterBoxed => (x) => {{(hasGetter ? $"{(isStatic ? propertySymbol.ContainingType.Name : $"(({parentTypeName})x)")}.{memberName}" : "throw new NotSupportedException()")}};
                            public override bool HasGetterBoxed => {{(hasGetter ? "true" : "false")}};

                            public override Action<object, object?> SetterBoxed => (x, value) => {{(hasSetter ? $"{(isStatic ? propertySymbol.ContainingType.Name : $"(({parentTypeName})x)")}.{memberName} = ({typeName})value!" : "throw new NotSupportedException()")}};
                            public override bool HasSetterBoxed => {{(hasSetter ? "true" : "false")}};

                            protected override Func<MemberDetail<{{parentTypeName}}, {{typeName}}>?> CreateBackingFieldDetail => {{(fieldClassName is null ? "() => null" : $"() => new {fieldClassName}(locker, loadMemberInfo)")}};
                        }
                """";

            membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }
        private static void GeneratePrivateMember(IPropertySymbol propertySymbol, int propertyTotal, IFieldSymbol[] fieldSymbols, HashSet<IFieldSymbol> backingFields, string parentTypeName, int methodNumber, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var memberName = propertySymbol.Name;
            var className = $"MemberDetail_{methodNumber}";
            var typeName = propertySymbol.Type.ToString();

            //<{property.Name}>k__BackingField
            //<{property.Name}>i__Field
            var backingName = $"<{propertySymbol.Name}>";
            IFieldSymbol? fieldSymbol = null;
            var fieldMemberNumber = propertyTotal;
            foreach (var f in fieldSymbols)
            {
                fieldMemberNumber++;
                if (f.Name.StartsWith(backingName))
                {
                    fieldSymbol = f;
                    break;
                }
            }
            var isBacked = false;
            string? fieldClassName = null;
            if (fieldSymbol is not null)
            {
                backingFields.Add(fieldSymbol);
                isBacked = true;
                fieldClassName = $"MemberDetail_{fieldMemberNumber}";
            }

            var attributes = GenerateAttributes(propertySymbol);

            var code = $$""""

                        public sealed class {{className}} : PrivateMemberDetailGenerationBase<{{parentTypeName}}, {{typeName}}>
                        {
                            public {{className}}(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

                            public override string Name => "{{memberName}}";

                            private readonly Type type = typeof({{typeName.Replace("?", "")}});
                            public override Type Type => type;

                            public override bool IsBacked => {{(isBacked ? "true" : "false")}};

                            public override Func<Attribute[]> CreateAttributes => () => {{attributes}};

                            protected override Func<MemberDetail<{{parentTypeName}}, {{typeName}}>?> CreateBackingFieldDetail => {{(fieldClassName is null ? "() => null" : $"() => new {fieldClassName}(locker, loadMemberInfo)")}};
                        }
                """";

            membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }
        private static void GeneratePublicMember(IFieldSymbol fieldSymbol, string parentTypeName, HashSet<IFieldSymbol> backingFields, int methodNumber, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var memberName = fieldSymbol.Name;
            var className = $"MemberDetail_{methodNumber}";
            var typeName = fieldSymbol.Type.ToString();

            var isStatic = fieldSymbol.IsStatic;

            var hasSetter = !fieldSymbol.IsReadOnly && !fieldSymbol.IsConst;

            var attributes = GenerateAttributes(fieldSymbol);

            var code = $$""""

                        public sealed class {{className}} : MemberDetailGenerationBase<{{parentTypeName}}, {{typeName}}>
                        {
                            public {{className}}(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

                            public override string Name => "{{memberName}}";

                            private readonly Type type = typeof({{typeName.Replace("?", "")}});
                            public override Type Type => type;

                            public override bool IsBacked => true;

                            public override Func<{{parentTypeName}}, {{typeName}}> Getter => (x) => {{(isStatic ? fieldSymbol.ContainingType.Name : "x")}}.{{memberName}};
                            public override bool HasGetter => true;

                            public override Action<{{parentTypeName}}, {{typeName}}> Setter => {{(hasSetter ? $"(x, value) => {(isStatic ? fieldSymbol.ContainingType.Name : "x")}.{memberName} = value" : "throw new NotSupportedException()")}};
                            public override bool HasSetter => {{(hasSetter ? "true" : "false")}};

                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};

                            public override Func<object, object?> GetterBoxed => (x) => {{(isStatic ? fieldSymbol.ContainingType.Name : $"(({parentTypeName})x)")}}.{{memberName}};
                            public override bool HasGetterBoxed => true;

                            public override Action<object, object?> SetterBoxed => (x, value) => {{(hasSetter ? $"{(isStatic ? fieldSymbol.ContainingType.Name : $"(({parentTypeName})x)")}.{memberName} = ({typeName})value!" : "throw new NotSupportedException()")}};
                            public override bool HasSetterBoxed => {{(hasSetter ? "true" : "false")}};

                            protected override Func<MemberDetail<{{parentTypeName}}, {{typeName}}>?> CreateBackingFieldDetail => () => null;
                        }
                """";

            if (!backingFields.Contains(fieldSymbol))
                membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }
        private static void GeneratePrivateMember(IFieldSymbol fieldSymbol, string parentTypeName, HashSet<IFieldSymbol> backingFields, int methodNumber, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var memberName = fieldSymbol.Name;
            var className = $"MemberDetail_{methodNumber}";
            var typeName = fieldSymbol.Type.ToString();

            var attributes = GenerateAttributes(fieldSymbol);

            var code = $$""""

                        public sealed class {{className}} : PrivateMemberDetailGenerationBase<{{parentTypeName}}, {{typeName}}>
                        {
                            public {{className}}(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

                            public override string Name => "{{memberName}}";

                            private readonly Type type = typeof({{typeName.Replace("?", "")}});
                            public override Type Type => type;

                            public override bool IsBacked => true;

                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};

                            protected override Func<MemberDetail<{{parentTypeName}}, {{typeName}}>?> CreateBackingFieldDetail => () => null;
                        }
                """";

            if (!backingFields.Contains(fieldSymbol))
                membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }

        private static string GenerateParameters(IMethodSymbol methodSymbol, string parentName, StringBuilder sbChildClasses)
        {
            var parametersToInitialize = new List<string>();
            var parameterNumber = 0;
            foreach (var parameter in methodSymbol.Parameters)
            {
                GenerateParameter(parameter, parentName, ++parameterNumber, parametersToInitialize, sbChildClasses);
            }

            var sbParameters = new StringBuilder();
            _ = sbParameters.Append('[');
            foreach (var parameterName in parametersToInitialize)
            {
                if (sbParameters.Length > 1)
                    _ = sbParameters.Append(", ");
                sbParameters.Append("new ").Append(parameterName).Append("(locker, LoadParameterInfo)");
            }
            _ = sbParameters.Append(']');
            var parameters = sbParameters.ToString();
            return parameters;
        }
        private static void GenerateParameter(IParameterSymbol parameterSymbol, string parentName, int parameterNumber, List<string> parametersToInitialize, StringBuilder sbChildClasses)
        {
            var parameterName = parameterSymbol.Name;
            var className = $"ParameterDetail_{parentName}_{parameterNumber}";
            var typeName = parameterSymbol.Type.ToString();

            var code = $$""""

                        public sealed class {{className}} : ParameterDetailGenerationBase
                        {
                            public {{className}}(object locker, Action loadParameterInfo) : base(locker, loadParameterInfo) { }

                            public override string Name => "{{parameterName}}";

                            private readonly Type type = typeof({{typeName.Replace("?", "")}});
                            public override Type Type => type;
                        }
                """";

            parametersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }

        private static string GenerateBaseTypes(INamedTypeSymbol typeSymbol)
        {
            var sbBaseTypes = new StringBuilder();
            _ = sbBaseTypes.Append('[');
            var baseTypeSymbol = typeSymbol.BaseType;
            while (baseTypeSymbol is not null)
            {
                if (sbBaseTypes.Length > 1)
                    _ = sbBaseTypes.Append(", ");
                GetTypeOf(baseTypeSymbol, sbBaseTypes);
                baseTypeSymbol = baseTypeSymbol.BaseType;
            }
            _ = sbBaseTypes.Append(']');
            var baseTypes = sbBaseTypes.ToString();
            return baseTypes;
        }
        private static string GenerateInterfaces(INamedTypeSymbol typeSymbol)
        {
            var sbInterfaceTypes = new StringBuilder();
            _ = sbInterfaceTypes.Append('[');
            foreach (var i in typeSymbol.AllInterfaces)
            {
                if (sbInterfaceTypes.Length > 1)
                    _ = sbInterfaceTypes.Append(", ");
                GetTypeOf(i, sbInterfaceTypes);
            }
            _ = sbInterfaceTypes.Append(']');
            var baseTypes = sbInterfaceTypes.ToString();
            return baseTypes;
        }
        private static string GenerateAttributes(ISymbol symbol)
        {
            var attributeSymbols = symbol.GetAttributes();
            var sbAttributes = new StringBuilder();
            _ = sbAttributes.Append('[');
            foreach (var attributeSymbol in attributeSymbols)
            {
                if (sbAttributes.Length > 1)
                    _ = sbAttributes.Append(", ");
                _ = sbAttributes.Append("new ").Append(attributeSymbol.AttributeClass.ToString()).Append('(');
                foreach (var arg in attributeSymbol.NamedArguments)
                {
                    _ = sbAttributes.Append(arg.Key).Append(": ");
                    TypedConstantToString(arg.Value, sbAttributes);
                }
                foreach (var arg in attributeSymbol.ConstructorArguments)
                {
                    TypedConstantToString(arg, sbAttributes);
                }
                _ = sbAttributes.Append(')');
            }
            _ = sbAttributes.Append(']');
            var attributes = sbAttributes.ToString();
            return attributes;
        }
        private static string GenerateInnerTypes(INamedTypeSymbol typeSymbol)
        {
            var sbInnerTypes = new StringBuilder();
            _ = sbInnerTypes.Append('[');
            foreach (var type in typeSymbol.TypeParameters)
            {
                if (sbInnerTypes.Length > 1)
                    _ = sbInnerTypes.Append(", ");
                _ = sbInnerTypes.Append($"typeof(").Append(type.ToString()).Append(')');
            }
            _ = sbInnerTypes.Append(']');
            var attributes = sbInnerTypes.ToString();
            return attributes;
        }

        private static void TypedConstantToString(TypedConstant constant, StringBuilder sb)
        {
            switch (constant.Kind)
            {
                case TypedConstantKind.Primitive:
                    if (constant.Type.Name == "String")
                        _ = sb.Append("\"").Append(constant.Value?.ToString()).Append("\"");
                    else if (constant.Type.Name == "Boolean")
                        _ = sb.Append((bool)constant.Value ? "true" : "false");
                    else
                        _ = sb.Append(constant.Value?.ToString() ?? "null");
                    break;
                case TypedConstantKind.Enum:
                    _ = sb.Append('(').Append(constant.Type.ToString()).Append(')').Append(constant.Value.ToString());
                    break;
                case TypedConstantKind.Type:
                    //guessing
                    _ = sb.Append("typeof(").Append(constant.Value.ToString()).Append(')');
                    break;
                case TypedConstantKind.Array:
                    var pastFirstValue = false;
                    foreach (var value in constant.Values)
                    {
                        if (pastFirstValue)
                            sb.Append(", ");
                        else
                            pastFirstValue = true;
                        TypedConstantToString(value, sb);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static void GetTypeOf(INamedTypeSymbol type, StringBuilder sb)
        {
            var genericArgsCount = type.TypeArguments.Length;
            if (genericArgsCount > 0)
            {
                _ = sb.Append($"typeof({type.ContainingNamespace}.{type.Name}<");
                for (var i = 1; i < genericArgsCount; i++)
                    _ = sb.Append(',');
                _ = sb.Append(">)");
            }
            else
            {
                _ = sb.Append($"typeof({type.ContainingNamespace}.{type.Name})");
            }
        }
    }
}
