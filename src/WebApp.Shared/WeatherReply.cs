using System.Collections.Generic;
using ProtoBuf;

namespace WebApp.Shared
{
    [ProtoContract]
    public class WeatherReply
    {
        [ProtoMember(1)] public IEnumerable<WeatherForecast> Forecasts { get; set; }
    }
}