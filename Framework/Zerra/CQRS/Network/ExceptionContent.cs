// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Network
{
    internal sealed class ExceptionContent
    {
        public string? ErrorMessage { get; set; }
        public string? ErrorType { get; set; }
        public byte[]? ErrorBytes { get; set; }
        public string? ErrorString { get; set; }
    }
}
