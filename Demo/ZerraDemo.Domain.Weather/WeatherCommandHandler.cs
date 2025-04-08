﻿using System.Threading;
using System.Threading.Tasks;
using ZerraDemo.Common;
using ZerraDemo.Domain.Weather.Commands;

namespace ZerraDemo.Domain.Weather
{
    public class WeatherCommandHandler : IWeatherCommandHandler
    {
        public async Task Handle(SetWeatherCommand command, CancellationToken cancellationToken)
        {
            Access.CheckRole("Admin");

            await WeatherServer.SetWeather(command.WeatherType);
        }
    }
}
