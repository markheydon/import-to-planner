using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ImportToPlanner.Web.Infrastructure;

internal static class E2ETestingServiceCollectionExtensions
{
    public const string AuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    public static IServiceCollection AddE2ETestingAuthenticationIfEnabled(
        this IServiceCollection services,
        IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        if (!E2ETestingHostEnvironment.IsE2ETesting(hostEnvironment))
        {
            return services;
        }

        services.AddAuthentication(AuthenticationScheme)
            .AddCookie(AuthenticationScheme, options =>
            {
                options.Cookie.Name = "ImportToPlanner.E2E.Auth";
            });

        services.AddAuthorization();

        return services;
    }

    public static IEndpointRouteBuilder MapE2ETestingEndpointsIfEnabled(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        if (!E2ETestingHostEnvironment.IsE2ETesting(hostEnvironment))
        {
            return endpoints;
        }

        endpoints.MapGet(
            "/e2e/sign-in",
            static async context =>
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, "e2e-test-user"),
                    new Claim(ClaimTypes.NameIdentifier, "e2e-test-user"),
                    new Claim("preferred_username", "e2e-test-user@contoso.com"),
                    new Claim("tid", "00000000-0000-0000-0000-000000000001"),
                    new Claim("http://schemas.microsoft.com/identity/claims/tenantid", "00000000-0000-0000-0000-000000000001"),
                };

                var identity = new ClaimsIdentity(claims, AuthenticationScheme);
                await context.SignInAsync(AuthenticationScheme, new ClaimsPrincipal(identity));
                context.Response.Redirect("/");
            }).AllowAnonymous();

        endpoints.MapGet(
            "/e2e/sign-out",
            static async context =>
            {
                await context.SignOutAsync(AuthenticationScheme);
                context.Response.Redirect("/");
            }).AllowAnonymous();

        return endpoints;
    }
}
