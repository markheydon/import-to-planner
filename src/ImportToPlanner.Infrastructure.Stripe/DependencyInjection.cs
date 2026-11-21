using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stripe;

namespace ImportToPlanner.Infrastructure.Stripe;

/// <summary>
/// Registers Stripe payment adapter services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Stripe webhook verification and client configuration for commercial credit checkout.
    /// </summary>
    public static IServiceCollection AddStripeInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<StripeOptions>()
            .Bind(configuration.GetSection(StripeOptions.ConfigurationSectionName));

        var apiKey = configuration[$"{StripeOptions.ConfigurationSectionName}:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            StripeConfiguration.ApiKey = apiKey;
        }

        services.AddSingleton<StripeClient>(_ => new StripeClient(apiKey ?? string.Empty));
        services.AddSingleton<IPaymentWebhookVerifier, StripePaymentWebhookVerifier>();
        services.AddScoped<ICreditCheckoutPaymentPort, StripeCreditCheckoutPaymentPort>();

        return services;
    }
}
