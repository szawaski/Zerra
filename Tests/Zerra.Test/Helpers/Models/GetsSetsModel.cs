// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Test.Helpers.Models
{
    [Zerra.SourceGeneration.GenerateTypeDetail]
    public class GetsSetsModel
    {
        private int getValue;
        private int setValue;

        public int GetOnly { get { return getValue + 1; } }
        public int SetOnly { set { setValue = value + 1; } }

        public GetsSetsModel()
        {
            getValue = default;
            setValue = default;
        }
        public GetsSetsModel(int getValue, int setValue)
        {
            this.getValue = getValue;
            this.setValue = setValue;
        }

        public string ToJsonString()
        {
            return $"{{\"GetOnly\":{getValue},\"SetOnly\":{setValue}}}";
        }
    }
}
