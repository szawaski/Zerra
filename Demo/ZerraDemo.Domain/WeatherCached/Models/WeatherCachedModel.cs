using System;
using ZerraDemo.Domain.WeatherCached.Constants;

namespace ZerraDemo.Domain.WeatherCached.Models
{
    public class WeatherCachedModel
    {
        public DateTime Date { get; set; }
        public WeatherCachedType WeatherType { get; set; }
    }
}
