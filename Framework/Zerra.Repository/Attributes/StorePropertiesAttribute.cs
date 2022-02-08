// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository
{
    [AttributeUsage(AttributeTargets.Property)]
    public class StorePropertiesAttribute : Attribute
    {
        public StoreTextEncodingOption TextEncoding { get; private set; }
        public bool NotNull { get; private set; }
        public int? PrecisionLength { get; private set; }
        public int? Scale { get; private set; }

        //attribute parameters cannot be null
        public StorePropertiesAttribute(bool notNull) : this(null, notNull, null, null) { }
        public StorePropertiesAttribute(bool notNull, int precisionLength) : this(null, notNull, precisionLength, null) { }
        public StorePropertiesAttribute(int precisionLength, int scale) : this(null, false, precisionLength, scale) { }
        public StorePropertiesAttribute(int precisionLength) : this(null, false, precisionLength, null) { }
        public StorePropertiesAttribute(StoreTextEncodingOption textEncoding) : this(textEncoding, false, null, null) { }
        public StorePropertiesAttribute(StoreTextEncodingOption textEncoding, int precisionLength) : this(textEncoding, false, precisionLength, null) { }
        public StorePropertiesAttribute(bool notNull, int precisionLength, int scale) : this(null, notNull, (int?)precisionLength, scale) { }
        private StorePropertiesAttribute(StoreTextEncodingOption? textEncoding, bool notNull, int? precisionLength, int? scale)
        {
            this.TextEncoding = textEncoding ?? StoreTextEncodingOption.NonUnicode;
            this.NotNull = notNull;
            this.PrecisionLength = precisionLength;
            this.Scale = scale;
        }
    }

    public enum StoreTextEncodingOption
    {
        Unicode,
        NonUnicode
    }
}
