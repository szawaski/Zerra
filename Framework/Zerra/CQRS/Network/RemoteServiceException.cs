// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS.Network
{
    public class RemoteServiceException : Exception
    {
        public RemoteServiceException()
            : base(GetMessage(null))
        { }

        public RemoteServiceException(string? message)
            : base(GetMessage(message))
        { }

        public RemoteServiceException(string? message, Exception? innerException) 
            : base(GetMessage(message), innerException)
        { }

        private static string GetMessage(string? message) => $"Error From Remote Service: {(String.IsNullOrWhiteSpace(message) ? "(Unknown)" : message)}";
    }
}