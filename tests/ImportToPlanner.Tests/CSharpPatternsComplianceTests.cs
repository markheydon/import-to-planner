namespace ImportToPlanner.Tests;

public sealed class CSharpPatternsComplianceTests
{
    private static readonly string[] ForbiddenSourceTokens =
    [
        "HttpContext.Current",
        ".Wait(",
        "GetAwaiter().GetResult()",
    ];

    [Fact]
    public void MaintainedSource_DoesNotUseForbiddenBlockingOrLegacyHttpPatterns()
    {
        var rootPath = GetRepositoryRootPath();
        var sourceFiles = Directory.EnumerateFiles(Path.Combine(rootPath, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("/bin/", StringComparison.Ordinal) && !path.Contains("/obj/", StringComparison.Ordinal));

        foreach (var file in sourceFiles)
        {
            var content = File.ReadAllText(file);
            foreach (var forbiddenToken in ForbiddenSourceTokens)
            {
                Assert.DoesNotContain(forbiddenToken, content, StringComparison.Ordinal);
            }
        }
    }

    private static string GetRepositoryRootPath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
}
