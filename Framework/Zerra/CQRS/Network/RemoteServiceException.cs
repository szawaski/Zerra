// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS.Network
{
    public sealed class RemoteServiceException : Exception
    {
        public RemoteServiceException(string? message) 
            : base($"Error From Remote Service: {(String.IsNullOrWhiteSpace(message) ? "(Unknown)" : message )}")
        {

        }
    }
}