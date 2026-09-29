using ImportToPlanner.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Web.Features.Demo;

/// <summary>
/// Resets demonstration mode on sign-in and sign-out.
/// </summary>
internal sealed class DemoModeOpenIdConnectPostConfigurer : IPostConfigureOptions<OpenIdConnectOptions>
{
    /// <inheritdoc />
    public void PostConfigure(string? name, OpenIdConnectOptions options)
    {
        if (!string.Equals(name, OpenIdConnectDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        var existingOnTokenValidated = options.Events.OnTokenValidated;
        options.Events.OnTokenValidated = async context =>
        {
            ResetDemoMode(context.HttpContext.RequestServices);
            if (existingOnTokenValidated is not null)
            {
                await existingOnTokenValidated(context);
            }
        };

    }

    private static void ResetDemoMode(IServiceProvider requestServices)
    {
        var session = requestServices.GetService<IDemoModeSession>();
        session?.Reset();
    }
}
