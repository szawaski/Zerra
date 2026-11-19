// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Reflection;
using Xunit;
using Zerra.Reflection;
using Zerra.Reflection.Dynamic;

namespace Zerra.Test.Reflection
{
    //every type detail the source generator registered must match what reflection builds for the same type,
    //Zerra's own types are generated in both test projects and Zerra.Test.NetStandard generates its models too
    public class GeneratedReflectionParityTests
    {
        [Fact]
        public void GeneratedMatchesReflection()
        {
            var registered = (IEnumerable)typeof(TypeAnalyzer).GetField("byType", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            var details = new List<TypeDetail>();
            foreach (var item in registered)
                details.Add((TypeDetail)item.GetType().GetProperty("Value")!.GetValue(item)!);

            var methodsField = typeof(TypeDetail).GetField("methods", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var differences = new List<string>();

            foreach (var generated in details)
            {
                //methods are only generated where the framework uses them, otherwise they're read by reflection on first use
                var hasGeneratedMethods = methodsField.GetValue(generated) is not null;
                var reflected = TypeDetailGenerator.GenerateTypeDetail(generated.Type);
                var typeName = generated.Type.ToString();

                foreach (var field in typeof(TypeDetail).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                        continue;
                    var generatedValue = Describe(field.GetValue(generated), field.Name != nameof(TypeDetail.InnerTypes));
                    var reflectedValue = Describe(field.GetValue(reflected), field.Name != nameof(TypeDetail.InnerTypes));
                    if (generatedValue != reflectedValue)
                        differences.Add($"{typeName} {field.Name}: generated {generatedValue}, reflection {reflectedValue}");
                }
                if (Describe(generated.Attributes) != Describe(reflected.Attributes))
                    differences.Add($"{typeName} Attributes: generated {Describe(generated.Attributes)}, reflection {Describe(reflected.Attributes)}");

                var generatedMembers = generated.Members.ToDictionary(x => x.Name);
                var reflectedMembers = reflected.Members.ToDictionary(x => x.Name);
                foreach (var name in generatedMembers.Keys.Except(reflectedMembers.Keys))
                    differences.Add($"{typeName}.{name}: only generated");
                foreach (var name in reflectedMembers.Keys.Except(generatedMembers.Keys))
                    differences.Add($"{typeName}.{name}: only reflection");
                foreach (var name in generatedMembers.Keys.Intersect(reflectedMembers.Keys))
                {
                    var a = generatedMembers[name];
                    var b = reflectedMembers[name];
                    var describeA = $"{a.Type} field {a.IsField} static {a.IsStatic} get {a.HasGetter} set {a.HasSetter} {Describe(a.Attributes)}";
                    var describeB = $"{b.Type} field {b.IsField} static {b.IsStatic} get {b.HasGetter} set {b.HasSetter} {Describe(b.Attributes)}";
                    describeA += $" null {a.AllowsNull}";
                    describeB += $" null {b.AllowsNull}";
                    if (describeA != describeB)
                        differences.Add($"{typeName}.{name}: generated {describeA}, reflection {describeB}");
                }

                var generatedConstructors = generated.Constructors.Select(x => Describe(x)).OrderBy(x => x).ToArray();
                var reflectedConstructors = reflected.Constructors.Select(x => Describe(x)).OrderBy(x => x).ToArray();
                if (!generatedConstructors.SequenceEqual(reflectedConstructors))
                    differences.Add($"{typeName} constructors: generated [{String.Join("; ", generatedConstructors)}], reflection [{String.Join("; ", reflectedConstructors)}]");

                if (hasGeneratedMethods)
                {
                    var generatedMethods = generated.Methods.Select(x => Describe(x)).OrderBy(x => x).ToArray();
                    var reflectedMethods = reflected.Methods.Select(x => Describe(x)).OrderBy(x => x).ToArray();
                    if (!generatedMethods.SequenceEqual(reflectedMethods))
                        differences.Add($"{typeName} methods: generated [{String.Join("; ", generatedMethods)}], reflection [{String.Join("; ", reflectedMethods)}]");
                }
            }

            Assert.True(differences.Count == 0, $"{differences.Count} differences:{Environment.NewLine}{String.Join(Environment.NewLine, differences.Take(50))}");
        }


        //only inner types are in a set order, interfaces and attributes can come in any order
        private static string Describe(object? value, bool sort = true) => value switch
        {
            null => "null",
            string text => text,
            ConstructorDetail constructor => $"({String.Join(", ", constructor.Parameters.Select(x => $"{x.Type} {x.Name}"))})",
            MethodDetail method => $"{method.ReturnType} {method.Name}({String.Join(", ", method.Parameters.Select(x => $"{x.Type} {x.Name}"))})",
            IEnumerable<Attribute> attributes => String.Join(", ", attributes.Select(x => x.GetType().FullName).OrderBy(x => x)),
            IEnumerable items when sort => String.Join(", ", items.Cast<object?>().Select(x => x?.ToString()).OrderBy(x => x)),
            IEnumerable items => String.Join(", ", items.Cast<object?>().Select(x => x?.ToString())),
            _ => value.ToString() ?? "null"
        };
    }
}
