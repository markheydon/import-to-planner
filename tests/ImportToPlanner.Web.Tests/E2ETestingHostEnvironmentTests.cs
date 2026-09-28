using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ImportToPlanner.Web.Tests;

[Collection(nameof(E2ETestingEndpointRouteTests))]
public sealed class E2ETestingHostEnvironmentTests
{
    [Fact]
    public void EnsureStartupAllowed_WhenE2ETestingWithoutOptIn_Throws()
    {
        var previousValue = Environment.GetEnvironmentVariable(E2ETestingHostEnvironment.AllowEnvironmentVariableName);
        Environment.SetEnvironmentVariable(E2ETestingHostEnvironment.AllowEnvironmentVariableName, null);

        try
        {
            var hostEnvironment = new TestHostEnvironment
            {
                EnvironmentName = E2ETestingHostEnvironment.EnvironmentName,
            };

            var exception = Record.Exception(() => E2ETestingHostEnvironment.EnsureStartupAllowed(hostEnvironment));

            Assert.IsType<InvalidOperationException>(exception);
        }
        finally
        {
            Environment.SetEnvironmentVariable(E2ETestingHostEnvironment.AllowEnvironmentVariableName, previousValue);
        }
    }

    [Fact]
    public void EnsureStartupAllowed_WhenE2ETestingWithOptIn_DoesNotThrow()
    {
        var previousValue = Environment.GetEnvironmentVariable(E2ETestingHostEnvironment.AllowEnvironmentVariableName);
        Environment.SetEnvironmentVariable(E2ETestingHostEnvironment.AllowEnvironmentVariableName, "true");

        try
        {
            var hostEnvironment = new TestHostEnvironment
            {
                EnvironmentName = E2ETestingHostEnvironment.EnvironmentName,
            };

            var exception = Record.Exception(() => E2ETestingHostEnvironment.EnsureStartupAllowed(hostEnvironment));

            Assert.Null(exception);
        }
        finally
        {
            Environment.SetEnvironmentVariable(E2ETestingHostEnvironment.AllowEnvironmentVariableName, previousValue);
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "ImportToPlanner.Web.Tests";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
