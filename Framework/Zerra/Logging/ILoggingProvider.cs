// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;

namespace Zerra.Logging
{
    public interface ILoggingProvider
    {
        Task TraceAsync(string message);
        Task DebugAsync(string message);
        Task InfoAsync(string message);
        Task WarnAsync(string message);
        Task ErrorAsync(string? message, Exception? ex);
        Task CriticalAsync(string? message, Exception? ex);
    }
}
