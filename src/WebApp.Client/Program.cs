using System;
using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Client;
using WebApp.Client.Authentication;
using WebApp.Shared;

namespace WebApp.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped<CustomAuthorizationMessageHandler>();
            builder.Services.AddHttpClient("WebApp.ServerAPI",
                    client => client.BaseAddress = new Uri("https://localhost:5005"))
                .AddHttpMessageHandler<CustomAuthorizationMessageHandler>()
                .AddHttpMessageHandler(() => new GrpcWebHandler(GrpcWebMode.GrpcWeb));

            // Supply HttpClient instances that include access tokens when making requests to the server project
            builder.Services.AddScoped(sp =>
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("WebApp.ServerAPI"));

            builder.Services.AddApiAuthorization(options =>
            {
                options.AuthenticationPaths.RemoteRegisterPath = "Https://localhost:5005/Identity/Account/Register";
                options.AuthenticationPaths.RemoteProfilePath = "Https://localhost:5005/Identity/Account/Manage";
                options.ProviderOptions.ConfigurationEndpoint = "https://localhost:5005/_configuration/WebApp.Client";
            });
            
            builder.Services.AddSingleton(services =>
            {
                var httpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient("WebApp.ServerAPI");
                var channel = GrpcChannel.ForAddress(httpClient.BaseAddress,
                    new GrpcChannelOptions {HttpClient = httpClient});
                return channel.CreateGrpcService<IWeatherService>();
            });

            await builder.Build().RunAsync();
        }
    }
}