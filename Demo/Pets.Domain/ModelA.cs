// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Pets.Service
{
    public class ModelA
    {
        public int Prop1 { get; set; }
        public int PropA { get; set; }
        public int PropC { get; set; }

        public static ModelA GetModelA()
        {
            return new ModelA()
            {
                Prop1 = -5,
                PropA = 64,
                PropC = 128,
            };
        }
    }
}
