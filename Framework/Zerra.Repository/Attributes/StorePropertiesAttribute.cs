// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class StorePropertiesAttribute : Attribute
    {
        public StoreTextEncoding TextEncoding { get; }
        public StoreDatePart DatePart { get; }
        public bool NotNull { get; }
        public int? PrecisionLength { get; }
        public int? Scale { get; }

        //attribute parameters cannot be null
        public StorePropertiesAttribute(bool notNull) : this(null, null, notNull, null, null) { }
        public StorePropertiesAttribute(bool notNull, int precisionLength) : this(null, null, notNull, precisionLength, null) { }
        public StorePropertiesAttribute(int precisionLength, int scale) : this(null, null, false, precisionLength, scale) { }
        public StorePropertiesAttribute(int precisionLength) : this(null, null, false, precisionLength, null) { }
        public StorePropertiesAttribute(StoreTextEncoding textEncoding) : this(textEncoding, null, false, null, null) { }
        public StorePropertiesAttribute(StoreTextEncoding textEncoding, int precisionLength) : this(textEncoding, null, false, precisionLength, null) { }
        public StorePropertiesAttribute(StoreDatePart date) : this(null, date, false, null, null) { }
        public StorePropertiesAttribute(StoreDatePart date, int precisionLength) : this(null, date, false, precisionLength, null) { }
        public StorePropertiesAttribute(bool notNull, int precisionLength, int scale) : this(null, null, notNull, (int?)precisionLength, scale) { }

        private StorePropertiesAttribute(StoreTextEncoding? textEncoding, StoreDatePart? date, bool notNull, int? precisionLength, int? scale)
        {
            this.TextEncoding = textEncoding ?? StoreTextEncoding.Unicode;
            this.DatePart = date ?? StoreDatePart.DateTime;
            this.NotNull = notNull;
            this.PrecisionLength = precisionLength;
            this.Scale = scale;
        }
    }
}
