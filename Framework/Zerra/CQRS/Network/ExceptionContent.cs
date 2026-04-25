// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.CQRS.Network
{
    [GenerateTypeDetail]
    internal sealed class ExceptionContent
    {
        public string? ErrorMessage { get; set; }
        public string? ErrorType { get; set; }
        public byte[]? ErrorBytes { get; set; }
    }
}
