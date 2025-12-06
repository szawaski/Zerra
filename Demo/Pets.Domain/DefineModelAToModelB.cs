// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Map;

namespace Pets.Service
{
    public sealed class DefineModelAToModelB : MapDefinition<ModelA, ModelB>
    {
        public override sealed void Define(IMapSetup<ModelA, ModelB> map)
        {
            map.Define(a => a.PropB, b => Int32.Parse(b.PropA.ToString() + "1"));
            map.DefineTwoWay(a => a.PropD, b => b.PropC);
        }
    }
}
