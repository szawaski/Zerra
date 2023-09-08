using Zerra.CQRS;
using ZerraDemo.Domain.WeatherCached.Constants;

namespace ZerraDemo.Domain.WeatherCached.Commands
{
    [ServiceExposed]
    public class SetWeatherCachedCommand : ICommand
    {
        public WeatherCachedType WeatherType { get; set; }
    }
}
