using ImportToPlanner.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Web.Features.Demo;

/// <summary>
/// Clears demonstration mode when the authentication cookie signs out.
/// </summary>
internal sealed class DemoModeCookieSignOutPostConfigurer : IPostConfigureOptions<CookieAuthenticationOptions>
{
    /// <inheritdoc />
    public void PostConfigure(string? name, CookieAuthenticationOptions options)
    {
        if (!string.Equals(name, CookieAuthenticationDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            return;
        }

        var existingOnSigningOut = options.Events.OnSigningOut;
        options.Events.OnSigningOut = async context =>
        {
            var session = context.HttpContext.RequestServices.GetService<IDemoModeSession>();
            session?.Reset();
            if (existingOnSigningOut is not null)
            {
                await existingOnSigningOut(context);
            }
        };
    }
}
