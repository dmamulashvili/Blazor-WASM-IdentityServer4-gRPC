using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace WebApp.Client.Authentication
{
    public class CustomAuthorizationMessageHandler : AuthorizationMessageHandler
    {
        public CustomAuthorizationMessageHandler(IAccessTokenProvider provider, NavigationManager navigation) : base(
            provider, navigation)
        {
            ConfigureHandler(authorizedUrls: new[] {"https://localhost:5005"});
        }
    }
}