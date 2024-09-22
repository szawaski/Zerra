// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
    [Generator]
    public class TypeDetailSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classProvider = context.SyntaxProvider.CreateSyntaxProvider(
                (node, cancellationToken) => node is BaseTypeDeclarationSyntax,
                (context, cancellationToken) => (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node)!
            ).Where(x => x.DeclaredAccessibility == Accessibility.Public && !x.IsStatic && !x.IsAbstract && !x.IsValueType && !x.IsGenericType)
            .Collect();

            var compilationAndClasses = classProvider.Combine(context.CompilationProvider);

            context.RegisterSourceOutput(compilationAndClasses, (a, b) => Generate(a, b.Left, b.Right));
        }

        private static void Generate(SourceProductionContext context, ImmutableArray<INamedTypeSymbol> symbols, Compilation compilation)
        {
            var classList = new List<string>();
            foreach (var symbol in symbols.GroupBy(x => $"{x.ContainingNamespace}.{x.Name}"))
                GenerateType(context, symbol.First(), classList);
            GeneriateInitializer(context, classList);
        }

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
#endif
        private static readonly string dictionaryTypeName = typeof(IDictionary).Name;
        private static readonly string dictionaryGenericTypeName = typeof(IDictionary<,>).Name;
        private static readonly string readOnlyDictionaryGenericTypeName = typeof(IReadOnlyDictionary<,>).Name;

        private static void GeneriateInitializer(SourceProductionContext context, List<string> classList)
        {
            var sb = new StringBuilder();
            foreach (var item in classList)
            {
                if (sb.Length > 0)
                    sb.Append(Environment.NewLine).Append("            ");
                sb.Append("TypeAnalyzer.InitializeTypeDetail(new ").Append(item).Append("());");
            }
            var lines = sb.ToString();

            var code = $$"""
                //Zerra Generated File
                #if NET5_0_OR_GREATER

                using System.Runtime.CompilerServices;
                using Zerra.Reflection;

                namespace Zerra.SourceGeneration
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

        private static void GenerateType(SourceProductionContext context, INamedTypeSymbol typeSymbol, List<string> classList)
        {
            var ns = typeSymbol.ContainingNamespace.ToString().Contains("<global namespace>") ? null : typeSymbol.ContainingNamespace.ToString();

            var typeName = typeSymbol.Name;
            var fullTypeName = typeSymbol.ToString();

            string className;
            string initializerName;
            string fileName;
            string? typeConstraints;
            if (typeSymbol.TypeArguments.Length > 0)
            {
                var sbClassName = new StringBuilder();
                var sbConstraints = new StringBuilder();
                _ = sbClassName.Append(typeName).Append("TypeDetail<");
                for (var i = 0; i < typeSymbol.TypeArguments.Length; i++)
                {
                    var typeArgument = (ITypeParameterSymbol)typeSymbol.TypeArguments[i];
                    if (i > 0)
                        _ = sbClassName.Append(',');
                    _ = sbClassName.Append(typeArgument.Name);
                    if (typeArgument.HasUnmanagedTypeConstraint || typeArgument.HasValueTypeConstraint || typeArgument.HasReferenceTypeConstraint || typeArgument.HasNotNullConstraint || typeArgument.HasConstructorConstraint)
                    {
                        _ = sbConstraints.Append(" where ").Append(typeArgument.Name).Append(" : ");
                        var wroteConstraint = false;

                        if (typeArgument.HasUnmanagedTypeConstraint)
                        {
                            if (wroteConstraint)
                                _ = sbConstraints.Append(", ");
                            else
                                wroteConstraint = true;
                            _ = sbConstraints.Append("unmanaged");
                        }
                        if (typeArgument.HasValueTypeConstraint)
                        {
                            if (wroteConstraint)
                                _ = sbConstraints.Append(", ");
                            else
                                wroteConstraint = true;
                            _ = sbConstraints.Append("struct");
                        }
                        if (typeArgument.HasReferenceTypeConstraint)
                        {
                            if (wroteConstraint)
                                _ = sbConstraints.Append(", ");
                            else
                                wroteConstraint = true;
                            _ = sbConstraints.Append("class");
                        }
                        if (typeArgument.HasNotNullConstraint)
                        {
                            if (wroteConstraint)
                                _ = sbConstraints.Append(", ");
                            else
                                wroteConstraint = true;
                            _ = sbConstraints.Append("notnull");
                        }
                        if (typeArgument.HasConstructorConstraint)
                        {
                            if (wroteConstraint)
                                _ = sbConstraints.Append(", ");
                            else
                                wroteConstraint = true;
                            _ = sbConstraints.Append("new()");
                        }
                    }
                }
                _ = sbClassName.Append('>');
                className = sbClassName.ToString();
                initializerName = $"{typeName}TypeDetailInitializerT{typeSymbol.TypeArguments.Length}";
                fileName = ns == null ? $"{typeName}TypeDetailT{typeSymbol.TypeArguments.Length}.cs" : $"{ns}.{typeName}TypeDetailT{typeSymbol.TypeArguments.Length}.cs";
                typeConstraints = sbConstraints.ToString();
            }
            else
            {
                className = $"{typeName}TypeDetail";
                initializerName = $"{typeName}TypeDetailInitializer";
                fileName = ns == null ? $"{typeName}TypeDetail.cs" : $"{ns}.{typeName}TypeDetail.cs";
                typeConstraints = null;
            }

            var qualifiedClassName = ns == null ? $"SourceGeneration.{className}" : $"{ns}.SourceGeneration.{className}";
            classList.Add(qualifiedClassName);

            var isArray = typeName.Contains('[');
            var interfaces = typeSymbol.AllInterfaces.Select(x => x.Name).ToImmutableHashSet();

            var hasIEnumerable = isArray || typeName == enumberableTypeName || interfaces.Contains(enumberableTypeName);
            var hasIEnumerableGeneric = isArray || typeName == enumberableGenericTypeName || interfaces.Contains(enumberableGenericTypeName);
            var hasICollection = typeName == collectionTypeName || interfaces.Contains(collectionTypeName);
            var hasICollectionGeneric = typeName == collectionGenericTypeName || interfaces.Contains(collectionGenericTypeName);
            var hasIReadOnlyCollectionGeneric = typeName == readOnlyCollectionGenericTypeName || interfaces.Contains(readOnlyCollectionGenericTypeName);
            var hasIList = typeName == listTypeName || interfaces.Contains(listTypeName);
            var hasIListGeneric = typeName == listGenericTypeName || interfaces.Contains(listGenericTypeName);
            var hasIReadOnlyListGeneric = typeName == readOnlyListTypeName || interfaces.Contains(readOnlyListTypeName);
            var hasISetGeneric = typeName == setGenericTypeName || interfaces.Contains(setGenericTypeName);
#if NET5_0_OR_GREATER
            var hasIReadOnlySetGeneric = name == readOnlySetGenericTypeName || interfaces.Contains(readOnlySetGenericTypeName);
#else
            var hasIReadOnlySetGeneric = false;
#endif
            var hasIDictionary = typeName == dictionaryTypeName || interfaces.Contains(dictionaryTypeName);
            var hasIDictionaryGeneric = typeName == dictionaryGenericTypeName || interfaces.Contains(dictionaryGenericTypeName);
            var hasIReadOnlyDictionaryGeneric = typeName == readOnlyDictionaryGenericTypeName || interfaces.Contains(readOnlyDictionaryGenericTypeName);

            var isIEnumerable = typeName == enumberableTypeName;
            var isIEnumerableGeneric = typeName == enumberableGenericTypeName;
            var isICollection = typeName == collectionTypeName;
            var isICollectionGeneric = typeName == collectionGenericTypeName;
            var isIReadOnlyCollectionGeneric = typeName == readOnlyCollectionGenericTypeName;
            var isIList = typeName == listTypeName;
            var isIListGeneric = typeName == listGenericTypeName;
            var isIReadOnlyListGeneric = typeName == readOnlyListTypeName;
            var isISetGeneric = typeName == setGenericTypeName;
#if NET5_0_OR_GREATER
            var isIReadOnlySetGeneric = name == readOnlySetGenericTypeName;
#else
            var isIReadOnlySetGeneric = false;
#endif
            var isIDictionary = typeName == dictionaryTypeName;
            var isIDictionaryGeneric = typeName == dictionaryGenericTypeName;
            var isIReadOnlyDictionaryGeneric = typeName == readOnlyDictionaryGenericTypeName;

            var hasCreator = typeSymbol.Constructors.Any(x => !x.IsStatic && x.Parameters.Length == 0);

            CoreType? coreType = null;
            if (TypeLookup.CoreTypeLookup(typeName, out var coreTypeParsed))
                coreType = coreTypeParsed;

            SpecialType? specialType = null;
            if (TypeLookup.SpecialTypeLookup(typeName, out var specialTypeParsed))
                specialType = specialTypeParsed;

            CoreEnumType? enumType = null;
            if (typeSymbol.EnumUnderlyingType is not null && TypeLookup.CoreEnumTypeLookup(typeSymbol.EnumUnderlyingType.Name, out var enumTypeParsed))
                enumType = enumTypeParsed;

            var innerType = typeSymbol.TypeArguments.Length == 1 ? typeSymbol.TypeArguments[0].Name : null;

            var isTask = specialType == SpecialType.Task;

            var sbBaseTypes = new StringBuilder();
            _ = sbBaseTypes.Append('[');
            var baseType = typeSymbol.BaseType;
            while (baseType is not null)
            {
                if (sbBaseTypes.Length > 1)
                    _ = sbBaseTypes.Append(", ");
                GetTypeOf(baseType, sbBaseTypes);
                baseType = baseType.BaseType;
                _ = sbBaseTypes;
            }
            _ = sbBaseTypes.Append(']');
            var baseTypes = sbBaseTypes.ToString();

            var symbolMembers = typeSymbol.GetMembers();

            var sbChildClasses = new StringBuilder();

            var membersToInitialize = new List<string>();
            var properties = symbolMembers.Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().ToArray();
            var fields = symbolMembers.Where(x => x.Kind == SymbolKind.Field).Cast<IFieldSymbol>().ToArray();
            var backingFields = new HashSet<IFieldSymbol>();
            foreach (var property in properties)
            {
                if (property.DeclaredAccessibility == Accessibility.Public)
                    GeneratePublicMember(property, fields, backingFields, fullTypeName, membersToInitialize, sbChildClasses);
                else
                    GeneratePrivateMember(property, fields, backingFields, fullTypeName, membersToInitialize, sbChildClasses);
            }
            foreach (var field in fields)
            {
                if (field.DeclaredAccessibility == Accessibility.Public)
                    GeneratePublicMember(field, fullTypeName, backingFields, membersToInitialize, sbChildClasses);
                else
                    GeneratePrivateMember(field, fullTypeName, backingFields, membersToInitialize, sbChildClasses);
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

                        public override bool IsNullable => false;

                        public override CoreType? CoreType => {{(coreType.HasValue ? "CoreType." + coreType.Value.ToString() : "null")}};
                        public override SpecialType? SpecialType => {{(specialType.HasValue ? "SpecialType." + specialType.Value.ToString() : "null")}};
                        public override CoreEnumType? EnumUnderlyingType => {{(enumType.HasValue ? "CoreEnumType." + enumType.Value.ToString() : "null")}};

                        private readonly Type? innerType = {{(innerType is null ? "null" : $"typeof({innerType})")}};
                        public override Type InnerType => innerType ?? throw new NotSupportedException();

                        public override bool IsTask => {{(isTask ? "true" : "false")}};

                        public override IReadOnlyList<Type> BaseTypes => {{baseTypes}};

                        public override IReadOnlyList<Type> Interfaces => [];

                        public override IReadOnlyList<Attribute> Attributes => [];

                        public override IReadOnlyList<Type> InnerTypes => [];
                        public override IReadOnlyList<TypeDetail> InnerTypeDetails => [];

                        public override TypeDetail InnerTypeDetail => throw new NotSupportedException();

                        public override Type IEnumerableGenericInnerType => throw new NotSupportedException();
                        public override TypeDetail IEnumerableGenericInnerTypeDetail => throw new NotSupportedException();

                        public override Type DictionaryInnerType => throw new NotSupportedException();
                        public override TypeDetail DictionaryInnerTypeDetail => throw new NotSupportedException();

                        public override Func<object, object?> TaskResultGetter => throw new NotSupportedException();
                        public override bool HasTaskResultGetter => false;

                        public override Func<object> CreatorBoxed => () => {{(hasCreator ? $"new {fullTypeName}()" : "throw new NotSupportedException()")}};
                        public override bool HasCreatorBoxed => {{(hasCreator ? "true" : "false")}};

                        protected override Func<MethodDetail<{{fullTypeName}}>[]> CreateMethodDetails => () => [];

                        protected override Func<ConstructorDetail<{{fullTypeName}}>[]> CreateConstructorDetails => () => [];

                        protected override Func<MemberDetail[]> CreateMemberDetails => () => {{members}};

                {{childClasses}}
                    }
                }
                """;

            context.AddSource(fileName, SourceText.From(code, Encoding.UTF8));
        }

        private static void GeneratePublicMember(IPropertySymbol propertySymbol, IFieldSymbol[] fieldSymbols, HashSet<IFieldSymbol> backingFields, string parentTypeName, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var memberName = propertySymbol.Name;
            var className = $"{propertySymbol.Name}MemberDetail";
            var typeName = propertySymbol.Type.ToString();

            //<{property.Name}>k__BackingField
            //<{property.Name}>i__Field
            var backingName = $"<{propertySymbol.Name}>";
            var fieldSymbol = fieldSymbols.FirstOrDefault(x => x.Name.StartsWith(backingName));
            var isBacked = false;
            string? fieldClassName = null;
            if (fieldSymbol is not null)
            {
                backingFields.Add(fieldSymbol);
                isBacked = true;
                fieldClassName = $"{fieldSymbol.Name.Replace('<', '_').Replace('>', '_')}MemberDetail";
            }

            var hasGetter = propertySymbol.GetMethod is not null;
            var hasSetter = propertySymbol.SetMethod is not null;

            var code = $$""""

                        public sealed class {{className}} : MemberDetailGenerationBase<{{parentTypeName}}, {{typeName}}>
                        {
                            public {{className}}(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

                            public override string Name => "{{memberName}}";

                            private readonly Type type = typeof({{typeName.Replace("?", "")}});
                            public override Type Type => type;

                            public override bool IsBacked => {{(isBacked ? "true" : "false")}};

                            public override Func<{{parentTypeName}}, {{typeName}}> Getter => (x) => {{(hasGetter ? $"x.{memberName}" : "throw new NotSupportedException()")}};
                            public override bool HasGetter => {{(hasGetter ? "true" : "false")}};

                            public override Action<{{parentTypeName}}, {{typeName}}> Setter => {{(hasSetter ? $"(x, value) => x.{memberName} = value" : "throw new NotSupportedException()")}};
                            public override bool HasSetter => {{(hasSetter ? "true" : "false")}};

                            public override IReadOnlyList<Attribute> Attributes => [];

                            public override Func<object, object?> GetterBoxed => (x) => {{(hasGetter ? $"(({parentTypeName})x).{memberName}" : "throw new NotSupportedException()")}};
                            public override bool HasGetterBoxed => {{(hasGetter ? "true" : "false")}};

                            public override Action<object, object?> SetterBoxed => (x, value) => {{(hasSetter ? $"(({parentTypeName})x).{memberName} = ({typeName})value!" : "throw new NotSupportedException()")}};
                            public override bool HasSetterBoxed => {{(hasSetter ? "true" : "false")}};

                            protected override Func<MemberDetail<{{parentTypeName}}, {{typeName}}>?> CreateBackingFieldDetail => {{(fieldClassName is null ? "() => null" : $"() => new {fieldClassName}(locker, loadMemberInfo)")}};
                        }
                """";

            membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }
        private static void GeneratePrivateMember(IPropertySymbol propertySymbol, IFieldSymbol[] fieldSymbols, HashSet<IFieldSymbol> backingFields, string parentTypeName, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var memberName = propertySymbol.Name;
            var className = $"{propertySymbol.Name}MemberDetail";
            var typeName = propertySymbol.Type.ToString();

            //<{property.Name}>k__BackingField
            //<{property.Name}>i__Field
            var backingName = $"<{propertySymbol.Name}>";
            var fieldSymbol = fieldSymbols.FirstOrDefault(x => x.Name.StartsWith(backingName));
            var isBacked = false;
            string? fieldClassName = null;
            if (fieldSymbol is not null)
            {
                backingFields.Add(fieldSymbol);
                isBacked = true;
                fieldClassName = $"{fieldSymbol.Name.Replace('<', '_').Replace('>', '_')}MemberDetail";
            }

            var code = $$""""

                        public sealed class {{className}} : PrivateMemberDetailGenerationBase<{{parentTypeName}}, {{typeName}}>
                        {
                            public {{className}}(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

                            public override string Name => "{{memberName}}";

                            private readonly Type type = typeof({{typeName.Replace("?", "")}});
                            public override Type Type => type;

                            public override bool IsBacked => {{(isBacked ? "true" : "false")}};

                            public override IReadOnlyList<Attribute> Attributes => [];

                            protected override Func<MemberDetail<{{parentTypeName}}, {{typeName}}>?> CreateBackingFieldDetail => {{(fieldClassName is null ? "() => null" : $"() => new {fieldClassName}(locker, loadMemberInfo)")}};
                        }
                """";

            membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }
        private static void GeneratePublicMember(IFieldSymbol fieldSymbol, string parentTypeName, HashSet<IFieldSymbol> backingFields, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var memberName = fieldSymbol.Name;
            var className = $"{fieldSymbol.Name.Replace('<', '_').Replace('>', '_')}MemberDetail";
            var typeName = fieldSymbol.Type.ToString();

            var code = $$""""

                        public sealed class {{className}} : MemberDetailGenerationBase<{{parentTypeName}}, {{typeName}}>
                        {
                            public {{className}}(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

                            public override string Name => "{{memberName}}";

                            private readonly Type type = typeof({{typeName.Replace("?", "")}});
                            public override Type Type => type;

                            public override bool IsBacked => true;

                            public override Func<{{parentTypeName}}, {{typeName}}> Getter => (x) => x.{{memberName}};
                            public override bool HasGetter => true;

                            public override Action<{{parentTypeName}}, {{typeName}}> Setter => (x, value) => x.{{memberName}} = value;
                            public override bool HasSetter => true;

                            public override IReadOnlyList<Attribute> Attributes => [];

                            public override Func<object, object?> GetterBoxed => (x) => (({{parentTypeName}})x).{{memberName}};
                            public override bool HasGetterBoxed => true;

                            public override Action<object, object?> SetterBoxed => (x, value) => (({{parentTypeName}})x).{{memberName}} = ({{typeName}})value!;
                            public override bool HasSetterBoxed => true;

                            protected override Func<MemberDetail<{{parentTypeName}}, {{typeName}}>?> CreateBackingFieldDetail => () => null;
                        }
                """";

            if (!backingFields.Contains(fieldSymbol))
                membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }
        private static void GeneratePrivateMember(IFieldSymbol fieldSymbol, string parentTypeName, HashSet<IFieldSymbol> backingFields, List<string> membersToInitialize, StringBuilder sbChildClasses)
        {
            var memberName = fieldSymbol.Name;
            var className = $"{fieldSymbol.Name.Replace('<', '_').Replace('>', '_')}MemberDetail";
            var typeName = fieldSymbol.Type.ToString();

            var code = $$""""

                        public sealed class {{className}} : PrivateMemberDetailGenerationBase<{{parentTypeName}}, {{typeName}}>
                        {
                            public {{className}}(object locker, Action loadMemberInfo) : base(locker, loadMemberInfo) { }

                            public override string Name => "{{memberName}}";

                            private readonly Type type = typeof({{typeName.Replace("?", "")}});
                            public override Type Type => type;

                            public override bool IsBacked => true;

                            public override IReadOnlyList<Attribute> Attributes => [];

                            protected override Func<MemberDetail<{{parentTypeName}}, {{typeName}}>?> CreateBackingFieldDetail => () => null;
                        }
                """";

            if (!backingFields.Contains(fieldSymbol))
                membersToInitialize.Add(className);
            _ = sbChildClasses.Append(code);
        }

        private static string GetTypeOf(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol namedTypeSymbol)
            {
                var sb = new StringBuilder();
                GetTypeOf(namedTypeSymbol, sb);
                return sb.ToString();
            }
            return $"typeof({type.ContainingNamespace}.{type.Name})";
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
