// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// Indicates that this class is a command with a result.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    public interface ICommand<TResult> : ICommand
    {

    }
}