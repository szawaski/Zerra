// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.SourceGeneration;

namespace Pets.Service
{
    public class ModelA
    {
        public int PropA { get; set; }
        public int PropC { get; set; }

        public static ModelA GetModelA()
        {
            return new ModelA()
            {
                PropA = 64,
                PropC = 128,
            };
        }
    }
}
