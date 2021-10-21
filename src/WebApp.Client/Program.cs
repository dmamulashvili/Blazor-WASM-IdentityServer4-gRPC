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

            // Register our custom AuthorizationMessageHandler
            builder.Services.AddScoped<CustomAuthorizationMessageHandler>();

            builder.Services.AddHttpClient("WebApp.ServerAPI",
                    client =>
                    {
                        // WebApp.Server BaseAddress
                        client.BaseAddress = new Uri("https://localhost:5005");
                    })
                // Replace .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>() with custom
                .AddHttpMessageHandler<CustomAuthorizationMessageHandler>()
                // Add GrpcWebHandler to be able make gRPC-Web calls.
                .AddHttpMessageHandler(() => new GrpcWebHandler(GrpcWebMode.GrpcWeb));

            // Supply HttpClient instances that include access tokens when making requests to the server project
            builder.Services.AddScoped(sp =>
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("WebApp.ServerAPI"));

            // Configure WebApp.Server RemoteRegisterPath, RemoteProfilePath & ConfigurationEndpoint
            builder.Services.AddApiAuthorization(options =>
            {
                options.AuthenticationPaths.RemoteRegisterPath = "Https://localhost:5005/Identity/Account/Register";
                options.AuthenticationPaths.RemoteProfilePath = "Https://localhost:5005/Identity/Account/Manage";
                options.ProviderOptions.ConfigurationEndpoint = "https://localhost:5005/_configuration/WebApp.Client";
            });

            builder.Services.AddSingleton(services =>
            {
                // Creates our configured HttpClient
                var httpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient("WebApp.ServerAPI");

                // Creates a gRPC channel
                var channel = GrpcChannel.ForAddress(httpClient.BaseAddress,
                    new GrpcChannelOptions
                    {
                        HttpClient = httpClient,
                    });

                // Creates a code-first client from the channel with the CreateGrpcService<IWeatherService> extension method
                return channel.CreateGrpcService<IWeatherService>();
            });

            await builder.Build().RunAsync();
        }
    }
}