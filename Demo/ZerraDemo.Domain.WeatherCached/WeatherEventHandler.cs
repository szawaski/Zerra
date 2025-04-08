using System;
using System.Threading;
using System.Threading.Tasks;
using ZerraDemo.Domain.WeatherCached.Events;

namespace ZerraDemo.Domain.WeatherCached
{
    public class WeatherEventHandler : IWeatherEventHandler
    {
        public async Task Handle(WeatherChangedEvent @event, CancellationToken cancellationToken)
        {
            var model = await WeatherCachedServer.GetWeather();
            if (@event.WeatherType != model)
                throw new Exception("Weather setting does not match after receiving event");
        }
    }
}
