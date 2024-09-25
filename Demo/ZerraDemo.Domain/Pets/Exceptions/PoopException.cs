using System;
using ZerraDemo.Domain.Weather.Constants;

namespace ZerraDemo.Domain.Pets.Exceptions
{
    public sealed class PoopException : Exception
    {
        public string? PetName { get; }
        public WeatherType? Weather { get; }

        public PoopException() : base() { }
        public PoopException(string petName, WeatherType weather)
            : base($"{petName} will not go out to poop in {weather.EnumName()} weather.")
        {
            this.PetName = petName;
            this.Weather = weather;
        }
    } 
}
