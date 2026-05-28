// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.CQRS.Network
{
    [GenerateTypeDetail]
    internal sealed class ExceptionContent
    {
        public required string ExceptionType { get; set; }
        public string? Message { get; set; }
        public string? StackTrace { get; set; }
    }
}
