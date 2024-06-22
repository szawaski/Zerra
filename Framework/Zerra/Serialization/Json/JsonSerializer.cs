// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;

namespace Zerra.Serialization.Json
{
    public partial class JsonSerializer
    {
        private const int defaultBufferSize = 8 * 1024;

        private static readonly Encoding encoding = Encoding.UTF8;

        private static readonly JsonSerializerOptions defaultOptions = new();
    }
}
