using Zerra.CQRS;
using ZerraDemo.Domain.WeatherCached.Events;

namespace ZerraDemo.Domain.WeatherCached
{
    public interface IWeatherEventHandler :
        IEventHandler<WeatherChangedEvent>
    {
    }
}
