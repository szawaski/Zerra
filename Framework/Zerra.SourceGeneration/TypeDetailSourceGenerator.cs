using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            ).Where(x => x.DeclaredAccessibility == Accessibility.Public && !x.IsStatic && !x.IsAbstract && !x.IsValueType && x.ContainingType is null)
            .Collect();

            var compilationAndClasses = classProvider.Combine(context.CompilationProvider);

            context.RegisterSourceOutput(compilationAndClasses, (a, b) => Generate(a, b.Left, b.Right));
        }

        private static void Generate(SourceProductionContext context, ImmutableArray<INamedTypeSymbol> symbols, Compilation compilation)
        {
            foreach (var symbol in symbols.GroupBy(x => $"{x.ContainingNamespace}.{x.Name}"))
                GenerateType(context, symbol.First());
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

        private static void GenerateType(SourceProductionContext context, INamedTypeSymbol symbol)
        {
            var ns = symbol.ContainingNamespace.ToString().Contains("<global namespace>") ? null : symbol.ContainingNamespace.ToString();

            var typeName = symbol.ToString();

            string className;
            string fileName;
            if (symbol.TypeArguments.Length > 0)
            {
                var sbClassName = new StringBuilder();
                _ = sbClassName.Append(symbol.Name).Append("TypeDetail<");
                for (var i = 0; i < symbol.TypeArguments.Length; i++)
                {
                    if (i > 0)
                        _ = sbClassName.Append(',');
                    _ = sbClassName.Append(symbol.TypeArguments[i].Name);
                }
                _ = sbClassName.Append('>');
                className = sbClassName.ToString();
                fileName = ns == null ? $"{symbol.Name}TypeDetailT{symbol.TypeArguments.Length}.cs" : $"{ns}.{symbol.Name}TypeDetailT{symbol.TypeArguments.Length}.cs";
            }
            else
            {
                className = $"{symbol.Name}TypeDetail";
                fileName = ns == null ? $"{symbol.Name}TypeDetail.cs" : $"{ns}.{symbol.Name}TypeDetail.cs";
            }

            var isArray = symbol.Name.Contains('[');
            var interfaces = symbol.AllInterfaces.Select(x => x.Name).ToImmutableHashSet();

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

            var hasCreator = symbol.Constructors.Any(x => !x.IsStatic && x.Parameters.Length == 0);

            CoreType? coreType = null;
            if (TypeLookup.CoreTypeLookup(typeName, out var coreTypeParsed))
                coreType = coreTypeParsed;

            SpecialType? specialType = null;
            if (TypeLookup.SpecialTypeLookup(typeName, out var specialTypeParsed))
                specialType = specialTypeParsed;

            CoreEnumType? enumType = null;
            if (symbol.EnumUnderlyingType is not null && TypeLookup.CoreEnumTypeLookup(symbol.EnumUnderlyingType.Name, out var enumTypeParsed))
                enumType = enumTypeParsed;

            var innerType = symbol.TypeArguments.Length == 1 ? symbol.TypeArguments[0].Name : null;

            var isTask = specialType == SpecialType.Task;

            var sbBaseTypes = new StringBuilder();
            _ = sbBaseTypes.Append('[');
            var baseType = symbol.BaseType;
            while (baseType is not null)
            {
                if (sbBaseTypes.Length > 1)
                    sbBaseTypes.Append(", ");
                GetTypeOf(baseType, sbBaseTypes);
                baseType = baseType.BaseType;
            }
            _ = sbBaseTypes.Append(']');
            var baseTypes = sbBaseTypes.ToString();

            var sb = new StringBuilder();
            _ = sb.Append($$"""
                using System;
                using System.Collections.Generic;
                using System.Reflection;
                using System.Runtime.CompilerServices;
                using Zerra.Reflection;
                using Zerra.Reflection.Generation;
                {{(ns == null ? null : $"using {ns};")}}

                namespace {{(ns == null ? null : $"{ns}.")}}SourceGeneration
                {
                    public sealed class {{className}} : TypeDetailTGenerationBase<{{typeName}}>
                    {
                        //[ModuleInitializer]
                        //public static void Initialize() => TypeAnalyzer.InitializeTypeDetail(new {{className}}());
                
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

                        public override Func<{{typeName}}> Creator => () => {{(hasCreator ? $"new {typeName}()" : "throw new NotSupportedException()")}};
                        public override bool HasCreator => {{(hasCreator ? "true" : "false")}};

                        public override bool IsNullable => false;

                        public override CoreType? CoreType => {{(coreType.HasValue ? "CoreType." + coreType.Value.ToString() : "null")}};
                        public override SpecialType? SpecialType => {{(specialType.HasValue ? "SpecialType." + specialType.Value.ToString() : "null")}};
                        public override CoreEnumType? EnumUnderlyingType => {{(enumType.HasValue ? "CoreEnumType." + enumType.Value.ToString() : "null")}};

                        private readonly Type innerType = {{(innerType is null ? "null" : $"typeof({innerType})")}};
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

                        public override Func<object> CreatorBoxed => () => {{(hasCreator ? $"new {typeName}()" : "throw new NotSupportedException()")}};
                        public override bool HasCreatorBoxed => {{(hasCreator ? "true" : "false")}};

                        protected override Func<MethodDetail<{{typeName}}>[]> CreateMethodDetails => () => [];

                        protected override Func<ConstructorDetail<{{typeName}}>[]> CreateConstructorDetails => () => [];

                        protected override Func<MemberDetail[]> CreateMemberDetails => () => [];
                    }
                }
                """);

            var code = sb.ToString();
            context.AddSource(fileName, SourceText.From(code, Encoding.UTF8));
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
