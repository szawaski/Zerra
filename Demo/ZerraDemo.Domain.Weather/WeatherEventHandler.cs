using System.Threading.Tasks;
using ZerraDemo.Domain.WeatherCached.Events;
using ZerraDemo.Domain.WeatherCached;
using ZerraDemo.Domain.Weather.Constants;
using System.Threading;

namespace ZerraDemo.Domain.Weather
{
    public class WeatherEventHandler : IWeatherEventHandler
    {
        public async Task Handle(WeatherChangedEvent @event, CancellationToken cancellationToken)
        {
            if (EnumName.TryParse<WeatherType>(@event.WeatherType.ToString(), out var value))
                await WeatherServer.SetWeather(value);
        }
    }
}
