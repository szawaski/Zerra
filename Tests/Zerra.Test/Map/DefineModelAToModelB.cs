// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Test.Map
{
    public class DefineModelAToModelB : IMapDefinition<ModelA, ModelB>
    {
        public void Define(IMapSetup<ModelA, ModelB> map)
        {
            map.Define(x => x.PropB, x => Int32.Parse(x.PropA.ToString() + "1"));
            map.DefineTwoWay(x => x.PropD, x => x.PropC);
        }
    }

}
