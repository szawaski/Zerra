// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.TestDev
{
    public class NormalJsonModel
    {
        public string String1 { get; set; }
        public string String2 { get; set; }
        public string String3 { get; set; }
        public string String4 { get; set; }
        public string String5 { get; set; }
        public string String6 { get; set; }
        public string String7 { get; set; }
        public string String8 { get; set; }
        public string String9 { get; set; }
        public string String0 { get; set; }

        public decimal Value1 { get; set; }
        public decimal Value2 { get; set; }
        public decimal Value3 { get; set; }

        public bool Flag1 { get; set; }
        public bool Flag2 { get; set; }
        public bool Flag3 { get; set; }

        public SubNormalJsonModel[] SubModels1 { get; set; }
        public SubNormalJsonModel[] SubModels2 { get; set; }
        public SubNormalJsonModel[] SubModels3 { get; set; }

        public static NormalJsonModel Create()
        {
            return new NormalJsonModel()
            {
                String1 = "s\"sdfk\"l;ajsdflkajsdf",
                String2 = "WERQEFASDFAasdfaf",
                String3 = "gSDGASDFASDFAGWE",
                String4 = "asgahreaefbadfvadfasasdfaweasdvasdawefasdvasdfawefasdvawefasdvasdawefasdvavergaergafasdf",
                String5 = "@#$)(@*)(#%#mamsidlfja",
                String6 = "askdfjav0memamsidlfja",
                String7 = "34rqevasdvasdv",
                String8 = "askdfjav0memamsidlfja",
                String9 = "vasdfa as dfa34",
                String0 = "3asdabasdfgda",

                Value1 = 123.45m,
                Value2 = 0.123515m,
                Value3 = 123412123.5m,

                Flag1 = true,
                Flag2 = false,
                Flag3 = true,

                SubModels1 = [SubNormalJsonModel.Create()],
                SubModels2 = [SubNormalJsonModel.Create(), SubNormalJsonModel.Create()],
                SubModels3 = [SubNormalJsonModel.Create(), SubNormalJsonModel.Create(), SubNormalJsonModel.Create(), SubNormalJsonModel.Create(), SubNormalJsonModel.Create(), SubNormalJsonModel.Create(), SubNormalJsonModel.Create(), SubNormalJsonModel.Create(), SubNormalJsonModel.Create(), SubNormalJsonModel.Create()]
            };
        }

        public class SubNormalJsonModel
        {
            public string SubString1 { get; set; }
            public string SubString2 { get; set; }
            public string SubString3 { get; set; }
            public string SubString4 { get; set; }
            public string SubString5 { get; set; }
            public string SubString6 { get; set; }
            public string SubString7 { get; set; }
            public string SubString8 { get; set; }
            public string SubString9 { get; set; }
            public string SubString0 { get; set; }

            public decimal SubValue1 { get; set; }
            public decimal SubValue2 { get; set; }
            public decimal SubValue3 { get; set; }

            public bool SubFlag1 { get; set; }
            public bool SubFlag2 { get; set; }
            public bool SubFlag3 { get; set; }

            public static SubNormalJsonModel Create()
            {
                return new SubNormalJsonModel()
                {
                    SubString1 = "ssdfkl;ajsdflkajsdf",
                    SubString2 = "WERQEFASDFAasdfaf",
                    SubString3 = "gSDGASDFASDFAGWE",
                    SubString4 = "asgahreaefbadfvadfasasdfaweasdvasdawefasdvasdfawefasdvawefasdvasdawefasdvavergaergafasdf",
                    SubString5 = "@#$)(@*)(#%#mamsidlfja",
                    SubString6 = "askdfjav0memamsidlfja",
                    SubString7 = "34rqevasdvasdv",
                    SubString8 = "askdfjav0memamsidlfja",
                    SubString9 = "vasdfa as dfa34",
                    SubString0 = "3asdabasdfgda",

                    SubValue1 = 123.45m,
                    SubValue2 = 0.123515m,
                    SubValue3 = 123412123.5m,

                    SubFlag1 = true,
                    SubFlag2 = false,
                    SubFlag3 = true
                };
            }
        }
    }
}