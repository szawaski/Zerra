﻿using System.Threading.Tasks;
using Zerra.Providers;
using Zerra.CQRS;
using ZerraDemo.Domain.WeatherCached.Commands;

namespace ZerraDemo.Domain.WeatherCached
{
    public class WeatherCachedCommandHandlerCache : LayerProvider<IWeatherCachedCommandHandler>, IWeatherCachedCommandHandler, IBusCache
    {
        public async Task Handle(SetWeatherCachedCommand command)
        {
            await NextProvider.Handle(command);
            WeatherCachedServerCache.Model = null;
        }
    }
}
