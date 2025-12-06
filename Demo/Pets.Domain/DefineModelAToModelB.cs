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
            map.Define(b => b.PropB, a => Int32.Parse(a.PropA.ToString() + "1"));
            map.DefineReverse(a => a.PropA, b => Int32.Parse(b.PropB.ToString().TrimEnd('1')));
            map.DefineTwoWay(b => b.PropD, a => a.PropC);
        }
    }
}
