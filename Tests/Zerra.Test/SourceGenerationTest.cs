using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Zerra.Reflection.Compiletime;
using Zerra.Reflection.Runtime;

namespace Zerra.Test
{
    [TestClass]
    public class SourceGenerationTest
    {
        [TestMethod]
        public void ValidateTypeDetailsGenerated()
        {
            var type = typeof(TypesToGenerate);
            foreach (var property in type.GetProperties())
            {
                var generated = property.PropertyType.GetTypeDetail();
                var runtime = TypeDetailRuntime<object>.New(property.PropertyType);

                Assert.AreEqual(runtime.Type, generated.Type);
                Assert.AreEqual(runtime.IsNullable, generated.IsNullable);
                Assert.AreEqual(runtime.CoreType, generated.CoreType);
                Assert.AreEqual(runtime.SpecialType, generated.SpecialType);
                Assert.AreEqual(runtime.IsTask, generated.IsTask);
                Assert.AreEqual(runtime.EnumUnderlyingType, generated.EnumUnderlyingType);

                Assert.AreEqual(runtime.HasIEnumerable, generated.HasIEnumerable);
                Assert.AreEqual(runtime.HasIEnumerableGeneric, generated.HasIEnumerableGeneric);
                Assert.AreEqual(runtime.HasICollection, generated.HasICollection);
                Assert.AreEqual(runtime.HasICollectionGeneric, generated.HasICollectionGeneric);
                Assert.AreEqual(runtime.HasIReadOnlyCollectionGeneric, generated.HasIReadOnlyCollectionGeneric);
                Assert.AreEqual(runtime.HasIList, generated.HasIList);
                Assert.AreEqual(runtime.HasIListGeneric, generated.HasIListGeneric);
                Assert.AreEqual(runtime.HasIReadOnlyListGeneric, generated.HasIReadOnlyListGeneric);
                Assert.AreEqual(runtime.HasISetGeneric, generated.HasISetGeneric);
                Assert.AreEqual(runtime.HasIReadOnlySetGeneric, generated.HasIReadOnlySetGeneric);
                Assert.AreEqual(runtime.HasIDictionary, generated.HasIDictionary);
                Assert.AreEqual(runtime.HasIDictionaryGeneric, generated.HasIDictionaryGeneric);
                Assert.AreEqual(runtime.HasIReadOnlyDictionaryGeneric, generated.HasIReadOnlyDictionaryGeneric);

                Assert.AreEqual(runtime.IsIEnumerable, generated.IsIEnumerable);
                Assert.AreEqual(runtime.IsIEnumerableGeneric, generated.IsIEnumerableGeneric);
                Assert.AreEqual(runtime.IsICollection, generated.IsICollection);
                Assert.AreEqual(runtime.IsICollectionGeneric, generated.IsICollectionGeneric);
                Assert.AreEqual(runtime.IsIReadOnlyCollectionGeneric, generated.IsIReadOnlyCollectionGeneric);
                Assert.AreEqual(runtime.IsIList, generated.IsIList);
                Assert.AreEqual(runtime.IsIListGeneric, generated.IsIListGeneric);
                Assert.AreEqual(runtime.IsIReadOnlyListGeneric, generated.IsIReadOnlyListGeneric);
                Assert.AreEqual(runtime.IsISetGeneric, generated.IsISetGeneric);
                Assert.AreEqual(runtime.IsIReadOnlySetGeneric, generated.IsIReadOnlySetGeneric);
                Assert.AreEqual(runtime.IsIDictionary, generated.IsIDictionary);
                Assert.AreEqual(runtime.IsIDictionaryGeneric, generated.IsIDictionaryGeneric);
                Assert.AreEqual(runtime.IsIReadOnlyDictionaryGeneric, generated.IsIReadOnlyDictionaryGeneric);

                //Assert.AreEqual(runtime.HasCreatorBoxed, generated.HasCreatorBoxed);

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
            Assert.AreEqual(String.Empty, differencesString);
        }
    }
}
