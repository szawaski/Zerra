﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Zerra;
using Zerra.CQRS;
using Zerra.Repository;
using ZerraDemo.Domain.Pets.Commands;
using ZerraDemo.Domain.Pets.DataModels;
using ZerraDemo.Domain.Weather;
using ZerraDemo.Domain.Weather.Constants;
using ZerraDemo.Common;
using ZerraDemo.Domain.Pets.Exceptions;
using System.Threading;

namespace ZerraDemo.Domain.Pets
{
    public class PetsCommandHandler : IPetsCommandHandler
    {
        public PetsCommandHandler()
        {
            PetsAssureData.AssureData();
        }

        public async Task Handle(AdoptPetCommand command, CancellationToken cancellationToken)
        {
            Access.CheckRole("Admin");

            var item = new PetDataModel()
            {
                ID = command.PetID,
                BreedID = command.BreedID,
                Name = command.Name
            };
            await Repo.PersistAsync(new Create<PetDataModel>(item));
        }

        public async Task<FeedPetCommandResult> Handle(FeedPetCommand command, CancellationToken cancellationToken)
        {
            Access.CheckRole("Admin");

            var item = Repo.Query(new QuerySingle<PetDataModel>(x => x.ID == command.PetID, new Graph<PetDataModel>(
                x => x.ID,
                x => x.LastEaten,
                x => x.AmountEaten
            )));
            if (item is null)
                throw new Exception("Pet not found");

            item.AmountEaten ??= 0;

            var hoursSinceEaten = (int)(DateTime.UtcNow - (item.LastEaten ?? DateTime.MinValue)).TotalHours;
            item.AmountEaten -= hoursSinceEaten;
            if (item.AmountEaten < 0)
                item.AmountEaten = 0;

            item.AmountEaten = (item.AmountEaten ?? 0) + command.Amount;
            item.LastEaten = DateTime.UtcNow;

            await Repo.PersistAsync(new Update<PetDataModel>(item, new Graph<PetDataModel>(
                x => x.AmountEaten,
                x => x.LastEaten
            )));

            return new FeedPetCommandResult()
            {
                AmountEaten = item.AmountEaten.Value
            };
        }

        public async Task Handle(LetPetOutToPoopCommand command, CancellationToken cancellationToken)
        {
            Access.CheckRole("Admin");

            using (var testStream = await Bus.Call<IWeatherQueryProvider>().TestStreams())
            {
                using (var ms = new MemoryStream())
                {
                    await testStream.CopyToAsync(ms);
                    var result = Encoding.UTF8.GetString(ms.ToArray());
                    if (result.Length != 1000000)
                        throw new Exception();
                    for (var i = 0; i < result.Length; i += 10)
                    {
                        if (result.Substring(i, 10) != "0123456789")
                            throw new Exception();
                    }
                }
            }

            var tasks = new Task[20];
            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Bus.Call<IWeatherQueryProvider>().GetWeather();
            }
            await Task.WhenAll(tasks);

            var weatherSync = Bus.Call<IWeatherQueryProvider>(TimeSpan.FromMilliseconds(1000)).GetWeatherSync(default);

            var weather = await Bus.Call<IWeatherQueryProvider>(TimeSpan.FromMilliseconds(200)).GetWeather();

            if (
                (weather.WeatherType & WeatherType.Rain) == WeatherType.Rain ||
                (weather.WeatherType & WeatherType.Hail) == WeatherType.Hail ||
                (weather.WeatherType & WeatherType.Tornado) == WeatherType.Tornado ||
                (weather.WeatherType & WeatherType.Hurricane) == WeatherType.Hurricane ||
                (weather.WeatherType & WeatherType.Asteroid) == WeatherType.Asteroid ||
                (weather.WeatherType & WeatherType.Sharks) == WeatherType.Sharks
                )
            {
                var name = Repo.Query(new QuerySingle<PetDataModel>(x => x.ID == command.PetID, new Graph<PetDataModel>(
                    x => x.Name
                )))?.Name!;

                throw new PoopException(name, weather.WeatherType);
            }

            var item = new PetDataModel()
            {
                ID = command.PetID,
                AmountEaten = null,
                LastPooped = DateTime.UtcNow
            };

            await Repo.PersistAsync(new Update<PetDataModel>(item, new Graph<PetDataModel>(
                x => x.AmountEaten,
                x => x.LastPooped
            )));
        }
    }
}
