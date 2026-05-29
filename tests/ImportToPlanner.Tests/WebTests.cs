using Microsoft.Extensions.Logging;

namespace ImportToPlanner.Tests;

public class WebTests
{
    // Timeout duration -- 60 in CI run in GitHub Actions being a bit slow.
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
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
    }

    private static EnvironmentVariableScope ConfigureRequiredAppHostParameters()
    {
        return new EnvironmentVariableScope(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Parameters__azureAdTenantId"] = "organizations",
            ["Parameters__azureAdClientId"] = "22222222-2222-2222-2222-222222222222",
            ["Parameters__azureAdHomeTenantId"] = "11111111-1111-1111-1111-111111111111",
            ["Parameters__enableCommercialMode"] = "false",
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
