// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Serialization
{
    public static partial class JsonSerializer
    {
        private class ReadFrame
        {
            public TypeDetail TypeDetail;
            public ReadFrameType FrameType;

            public object ResultObject;
            public string ResultString;

            public char Char;
            public int State;
        }
    }
}