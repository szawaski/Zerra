using System.Threading.Tasks;
using Zerra.CQRS;
using ZerraDemo.Common;
using ZerraDemo.Domain.WeatherCached.Commands;
using ZerraDemo.Domain.WeatherCached.Events;

namespace ZerraDemo.Domain.WeatherCached
{
    public class WeatherCachedCommandHandler : IWeatherCachedCommandHandler
    {
        public async Task Handle(SetWeatherCachedCommand command)
        {
            Access.CheckRole("Admin");

            await WeatherCachedServer.SetWeather(command.WeatherType);

            await Bus.DispatchAsync(new WeatherChangedEvent() { WeatherType = command.WeatherType });
        }
    }
}
