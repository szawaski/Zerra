// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Map;

namespace Zerra.Test.Map
{
    public class DefineModelAToModelB : IMapDefinition<ModelA, ModelB>
    {
        public void Define(IMapSetup<ModelA, ModelB> map)
        {
            map.Define(a => a.PropB, b => Int32.Parse(b.PropA.ToString() + "1"));
            map.DefineTwoWay(a => a.PropD, b => b.PropC);
        }
    }

}
