namespace ImportToPlanner.Tests;

public sealed class ArchitectureComplianceTests
{
    [Fact]
    public void DomainAndApplication_DoNotReferenceProviderOrUiPackages()
    {
        // This guards clean architecture boundaries in inner layers.
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var files = Directory.EnumerateFiles(Path.Combine(rootPath, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path =>
                path.Contains("ImportToPlanner.Domain", StringComparison.Ordinal)
                || path.Contains("ImportToPlanner.Application", StringComparison.Ordinal))
            .Where(path => !path.Contains("/bin/", StringComparison.Ordinal) && !path.Contains("/obj/", StringComparison.Ordinal));

        var forbiddenTokens = new[]
        {
            "Microsoft.Graph",
            "Microsoft.Kiota",
            "MudBlazor",
            "CsvHelper",
            "PlannerGraph",
            "Microsoft.AspNetCore.Http",
            "System.Security.Claims",
            "Azure.Data.Tables",
        };

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            foreach (var token in forbiddenTokens)
            {
                Assert.DoesNotContain(token, content, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Application_ContainsTenantBoundaryAbstractionsForHostedMode()
    {
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var currentTenantAccessorPath = Path.Combine(
            rootPath,
            "src",
            "ImportToPlanner.Application",
            "Abstractions",
            "ICurrentTenantContextAccessor.cs");
        var tenantMetadataStorePath = Path.Combine(
            rootPath,
            "src",
            "ImportToPlanner.Application",
            "Abstractions",
            "ITenantOperationalMetadataStore.cs");

        Assert.True(File.Exists(currentTenantAccessorPath));
        Assert.True(File.Exists(tenantMetadataStorePath));
    }

    [Fact]
    public void MaintainedSource_DoesNotContainRemovedRuntimeModeConcepts()
    {
        var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var sourceFiles = Directory.EnumerateFiles(Path.Combine(rootPath, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains("ImportToPlanner.Application", StringComparison.Ordinal)
                || path.Contains("ImportToPlanner.Domain", StringComparison.Ordinal)
                || path.Contains("ImportToPlanner.Web", StringComparison.Ordinal))
            .Where(path => !path.Contains("/bin/", StringComparison.Ordinal)
                && !path.Contains("/obj/", StringComparison.Ordinal)
                && !path.EndsWith("StartupConfigurationValidator.cs", StringComparison.Ordinal));

        foreach (var file in sourceFiles)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("DeploymentModeConfiguration", content, StringComparison.Ordinal);
            Assert.DoesNotContain("enum DeploymentMode", content, StringComparison.Ordinal);
        }
    }
}
