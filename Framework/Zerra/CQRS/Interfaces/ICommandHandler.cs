// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Threading.Tasks;

namespace Zerra.CQRS
{
    public interface ICommandHandler<T> where T : ICommand
    {
        Task Handle(T command);
    }
}