using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using ProtoBuf.Grpc;
using WebApp.Shared;

namespace WebApp.Server.Services
{
    [Authorize]
    public class WeatherService : IWeatherService
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public Task<WeatherReply> GetWeather(WeatherRequest request, CallContext context)
        {
            var reply = new WeatherReply();
            var rng = new Random();

            reply.Forecasts = Enumerable.Range(1, 10).Select(index => new WeatherForecast
            {
                Date = DateTime.UtcNow.AddDays(index),
                TemperatureC = rng.Next(20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            });

            return Task.FromResult(reply);
        }
    }
}