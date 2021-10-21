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