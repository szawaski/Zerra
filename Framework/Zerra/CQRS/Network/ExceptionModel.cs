// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization;

namespace Zerra.CQRS.Network
{
    public sealed class ExceptionModel
    {
        [SerializerIndex(0)]
        public string? Message { get; set; }
    }
}
