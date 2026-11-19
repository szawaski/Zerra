// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;

namespace Zerra.SourceGeneration
{
    /// <summary>
    /// Finds another assembly's internal property accessors, which the compiler doesn't load from metadata without InternalsVisibleTo.
    /// A copy of the compilation that loads every member is made the first time one is needed.
    /// </summary>
    public sealed class HiddenAccessorLookup
    {
        private readonly Compilation compilation;
        private Compilation? allMembersCompilation;

        /// <summary>
        /// Creates a lookup for the compilation being generated.
        /// </summary>
        /// <param name="compilation">The compilation being generated.</param>
        public HiddenAccessorLookup(Compilation compilation)
        {
            this.compilation = compilation;
        }

        /// <summary>
        /// Finds the same property with its internal accessors loaded, or null when its assembly's internals are already visible or it isn't found.
        /// </summary>
        /// <param name="property">The property as the compilation sees it.</param>
        /// <returns>The property with every accessor, or null.</returns>
        public IPropertySymbol? Find(IPropertySymbol property)
        {
            var assembly = property.ContainingAssembly;
            if (assembly is null || SymbolEqualityComparer.Default.Equals(assembly, compilation.Assembly) || assembly.GivesAccessTo(compilation.Assembly))
                return null;

            allMembersCompilation ??= compilation.WithOptions(compilation.Options.WithMetadataImportOptions(MetadataImportOptions.All));

            IAssemblySymbol? allMembersAssembly = null;
            foreach (var referenced in allMembersCompilation.SourceModule.ReferencedAssemblySymbols)
            {
                if (referenced.Identity.Equals(assembly.Identity))
                {
                    allMembersAssembly = referenced;
                    break;
                }
            }
            if (allMembersAssembly is null)
                return null;

            var type = allMembersAssembly.GetTypeByMetadataName(GetMetadataName(property.ContainingType));
            if (type is null)
                return null;

            foreach (var member in type.GetMembers(property.Name))
            {
                if (member is IPropertySymbol found && !found.IsIndexer && found.IsStatic == property.IsStatic)
                    return found;
            }
            return null;
        }

        private static string GetMetadataName(INamedTypeSymbol type)
        {
            if (type.ContainingType is not null)
                return $"{GetMetadataName(type.ContainingType)}+{type.MetadataName}";
            if (type.ContainingNamespace is null || type.ContainingNamespace.IsGlobalNamespace)
                return type.MetadataName;
            return $"{type.ContainingNamespace.ToDisplayString()}.{type.MetadataName}";
        }
    }
}
