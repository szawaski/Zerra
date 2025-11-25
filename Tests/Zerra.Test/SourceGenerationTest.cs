using Xunit;
using System;
using System.Collections.Generic;
using Zerra.Reflection.Compiletime;
using Zerra.Reflection.Runtime;

namespace Zerra.Test
{
    public class SourceGenerationTest
    {
        [Fact]
        public void ValidateTypeDetailsGenerated()
        {
            var type = typeof(TypesToGenerate);
            foreach (var property in type.GetProperties())
            {
                var generated = property.PropertyType.GetTypeDetail();
                var runtime = TypeDetailRuntime<object>.New(property.PropertyType);

                Assert.Equal(runtime.Type, generated.Type);
                Assert.Equal(runtime.IsNullable, generated.IsNullable);
                Assert.Equal(runtime.CoreType, generated.CoreType);
                Assert.Equal(runtime.SpecialType, generated.SpecialType);
                Assert.Equal(runtime.IsTask, generated.IsTask);
                Assert.Equal(runtime.EnumUnderlyingType, generated.EnumUnderlyingType);

                Assert.Equal(runtime.HasIEnumerable, generated.HasIEnumerable);
                Assert.Equal(runtime.HasIEnumerableGeneric, generated.HasIEnumerableGeneric);
                Assert.Equal(runtime.HasICollection, generated.HasICollection);
                Assert.Equal(runtime.HasICollectionGeneric, generated.HasICollectionGeneric);
                Assert.Equal(runtime.HasIReadOnlyCollectionGeneric, generated.HasIReadOnlyCollectionGeneric);
                Assert.Equal(runtime.HasIList, generated.HasIList);
                Assert.Equal(runtime.HasIListGeneric, generated.HasIListGeneric);
                Assert.Equal(runtime.HasIReadOnlyListGeneric, generated.HasIReadOnlyListGeneric);
                Assert.Equal(runtime.HasISetGeneric, generated.HasISetGeneric);
                Assert.Equal(runtime.HasIReadOnlySetGeneric, generated.HasIReadOnlySetGeneric);
                Assert.Equal(runtime.HasIDictionary, generated.HasIDictionary);
                Assert.Equal(runtime.HasIDictionaryGeneric, generated.HasIDictionaryGeneric);
                Assert.Equal(runtime.HasIReadOnlyDictionaryGeneric, generated.HasIReadOnlyDictionaryGeneric);

                Assert.Equal(runtime.IsIEnumerable, generated.IsIEnumerable);
                Assert.Equal(runtime.IsIEnumerableGeneric, generated.IsIEnumerableGeneric);
                Assert.Equal(runtime.IsICollection, generated.IsICollection);
                Assert.Equal(runtime.IsICollectionGeneric, generated.IsICollectionGeneric);
                Assert.Equal(runtime.IsIReadOnlyCollectionGeneric, generated.IsIReadOnlyCollectionGeneric);
                Assert.Equal(runtime.IsIList, generated.IsIList);
                Assert.Equal(runtime.IsIListGeneric, generated.IsIListGeneric);
                Assert.Equal(runtime.IsIReadOnlyListGeneric, generated.IsIReadOnlyListGeneric);
                Assert.Equal(runtime.IsISetGeneric, generated.IsISetGeneric);
                Assert.Equal(runtime.IsIReadOnlySetGeneric, generated.IsIReadOnlySetGeneric);
                Assert.Equal(runtime.IsIDictionary, generated.IsIDictionary);
                Assert.Equal(runtime.IsIDictionaryGeneric, generated.IsIDictionaryGeneric);
                Assert.Equal(runtime.IsIReadOnlyDictionaryGeneric, generated.IsIReadOnlyDictionaryGeneric);

                //Assert.Equal(runtime.HasCreatorBoxed, generated.HasCreatorBoxed);

                //CollectionsAreEqual(runtime.InnerTypes, generated.InnerTypes);
                //CollectionsAreEqual(runtime.BaseTypes, generated.BaseTypes);
                ////CollectionsAreEqual(runtime.Attributes, generated.Attributes);
                //CollectionsAreEqual(runtime.Interfaces.Select(x => x.Name), generated.Interfaces.Select(x => x.Name));
                //CollectionsAreEqual(runtime.MemberDetails.Select(x => x.Name), generated.MemberDetails.Select(x => x.Name));
                //CollectionsAreEqual(runtime.MethodDetailsBoxed.Select(x => $"{x.Name}({String.Join(", ", x.ParameterDetails.Select(x => x.Type?.Name ?? "?"))})"), generated.MethodDetailsBoxed.Select(x => $"{x.Name}({String.Join(", ", x.ParameterDetails.Select(x => x.Type?.Name ?? "?"))})"));
                //CollectionsAreEqual(runtime.ConstructorDetailsBoxed.Select(x => $"new ({String.Join(", ", x.ParameterDetails.Select(x => x.Type?.Name ?? "?"))})"), generated.ConstructorDetailsBoxed.Select(x => $"new ({String.Join(", ", x.ParameterDetails.Select(x => x.Type?.Name ?? "?"))})"));
            }
        }

        private static void CollectionsAreEqual(IEnumerable<object> c1, IEnumerable<object> c2)
        {
            var differences = new List<string>();
            foreach (var item1 in c1)
            {
                var found = false;
                foreach (var item2 in c2)
                {
                    if (item1.Equals(item2))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    differences.Add($"Left: {item1}");
                }
            }

            foreach (var item2 in c2)
            {
                var found = false;
                foreach (var item1 in c1)
                {
                    if (item2.Equals(item1))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    differences.Add($"Right: {item2}");
                }
            }

            var differencesString = String.Join(Environment.NewLine, differences);
            Assert.Equal(String.Empty, differencesString);
        }
    }
}
