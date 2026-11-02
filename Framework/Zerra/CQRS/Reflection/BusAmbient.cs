// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Reflection
{
    //what a caller has open for its own unit of work, a handler the bus runs in-process starts without it
    internal static class BusAmbient
    {
        private static readonly AsyncLocal<object?> current = new();
        public static object? Current
        {
            get => current.Value;
            set => current.Value = value;
        }
    }
}
