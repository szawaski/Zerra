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
        private bool? notNull;
        /// <summary>
        /// Gets or sets a value indicating whether the stored value must not be null, such as <c>[StoreProperties(StoreTextEncoding.NonUnicode, 50, NotNull = true)]</c>.
        /// When it isn't set, the member's nullability decides: <c>string</c> and <c>int</c> can't be null, <c>string?</c> and <c>int?</c> can.
        /// </summary>
        public bool NotNull { get => notNull ?? false; set => notNull = value; }
        /// <summary>Gets a value indicating whether <see cref="NotNull"/> was set, by a constructor argument or by name.</summary>
        public bool IsNotNullSet => notNull.HasValue;
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
        public StorePropertiesAttribute(int precisionLength, int scale) : this(null, null, null, precisionLength, scale) { }
        /// <summary>Initializes a new instance of <see cref="StorePropertiesAttribute"/> with a precision length.</summary>
        /// <param name="precisionLength">The precision or maximum length.</param>
        public StorePropertiesAttribute(int precisionLength) : this(null, null, null, precisionLength, null) { }
        /// <summary>Initializes a new instance of <see cref="StorePropertiesAttribute"/> with a text encoding.</summary>
        /// <param name="textEncoding">The text encoding to use.</param>
        public StorePropertiesAttribute(StoreTextEncoding textEncoding) : this(textEncoding, null, null, null, null) { }
        /// <summary>Initializes a new instance of <see cref="StorePropertiesAttribute"/> with a text encoding and precision length.</summary>
        /// <param name="textEncoding">The text encoding to use.</param>
        /// <param name="precisionLength">The precision or maximum length.</param>
        public StorePropertiesAttribute(StoreTextEncoding textEncoding, int precisionLength) : this(textEncoding, null, null, precisionLength, null) { }
        /// <summary>Initializes a new instance of <see cref="StorePropertiesAttribute"/> with a date part.</summary>
        /// <param name="date">The date part to use.</param>
        public StorePropertiesAttribute(StoreDatePart date) : this(null, date, null, null, null) { }
        /// <summary>Initializes a new instance of <see cref="StorePropertiesAttribute"/> with a date part and precision length.</summary>
        /// <param name="date">The date part to use.</param>
        /// <param name="precisionLength">The precision or maximum length.</param>
        public StorePropertiesAttribute(StoreDatePart date, int precisionLength) : this(null, date, null, precisionLength, null) { }
        /// <summary>Initializes a new instance of <see cref="StorePropertiesAttribute"/> with a not-null constraint, precision length, and scale.</summary>
        /// <param name="notNull">Whether the value must not be null.</param>
        /// <param name="precisionLength">The precision or maximum length.</param>
        /// <param name="scale">The scale of the numeric value.</param>
        public StorePropertiesAttribute(bool notNull, int precisionLength, int scale) : this(null, null, notNull, (int?)precisionLength, scale) { }

        private StorePropertiesAttribute(StoreTextEncoding? textEncoding, StoreDatePart? date, bool? notNull, int? precisionLength, int? scale)
        {
            this.TextEncoding = textEncoding ?? StoreTextEncoding.Unicode;
            this.DatePart = date ?? StoreDatePart.DateTime;
            this.notNull = notNull;
            this.PrecisionLength = precisionLength;
            this.Scale = scale;
        }
    }
}
