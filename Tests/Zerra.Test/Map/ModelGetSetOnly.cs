// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Test.Map
{
    public class ModelGetSetOnly
    {
        public int PropA { get; set; }
        public string PropB { get; set; }

        public string PropABGet => PropA + PropB;

        private int propD;
        public int PropDSet { set => propD = value; }
        public int PropDGet => propD;
    }
}
