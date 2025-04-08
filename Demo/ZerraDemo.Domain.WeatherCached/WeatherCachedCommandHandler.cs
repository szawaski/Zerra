using System;
using System.Threading;
using System.Threading.Tasks;
using Zerra.CQRS;
using ZerraDemo.Common;
using ZerraDemo.Domain.WeatherCached.Commands;
using ZerraDemo.Domain.WeatherCached.Events;

namespace ZerraDemo.Domain.WeatherCached
{
    public class WeatherCachedCommandHandler : IWeatherCachedCommandHandler
    {
        public async Task Handle(SetWeatherCachedCommand command, CancellationToken cancellationToken)
        {
            Access.CheckRole("Admin");

            await WeatherCachedServer.SetWeather(command.WeatherType);

            //testing ack uniqueness
            var @event = new WeatherChangedEvent() { WeatherType = command.WeatherType };


            await Bus.DispatchAsync(@event, TimeSpan.FromMilliseconds(100));


            await Bus.DispatchAsync(@event);
            await Bus.DispatchAsync(@event);
        }
    }
}
