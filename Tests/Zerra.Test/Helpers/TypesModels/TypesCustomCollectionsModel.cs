// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Test.Helpers.TypesModels
{
    [Zerra.SourceGeneration.GenerateTypeDetail]
    public class TypesCustomCollectionsModel
    {
        public CustomListGeneric ListGenericThing { get; set; }
        public CustomHashSetGeneric HashSetGenericThing { get; set; }
        public CustomDictionaryGeneric DictionaryGenericThing { get; set; }

        public static TypesCustomCollectionsModel Create()
        {
            var model = new TypesCustomCollectionsModel()
            {
                ListGenericThing = new() { "one", "two", "three" },
                HashSetGenericThing = new() { "one", "two", "three" },
                DictionaryGenericThing = new() { { "one", "uno" }, { "two", "dos" }, { "three", "tres" } }
            };
            return model;
        }

        public class CustomListGeneric : List<string> { }

        public class CustomHashSetGeneric : HashSet<string> { }

        public class CustomDictionaryGeneric : Dictionary<string, string> { }
    }
}