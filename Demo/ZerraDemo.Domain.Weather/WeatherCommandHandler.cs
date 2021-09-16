using System.Threading.Tasks;
using ZerraDemo.Domain.Weather.Commands;
using ZerraDemo.Common;

namespace ZerraDemo.Domain.Weather
{
    public class WeatherCommandHandler : IWeatherCommandHandler
    {
        public async Task Handle(SetWeatherCommand command)
        {
            Access.CheckRole("Admin");

            await WeatherServer.SetWeather(command.WeatherType);
        }
    }
}
