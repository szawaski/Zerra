using System.Threading.Tasks;
using ZerraDemo.Common;
using ZerraDemo.Domain.WeatherCached.Commands;

namespace ZerraDemo.Domain.WeatherCached
{
    public class WeatherCachedCommandHandler : IWeatherCachedCommandHandler
    {
        public async Task Handle(SetWeatherCachedCommand command)
        {
            Access.CheckRole("Admin");

            await WeatherCachedServer.SetWeather(command.WeatherType);
        }
    }
}
