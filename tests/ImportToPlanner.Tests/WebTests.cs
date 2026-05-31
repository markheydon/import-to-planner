using Microsoft.Extensions.Logging;

namespace ImportToPlanner.Tests;

public class WebTests
{
    // Timeout duration for distributed app resource startup in local and CI runs.
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(120);

    [Fact]
    public async Task SelfHostModeGetHealthEndpointReturnsOkStatusCode()
    {
        using var _ = ConfigureRequiredAppHostParameters();

        // Arrange
        var cancellationToken = new CancellationTokenSource(DefaultTimeout).Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.ImportToPlanner_AppHost>(cancellationToken);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            // Override the logging filters from the app's configuration
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
            // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // A test-only HTTP client handler that accepts the Aspire test server certificate.
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            });
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        var httpClient = app.CreateHttpClient("web");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("web", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        var response = await httpClient.GetAsync("/health", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.ThrowsAny<Exception>(() => app.CreateHttpClient("commercialapiservice"));
    }

    [Fact]
    public async Task CommercialModeStartsWebAndCommercialApiServiceResources()
    {
        using var _ = ConfigureRequiredAppHostParameters(enableCommercialMode: true);

        // Arrange
        var cancellationToken = new CancellationTokenSource(DefaultTimeout).Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.ImportToPlanner_AppHost>(cancellationToken);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            });
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        var httpClient = app.CreateHttpClient("web");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("web", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("commercialapiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        var response = await httpClient.GetAsync("/", cancellationToken);

        // Assert
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private static EnvironmentVariableScope ConfigureRequiredAppHostParameters(bool enableCommercialMode = false)
    {
        return new EnvironmentVariableScope(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Parameters__azureAdTenantId"] = "organizations",
            ["Parameters__azureAdClientId"] = "22222222-2222-2222-2222-222222222222",
            ["Parameters__azureAdHomeTenantId"] = "11111111-1111-1111-1111-111111111111",
            ["Parameters__enableCommercialMode"] = enableCommercialMode ? "true" : "false",
            ["Parameters__graphClientCertificatePassword"] = string.Empty,
            ["Parameters__graphClientCertificateBase64"] = string.Empty,
        });
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly Dictionary<string, string?> _originalValues = new(StringComparer.Ordinal);

        public EnvironmentVariableScope(IReadOnlyDictionary<string, string?> values)
        {
            foreach (var (key, value) in values)
            {
                _originalValues[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        public void Dispose()
        {
            foreach (var (key, originalValue) in _originalValues)
            {
                Environment.SetEnvironmentVariable(key, originalValue);
            }
        }
    }
}
