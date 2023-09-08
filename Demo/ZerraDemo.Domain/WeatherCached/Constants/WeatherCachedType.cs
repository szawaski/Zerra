using System;

namespace ZerraDemo.Domain.WeatherCached.Constants
{
    [Flags]
    public enum WeatherCachedType
    {
        Sunny = 0,
        OhioGraySkies = 1,
        Cloudy = 2,
        Windy = 4,
        Rain = 8,
        Snow = 16,
        Hail = 32,
        Tornado = 64,
        Hurricane = 128,
        Asteroid = 256,
        Sharks = 512
    }
}
