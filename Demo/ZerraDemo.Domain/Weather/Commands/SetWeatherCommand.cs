using Zerra.CQRS;
using ZerraDemo.Domain.Weather.Constants;

namespace ZerraDemo.Domain.Weather.Commands
{
    [ServiceExposed]
    public class SetWeatherCommand : ICommand
    {
        public WeatherType WeatherType { get; set; }
    }
}
