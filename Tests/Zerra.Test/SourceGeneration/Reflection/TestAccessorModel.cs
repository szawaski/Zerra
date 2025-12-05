// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Test.SourceGeneration.Reflection
{
    public class TestAccessorModel
    {
        public int IntProperty { get; set; }
        public string StringProperty { get; set; } = string.Empty;
        public bool BoolProperty { get; set; }
        public double DoubleProperty { get; set; }
        public int? NullableIntProperty { get; set; }

        public int IntField;
        public string StringField = string.Empty;
        public bool BoolField;
        public double DoubleField;
        public int? NullableIntField;

        private int _privateIntProperty;
        public int PrivateIntProperty
        {
            get { return _privateIntProperty; }
            set { _privateIntProperty = value; }
        }

        public int ReadOnlyProperty { get; private set; } = 0;

        public TestAccessorModel()
        {
        }

        public TestAccessorModel(int intValue, string stringValue)
        {
            IntProperty = intValue;
            StringProperty = stringValue;
        }

        public int GetIntValue()
        {
            return IntProperty;
        }

        public void SetIntValue(int value)
        {
            IntProperty = value;
        }

        public string ConcatenateProperties()
        {
            return $"{IntProperty}:{StringProperty}";
        }

        public int AddNumbers(int a, int b)
        {
            return a + b;
        }

        public string? GetNullableString(string? input)
        {
            return input;
        }
    }

    public struct TestAccessorStruct
    {
        public int IntProperty { get; set; }
        public string StringProperty { get; set; }
    }

    public class TestAccessorVirtualModel
    {
        public virtual int VirtualProperty { get; set; }

        public virtual int GetValue()
        {
            return 42;
        }
    }
}
