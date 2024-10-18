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
using System.Text;

namespace Zerra.SourceGeneration
{
    public static class TypeDetailGenerator
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
                var fullTypeOf = item.Item1;
                var fullClassName = item.Item2;
                if (sb.Length > 0)
                    sb.Append(Environment.NewLine).Append("            ");
                sb.Append("TypeAnalyzer.AddTypeDetailCreator(").Append(fullTypeOf).Append(", () => new global::").Append(fullClassName).Append("());");
            }
            var lines = sb.ToString();

            var code = $$"""
                //Zerra Generated File
                #if NET5_0_OR_GREATER

                using System;
                using Zerra.Reflection;

                namespace {{compilation.AssemblyName}}.SourceGeneration
                {
                    internal static class TypeDetailInitializer
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

            context.AddSource("TypeDetailInitializer.cs", SourceText.From(code, Encoding.UTF8));
        }

        private static bool ShouldGenerateType(ITypeSymbol typeSymbol, IReadOnlyCollection<ITypeSymbol> allTypeSymbol)
        {
            //We only want types in this build or generics that contain the types in this build.

            if (typeSymbol.Name == "Void")
                return false;

            if (allTypeSymbol.Contains(typeSymbol, SymbolEqualityComparer.Default))
                return true;

            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.TypeParameters.Any(x => allTypeSymbol.Contains(x, SymbolEqualityComparer.Default)))
                    return true;
            }
            else if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                if (allTypeSymbol.Contains(arrayTypeSymbol.ElementType, SymbolEqualityComparer.Default))
                    return true;
            }

            return false;
        }

        public static void GenerateType(SourceProductionContext context, ITypeSymbol typeSymbol, IReadOnlyCollection<ITypeSymbol> allTypeSymbol, List<Tuple<string, string>> classList, bool recursive, bool publicAndImplicitOnly)
        {
            var namespaceRecursionCheck = typeSymbol;
            while (namespaceRecursionCheck is not null)
            {
                if (namespaceRecursionCheck.ContainingNamespace is not null && namespaceRecursionCheck.ContainingNamespace.ToString().StartsWith("Zerra.Reflection"))
                    return;
                namespaceRecursionCheck = namespaceRecursionCheck.BaseType;
            }

            if (typeSymbol.GetAttributes().Any(x => x.AttributeClass?.Name == "ObsoleteAttribute"))
                return;

            var ns = typeSymbol.ContainingNamespace is null || typeSymbol.ContainingNamespace.ToString().Contains("<global namespace>") ? null : typeSymbol.ContainingNamespace.ToString();

            var fullTypeName = Helpers.GetFullName(typeSymbol);

            var typeNameForClass = Helpers.GetNameForClass(typeSymbol);
            var className = $"{typeNameForClass}TypeDetail";
            var fileName = ns is null ? $"{typeNameForClass}TypeDetail.cs" : $"{ns}.{typeNameForClass}TypeDetail.cs";

            var fullClassName = ns is null ? $"SourceGeneration.{className}" : $"{ns}.SourceGeneration.{className}";
            var classListItem = new Tuple<string, string>(Helpers.GetTypeOfName(typeSymbol), fullClassName);
            if (classList.Contains(classListItem))
                return;
            classList.Add(classListItem);

            var namedTypeSymbol = typeSymbol as INamedTypeSymbol;

            string? typeConstraints = null;
            if (namedTypeSymbol is not null)
            {
                if (namedTypeSymbol.TypeArguments.Length > 0)
                {
                    var sbConstraints = new StringBuilder();
                    for (var i = 0; i < namedTypeSymbol.TypeArguments.Length; i++)
                    {
                        var typeArgument = namedTypeSymbol.TypeArguments[i];
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
            }

            var isArray = typeSymbol.Kind == SymbolKind.ArrayType;
            var interfaceNames = typeSymbol.AllInterfaces.Select(x => x.MetadataName).ToImmutableHashSet();

            var hasIEnumerable = isArray || typeSymbol.MetadataName == enumberableTypeName || interfaceNames.Contains(enumberableTypeName);
            var hasIEnumerableGeneric = isArray || typeSymbol.MetadataName == enumberableGenericTypeName || interfaceNames.Contains(enumberableGenericTypeName);
            var hasICollection = typeSymbol.MetadataName == collectionTypeName || interfaceNames.Contains(collectionTypeName);
            var hasICollectionGeneric = typeSymbol.MetadataName == collectionGenericTypeName || interfaceNames.Contains(collectionGenericTypeName);
            var hasIReadOnlyCollectionGeneric = typeSymbol.MetadataName == readOnlyCollectionGenericTypeName || interfaceNames.Contains(readOnlyCollectionGenericTypeName);
            var hasIList = typeSymbol.MetadataName == listTypeName || interfaceNames.Contains(listTypeName);
            var hasIListGeneric = typeSymbol.MetadataName == listGenericTypeName || interfaceNames.Contains(listGenericTypeName);
            var hasIReadOnlyListGeneric = typeSymbol.MetadataName == readOnlyListTypeName || interfaceNames.Contains(readOnlyListTypeName);
            var hasISetGeneric = typeSymbol.MetadataName == setGenericTypeName || interfaceNames.Contains(setGenericTypeName);
            var hasIReadOnlySetGeneric = typeSymbol.MetadataName == readOnlySetGenericTypeName || interfaceNames.Contains(readOnlySetGenericTypeName);
            var hasIDictionary = typeSymbol.MetadataName == dictionaryTypeName || interfaceNames.Contains(dictionaryTypeName);
            var hasIDictionaryGeneric = typeSymbol.MetadataName == dictionaryGenericTypeName || interfaceNames.Contains(dictionaryGenericTypeName);
            var hasIReadOnlyDictionaryGeneric = typeSymbol.MetadataName == readOnlyDictionaryGenericTypeName || interfaceNames.Contains(readOnlyDictionaryGenericTypeName);

            var isIEnumerable = typeSymbol.MetadataName == enumberableTypeName;
            var isIEnumerableGeneric = typeSymbol.MetadataName == enumberableGenericTypeName;
            var isICollection = typeSymbol.MetadataName == collectionTypeName;
            var isICollectionGeneric = typeSymbol.MetadataName == collectionGenericTypeName;
            var isIReadOnlyCollectionGeneric = typeSymbol.MetadataName == readOnlyCollectionGenericTypeName;
            var isIList = typeSymbol.MetadataName == listTypeName;
            var isIListGeneric = typeSymbol.MetadataName == listGenericTypeName;
            var isIReadOnlyListGeneric = typeSymbol.MetadataName == readOnlyListTypeName;
            var isISetGeneric = typeSymbol.MetadataName == setGenericTypeName;
            var isIReadOnlySetGeneric = typeSymbol.MetadataName == readOnlySetGenericTypeName;
            var isIDictionary = typeSymbol.MetadataName == dictionaryTypeName;
            var isIDictionaryGeneric = typeSymbol.MetadataName == dictionaryGenericTypeName;
            var isIReadOnlyDictionaryGeneric = typeSymbol.MetadataName == readOnlyDictionaryGenericTypeName;

            var hasCreator = false;
            if (namedTypeSymbol is not null)
                hasCreator = namedTypeSymbol.Constructors.Any(x => !x.IsStatic && x.Parameters.Length == 0);

            var isNullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;

            CoreType? coreType = null;
            if (TypeLookup.CoreTypeLookup(fullTypeName.Split('.').Last(), out var coreTypeParsed))
                coreType = coreTypeParsed;

            SpecialType? specialType = null;
            if (TypeLookup.SpecialTypeLookup(fullTypeName.Split('.').Last(), out var specialTypeParsed))
                specialType = specialTypeParsed;

            CoreEnumType? enumType = null;
            if (namedTypeSymbol is not null)
            {
                if (namedTypeSymbol.EnumUnderlyingType is not null && TypeLookup.CoreEnumTypeLookup(namedTypeSymbol.EnumUnderlyingType.Name, out var enumTypeParsed))
                    enumType = enumTypeParsed;
            }

            string? innerTypeOf = "null";
            if (namedTypeSymbol is not null)
            {
                if (namedTypeSymbol.TypeArguments.Length == 1)
                    innerTypeOf = Helpers.GetTypeOfName(namedTypeSymbol.TypeArguments[0]);
            }
            else if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                innerTypeOf = Helpers.GetTypeOfName(arrayTypeSymbol.ElementType);
            }

            var isTask = specialType == SpecialType.Task;

            string? iEnumerableInnerTypeOf = "null";
            if (isIEnumerableGeneric || typeSymbol.TypeKind == TypeKind.Array)
            {
                iEnumerableInnerTypeOf = innerTypeOf;
            }
            else if (hasIEnumerableGeneric)
            {
                var interfaceSymbol = typeSymbol.AllInterfaces.FirstOrDefault(x => x.MetadataName == enumberableGenericTypeName);
                if (interfaceSymbol is not null)
                    iEnumerableInnerTypeOf = Helpers.GetTypeOfName(interfaceSymbol.TypeArguments[0]);
            }

            var baseTypes = GenerateBaseTypes(typeSymbol);

            var innerTypes = GenerateInnerTypes(typeSymbol);

            var attributes = GenerateAttributes(typeSymbol);

            var interfaces = GenerateInterfaces(typeSymbol);

            var sbChildClasses = new StringBuilder();

            var constructors = GenerateConstructors(context, typeSymbol, allTypeSymbol, classList, recursive, publicAndImplicitOnly, sbChildClasses);

            var methods = GenerateMethods(context, typeSymbol, allTypeSymbol, classList, recursive, publicAndImplicitOnly, sbChildClasses);

            var members = GenerateMembers(context, typeSymbol, allTypeSymbol, classList, recursive, publicAndImplicitOnly, sbChildClasses);

            var childClasses = sbChildClasses.ToString();

            var canConstruct = typeSymbol.Kind == SymbolKind.ArrayType || (namedTypeSymbol is not null && !namedTypeSymbol.IsStatic && Helpers.IsGenericDefined(namedTypeSymbol));

            string code;
            if (canConstruct)
            {
                code = $$"""
                //Zerra Generated File

                #pragma warning disable CS0105 // Using directive appeared previously in this namespace
                #pragma warning disable CS8609 // Nullability of reference types in return type doesn't match overridden member.

                using System;
                using System.Collections.Generic;
                using Zerra.Reflection;
                using Zerra.Reflection.Compiletime;
                {{(ns is null ? null : $"using {ns};")}}

                namespace {{(ns is null ? null : $"{ns}.")}}SourceGeneration
                {
                    public sealed class {{className}} : TypeDetailCompiletimeBase<{{fullTypeName}}>{{typeConstraints}}
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

                        public override Func<{{fullTypeName}}> Creator => () => {{(hasCreator ? $"new {fullTypeName}()!" : "throw new NotSupportedException()")}};
                        public override bool HasCreator => {{(hasCreator ? "true" : "false")}};

                        public override bool IsNullable => {{(isNullable ? "true" : "false")}};

                        public override CoreType? CoreType => {{(coreType.HasValue ? "Zerra.Reflection.CoreType." + coreType.Value.ToString() : "null")}};
                        public override SpecialType? SpecialType => {{(specialType.HasValue ? "Zerra.Reflection.SpecialType." + specialType.Value.ToString() : "null")}};
                        public override CoreEnumType? EnumUnderlyingType => {{(enumType.HasValue ? "Zerra.Reflection.CoreEnumType." + enumType.Value.ToString() : "null")}};

                        private readonly Type? innerType = {{innerTypeOf}};
                        public override Type InnerType => innerType!;

                        public override bool IsTask => {{(isTask ? "true" : "false")}};

                        public override IReadOnlyList<Type> BaseTypes => {{baseTypes}};

                        private readonly Type[] interfaces = {{interfaces}};
                        public override IReadOnlyList<Type> Interfaces => interfaces;

                        protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};

                        private readonly Type[] innerTypes = {{innerTypes}};
                        public override IReadOnlyList<Type> InnerTypes => innerTypes;

                        private readonly Type? iEnumerableGenericInnerType = {{iEnumerableInnerTypeOf}};
                        public override Type IEnumerableGenericInnerType => iEnumerableGenericInnerType!;

                        public override Func<object, object?> TaskResultGetter => (obj) => {{(isTask ? "((System.Threading.Tasks.Task)obj).Result" : "throw new NotSupportedException()")}};
                        public override bool HasTaskResultGetter => {{(isTask ? "true" : "false")}};

                        public override Func<object> CreatorBoxed => {{(hasCreator ? $"() => new {fullTypeName}()!" : "throw new NotSupportedException()")}};
                        public override bool HasCreatorBoxed => {{(hasCreator ? "true" : "false")}};

                        protected override Func<MethodDetail<{{fullTypeName}}>[]> CreateMethodDetails => () => {{methods}};

                        protected override Func<ConstructorDetail<{{fullTypeName}}>[]> CreateConstructorDetails => () => {{constructors}};

                        protected override Func<MemberDetail[]> CreateMemberDetails => () => {{members}};

                {{childClasses}}
                    }
                }
                """;
            }
            else
            {
                code = $$"""
                //Zerra Generated File

                #pragma warning disable CS0105 // Using directive appeared previously in this namespace
                #pragma warning disable CS8609 // Nullability of reference types in return type doesn't match overridden member.

                using System;
                using System.Collections.Generic;
                using Zerra.Reflection;
                using Zerra.Reflection.Compiletime;
                {{(ns is null ? null : $"using {ns};")}}

                namespace {{(ns is null ? null : $"{ns}.")}}SourceGeneration
                {
                    public sealed class {{className}} : TypeDetailCompiletimeBase
                    {   
                        public {{className}}() : base(typeof({{fullTypeName}})) { }

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

                        public override bool IsNullable => {{(isNullable ? "true" : "false")}};

                        public override CoreType? CoreType => {{(coreType.HasValue ? "Zerra.Reflection.CoreType." + coreType.Value.ToString() : "null")}};
                        public override SpecialType? SpecialType => {{(specialType.HasValue ? "Zerra.Reflection.SpecialType." + specialType.Value.ToString() : "null")}};
                        public override CoreEnumType? EnumUnderlyingType => {{(enumType.HasValue ? "Zerra.Reflection.CoreEnumType." + enumType.Value.ToString() : "null")}};

                        private readonly Type? innerType = {{innerTypeOf}};
                        public override Type InnerType => innerType!;

                        public override bool IsTask => {{(isTask ? "true" : "false")}};

                        private readonly Type[] baseTypes = {{baseTypes}};
                        public override IReadOnlyList<Type> BaseTypes => baseTypes;

                        private readonly Type[] interfaces = {{interfaces}};
                        public override IReadOnlyList<Type> Interfaces => interfaces;

                        protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};

                        private readonly Type[] innerTypes = {{innerTypes}};
                        public override IReadOnlyList<Type> InnerTypes => innerTypes;

                        private readonly Type? iEnumerableGenericInnerType = {{iEnumerableInnerTypeOf}};
                        public override Type IEnumerableGenericInnerType => iEnumerableGenericInnerType!;

                        public override Func<object, object?> TaskResultGetter => (obj) => {{(isTask ? "((System.Threading.Tasks.Task)obj).Result" : "throw new NotSupportedException()")}};
                        public override bool HasTaskResultGetter => {{(isTask ? "true" : "false")}};

                        protected override Func<MethodDetail[]> CreateMethodDetails => () => {{methods}};

                        protected override Func<ConstructorDetail[]> CreateConstructorDetails => () => [];

                        protected override Func<MemberDetail[]> CreateMemberDetails => () => {{members}};

                {{childClasses}}
                    }
                }
                """;
            }


            context.AddSource(fileName, SourceText.From(code, Encoding.UTF8));
        }

        private static string GenerateConstructors(SourceProductionContext context, ITypeSymbol typeSymbol, IReadOnlyCollection<ITypeSymbol> allTypeSymbol, List<Tuple<string, string>> classList, bool recursive, bool publicAndImplicitOnly, StringBuilder sbChildClasses)
        {
            var symbolMembers = typeSymbol.GetMembers();
            var fullTypeName = Helpers.GetFullName(typeSymbol);

            var membersToInitialize = new List<string>();

            var constructors = symbolMembers.Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Constructor).ToArray();
            var constructorNumber = 0;
            foreach (var constructor in constructors)
            {
                if (constructor.GetAttributes().Any(x => x.AttributeClass?.Name == "ObsoleteAttribute"))
                    continue;

                var isPublic = constructor.DeclaredAccessibility == Accessibility.Public;
                if (publicAndImplicitOnly && !isPublic)
                    continue;

                if (isPublic)
                    GeneratePublicConstructor(constructor, fullTypeName, ++constructorNumber, membersToInitialize, sbChildClasses);
                else
                    GeneratePrivateConstructor(constructor, fullTypeName, ++constructorNumber, membersToInitialize, sbChildClasses);

                if (recursive)
                {
                    foreach (var arg in constructor.Parameters)
                    {
                        if (ShouldGenerateType(arg.Type, allTypeSymbol))
                            GenerateType(context, arg.Type, allTypeSymbol, classList, false, publicAndImplicitOnly);
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
        private static void GeneratePublicConstructor(IMethodSymbol methodSymbol, string parentTypeName, int constructorNumber, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var memberName = methodSymbol.Name;
            var className = $"ConstructorDetail_{constructorNumber}";

            var hasCreator = methodSymbol.Parameters.Length == 0;
            var hasCreatorWithArgs = methodSymbol.Parameters.All(x => !x.Type.IsRefLikeType && x.Type.Kind != SymbolKind.PointerType);

            string? creatorWithArgs = null;
            if (hasCreatorWithArgs)
            {
                var sbCreatorWithArgs = new StringBuilder();
                _ = sbCreatorWithArgs.Append("new ").Append(parentTypeName).Append('(');
                for (var i = 0; i < methodSymbol.Parameters.Length; i++)
                {
                    var parameter = methodSymbol.Parameters[i];
                    if (i > 0)
                        _ = sbCreatorWithArgs.Append(", ");
                    //if (parameter.RefKind == RefKind.Out)
                    //    _ = sbCreatorWithArgs.Append("out ");
                    //else if (parameter.RefKind == RefKind.In)
                    //    _ = sbCreatorWithArgs.Append("in ");
                    //else if (parameter.RefKind == RefKind.Ref)
                    //    _ = sbCreatorWithArgs.Append("ref ");
                    _ = sbCreatorWithArgs.Append('(').Append(parameter.Type.ToString()).Append(")args![").Append(i).Append("]!");
                }
                _ = sbCreatorWithArgs.Append(')');
                creatorWithArgs = sbCreatorWithArgs.ToString();
            }

            var attributes = GenerateAttributes(methodSymbol);

            var parameters = GenerateParameters(methodSymbol, $"c{constructorNumber}", sbChildClasses);

            var code = $$""""

                        public sealed class {{className}} : ConstructorDetailCompiletimeBase<{{parentTypeName}}>
                        {
                            public {{className}}(object locker, Action loadConstructorInfo) : base(locker, loadConstructorInfo) { }

                            public override Func<object?[]?, {{parentTypeName}}> CreatorWithArgs => {{(hasCreatorWithArgs ? $"(args) => {creatorWithArgs}" : "throw new NotSupportedException()")}};
                            public override bool HasCreatorWithArgs => {{(hasCreatorWithArgs ? "true" : "false")}};

                            public override Func<{{parentTypeName}}> Creator => {{(hasCreator ? $"() => new {parentTypeName}()!" : "throw new NotSupportedException()")}};
                            public override bool HasCreator => {{(hasCreator ? "true" : "false")}};

                            public override string Name => "{{memberName}}";

                            protected override Func<ParameterDetail[]> CreateParameterDetails => () => {{parameters}};

                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};

                            public override Func<object?[]?, object> CreatorWithArgsBoxed => {{(hasCreatorWithArgs ? $"(args) => {creatorWithArgs}!" : "throw new NotSupportedException()")}};
                            public override bool HasCreatorWithArgsBoxed => {{(hasCreatorWithArgs ? "true" : "false")}};

                            public override Func<object> CreatorBoxed => {{(hasCreator ? $"() => new {parentTypeName}()!" : "throw new NotSupportedException()")}};
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

            var code = $$""""

                        public sealed class {{className}} : PrivateConstructorDetailCompiletimeBase<{{parentTypeName}}>
                        {
                            public {{className}}(object locker, Action loadConstructorInfo) : base(locker, loadConstructorInfo) { }

                            public override string Name => "{{memberName}}";

                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};
                        }
                """";

            membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }

        private static string GenerateMethods(SourceProductionContext context, ITypeSymbol typeSymbol, IReadOnlyCollection<ITypeSymbol> allTypeSymbol, List<Tuple<string, string>> classList, bool recursive, bool publicAndImplicitOnly, StringBuilder sbChildClasses)
        {
            var symbolMembers = typeSymbol.GetMembers();
            var fullTypeName = Helpers.GetFullName(typeSymbol);

            var membersToInitialize = new List<string>();

            var methods = symbolMembers.Where(x => x.Kind == SymbolKind.Method).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Ordinary || x.MethodKind == MethodKind.Destructor || x.MethodKind == MethodKind.PropertyGet || x.MethodKind == MethodKind.PropertySet || x.MethodKind == MethodKind.ExplicitInterfaceImplementation).Select(x => new Tuple<IMethodSymbol, string>(x, x.Name)).ToList();

            if (!publicAndImplicitOnly && typeSymbol.AllInterfaces.Length > 0)
            {
                var names = new HashSet<string>();
                foreach (var method in methods)
                    names.Add(method.Item1.Name);

                foreach (var i in typeSymbol.AllInterfaces)
                {
                    var iMethods = i.GetMembers().Where(x => x.Kind == SymbolKind.Method && !x.IsStatic).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Ordinary || x.MethodKind == MethodKind.Destructor || x.MethodKind == MethodKind.PropertyGet || x.MethodKind == MethodKind.PropertySet || x.MethodKind == MethodKind.ExplicitInterfaceImplementation).ToArray();
                    foreach (var iMethod in iMethods)
                    {
                        string name;
                        if (typeSymbol.TypeKind == TypeKind.Interface && !names.Contains(iMethod.Name))
                            name = iMethod.Name;
                        else
                            name = $"{Helpers.AdjustName(iMethod.ContainingType.ToString(), false)}.{Helpers.AdjustName(iMethod.Name, true)}";

                        if (!names.Contains(name))
                        {
                            methods.Add(new Tuple<IMethodSymbol, string>(iMethod, name));
                            names.Add(name);
                        }
                    }
                }
            }

            var methodNumber = 0;
            foreach (var methodTuple in methods)
            {
                var method = methodTuple.Item1;
                var explicitDeclaration = methodTuple.Item2;

                if (method.ContainingType.TypeKind == TypeKind.Interface && method.IsStatic)
                    continue;

                if (method.GetAttributes().Any(x => x.AttributeClass?.Name == "ObsoleteAttribute"))
                    continue;

                var isPublic = method.DeclaredAccessibility == Accessibility.Public;
                if (publicAndImplicitOnly && !isPublic)
                    continue;

                var isPublicReturn = Helpers.IsPublic(method.ReturnType);
                if (isPublic && isPublicReturn && !method.ContainingType.IsStatic && method.MethodKind != MethodKind.PropertyGet && method.MethodKind != MethodKind.PropertySet)
                    GeneratePublicMethod(explicitDeclaration, method, fullTypeName, ++methodNumber, membersToInitialize, sbChildClasses);
                else if (isPublicReturn && !method.ContainingType.IsStatic)
                    GeneratePrivateMethod(explicitDeclaration, method, fullTypeName, ++methodNumber, membersToInitialize, sbChildClasses);
                else
                    GeneratePrivateTypeMethod(explicitDeclaration, method, fullTypeName, ++methodNumber, membersToInitialize, sbChildClasses);

                if (recursive)
                {
                    if (ShouldGenerateType(method.ReturnType, allTypeSymbol))
                        GenerateType(context, method.ReturnType, allTypeSymbol, classList, false, publicAndImplicitOnly);

                    foreach (var arg in method.Parameters)
                    {
                        if (ShouldGenerateType(arg.Type, allTypeSymbol))
                            GenerateType(context, arg.Type, allTypeSymbol, classList, false, publicAndImplicitOnly);
                    }
                }
            }

            var sbMembers = new StringBuilder();
            _ = sbMembers.Append('[');
            foreach (var memberName in membersToInitialize)
            {
                if (sbMembers.Length > 1)
                    _ = sbMembers.Append(", ");
                sbMembers.Append("new ").Append(memberName).Append("(locker, LoadMethodInfo)");
            }
            _ = sbMembers.Append(']');
            var members = sbMembers.ToString();
            return members;
        }
        private static void GeneratePublicMethod(string memberName, IMethodSymbol methodSymbol, string parentTypeName, int methodNumber, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var className = $"MethodDetail_{methodNumber}";
            var isInterface = methodSymbol.ContainingType.TypeKind == TypeKind.Interface;

            var hasCaller = !methodSymbol.IsGenericMethod && !methodSymbol.ReturnType.IsRefLikeType && methodSymbol.Parameters.All(x => !x.Type.IsRefLikeType && x.Type.Kind != SymbolKind.PointerType);
            string caller;
            string callerBoxed;
            if (hasCaller)
            {
                var sbCaller = new StringBuilder();
                var sbCallerBoxed = new StringBuilder();

                _ = sbCaller.Append("(obj, args) => ");
                _ = sbCallerBoxed.Append("(obj, args) => ");

                var hasByRef = methodSymbol.Parameters.Any(x => x.RefKind != RefKind.None);

                if (hasByRef || methodSymbol.ReturnsVoid)
                {
                    _ = sbCaller.Append('{');
                    _ = sbCallerBoxed.Append('{');
                }

                if (hasByRef)
                {
                    for (var i = 0; i < methodSymbol.Parameters.Length; i++)
                    {
                        var parameter = methodSymbol.Parameters[i];
                        if (parameter.RefKind == RefKind.None)
                            continue;

                        _ = sbCaller.Append("var arg").Append(i).Append('=');
                        _ = sbCallerBoxed.Append("var arg").Append(i).Append('=');

                        _ = sbCaller.Append('(').Append(parameter.Type.ToString()).Append(")args![").Append(i).Append("]!");
                        _ = sbCallerBoxed.Append('(').Append(parameter.Type.ToString()).Append(")args![").Append(i).Append("]!");

                        _ = sbCaller.Append(';');
                        _ = sbCallerBoxed.Append(';');
                    }

                    if (!methodSymbol.ReturnsVoid)
                    {
                        _ = sbCaller.Append("return ");
                        _ = sbCallerBoxed.Append("return ");
                    }
                }

                if (isInterface)
                {
                    if (methodSymbol.IsStatic)
                    {
                        throw new InvalidOperationException();
                    }
                    else
                    {
                        _ = sbCaller.Append("((").Append(methodSymbol.ContainingType.ToString()).Append(')').Append("obj!)");
                        _ = sbCallerBoxed.Append("((").Append(methodSymbol.ContainingType.ToString()).Append(')').Append("obj!)");
                    }
                }
                else
                {
                    if (methodSymbol.IsStatic)
                    {
                        _ = sbCaller.Append(parentTypeName);
                        _ = sbCallerBoxed.Append(parentTypeName);
                    }
                    else
                    {
                        _ = sbCaller.Append("obj");
                        _ = sbCallerBoxed.Append("((").Append(parentTypeName).Append(')').Append("obj!)");
                    }
                }
                _ = sbCaller.Append(".").Append(methodSymbol.Name).Append('(');
                _ = sbCallerBoxed.Append(".").Append(methodSymbol.Name).Append('(');

                for (var i = 0; i < methodSymbol.Parameters.Length; i++)
                {
                    var parameter = methodSymbol.Parameters[i];
                    if (i > 0)
                    {
                        _ = sbCaller.Append(", ");
                        _ = sbCallerBoxed.Append(", ");
                    }

                    if (parameter.RefKind == RefKind.Out)
                    {
                        _ = sbCaller.Append("out ");
                        _ = sbCallerBoxed.Append("out ");
                    }
                    else if (parameter.RefKind == RefKind.In)
                    {
                        _ = sbCaller.Append("in ");
                        _ = sbCallerBoxed.Append("in ");
                    }
                    else if (parameter.RefKind == RefKind.Ref || parameter.RefKind == RefKind.RefReadOnly)
                    {
                        _ = sbCaller.Append("ref ");
                        _ = sbCallerBoxed.Append("ref ");
                    }

                    if (parameter.RefKind == RefKind.None)
                    {
                        _ = sbCaller.Append('(').Append(parameter.Type.ToString()).Append(")args![").Append(i).Append("]!");
                        _ = sbCallerBoxed.Append('(').Append(parameter.Type.ToString()).Append(")args![").Append(i).Append("]!");
                    }
                    else
                    {
                        _ = sbCaller.Append("arg").Append(i);
                        _ = sbCallerBoxed.Append("arg").Append(i);
                    }
                }
                _ = sbCaller.Append(')');
                _ = sbCallerBoxed.Append(')');

                if (hasByRef || methodSymbol.ReturnsVoid)
                {
                    _ = sbCaller.Append(';');
                    _ = sbCallerBoxed.Append(';');

                    if (methodSymbol.ReturnsVoid)
                    {
                        _ = sbCaller.Append("return null;");
                        _ = sbCallerBoxed.Append("return null;");
                    }

                    _ = sbCaller.Append('}');
                    _ = sbCallerBoxed.Append('}');
                }

                caller = sbCaller.ToString();
                callerBoxed = sbCallerBoxed.ToString();
            }
            else
            {
                caller = "throw new NotSupportedException()";
                callerBoxed = "throw new NotSupportedException()";
            }

            var attributes = GenerateAttributes(methodSymbol);

            var parameters = GenerateParameters(methodSymbol, $"m{methodNumber}", sbChildClasses);

            var code = $$""""

                        public sealed class {{className}} : MethodDetailCompiletimeBase<{{parentTypeName}}>
                        {
                            public {{className}}(object locker, Action loadMethodInfo) : base(locker, loadMethodInfo) { }

                            private Type? returnType = {{Helpers.GetTypeOfName(methodSymbol.ReturnType)}};
                            public override Type ReturnType => returnType!;

                            public override Func<{{parentTypeName}}, object?[]?, object?> Caller => {{caller}};
                            public override bool HasCaller => {{(hasCaller ? "true" : "false")}};

                            public override string Name => "{{memberName}}";
                            public override bool IsStatic => {{(methodSymbol.IsStatic ? "true" : "false")}};

                            protected override Func<ParameterDetail[]> CreateParameterDetails => () => {{parameters}};

                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};

                            public override Func<object?, object?[]?, object?> CallerBoxed => {{callerBoxed}};
                            public override bool HasCallerBoxed => {{(hasCaller ? "true" : "false")}};
                        }
                """";

            membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }
        private static void GeneratePrivateMethod(string memberName, IMethodSymbol methodSymbol, string parentTypeName, int methodNumber, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var className = $"MethodDetail_{methodNumber}";
            var isInterface = methodSymbol.ContainingType.TypeKind == TypeKind.Interface;

            var attributes = GenerateAttributes(methodSymbol);

            var parameters = GenerateParameters(methodSymbol, $"m{methodNumber}", sbChildClasses);

            var code = $$""""

                        public sealed class {{className}} : PrivateMethodDetailCompiletimeBase<{{parentTypeName}}>
                        {
                            public {{className}}(object locker, Action loadMethodInfo) : base(locker, loadMethodInfo) { }
                
                            private Type? returnType = {{Helpers.GetTypeOfName(methodSymbol.ReturnType)}};
                            public override Type ReturnType => returnType!;

                            public override string Name => "{{memberName}}";
                            public override bool IsStatic => {{(methodSymbol.IsStatic ? "true" : "false")}};

                            protected override Func<ParameterDetail[]> CreateParameterDetails => () => {{parameters}};
                
                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};
                        }
                """";

            membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }
        private static void GeneratePrivateTypeMethod(string memberName, IMethodSymbol methodSymbol, string parentTypeName, int methodNumber, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var className = $"MethodDetail_{methodNumber}";
            var isInterface = methodSymbol.ContainingType.TypeKind == TypeKind.Interface;

            var attributes = GenerateAttributes(methodSymbol);

            var parameters = GenerateParameters(methodSymbol, $"m{methodNumber}", sbChildClasses);

            var code = $$""""

                        public sealed class {{className}} : PrivateTypeMethodDetailCompiletimeBase
                        {
                            public {{className}}(object locker, Action loadMethodInfo) : base(locker, loadMethodInfo) { }
                
                            private Type? returnType = {{Helpers.GetTypeOfName(methodSymbol.ReturnType)}};
                            public override Type ReturnType => returnType!;

                            public override string Name => "{{memberName}}";
                            public override bool IsStatic => {{(methodSymbol.IsStatic ? "true" : "false")}};

                            protected override Func<ParameterDetail[]> CreateParameterDetails => () => {{parameters}};
                
                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};
                        }
                """";

            membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }

        private static bool SignatureCompare(IMethodSymbol methodSymbol1, IMethodSymbol methodSymbol2)
        {
            if (methodSymbol1.Name != methodSymbol2.Name)
                return false;
            if (methodSymbol1.Parameters.Length != methodSymbol2.Parameters.Length)
                return false;
            for (var i = 0; i < methodSymbol1.Parameters.Length; i++)
            {
                if (methodSymbol1.Parameters[i].Type.ToString() != methodSymbol2.Parameters[i].Type.ToString())
                    return false;
            }
            return true;
        }

        private static string GenerateMembers(SourceProductionContext context, ITypeSymbol typeSymbol, IReadOnlyCollection<ITypeSymbol> allTypeSymbol, List<Tuple<string, string>> classList, bool recursive, bool publicAndImplicitOnly, StringBuilder sbChildClasses)
        {
            var symbolMembers = typeSymbol.GetMembers();
            var fullTypeName = Helpers.GetFullName(typeSymbol);

            var properties = symbolMembers.Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Where(x => !x.IsStatic || x.ExplicitInterfaceImplementations.Length == 0).Select(x => new Tuple<IPropertySymbol, string>(x, x.Name)).ToList();
            var fields = symbolMembers.Where(x => x.Kind == SymbolKind.Field).Cast<IFieldSymbol>().ToList();

            if (!publicAndImplicitOnly && typeSymbol.AllInterfaces.Length > 0)
            {
                var names = new HashSet<string>();
                if (typeSymbol.TypeKind == TypeKind.Interface)
                {
                    foreach (var property in properties)
                        names.Add(property.Item1.Name);
                    foreach (var field in fields)
                        names.Add(field.Name);
                }

                foreach (var i in typeSymbol.AllInterfaces)
                {
                    var iMembers = i.GetMembers();
                    foreach (var iProperty in iMembers.Where(x => x.Kind == SymbolKind.Property && !x.IsStatic).Cast<IPropertySymbol>())
                    {
                        string name;
                        if (typeSymbol.TypeKind == TypeKind.Interface && !names.Contains(iProperty.Name))
                            name = iProperty.Name;
                        else
                            name = $"{iProperty.ContainingType.ToString()}.{iProperty.Name}";

                        if (!names.Contains(name))
                        {
                            properties.Add(new Tuple<IPropertySymbol, string>(iProperty, name));
                            names.Add(name);
                        }
                    }
                }
            }

            var membersToInitialize = new List<string>();

#pragma warning disable RS1024 // Comparing instance so no need for special comparer
            var backingFields = new HashSet<IFieldSymbol>();
#pragma warning restore RS1024 // Comparing instance so no need for special comparer
            var memberNumber = 0;
            var propertyCount = properties.Count();
            foreach (var propertyTuple in properties)
            {
                var property = propertyTuple.Item1;
                var explicitDeclaration = propertyTuple.Item2;

                if (property.IsIndexer)
                    continue;
                if (property.GetAttributes().Any(x => x.AttributeClass?.Name == "ObsoleteAttribute"))
                    continue;

                var isPublic = property.DeclaredAccessibility == Accessibility.Public;
                if (publicAndImplicitOnly && !isPublic)
                    continue;

                var canBeReferencedAsCode = Helpers.CanBeReferencedAsCode(property.Type);
                if (isPublic && canBeReferencedAsCode && !property.ContainingType.IsStatic)
                    GeneratePublicMember(explicitDeclaration, property, propertyCount, fields, backingFields, fullTypeName, ++memberNumber, membersToInitialize, sbChildClasses);
                else if (canBeReferencedAsCode && !property.ContainingType.IsStatic)
                    GeneratePrivateMember(explicitDeclaration, property, propertyCount, fields, backingFields, fullTypeName, ++memberNumber, membersToInitialize, sbChildClasses);
                else
                    GeneratePrivateTypeMember(explicitDeclaration, property, propertyCount, fields, backingFields, fullTypeName, ++memberNumber, membersToInitialize, sbChildClasses);

                if (recursive)
                {
                    if (ShouldGenerateType(property.Type, allTypeSymbol))
                        GenerateType(context, property.Type, allTypeSymbol, classList, false, publicAndImplicitOnly);
                }
            }
            foreach (var field in fields)
            {
                if (field.GetAttributes().Any(x => x.AttributeClass?.Name == "ObsoleteAttribute"))
                    continue;

                var isPublic = field.DeclaredAccessibility == Accessibility.Public;
                if (publicAndImplicitOnly && !isPublic && !backingFields.Contains(field))
                    continue;

                var canBeReferencedAsCode = Helpers.CanBeReferencedAsCode(field.Type);
                if (isPublic && canBeReferencedAsCode && !field.ContainingType.IsStatic)
                    GeneratePublicMember(field, fullTypeName, backingFields, ++memberNumber, membersToInitialize, sbChildClasses);
                else if (canBeReferencedAsCode && !field.ContainingType.IsStatic)
                    GeneratePrivateMember(field, fullTypeName, backingFields, ++memberNumber, membersToInitialize, sbChildClasses);
                else
                    GeneratePrivateTypeMember(field, fullTypeName, backingFields, ++memberNumber, membersToInitialize, sbChildClasses);

                if (recursive)
                {
                    if (ShouldGenerateType(field.Type, allTypeSymbol))
                        GenerateType(context, field.Type, allTypeSymbol, classList, false, publicAndImplicitOnly);
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
        private static void GeneratePublicMember(string memberName, IPropertySymbol propertySymbol, int propertyTotal, IEnumerable<IFieldSymbol> fieldSymbols, HashSet<IFieldSymbol> backingFields, string parentTypeName, int methodNumber, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var className = $"MemberDetail_{methodNumber}";
            var typeName = propertySymbol.Type.ToString();
            var isInterface = propertySymbol.ContainingType.TypeKind == TypeKind.Interface;

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

            var hasGetter = propertySymbol.GetMethod is not null && !propertySymbol.IsIndexer;
            var hasSetter = propertySymbol.SetMethod is not null && !propertySymbol.IsIndexer;

            string getter;
            string getterBoxed;
            if (hasGetter)
            {
                if (isInterface)
                {
                    if (propertySymbol.IsStatic)
                    {
                        hasGetter = false;
                        getter = $"throw new NotSupportedException()";
                        getterBoxed = $"throw new NotSupportedException()";
                    }
                    else
                    {
                        getter = $"(({propertySymbol.ContainingType.ToString()})x!).{propertySymbol.Name}";
                        getterBoxed = $"(({propertySymbol.ContainingType.ToString()})x!).{propertySymbol.Name}";
                    }
                }
                else
                {
                    if (propertySymbol.IsStatic)
                    {
                        getter = $"{propertySymbol.ContainingType.ToString()}.{propertySymbol.Name}";
                        getterBoxed = $"{propertySymbol.ContainingType.ToString()}.{propertySymbol.Name}";
                    }
                    else
                    {
                        getter = $"x!.{propertySymbol.Name}";
                        getterBoxed = $"(({parentTypeName})x!).{propertySymbol.Name}";
                    }
                }
            }
            else
            {
                getter = $"throw new NotSupportedException()";
                getterBoxed = $"throw new NotSupportedException()";
            }

            string setter;
            string setterBoxed;
            if (hasSetter)
            {
                if (isInterface)
                {
                    if (propertySymbol.IsStatic)
                    {
                        hasSetter = false;
                        setter = $"throw new NotSupportedException()";
                        setterBoxed = $"throw new NotSupportedException()";
                    }
                    else
                    {
                        setter = $"(({propertySymbol.ContainingType.ToString()})x!).{propertySymbol.Name} = value";
                        setterBoxed = $"(({propertySymbol.ContainingType.ToString()})x!).{propertySymbol.Name} = ({typeName})value!";
                    }
                }
                else
                {
                    if (propertySymbol.IsStatic)
                    {
                        setter = $"{propertySymbol.ContainingType.ToString()}.{propertySymbol.Name} = value";
                        setterBoxed = $"{propertySymbol.ContainingType.ToString()}.{propertySymbol.Name} = ({typeName})value!";
                    }
                    else
                    {
                        setter = $"x!.{propertySymbol.Name} = value";
                        setterBoxed = $"(({propertySymbol.ContainingType.ToString()})x!).{propertySymbol.Name} = ({typeName})value!";
                    }
                }
            }
            else
            {
                setter = $"throw new NotSupportedException()";
                setterBoxed = $"throw new NotSupportedException()";
            }


            var attributes = GenerateAttributes(propertySymbol);

            var code = $$""""

                        public sealed class {{className}} : MemberDetailCompiletimeBase<{{parentTypeName}}, {{typeName}}>
                        {
                            public {{className}}(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

                            public override string Name => "{{memberName}}";
                            public override bool IsStatic => {{(propertySymbol.IsStatic ? "true" : "false")}};

                            private readonly Type type = {{Helpers.GetTypeOfName(propertySymbol.Type)}};
                            public override Type Type => type;

                            public override bool IsBacked => {{(isBacked ? "true" : "false")}};

                            public override Func<{{parentTypeName}}, {{typeName}}> Getter => (x) => {{getter}};
                            public override bool HasGetter => {{(hasGetter ? "true" : "false")}};

                            public override Action<{{parentTypeName}}, {{typeName}}> Setter => (x, value) => {{setter}};
                            public override bool HasSetter => {{(hasSetter ? "true" : "false")}};

                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};

                            public override Func<object, object?> GetterBoxed => (x) => {{getterBoxed}};
                            public override bool HasGetterBoxed => {{(hasGetter ? "true" : "false")}};

                            public override Action<object, object?> SetterBoxed => (x, value) => {{setterBoxed}};
                            public override bool HasSetterBoxed => {{(hasSetter ? "true" : "false")}};

                            protected override Func<MemberDetail<{{parentTypeName}}, {{typeName}}>?> CreateBackingFieldDetail => {{(fieldClassName is null ? "() => null" : $"() => new {fieldClassName}(locker, loadMemberInfo)")}};
                        }
                """";

            membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }
        private static void GeneratePrivateMember(string memberName, IPropertySymbol propertySymbol, int propertyTotal, IEnumerable<IFieldSymbol> fieldSymbols, HashSet<IFieldSymbol> backingFields, string parentTypeName, int methodNumber, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var className = $"MemberDetail_{methodNumber}";
            var typeName = propertySymbol.Type.ToString();
            var isInterface = propertySymbol.ContainingType.TypeKind == TypeKind.Interface;

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

                        public sealed class {{className}} : PrivateMemberDetailCompiletimeBase<{{parentTypeName}}, {{typeName}}>
                        {
                            public {{className}}(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

                            public override string Name => "{{memberName}}";
                            public override bool IsStatic => {{(propertySymbol.IsStatic ? "true" : "false")}};

                            public override bool IsBacked => {{(isBacked ? "true" : "false")}};

                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};

                            protected override Func<MemberDetail<{{parentTypeName}}, {{typeName}}>?> CreateBackingFieldDetail => {{(fieldClassName is null ? "() => null" : $"() => new {fieldClassName}(locker, loadMemberInfo)")}};
                        }
                """";

            membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }
        private static void GeneratePrivateTypeMember(string memberName, IPropertySymbol propertySymbol, int propertyTotal, IEnumerable<IFieldSymbol> fieldSymbols, HashSet<IFieldSymbol> backingFields, string parentTypeName, int methodNumber, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var className = $"MemberDetail_{methodNumber}";
            var typeName = propertySymbol.Type.ToString();
            var isInterface = propertySymbol.ContainingType.TypeKind == TypeKind.Interface;

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
            if (fieldSymbol is not null)
            {
                backingFields.Add(fieldSymbol);
                isBacked = true;
            }

            var attributes = GenerateAttributes(propertySymbol);

            var code = $$""""

                        public sealed class {{className}} : PrivateTypeMemberDetailCompiletimeBase
                        {
                            public {{className}}(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

                            public override string Name => "{{memberName}}";
                            public override bool IsStatic => {{(propertySymbol.IsStatic ? "true" : "false")}};

                            public override bool IsBacked => {{(isBacked ? "true" : "false")}};

                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};
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

            var hasSetter = !fieldSymbol.IsReadOnly && !fieldSymbol.IsConst;

            var attributes = GenerateAttributes(fieldSymbol);

            var code = $$""""

                        public sealed class {{className}} : MemberDetailCompiletimeBase<{{parentTypeName}}, {{typeName}}>
                        {
                            public {{className}}(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

                            public override string Name => "{{memberName}}";
                            public override bool IsStatic => {{(fieldSymbol.IsStatic ? "true" : "false")}};

                            private readonly Type type = {{Helpers.GetTypeOfName(fieldSymbol.Type)}};
                            public override Type Type => type;

                            public override bool IsBacked => true;

                            public override Func<{{parentTypeName}}, {{typeName}}> Getter => (x) => {{(fieldSymbol.IsStatic ? fieldSymbol.ContainingType.ToString() : "x!")}}.{{fieldSymbol.Name}};
                            public override bool HasGetter => true;

                            public override Action<{{parentTypeName}}, {{typeName}}> Setter => {{(hasSetter ? $"(x, value) => {(fieldSymbol.IsStatic ? fieldSymbol.ContainingType.ToString() : "x!")}.{fieldSymbol.Name} = value" : "throw new NotSupportedException()")}};
                            public override bool HasSetter => {{(hasSetter ? "true" : "false")}};

                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};

                            public override Func<object, object?> GetterBoxed => (x) => {{(fieldSymbol.IsStatic ? fieldSymbol.ContainingType.ToString() : $"(({fieldSymbol.ContainingType.ToString()})x!)")}}.{{fieldSymbol.Name}};
                            public override bool HasGetterBoxed => true;

                            public override Action<object, object?> SetterBoxed => (x, value) => {{(hasSetter ? $"{(fieldSymbol.IsStatic ? fieldSymbol.ContainingType.ToString() : $"(({fieldSymbol.ContainingType.ToString()})x!)")}.{fieldSymbol.Name} = ({typeName})value!" : "throw new NotSupportedException()")}};
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

                        public sealed class {{className}} : PrivateMemberDetailCompiletimeBase<{{parentTypeName}}, {{typeName}}>
                        {
                            public {{className}}(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

                            public override string Name => "{{memberName}}";
                            public override bool IsStatic => {{(fieldSymbol.IsStatic ? "true" : "false")}};

                            public override bool IsBacked => true;

                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};

                            protected override Func<MemberDetail<{{parentTypeName}}, {{typeName}}>?> CreateBackingFieldDetail => () => null;
                        }
                """";

            if (!backingFields.Contains(fieldSymbol))
                membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }
        private static void GeneratePrivateTypeMember(IFieldSymbol fieldSymbol, string parentTypeName, HashSet<IFieldSymbol> backingFields, int methodNumber, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var memberName = fieldSymbol.Name;
            var className = $"MemberDetail_{methodNumber}";
            var typeName = fieldSymbol.Type.ToString();

            var attributes = GenerateAttributes(fieldSymbol);

            var code = $$""""

                        public sealed class {{className}} : PrivateTypeMemberDetailCompiletimeBase
                        {
                            public {{className}}(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

                            public override string Name => "{{memberName}}";
                            public override bool IsStatic => {{(fieldSymbol.IsStatic ? "true" : "false")}};

                            public override bool IsBacked => true;

                            protected override Func<Attribute[]> CreateAttributes => () => {{attributes}};
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

            var t = Helpers.GetTypeOfName(parameterSymbol.Type);

            var code = $$""""

                        public sealed class {{className}} : ParameterDetailCompiletimeBase
                        {
                            public {{className}}(object locker, Action loadParameterInfo) : base(locker, loadParameterInfo) { }

                            public override string? Name => "{{parameterName}}";

                            private readonly Type? type = {{Helpers.GetTypeOfName(parameterSymbol.Type, parameterSymbol.RefKind != RefKind.None)}};
                            public override Type Type => type!;
                        }
                """";

            parametersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }

        private static string GenerateBaseTypes(ITypeSymbol typeSymbol)
        {
            var sbBaseTypes = new StringBuilder();
            _ = sbBaseTypes.Append('[');
            var baseTypeSymbol = typeSymbol.BaseType;
            while (baseTypeSymbol is not null)
            {
                if (!Helpers.CanBeReferencedAsCode(baseTypeSymbol))
                    break;
                if (sbBaseTypes.Length > 1)
                    _ = sbBaseTypes.Append(", ");
                _ = sbBaseTypes.Append(Helpers.GetTypeOfName(baseTypeSymbol));
                baseTypeSymbol = baseTypeSymbol.BaseType;
            }
            _ = sbBaseTypes.Append(']');
            var baseTypes = sbBaseTypes.ToString();
            return baseTypes;
        }
        private static string GenerateInterfaces(ITypeSymbol typeSymbol)
        {
            var sbInterfaceTypes = new StringBuilder();
            _ = sbInterfaceTypes.Append('[');
            foreach (var i in typeSymbol.AllInterfaces)
            {
                if (!Helpers.CanBeReferencedAsCode(i))
                    break;
                if (sbInterfaceTypes.Length > 1)
                    _ = sbInterfaceTypes.Append(", ");
                _ = sbInterfaceTypes.Append(Helpers.GetTypeOfName(i));
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
                if (attributeSymbol.AttributeClass is null)
                    continue;
                if (attributeSymbol.AttributeClass.ContainingNamespace.ToString().StartsWith("System.Runtime.CompilerServices"))
                    continue;
                if (attributeSymbol.AttributeClass.ContainingNamespace.ToString().StartsWith("System.Diagnostics"))
                    continue;
                if (!Helpers.CanBeReferencedAsCode(attributeSymbol.AttributeClass))
                    continue;
                if (attributeSymbol.ConstructorArguments.Any(x => x.Type is null || !Helpers.CanBeReferencedAsCode(x.Type)))
                    continue;

                if (sbAttributes.Length > 1)
                    _ = sbAttributes.Append(", ");
                _ = sbAttributes.Append("new ").Append(attributeSymbol.AttributeClass.ToString()).Append('(');
                var usedFirst = false;
                //if (attributeSymbol.NamedArguments.Length > 0)
                //{
                //    foreach (var arg in attributeSymbol.NamedArguments)
                //    {
                //        if (usedFirst)
                //            _ = sbAttributes.Append(", ");
                //        else
                //            usedFirst = true;
                //        _ = sbAttributes.Append(arg.Key).Append(": ");
                //        TypedConstantToString(arg.Value, sbAttributes);
                //    }
                //}
                //else
                //{
                foreach (var arg in attributeSymbol.ConstructorArguments)
                {
                    if (usedFirst)
                        _ = sbAttributes.Append(", ");
                    else
                        usedFirst = true;
                    Helpers.TypedConstantToString(arg, sbAttributes);
                }
                //}
                _ = sbAttributes.Append(')');
            }
            _ = sbAttributes.Append(']');
            var attributes = sbAttributes.ToString();
            return attributes;
        }
        private static string GenerateInnerTypes(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                if (!Helpers.IsGenericDefined(namedTypeSymbol))
                    return "[]";

                var sbInnerTypes = new StringBuilder();
                _ = sbInnerTypes.Append('[');
                foreach (var type in namedTypeSymbol.TypeArguments)
                {
                    if (sbInnerTypes.Length > 1)
                        _ = sbInnerTypes.Append(", ");
                    _ = sbInnerTypes.Append(Helpers.GetTypeOfName(type));
                }
                _ = sbInnerTypes.Append(']');
                var attributes = sbInnerTypes.ToString();
                return attributes;
            }
            else if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                return $"[{Helpers.GetTypeOfName(arrayTypeSymbol.ElementType)}]";
            }

            return "[]";
        }
    }
}
