using Zerra.CQRS;
using ZerraDemo.Domain.WeatherCached.Constants;

namespace ZerraDemo.Domain.WeatherCached.Events
{
    [ServiceExposed]
    public class WeatherChangedEvent : IEvent
    {
        public WeatherCachedType WeatherType { get; set; }
    }
}
