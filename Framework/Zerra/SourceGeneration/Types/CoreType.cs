// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    /// <summary>
    /// These are the fundamental types supported by the Zerra framework.
    /// Includes primitive types, date/time types, and their nullable equivalents.
    /// Used by the source generator and runtime type analysis to provide efficient handling.
    /// </summary>
    public enum CoreType : byte
    {
        /// <summary>System.Boolean - 1-byte boolean value.</summary>
        Boolean,
        /// <summary>System.Byte - unsigned 8-bit integer.</summary>
        Byte,
        /// <summary>System.SByte - signed 8-bit integer.</summary>
        SByte,
        /// <summary>System.Int16 - signed 16-bit integer.</summary>
        Int16,
        /// <summary>System.UInt16 - unsigned 16-bit integer.</summary>
        UInt16,
        /// <summary>System.Int32 - signed 32-bit integer.</summary>
        Int32,
        /// <summary>System.UInt32 - unsigned 32-bit integer.</summary>
        UInt32,
        /// <summary>System.Int64 - signed 64-bit integer.</summary>
        Int64,
        /// <summary>System.UInt64 - unsigned 64-bit integer.</summary>
        UInt64,
        /// <summary>System.Single - 32-bit floating point number.</summary>
        Single,
        /// <summary>System.Double - 64-bit floating point number.</summary>
        Double,
        /// <summary>System.Decimal - 128-bit decimal number.</summary>
        Decimal,
        /// <summary>System.Char - single Unicode character.</summary>
        Char,
        /// <summary>System.DateTime - date and time value.</summary>
        DateTime,
        /// <summary>System.DateTimeOffset - date, time, and UTC offset value.</summary>
        DateTimeOffset,
        /// <summary>System.TimeSpan - time duration or interval.</summary>
        TimeSpan,
#if NET6_0_OR_GREATER
        /// <summary>System.DateOnly - date without time component (NET6.0 or greater).</summary>
        DateOnly,
        /// <summary>System.TimeOnly - time without date component (NET6.0 or greater).</summary>
        TimeOnly,
#endif
        /// <summary>System.Guid - globally unique identifier.</summary>
        Guid,

        /// <summary>System.Nullable&lt;System.Boolean&gt; - nullable boolean.</summary>
        BooleanNullable,
        /// <summary>System.Nullable&lt;System.Byte&gt; - nullable unsigned 8-bit integer.</summary>
        ByteNullable,
        /// <summary>System.Nullable&lt;System.SByte&gt; - nullable signed 8-bit integer.</summary>
        SByteNullable,
        /// <summary>System.Nullable&lt;System.Int16&gt; - nullable signed 16-bit integer.</summary>
        Int16Nullable,
        /// <summary>System.Nullable&lt;System.UInt16&gt; - nullable unsigned 16-bit integer.</summary>
        UInt16Nullable,
        /// <summary>System.Nullable&lt;System.Int32&gt; - nullable signed 32-bit integer.</summary>
        Int32Nullable,
        /// <summary>System.Nullable&lt;System.UInt32&gt; - nullable unsigned 32-bit integer.</summary>
        UInt32Nullable,
        /// <summary>System.Nullable&lt;System.Int64&gt; - nullable signed 64-bit integer.</summary>
        Int64Nullable,
        /// <summary>System.Nullable&lt;System.UInt64&gt; - nullable unsigned 64-bit integer.</summary>
        UInt64Nullable,
        /// <summary>System.Nullable&lt;System.Single&gt; - nullable 32-bit floating point number.</summary>
        SingleNullable,
        /// <summary>System.Nullable&lt;System.Double&gt; - nullable 64-bit floating point number.</summary>
        DoubleNullable,
        /// <summary>System.Nullable&lt;System.Decimal&gt; - nullable 128-bit decimal number.</summary>
        DecimalNullable,
        /// <summary>System.Nullable&lt;System.Char&gt; - nullable single Unicode character.</summary>
        CharNullable,
        /// <summary>System.Nullable&lt;System.DateTime&gt; - nullable date and time value.</summary>
        DateTimeNullable,
        /// <summary>System.Nullable&lt;System.DateTimeOffset&gt; - nullable date, time, and UTC offset value.</summary>
        DateTimeOffsetNullable,
        /// <summary>System.Nullable&lt;System.TimeSpan&gt; - nullable time duration or interval.</summary>
        TimeSpanNullable,
#if NET6_0_OR_GREATER
        /// <summary>System.Nullable&lt;System.DateOnly&gt; - nullable date without time component (NET6.0 or greater).</summary>
        DateOnlyNullable,
        /// <summary>System.Nullable&lt;System.TimeOnly&gt; - nullable time without date component (NET6.0 or greater).</summary>
        TimeOnlyNullable,
#endif
        /// <summary>System.Nullable&lt;System.Guid&gt; - nullable globally unique identifier.</summary>
        GuidNullable,

        /// <summary>System.String - sequence of Unicode characters.</summary>
        String
    }
}
