// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Zerra.SourceGeneration
{
    /// <summary>
    /// Provides utilities for discovering and collecting types, properties, methods, constructors, and required members during source generation.
    /// </summary>
    public static partial class TypeFinder
    {
        /// <summary>
        /// Discovers all model types reachable from the given type symbol and adds them to the provided dictionary.
        /// </summary>
        /// <param name="typeSymbol">The root type symbol to begin discovery from.</param>
        /// <param name="models">The dictionary to populate with discovered <see cref="TypeToGenerate"/> entries, keyed by full type name.</param>
        public static void FindModels(ITypeSymbol typeSymbol, Dictionary<string, TypeToGenerate> models)
        {
            if (typeSymbol.TypeKind != TypeKind.Class && typeSymbol.TypeKind != TypeKind.Interface)
                return;
            if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                return;

            var stack = new Stack<string>();
            if (Filter(namedTypeSymbol))
            {
                if (namedTypeSymbol.TypeKind == TypeKind.Interface)
                    SearchInterface(namedTypeSymbol, models, stack);
                else if (namedTypeSymbol.TypeKind == TypeKind.Class)
                    CheckModel(namedTypeSymbol, models, stack);
            }
        }
        private static bool Filter(INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.AllInterfaces.Any(x => x.ContainingNamespace.ToString() == "Zerra.CQRS" && (x.Name == "ICommand" || x.Name == "IEvent" || x.Name == "IQueryHandler" || x.Name == "ICommandHandler")))
                return true;
            if (!namedTypeSymbol.GetAttributes().Any(x => x.AttributeClass?.Name == "IgnoreGenerateTypeDetailAttribute"))
            {
                foreach (var attribute in namedTypeSymbol.GetAttributes())
                {
                    var attributeClass = attribute.AttributeClass;
                    while (attributeClass != null)
                    {
                        if (attributeClass.Name == "GenerateTypeDetailAttribute")
                            return true;
                        attributeClass = attributeClass.BaseType;
                    }
                }
            }
            if (Helper.FindBase("Zerra.Map", "MapDefinition", namedTypeSymbol) != null)
                return true;
            if (namedTypeSymbol.AllInterfaces.Any(x => x.Name == "IMapDefinition" && x.ContainingNamespace.ToString() == "Zerra.Map"))
                return true;
            return false;
        }

        private static void CheckModel(ITypeSymbol typeSymbol, Dictionary<string, TypeToGenerate> models, Stack<string> stack)
        {
            var name = Helper.GetFullName(typeSymbol);
            if (models.ContainsKey(name))
                return;
            var namedTypeSymbol = typeSymbol as INamedTypeSymbol;
            var arrayTypeSymbol = typeSymbol as IArrayTypeSymbol;
            if (namedTypeSymbol == null && arrayTypeSymbol == null)
                return;
            if (typeSymbol.DeclaredAccessibility != Accessibility.Public && typeSymbol.DeclaredAccessibility != Accessibility.Internal && typeSymbol.DeclaredAccessibility != Accessibility.NotApplicable)
                return;

            if (typeSymbol.TypeKind != TypeKind.Class && typeSymbol.TypeKind != TypeKind.Struct && typeSymbol.TypeKind != TypeKind.Enum
                && arrayTypeSymbol == null
                && (namedTypeSymbol == null || !namedTypeSymbol.IsGenericType))
            {
                return;
            }

            if (TypeLookup.SpecialTypeLookup(name, out var specialType))
            {
                if (specialType == SpecialType.Task || specialType == SpecialType.Dictionary)
                {
                    if (namedTypeSymbol != null)
                    {
                        foreach (var typeArg in namedTypeSymbol.TypeArguments)
                            CheckModel(typeArg, models, stack);
                    }
                }
                if (specialType != SpecialType.Dictionary && specialType != SpecialType.Object)
                    return;
            }
            else
            {
                SearchModel(typeSymbol, models, stack);
            }

            if (Helper.IsUnclosedGeneric(typeSymbol))
                return;
            if (models.ContainsKey(name))
                return;
            var stackString = String.Join(" - ", stack);
            models.Add(name, new TypeToGenerate(typeSymbol, name, stackString));
        }

        private static void SearchInterface(INamedTypeSymbol namedTypeSymbol, Dictionary<string, TypeToGenerate> models, Stack<string> stack)
        {
            var name = Helper.GetFullName(namedTypeSymbol);
            if (stack.Contains(name))
                return;

            var symbolMembers = namedTypeSymbol.GetMembers();
            (var properties, _) = GetPropertiesAndFields(namedTypeSymbol, symbolMembers);

            foreach (var property in properties)
            {
                stack.Push(name);
                CheckModel(property.PropertySymbol.Type, models, stack);
                _ = stack.Pop();
            }

            var methods = GetMethods(namedTypeSymbol, symbolMembers);
            foreach (var method in methods)
            {
                stack.Push(name);
                CheckModel(method.MethodSymbol.ReturnType, models, stack);
                _ = stack.Pop();
                foreach (var parameter in method.MethodSymbol.Parameters)
                {
                    stack.Push(name);
                    CheckModel(parameter.Type, models, stack);
                    _ = stack.Pop();
                }
            }

            foreach (var typeArg in namedTypeSymbol.TypeArguments)
            {
                stack.Push(name);
                CheckModel(typeArg, models, stack);
                _ = stack.Pop();
            }
        }
        private static void SearchModel(ITypeSymbol typeSymbol, Dictionary<string, TypeToGenerate> models, Stack<string> stack)
        {
            var name = Helper.GetFullName(typeSymbol);
            if (TypeLookup.CoreTypeLookup(name, out var coreType))
                return;
            if (stack.Contains(name))
                return;

            if (typeSymbol.Kind != SymbolKind.ArrayType)
            {
                var symbolMembers = typeSymbol.GetMembers();
                (var properties, var fields) = GetPropertiesAndFields(typeSymbol, symbolMembers);

                foreach (var @field in fields)
                {
                    stack.Push(name);
                    CheckModel(@field.Type, models, stack);
                    _ = stack.Pop();
                }
                foreach (var property in properties)
                {
                    stack.Push(name);
                    CheckModel(property.PropertySymbol.Type, models, stack);
                    _ = stack.Pop();
                }
            }

            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                foreach (var typeArg in namedTypeSymbol.TypeArguments)
                {
                    stack.Push(name);
                    CheckModel(typeArg, models, stack);
                    _ = stack.Pop();
                }
            }
            else if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                stack.Push(name);
                CheckModel(arrayTypeSymbol.ElementType, models, stack);
                _ = stack.Pop();
            }

            //MapDefinition will create empty base types which are unnecessary to process
            //var baseType = typeSymbol.BaseType;
            //while (baseType != null)
            //{
            //    stack.Push(name);
            //    CheckModel(baseType, models, stack);
            //    _ = stack.Pop();
            //    baseType = baseType.BaseType;
            //}

            //Helps find IMapDefinition Type Arguments
            foreach (var i in typeSymbol.AllInterfaces)
            {
                foreach (var typeArg in i.TypeArguments)
                {
                    stack.Push(name);
                    CheckModel(typeArg, models, stack);
                    _ = stack.Pop();
                }
            }
        }

        /// <summary>
        /// Retrieves all properties and fields for the given type, including those inherited from interfaces.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to inspect.</param>
        /// <param name="symbolMembers">The flattened member list for the type.</param>
        /// <returns>A tuple containing the list of discovered <see cref="FoundProperty"/> instances and the list of <see cref="IFieldSymbol"/> instances.</returns>
        public static (List<FoundProperty>, List<IFieldSymbol>) GetPropertiesAndFields(ITypeSymbol typeSymbol, ImmutableArray<ISymbol> symbolMembers)
        {
            var properties = symbolMembers.Where(x => x.Kind == SymbolKind.Property).Cast<IPropertySymbol>().Where(x => x.ExplicitInterfaceImplementations.Length == 0 && !x.IsIndexer).Select(x => new FoundProperty(x, x.Name, false)).ToList();
            var fields = symbolMembers.Where(x => x.Kind == SymbolKind.Field).Cast<IFieldSymbol>().ToList();

            if (typeSymbol.AllInterfaces.Length > 0)
            {
                var memberNames = new HashSet<string>();
                if (typeSymbol.TypeKind == TypeKind.Interface)
                {
                    foreach (var property in properties)
                        _ = memberNames.Add(property.Name);
                    foreach (var field in fields)
                        _ = memberNames.Add(field.Name);
                }

                foreach (var i in typeSymbol.AllInterfaces)
                {
                    var iMembers = i.GetMembers();
                    foreach (var iProperty in iMembers.Where(x => x.Kind == SymbolKind.Property && !x.IsStatic).Cast<IPropertySymbol>())
                    {
                        string memberName;
                        bool isExplicitFromInterface;
                        if (typeSymbol.TypeKind == TypeKind.Interface && !memberNames.Contains(iProperty.Name))
                        {
                            memberName = iProperty.Name;
                            isExplicitFromInterface = false;
                        }
                        else
                        {
                            memberName = $"{iProperty.ContainingType}.{iProperty.Name}";
                            isExplicitFromInterface = true;
                        }

                        if (!memberNames.Contains(memberName))
                        {
                            properties.Add(new FoundProperty(iProperty, memberName, isExplicitFromInterface));
                            _ = memberNames.Add(memberName);
                        }
                    }
                }
            }

            return (properties, fields);
        }
        /// <summary>
        /// Retrieves all public methods for the given type, including those inherited from interfaces.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to inspect.</param>
        /// <param name="symbolMembers">The flattened member list for the type.</param>
        /// <returns>A list of <see cref="FoundMethod"/> instances representing the discovered methods.</returns>
        public static List<FoundMethod> GetMethods(ITypeSymbol typeSymbol, ImmutableArray<ISymbol> symbolMembers)
        {
            var methods = symbolMembers.Where(x => x.Kind == SymbolKind.Method && x.DeclaredAccessibility == Accessibility.Public).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Ordinary || x.MethodKind == MethodKind.Destructor || x.MethodKind == MethodKind.PropertyGet || x.MethodKind == MethodKind.PropertySet || x.MethodKind == MethodKind.ExplicitInterfaceImplementation).Select(x => new FoundMethod(x, x.Name, false)).ToList();

            if (typeSymbol.AllInterfaces.Length > 0)
            {
                var methodNames = new HashSet<string>();
                foreach (var method in methods)
                    _ = methodNames.Add(method.Name);

                foreach (var i in typeSymbol.AllInterfaces)
                {
                    var iMethods = i.GetMembers().Where(x => x.Kind == SymbolKind.Method && !x.IsStatic).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Ordinary || x.MethodKind == MethodKind.Destructor || x.MethodKind == MethodKind.PropertyGet || x.MethodKind == MethodKind.PropertySet || x.MethodKind == MethodKind.ExplicitInterfaceImplementation).ToArray();
                    foreach (var iMethod in iMethods)
                    {
                        string methodName;
                        bool isExplicitFromInterface;
                        if (typeSymbol.TypeKind == TypeKind.Interface && !methodNames.Contains(iMethod.Name))
                        {
                            methodName = iMethod.Name;
                            isExplicitFromInterface = false;
                        }
                        else
                        {
                            methodName = $"{Helper.AdjustName(iMethod.ContainingType.ToString(), false)}.{Helper.AdjustName(iMethod.Name, true)}";
                            isExplicitFromInterface = true;
                        }

                        if (!methodNames.Contains(methodName))
                        {
                            methods.Add(new FoundMethod(iMethod, methodName, isExplicitFromInterface));
                            _ = methodNames.Add(methodName);
                        }
                    }
                }
            }

            return methods;
        }
        /// <summary>
        /// Retrieves all public constructors for the given type.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to inspect.</param>
        /// <param name="symbolMembers">The flattened member list for the type.</param>
        /// <returns>An array of <see cref="IMethodSymbol"/> representing the public constructors.</returns>
        public static IMethodSymbol[] GetConstructors(ITypeSymbol typeSymbol, ImmutableArray<ISymbol> symbolMembers)
        {
            var constructors = symbolMembers.Where(x => x.Kind == SymbolKind.Method && x.DeclaredAccessibility == Accessibility.Public).Cast<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Constructor).ToArray();
            return constructors;
        }
        /// <summary>
        /// Retrieves all required members from the given properties and fields.
        /// </summary>
        /// <param name="properties">The list of discovered properties to inspect.</param>
        /// <param name="fields">The list of discovered fields to inspect.</param>
        /// <returns>An array of <see cref="RequiredMembers"/> representing all required members.</returns>
        public static RequiredMembers[] GetRequiredMembers(List<FoundProperty> properties, List<IFieldSymbol> fields)
        {
            return properties.Where(x => x.IsExplicitFromInterface == false && x.PropertySymbol.IsRequired).Select(x => new RequiredMembers(x.PropertySymbol.Type, x.PropertySymbol.Name)).Concat(fields.Where(x => x.IsRequired).Select(x => new RequiredMembers( x.Type, x.Name))).ToArray();
        }
    }
}

