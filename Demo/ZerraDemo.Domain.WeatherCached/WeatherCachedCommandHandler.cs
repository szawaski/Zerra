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
        public async Task Handle(SetWeatherCachedCommand command)
        {
            Access.CheckRole("Admin");

            await WeatherCachedServer.SetWeather(command.WeatherType);

            //testing ack uniqueness
            var @event = new WeatherChangedEvent() { WeatherType = command.WeatherType };


            using (var timeoutCancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(1)))
            {
                await Bus.DispatchAsync(@event, timeoutCancellationToken.Token);
            }

            await Bus.DispatchAsync(@event);
            await Bus.DispatchAsync(@event);
        }
    }
}
