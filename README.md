# Blazor-WASM-IdentityServer4-gRPC

A Step-by-Step Guide on how to configure Blazor WebAssembly standalone app & ASP.NET Core Identity combined with IdentityServer4 server app using gRPC-Web(Code-first) middleware.    

## Source projects Startup
```console
dotnet run --project src/WebApp.Server/WebApp.Server.csproj
```
```console
dotnet run --project src/WebApp.Client/WebApp.Client.csproj
```

## Create a new Blazor WebAssembly project
```console
dotnet new blazorwasm -au Individual -ho -o WebApp
```

## WebApp.Shared project Configuration
- Add a [Grpc.Net.Client](https://www.nuget.org/packages/Grpc.Net.Client) package reference
- Add a [protobuf-net.Grpc](https://www.nuget.org/packages/protobuf-net.Grpc) package reference
- Create service and data contract types.

```cs
using System;
using ProtoBuf;

namespace WebApp.Shared
{
    [ProtoContract]
    public class WeatherForecast
    {
        [ProtoMember(1)] public DateTime Date { get; set; }

        [ProtoMember(2)] public int TemperatureC { get; set; }

        [ProtoMember(3)] public string Summary { get; set; }

        public int TemperatureF => 32 + (int) (TemperatureC / 0.5556);
    }
}
```

```cs
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
```

```cs
using System.Threading.Tasks;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace WebApp.Shared
{
    [Service]
    public interface IWeatherService
    {
        [Operation]
        Task<WeatherReply> GetWeather(CallContext context = default);
    }
}
```
## WebbApp.Server project Configuration
- Add a [Grpc.AspNetCore.Web](https://www.nuget.org/packages/Grpc.AspNetCore.Web) package reference
- Add a [protobuf-net.Grpc.AspNetCore](https://www.nuget.org/packages/protobuf-net.Grpc.AspNetCore) package reference
- Delete `WebApp.Client` project reference so we can host `WebApp.Client` & `WebApp.Server` apps independently
```cs 
<ProjectReference Include="..\Client\WebApp.Client.csproj" />
```
- Update `launchSettings.json` applicationUrl ports to *5005* & *5004*, we will use *5001* & *5000* ports for `WebApp.Client`
```json
"applicationUrl": "https://localhost:5005;http://localhost:5004",
```
- Update `appsettings.json` IdentityServer Client configuration, change Profile to **SPA** & add `WebApp.Client` **RedirectUri/LogoutUri** 
```json
"IdentityServer": {
  "Clients": {
    "WebApp.Client": {
      "Profile": "SPA",
      "RedirectUri": "https://localhost:5001/authentication/login-callback",
      "LogoutUri": "https://localhost:5001/authentication/logout-callback"
    }
  }
}
```
- Implement `WeatherService : IWeatherService`
```cs
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

        public Task<WeatherReply> GetWeather(CallContext context)
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

```
- Configure Code-first gRPC & CORS services
```cs
services.AddCodeFirstGrpc();

services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
{
    builder
        // WebApp.Client ApplicationUrls
        .WithOrigins("https://localhost:5001", "http://localhost:5000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        // To allow a browser app to make cross-origin gRPC-Web calls
        .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
}));
```
- Configure middlewares
```cs
// Should be placed before app.UseIdentityServer();
app.UseCors("CorsPolicy");

app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();

// new GrpcWebOptions() {DefaultEnabled = true} configures so all services support gRPC-Web by default
app.UseGrpcWeb(new GrpcWebOptions() {DefaultEnabled = true});

app.UseEndpoints(endpoints =>
{
    // Adds the code-first service endpoint
    endpoints.MapGrpcService<WeatherService>();

    endpoints.MapRazorPages();
    endpoints.MapControllers();
    endpoints.MapFallbackToFile("index.html");
});
```
## WebbApp.Client project Configuration
- Add a [Grpc.Net.Client.Web](https://www.nuget.org/packages/Grpc.Net.Client.Web) package reference
- Implement `CustomAuthorizationMessageHandler : AuthorizationMessageHandler`
```cs
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace WebApp.Client.Authentication
{
    public class CustomAuthorizationMessageHandler : AuthorizationMessageHandler
    {
        public CustomAuthorizationMessageHandler(IAccessTokenProvider provider, NavigationManager navigation) : base(
            provider, navigation)
        {
            // Configures this handler to authorize outbound HTTP requests using an access token.
            // authorizedUrls – The base addresses of endpoint URLs to which the token will be attached
            ConfigureHandler(authorizedUrls: new[] {"https://localhost:5005"}); // WebApp.Server Url
        }
    }
}
```
- Update `Program.cs`
```cs
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
```
- Implement `CustomRemoteAuthenticatorView : RemoteAuthenticatorViewCore<RemoteAuthenticationState>` to avoid [RemoteRegisterPath & RemoteProfilePath Issue](https://github.com/dotnet/aspnetcore/issues/29246)
```cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;

namespace WebApp.Client.Authentication
{
    public class CustomRemoteAuthenticatorView : RemoteAuthenticatorViewCore<RemoteAuthenticationState>
    {
        [Inject] internal IJSRuntime JS { get; set; }
        [Inject] internal NavigationManager Navigation { get; set; }

        public CustomRemoteAuthenticatorView() => AuthenticationState = new RemoteAuthenticationState();

        protected override async Task OnParametersSetAsync()
        {
            switch (Action)
            {
                case RemoteAuthenticationActions.Profile:
                    if (ApplicationPaths.RemoteProfilePath == null)
                    {
                        UserProfile ??= ProfileNotSupportedFragment;
                    }
                    else
                    {
                        UserProfile ??= LoggingIn;
                        await RedirectToProfile();
                    }

                    break;
                case RemoteAuthenticationActions.Register:
                    if (ApplicationPaths.RemoteRegisterPath == null)
                    {
                        Registering ??= RegisterNotSupportedFragment;
                    }
                    else
                    {
                        Registering ??= LoggingIn;
                        await RedirectToRegister();
                    }

                    break;
                default:
                    await base.OnParametersSetAsync();
                    break;
            }
        }

        private static void ProfileNotSupportedFragment(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "Editing the profile is not supported.");
            builder.CloseElement();
        }

        private static void RegisterNotSupportedFragment(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "Registration is not supported.");
            builder.CloseElement();
        }

        private ValueTask RedirectToProfile() => JS.InvokeVoidAsync("location.replace",
            Navigation.ToAbsoluteUri(ApplicationPaths.RemoteProfilePath));

        private ValueTask RedirectToRegister()
        {
            var loginUrl = Navigation.ToAbsoluteUri(ApplicationPaths.LogInPath).PathAndQuery;
            var registerUrl = Navigation
                .ToAbsoluteUri($"{ApplicationPaths.RemoteRegisterPath}?returnUrl={Uri.EscapeDataString(loginUrl)}");

            return JS.InvokeVoidAsync("location.replace", registerUrl);
        }
    }
}
```
- Update `Authentication.razor` to use `CustomRemoteAuthenticatorView`
```razor
@page "/authentication/{action}"
@using WebApp.Client.Authentication
@* <RemoteAuthenticatorView Action="@Action" /> *@
<CustomRemoteAuthenticatorView Action="@Action"/>

@code{

    [Parameter]
    public string Action { get; set; }

}
```
- Update `NavMenu.razor` wrap Fetch data navlink in `<AuthorizeView>` to make it visible based on Authentication state
```razor
<AuthorizeView>
    <li class="nav-item px-3">
        <NavLink class="nav-link" href="fetchdata">
            <span class="oi oi-list-rich" aria-hidden="true"></span> Fetch data
        </NavLink>
    </li>
</AuthorizeView>
```
- Update `FetchData.razor` inject `IWeatherService` instead of `HttpClient`
```razor
@attribute [Authorize]
@* @inject HttpClient Http *@
@inject IWeatherService _weatherService
```
- Update `OnInitializedAsync` to use `IWeatherService`
```razor
@code {
    // private WeatherForecast[] forecasts;
    private IEnumerable<WeatherForecast> forecasts;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // forecasts = await Http.GetFromJsonAsync<WeatherForecast[]>("WeatherForecast");
            forecasts = (await _weatherService.GetWeather()).Forecasts;
        }
        catch (AccessTokenNotAvailableException exception)
        {
            exception.Redirect();
        }
    }

}
```
