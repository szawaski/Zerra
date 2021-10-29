// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository
{
    [AttributeUsage(AttributeTargets.Property)]
    public class StorePropertiesAttribute : Attribute
    {
        public bool NotNull { get; private set; }
        public int? PrecisionLength { get; private set; }
        public int? Scale { get; private set; }


        //attribute parameters cannot be null
        public StorePropertiesAttribute(bool notNull) : this(notNull, null, null) { }
        public StorePropertiesAttribute(bool notNull, int precisionLength) : this(notNull, precisionLength, null) { }
        public StorePropertiesAttribute(int precisionLength, int scale) : this(false, precisionLength, scale) { }
        public StorePropertiesAttribute(int precisionLength) : this(false, precisionLength, null) { }
        public StorePropertiesAttribute(bool notNull, int precisionLength, int scale) : this(false, (int?)precisionLength, scale) { }
        private StorePropertiesAttribute(bool notNull, int? precisionLength, int? scale)
        {
            this.NotNull = notNull;
            this.PrecisionLength = precisionLength;
            this.Scale = scale;
        }
    }
}
