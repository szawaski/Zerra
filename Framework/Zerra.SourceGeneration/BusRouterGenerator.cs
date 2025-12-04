// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Zerra.SourceGeneration
{
    public static class BusRouterGenerator
    {
        public static void Generate(SourceProductionContext context, string ns, StringBuilder sbInitializer, ITypeSymbol typeSymbol)
        {
            if (typeSymbol.TypeKind != TypeKind.Interface)
                return;
            if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                return;

            if (namedTypeSymbol.AllInterfaces.Any(x => x.Name == "IQueryHandler"))
            {
                var typeNameForClass = Helper.GetClassSafeName(typeSymbol);
                var className = $"Caller_{typeNameForClass}";

                var sb = new StringBuilder();

                WriteMembers(namedTypeSymbol, namedTypeSymbol, sb);

                foreach (var i in namedTypeSymbol.AllInterfaces)
                    WriteMembers(namedTypeSymbol, i, sb);

                var membersLines = sb.ToString();
                _ = sb.Clear();

                var code = $$"""
                //Zerra Generated File

                namespace {{ns}}.SourceGeneration
                {
                    public class {{className}} : {{Helper.GetFullName(typeSymbol)}}
                    {        
                        private readonly Zerra.CQRS.IBusInternal bus;
                        private readonly string source;
                        public {{className}}(Zerra.CQRS.IBusInternal bus, string source)
                        {
                            this.bus = bus;
                            this.source = source;
                        }

                        {{membersLines}}
                    }
                }
                """;

                context.AddSource($"{className}.cs", SourceText.From(code, Encoding.UTF8));

                var typeNameForSymbol = Helper.GetTypeOfName(namedTypeSymbol);
                var fullClassName = $"{ns}.SourceGeneration.{className}";
                _ = sbInitializer.Append(Environment.NewLine).Append("            ");
                _ = sbInitializer.Append("global::Zerra.SourceGeneration.Register.Router(").Append(typeNameForSymbol).Append(", static (Zerra.CQRS.IBusInternal bus, string source) => new ").Append(fullClassName).Append("(bus, source)").Append(");");
            }
            if (namedTypeSymbol.AllInterfaces.Any(x => x.MetadataName == "ICommandHandler`2"))
            {
                Generate(namedTypeSymbol, sbInitializer);
                foreach (var i in namedTypeSymbol.AllInterfaces)
                    Generate(i, sbInitializer);
            }
        }

        private static void WriteMembers(INamedTypeSymbol parent, INamedTypeSymbol namedTypeSymbol, StringBuilder sb)
        {
            var members = namedTypeSymbol.GetMembers();

            var parentTypeOf = Helper.GetTypeOfName(parent);
            foreach (IMethodSymbol method in members.Where(x => x.Kind == SymbolKind.Method))
            {
                if (method.MethodKind != MethodKind.Ordinary)
                    continue;

                var isTask = method.ReturnType.Name == "Task";
                string? taskInnerTypeName = null;
                var typeOfTaskInnerType = "null";
                if (isTask)
                {
                    var returnType = (INamedTypeSymbol)method.ReturnType;
                    if (returnType.TypeArguments.Length == 1)
                    {
                        taskInnerTypeName = Helper.GetFullName(returnType.TypeArguments[0]);
                        typeOfTaskInnerType = Helper.GetTypeOfName(returnType.TypeArguments[0]);
                    }
                }

                if (sb.Length > 0)
                    _ = sb.Append(Environment.NewLine).Append("        ");

                _ = sb.Append("public ").Append(method.ReturnsVoid ? "void" : method.ReturnType.ToString()).Append(' ').Append(method.Name);
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
                        _ = sb.Append(genericParameter.Name);
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
                    _ = sb.Append(parameter.Type.ToString()).Append(" @").Append(parameter.Name);
                }
                _ = sb.Append(") ");

                var firstConstraintPassed = false;
                var constraints = new List<string>();
                foreach (var constraintType in method.TypeParameters)
                {
                    foreach (var c in constraintType.ConstraintTypes)
                    {
                        constraints.Add(c.ToString());
                    }
                    if (constraintType.HasConstructorConstraint)
                        constraints.Add("new()");
                    if (constraintType.HasReferenceTypeConstraint)
                        constraints.Add("class");
                    if (constraintType.HasValueTypeConstraint)
                        constraints.Add("struct");
                    if (constraintType.HasNotNullConstraint)
                        constraints.Add("notnull");
                    if (constraintType.HasUnmanagedTypeConstraint)
                        constraints.Add("unmanaged");
                    if (constraints.Count > 0)
                    {
                        _ = sb.Append("where ").Append(constraintType.Name).Append(" : ");
                        foreach (var c in constraints)
                        {
                            if (firstConstraintPassed)
                                _ = sb.Append(", ");
                            else
                                firstConstraintPassed = true;
                            _ = sb.Append(c);
                        }
                    }
                }
                _ = sb.Append(" => ");

                if (isTask)
                {
                    if (taskInnerTypeName is not null)
                        _ = sb.Append("bus._CallMethodTaskGeneric<").Append(taskInnerTypeName).Append(">(");
                    else
                        _ = sb.Append("bus._CallMethodTask(");
                }
                else
                {
                    _ = sb.Append("bus._CallMethod<").Append(method.ReturnsVoid ? "object" : method.ReturnType.ToString()).Append(">(");
                }

                _ = sb.Append(parentTypeOf).Append(", \"").Append(method.Name).Append("\", [");
                firstPassed = false;
                foreach (var parameter in method.Parameters)
                {
                    if (firstPassed)
                        _ = sb.Append(", ");
                    else
                        firstPassed = true;
                    _ = sb.Append('@').Append(parameter.Name);
                }
                _ = sb.Append("], source);");
            }

            foreach (IPropertySymbol property in members.Where(x => x.Kind == SymbolKind.Property))
            {
                if (sb.Length > 0)
                    _ = sb.Append(Environment.NewLine).Append("        ");

                _ = sb.Append("public ").Append(property.Type.ToString()).Append(" @").Append(property.Name).Append(" {");
                if (property.GetMethod is not null && property.GetMethod.DeclaredAccessibility == Accessibility.Public)
                    _ = sb.Append(" get => throw new System.NotSupportedException();");
                if (property.SetMethod is not null && property.SetMethod.DeclaredAccessibility == Accessibility.Public && !property.SetMethod.IsInitOnly)
                    _ = sb.Append(" set => throw new System.NotSupportedException();");
                _ = sb.Append(" }");
            }
        }

        private static void Generate(INamedTypeSymbol namedTypeSymbol, StringBuilder sb)
        {
            if (namedTypeSymbol.MetadataName != "ICommandHandler`2")
                return;

            var members = namedTypeSymbol.GetMembers();

            foreach (IMethodSymbol method in members.Where(x => x.Kind == SymbolKind.Method))
            {
                if (method.MethodKind != MethodKind.Ordinary)
                    continue;

                var commandType = method.Parameters[0].Type;
                var commandTypeName = Helper.GetFullName(commandType);
                var returnType = namedTypeSymbol.TypeArguments[1];
                _ = sb.Append(Environment.NewLine).Append("            ");
                _ = sb.Append("global::Zerra.SourceGeneration.Register.Router(typeof(").Append(commandTypeName).Append("), ");

                _ = sb.Append("static (global::Zerra.CQRS.IBusInternal bus, global::Zerra.CQRS.ICommand command, global::System.Type type, string source, global::System.Threading.CancellationToken cancellationToken) => ");

                _ = sb.Append("bus._DispatchCommandWithResultInternalAsync<").Append(Helper.GetFullName(returnType)).Append(">(");

                _ = sb.Append("(").Append(Helper.GetFullName(commandType)).Append(")command, type, source, cancellationToken), ");

                _ = sb.Append("static (object task) => ");
                var returnTypeName = Helper.GetFullName(method.ReturnType);
                _ = sb.Append("((").Append(returnTypeName).Append(")task).Result");

                _ = sb.Append(");");
            }
        }
    }
}
