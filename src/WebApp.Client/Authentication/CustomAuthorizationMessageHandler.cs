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