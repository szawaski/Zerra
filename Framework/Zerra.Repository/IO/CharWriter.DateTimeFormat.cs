// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.IO
{
    public ref partial struct CharWriter
    {
        /// <summary>
        /// Specifies the text format to use when writing a <see cref="System.DateTime"/> or <see cref="System.DateTimeOffset"/> value.
        /// </summary>
        public enum DateTimeFormat : byte
        {
            /// <summary>Standard ISO 8601 format, e.g. <c>yyyy-MM-ddTHH:mm:ss.fffffff+00:00</c>.</summary>
            ISO8601,
            /// <summary>Microsoft SQL Server datetime literal, e.g. <c>yyyy-MM-dd HH:mm:ss.fff</c>, converted to UTC.</summary>
            MsSql,
            /// <summary>MySQL datetime literal, e.g. <c>yyyy-MM-dd HH:mm:ss.ffffff</c>, converted to UTC.</summary>
            MySql,
            /// <summary>PostgreSQL timestamp literal, e.g. <c>yyyy-MM-dd HH:mm:ss.ffffff</c>, converted to UTC.</summary>
            PostgreSql
        }
    }
}