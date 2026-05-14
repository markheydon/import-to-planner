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

        var forbiddenTokens = new[] { "Microsoft.Graph", "Microsoft.Kiota", "MudBlazor", "CsvHelper", "PlannerGraph" };

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            foreach (var token in forbiddenTokens)
            {
                Assert.DoesNotContain(token, content, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
