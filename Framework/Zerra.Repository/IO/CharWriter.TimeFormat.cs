// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.IO
{
    public ref partial struct CharWriter
    {
        public enum TimeFormat : byte
        {
            ISO8601,
            MsSql,
            MySql,
            PostgreSql
        }
    }
}