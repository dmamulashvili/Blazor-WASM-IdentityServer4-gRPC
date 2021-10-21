using System.Threading.Tasks;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace WebApp.Shared
{
    [Service]
    public interface IWeatherService
    {
        [Operation]
        Task<WeatherReply> GetWeather(WeatherRequest request,
            CallContext context = default);
    }
}