using System;
using ZerraDemo.Domain.Weather.Constants;

namespace ZerraDemo.Domain.Weather.Models
{
    public class WeatherModel
    {
        public DateTime Date { get; set; }
        public WeatherType WeatherType { get; set; }
    }
}
