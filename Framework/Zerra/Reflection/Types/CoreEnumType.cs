// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Reflection
{
    /// <summary>
    /// Enumeration of integral types supported as enum underlying types in the Zerra framework.
    /// Includes signed and unsigned integer types (byte through long) and their nullable equivalents.
    /// Used by the source generator and runtime type analysis to provide efficient handling.
    /// </summary>
    public enum CoreEnumType : byte
    {
        /// <summary>System.Byte - unsigned 8-bit integer enum underlying type.</summary>
        Byte,
        /// <summary>System.SByte - signed 8-bit integer enum underlying type.</summary>
        SByte,
        /// <summary>System.Int16 - signed 16-bit integer enum underlying type.</summary>
        Int16,
        /// <summary>System.UInt16 - unsigned 16-bit integer enum underlying type.</summary>
        UInt16,
        /// <summary>System.Int32 - signed 32-bit integer enum underlying type.</summary>
        Int32,
        /// <summary>System.UInt32 - unsigned 32-bit integer enum underlying type.</summary>
        UInt32,
        /// <summary>System.Int64 - signed 64-bit integer enum underlying type.</summary>
        Int64,
        /// <summary>System.UInt64 - unsigned 64-bit integer enum underlying type.</summary>
        UInt64,

        /// <summary>System.Nullable&lt;System.Byte&gt; - nullable unsigned 8-bit integer enum underlying type.</summary>
        ByteNullable,
        /// <summary>System.Nullable&lt;System.SByte&gt; - nullable signed 8-bit integer enum underlying type.</summary>
        SByteNullable,
        /// <summary>System.Nullable&lt;System.Int16&gt; - nullable signed 16-bit integer enum underlying type.</summary>
        Int16Nullable,
        /// <summary>System.Nullable&lt;System.UInt16&gt; - nullable unsigned 16-bit integer enum underlying type.</summary>
        UInt16Nullable,
        /// <summary>System.Nullable&lt;System.Int32&gt; - nullable signed 32-bit integer enum underlying type.</summary>
        Int32Nullable,
        /// <summary>System.Nullable&lt;System.UInt32&gt; - nullable unsigned 32-bit integer enum underlying type.</summary>
        UInt32Nullable,
        /// <summary>System.Nullable&lt;System.Int64&gt; - nullable signed 64-bit integer enum underlying type.</summary>
        Int64Nullable,
        /// <summary>System.Nullable&lt;System.UInt64&gt; - nullable unsigned 64-bit integer enum underlying type.</summary>
        UInt64Nullable,
    }
}
