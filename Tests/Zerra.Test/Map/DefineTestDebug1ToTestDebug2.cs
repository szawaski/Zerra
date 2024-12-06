// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Map;

namespace Zerra.Test.Map
{
    public class DefineTestDebug1ToTestDebug2 : IMapDefinition<TestDebug1, TestDebug2>
    {
        public void Define(IMapSetup<TestDebug1, TestDebug2> map)
        {
            map.Define(x => x.Prop, x => x.Prop.Remove(10));
        }
    }
}
