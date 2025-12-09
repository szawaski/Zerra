// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Text.RegularExpressions;

namespace Zerra.SourceGeneration.Discovery
{
    public static class EmptyImplementationGenerator
    {
        public static void Generate(SourceProductionContext context, string ns, StringBuilder sbInitializer, Dictionary<string, TypeToGenerate> models)
        {
            foreach (var model in models.Values)
            {
                if (model.TypeSymbol.TypeKind != TypeKind.Interface)
                    continue;
                if (model.TypeSymbol is not INamedTypeSymbol namedTypeSymbol)
                    continue;
                if (namedTypeSymbol.IsGenericType && namedTypeSymbol.TypeArguments.Any(x => x.BaseType == null))
                    continue;
                if (namedTypeSymbol.AllInterfaces.Any(x => x.Name == "IEnumerable"))
                    continue;

                var typeNameForClass = Helper.GetClassSafeName(model.TypeSymbol);
                var className = $"Empty_{typeNameForClass}";
                var typeName = namedTypeSymbol.IsGenericType
                    ? Regex.Replace(className, @"<[^>]+>", m => "<" + string.Concat(Enumerable.Repeat(",", m.Value.Count(c => c == ','))) + ">")
                    : className;

                string? where = null;
                if (namedTypeSymbol.IsGenericType)
                {
                    var sbWhere = new StringBuilder();
                    foreach (var genericParameter in namedTypeSymbol.TypeParameters)
                    {
                        var constraints = new List<string>();
                        foreach (var constraintType in genericParameter.ConstraintTypes)
                            constraints.Add(Helper.GetFullName(constraintType));
                        if (genericParameter.HasConstructorConstraint)
                            constraints.Add("new()");
                        if (genericParameter.HasReferenceTypeConstraint)
                            constraints.Add("class");
                        if (genericParameter.HasValueTypeConstraint)
                            constraints.Add("struct");
                        if (genericParameter.HasNotNullConstraint)
                            constraints.Add("notnull");
                        if (genericParameter.HasUnmanagedTypeConstraint)
                            constraints.Add("unmanaged");

                        if (constraints.Count == 0)
                            continue;

                        _ = sbWhere.Append(" where ").Append(Helper.GetFullName(genericParameter)).Append(" : ");
                        _ = sbWhere.Append(string.Join(", ", constraints));
                    }
                    where = sbWhere.ToString();
                }

                var sb = new StringBuilder();

                WriteMembers(namedTypeSymbol, sb);

                foreach (var i in namedTypeSymbol.AllInterfaces)
                    WriteMembers(i, sb);

                var membersLines = sb.ToString();

                var code = $$"""
                //Zerra Generated File

                namespace {{ns}}.SourceGeneration
                {
                    public class {{className}} : {{Helper.GetFullName(model.TypeSymbol)}}{{where}}
                    {
                        {{membersLines}}
                    }
                }
            
                """;

                context.AddSource($"{className}.cs", SourceText.From(code, Encoding.UTF8));

                var interfacefullTypeOf = Helper.GetTypeOfName(model.TypeSymbol);
                var classFullTypeOf = $"typeof({className})";
                _ = sbInitializer.Append(Environment.NewLine).Append("            ");
                _ = sbInitializer.Append("global::Zerra.Reflection.Register.EmptyImplementation(").Append(interfacefullTypeOf).Append(", ").Append(classFullTypeOf).Append(");");
            }
        }

        private static void WriteMembers(INamedTypeSymbol namedTypeSymbol, StringBuilder sb)
        {
            var members = namedTypeSymbol.GetMembers();

            foreach (IMethodSymbol method in members.Where(x => x.Kind == SymbolKind.Method))
            {
                if (method.MethodKind != MethodKind.Ordinary)
                    continue;

                if (sb.Length > 0)
                    _ = sb.Append(Environment.NewLine).Append("        ");

                _ = sb.Append(method.ReturnsVoid ? "void" : Helper.GetFullName(method.ReturnType)).Append(' ');
                _ = sb.Append(Helper.GetFullName(method.ContainingType)).Append('.').Append(method.Name);

                if (method.IsGenericMethod)
                {
                    _ = sb.Append('<');
                    var firstGenericPassed = false;
                    foreach (var genericParameter in method.TypeParameters)
                    {
                        if (firstGenericPassed)
                            _ = sb.Append(", ");
                        else
                            firstGenericPassed = true;
                        _ = sb.Append(Helper.GetFullName(genericParameter));
                    }
                    _ = sb.Append('>');
                }

                _ = sb.Append('(');
                var firstPassed = false;
                foreach (var parameter in method.Parameters)
                {
                    if (firstPassed)
                        _ = sb.Append(", ");
                    else
                        firstPassed = true;

                    _ = sb.Append(Helper.GetFullName(parameter.Type)).Append(" @").Append(parameter.Name);
                }
                _ = sb.Append(')');
                if (method.ReturnsVoid)
                {
                    _ = sb.Append(" { }");
                }
                else
                {
                    string returnValue;
                    if (method.ReturnType.Name == "Task")
                    {
                        var namedReturnType = (INamedTypeSymbol)method.ReturnType;
                        if (namedReturnType.TypeParameters.Length > 0)
                            returnValue = $"System.Threading.Tasks.Task.FromResult(default({namedReturnType.TypeArguments[0].ToString()}))";
                        else
                            returnValue = "System.Threading.Tasks.Task.CompletedTask";
                    }
                    else
                    {
                        returnValue = $"default({method.ReturnType.ToString()})";
                    }

                    _ = sb.Append(" => ").Append(returnValue).Append("!;");
                }
            }

            foreach (IPropertySymbol property in members.Where(x => x.Kind == SymbolKind.Property))
            {
                if (sb.Length > 0)
                    _ = sb.Append(Environment.NewLine).Append("        ");

                _ = sb.Append(Helper.GetFullName(property.Type)).Append(" ");
                _ = sb.Append(Helper.GetFullName(property.ContainingType)).Append('.');
                if (property.IsIndexer)
                {
                    _ = sb.Append("this[");
                    var firstPassed = false;
                    foreach (var parameter in property.Parameters)
                    {
                        if (firstPassed)
                            _ = sb.Append(", ");
                        else
                            firstPassed = true;
                        _ = sb.Append(Helper.GetFullName(parameter.Type)).Append(" @").Append(parameter.Name);
                    }
                    _ = sb.Append("]");
                    _ = sb.Append(" { get => throw new global::System.NotImplementedException(); set => throw new global::System.NotImplementedException(); }");
                }
                else
                {
                    _ = sb.Append(property.Name);
                    _ = sb.Append(" {");
                    if (property.GetMethod is not null && property.GetMethod.DeclaredAccessibility == Accessibility.Public)
                        _ = sb.Append(" get;");
                    if (property.SetMethod is not null && property.SetMethod.DeclaredAccessibility == Accessibility.Public && !property.SetMethod.IsInitOnly)
                        _ = sb.Append(" set;");
                    _ = sb.Append(" }");
                }
            }
        }
    }
}
