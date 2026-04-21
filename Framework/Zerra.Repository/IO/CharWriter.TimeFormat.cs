// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.IO
{
    public ref partial struct CharWriter
    {
        /// <summary>
        /// Specifies the text format to use when writing a <see cref="System.TimeSpan"/> value.
        /// </summary>
        public enum TimeFormat : byte
        {
            /// <summary>Standard ISO 8601 time format, e.g. <c>(-)d.HH:mm:ss.fffffff</c>.</summary>
            ISO8601,
            /// <summary>Microsoft SQL Server time literal format, e.g. <c>(-)d.HH:mm:ss.fffffff</c>.</summary>
            MsSql,
            /// <summary>MySQL time literal format, e.g. <c>(-)d.HH:mm:ss.ffffff</c>.</summary>
            MySql,
            /// <summary>PostgreSQL time literal format, e.g. <c>(-)d.HH:mm:ss.ffffff</c>.</summary>
            PostgreSql
        }
    }
}