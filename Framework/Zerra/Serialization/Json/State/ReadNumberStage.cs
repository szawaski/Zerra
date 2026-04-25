// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json.State
{
    /// <summary>
    /// Represents the different stages of reading and parsing a JSON number.
    /// </summary>
    public enum ReadNumberStage : byte
    {
        /// <summary>
        /// Initial setup stage before reading a number.
        /// </summary>
        Setup = 0,

        /// <summary>
        /// Reading the main value (integer or beginning of decimal) of the number.
        /// </summary>
        Value = 1,

        /// <summary>
        /// Continuing to read additional digits of the number value.
        /// </summary>
        ValueContinue = 2,

        /// <summary>
        /// Reading the decimal portion of the number after the decimal point.
        /// </summary>
        Decimal = 3,

        /// <summary>
        /// Reading the exponent indicator (e or E) of the number.
        /// </summary>
        Exponent = 4,

        /// <summary>
        /// Continuing to read the exponent digits of the number.
        /// </summary>
        ExponentContinue = 5
    }
}