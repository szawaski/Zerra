// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Providers;

namespace Zerra.CQRS
{
    /// <summary>
    /// Indicates this class is an <see cref="Bus"/> level caching layer.
    /// Use in conjunction with <see cref="LayerProvider{TProvider}"/>.
    /// The Bus will detect this class and insert it as a layer for senders or handlers.
    /// </summary>
    public interface IBusCache : IIgnoreProviderResolver
    {

    }
}