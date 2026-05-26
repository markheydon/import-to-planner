using Microsoft.Extensions.Configuration;

namespace ImportToPlanner.Web.Tests;

public sealed class StartupValidationTests
{
    [Fact]
    public void Validate_WhenAzureAdTenantIdMissing_ThrowsFriendlyError()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Storage:TenantMetadataTable"] = "TenantOperationalMetadata",
            ["Storage:DataProtectionContainer"] = "dataprotection",
            ["Storage:DataProtectionBlob"] = "keys.xml",
        });

        var exception = Assert.Throws<InvalidOperationException>(() => StartupConfigurationValidator.Validate(configuration));

        Assert.Contains("AzureAd:TenantId", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("PlannerGateway:UseGraph", "PlannerGateway")]
    [InlineData("HostedStorage:Enabled", "HostedStorage")]
    [InlineData("DeploymentMode:Mode", "DeploymentMode")]
    public void Validate_WhenRemovedRuntimeKeysArePresent_ThrowsFriendlyError(string obsoleteKey, string expectedSection)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AzureAd:TenantId"] = "organizations",
            ["Storage:TenantMetadataTable"] = "TenantOperationalMetadata",
            ["Storage:DataProtectionContainer"] = "dataprotection",
            ["Storage:DataProtectionBlob"] = "keys.xml",
            [obsoleteKey] = "true",
        });

        var exception = Assert.Throws<InvalidOperationException>(() => StartupConfigurationValidator.Validate(configuration));

        Assert.Contains(expectedSection, exception.Message, StringComparison.Ordinal);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
