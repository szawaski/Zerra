// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface ICommandHandler<T, TResult> where T : ICommand<TResult>
    {
        Task<TResult> Handle(T command);
    }
}