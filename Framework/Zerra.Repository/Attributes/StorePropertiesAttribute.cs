// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Specifies storage properties for a property or field, such as text encoding, date part, nullability, precision, and scale.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class StorePropertiesAttribute : Attribute
    {
        /// <summary>Gets the text encoding to use when storing string values.</summary>
        public StoreTextEncoding TextEncoding { get; }
        /// <summary>Gets the date part to use when storing date/time values.</summary>
        public StoreDatePart DatePart { get; }
        /// <summary>Gets a value indicating whether the stored value must not be null.</summary>
        public bool NotNull { get; }
        /// <summary>Gets the precision or maximum length of the stored value, if specified.</summary>
        public int? PrecisionLength { get; }
        /// <summary>Gets the scale of the stored numeric value, if specified.</summary>
        public int? Scale { get; }

        //attribute parameters cannot be null
        /// <summary>Initializes a new instance of <see cref="StorePropertiesAttribute"/> with a not-null constraint.</summary>
        /// <param name="notNull">Whether the value must not be null.</param>
        public StorePropertiesAttribute(bool notNull) : this(null, null, notNull, null, null) { }
        /// <summary>Initializes a new instance of <see cref="StorePropertiesAttribute"/> with a not-null constraint and precision length.</summary>
        /// <param name="notNull">Whether the value must not be null.</param>
        /// <param name="precisionLength">The precision or maximum length.</param>
        public StorePropertiesAttribute(bool notNull, int precisionLength) : this(null, null, notNull, precisionLength, null) { }
        /// <summary>Initializes a new instance of <see cref="StorePropertiesAttribute"/> with precision and scale.</summary>
        /// <param name="precisionLength">The precision or maximum length.</param>
        /// <param name="scale">The scale of the numeric value.</param>
        public StorePropertiesAttribute(int precisionLength, int scale) : this(null, null, false, precisionLength, scale) { }
        /// <summary>Initializes a new instance of <see cref="StorePropertiesAttribute"/> with a precision length.</summary>
        /// <param name="precisionLength">The precision or maximum length.</param>
        public StorePropertiesAttribute(int precisionLength) : this(null, null, false, precisionLength, null) { }
        /// <summary>Initializes a new instance of <see cref="StorePropertiesAttribute"/> with a text encoding.</summary>
        /// <param name="textEncoding">The text encoding to use.</param>
        public StorePropertiesAttribute(StoreTextEncoding textEncoding) : this(textEncoding, null, false, null, null) { }
        /// <summary>Initializes a new instance of <see cref="StorePropertiesAttribute"/> with a text encoding and precision length.</summary>
        /// <param name="textEncoding">The text encoding to use.</param>
        /// <param name="precisionLength">The precision or maximum length.</param>
        public StorePropertiesAttribute(StoreTextEncoding textEncoding, int precisionLength) : this(textEncoding, null, false, precisionLength, null) { }
        /// <summary>Initializes a new instance of <see cref="StorePropertiesAttribute"/> with a date part.</summary>
        /// <param name="date">The date part to use.</param>
        public StorePropertiesAttribute(StoreDatePart date) : this(null, date, false, null, null) { }
        /// <summary>Initializes a new instance of <see cref="StorePropertiesAttribute"/> with a date part and precision length.</summary>
        /// <param name="date">The date part to use.</param>
        /// <param name="precisionLength">The precision or maximum length.</param>
        public StorePropertiesAttribute(StoreDatePart date, int precisionLength) : this(null, date, false, precisionLength, null) { }
        /// <summary>Initializes a new instance of <see cref="StorePropertiesAttribute"/> with a not-null constraint, precision length, and scale.</summary>
        /// <param name="notNull">Whether the value must not be null.</param>
        /// <param name="precisionLength">The precision or maximum length.</param>
        /// <param name="scale">The scale of the numeric value.</param>
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
