using Zerra.CQRS;
using ZerraDemo.Domain.WeatherCached.Commands;

namespace ZerraDemo.Domain.WeatherCached
{
    public interface IWeatherCachedCommandHandler :
        ICommandHandler<SetWeatherCachedCommand>
    {
    }
}
