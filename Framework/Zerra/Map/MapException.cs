// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Map
{
    public sealed class MapException : Exception
    {
        public MapException(string message) : base(message) { }
        public MapException(string message, Exception innerException) : base(message, innerException) { }
    }
}
