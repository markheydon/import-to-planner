using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers typed HTTP clients for internal service references.
/// </summary>
public static class ServiceReferenceExtensions
{
    /// <summary>
    /// Adds an HTTP service reference and assigns the provided base address.
    /// </summary>
    /// <typeparam name="TClient">Typed HTTP client type.</typeparam>
    /// <param name="services">Service registrations.</param>
    /// <param name="baseAddress">Absolute base URI, including service discovery scheme.</param>
    /// <returns>The HTTP client builder for further configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="baseAddress"/> is not an absolute URI.</exception>
    public static IHttpClientBuilder AddHttpServiceReference<TClient>(this IServiceCollection services, string baseAddress)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!Uri.IsWellFormedUriString(baseAddress, UriKind.Absolute))
        {
            throw new ArgumentException("Base address must be a valid absolute URI.", nameof(baseAddress));
        }

        return services.AddHttpClient<TClient>(client => client.BaseAddress = new Uri(baseAddress, UriKind.Absolute));
    }

    /// <summary>
    /// Adds an HTTP service reference and registers a downstream health check.
    /// </summary>
    /// <typeparam name="TClient">Typed HTTP client type.</typeparam>
    /// <param name="services">Service registrations.</param>
    /// <param name="baseAddress">Absolute base URI, including service discovery scheme.</param>
    /// <param name="healthRelativePath">Relative path of the downstream health endpoint.</param>
    /// <param name="healthCheckName">Optional health check name.</param>
    /// <param name="failureStatus">Health status to report when the dependency check fails.</param>
    /// <returns>The HTTP client builder for further configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when addresses are invalid.</exception>
    public static IHttpClientBuilder AddHttpServiceReference<TClient>(
        this IServiceCollection services,
        string baseAddress,
        string healthRelativePath,
        string? healthCheckName = default,
        HealthStatus failureStatus = default)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(healthRelativePath);

        if (!Uri.IsWellFormedUriString(baseAddress, UriKind.Absolute))
        {
            throw new ArgumentException("Base address must be a valid absolute URI.", nameof(baseAddress));
        }

        if (!Uri.IsWellFormedUriString(healthRelativePath, UriKind.Relative))
        {
            throw new ArgumentException("Health check path must be a valid relative URI.", nameof(healthRelativePath));
        }

        var uri = new Uri(baseAddress, UriKind.Absolute);
        var builder = services.AddHttpClient<TClient>(client => client.BaseAddress = uri);

        var checkName = healthCheckName ?? $"{typeof(TClient).Name}-health";
        var normalizedFailureStatus = failureStatus == default ? HealthStatus.Unhealthy : failureStatus;
        services.AddHealthChecks()
            .AddTypeActivatedCheck<HttpServiceReferenceHealthCheck>(
                checkName,
                failureStatus: normalizedFailureStatus,
                args: new object[]
                {
                    typeof(TClient).Name,
                    healthRelativePath,
                });

        return builder;
    }

    private sealed class HttpServiceReferenceHealthCheck(
        IHttpClientFactory httpClientFactory,
        string clientName,
        string healthRelativePath) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = httpClientFactory.CreateClient(clientName);
                using var request = new HttpRequestMessage(HttpMethod.Get, healthRelativePath);
                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

                return response.IsSuccessStatusCode
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy($"Dependency check returned status code {(int)response.StatusCode}.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return HealthCheckResult.Unhealthy(exception: ex);
            }
        }
    }
}
