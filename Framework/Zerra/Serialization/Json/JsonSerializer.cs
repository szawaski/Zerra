// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json
{
    public partial class JsonSerializer
    {
        private const int defaultBufferSize = 16 * 1024;

        private static readonly JsonSerializerOptions defaultOptions = new();
    }
}
