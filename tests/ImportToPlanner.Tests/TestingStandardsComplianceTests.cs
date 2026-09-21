namespace ImportToPlanner.Tests;

public sealed class TestingStandardsComplianceTests
{
    private static readonly string[] ForbiddenTestPackages =
    [
        "Moq",
        "FluentAssertions",
        "AwesomeAssertions",
        "Shouldly",
        "NUnit",
        "MSTest",
        "Aspire.Hosting.Testing",
    ];

    private static readonly string[] RequiredTestPackages =
    [
        "xunit.v3",
        "NSubstitute",
    ];

    [Fact]
    public void CentralPackageManagement_DoesNotDeclareForbiddenTestPackages()
    {
        var rootPath = GetRepositoryRootPath();
        var packagesContent = File.ReadAllText(Path.Combine(rootPath, "Directory.Packages.props"));

        foreach (var forbiddenPackage in ForbiddenTestPackages)
        {
            Assert.DoesNotContain(
                $"Include=\"{forbiddenPackage}\"",
                packagesContent,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TestProjects_ReferenceRequiredPackagesAndAvoidForbiddenPackages()
    {
        var rootPath = GetRepositoryRootPath();
        var testProjectFiles = Directory.EnumerateFiles(
                Path.Combine(rootPath, "tests"),
                "*.csproj",
                SearchOption.AllDirectories)
            .Where(path => !path.Contains("/bin/", StringComparison.Ordinal) && !path.Contains("/obj/", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(testProjectFiles);

        foreach (var testProjectFile in testProjectFiles)
        {
            var projectContent = File.ReadAllText(testProjectFile);

            foreach (var requiredPackage in RequiredTestPackages)
            {
                Assert.Contains(
                    $"Include=\"{requiredPackage}\"",
                    projectContent,
                    StringComparison.OrdinalIgnoreCase);
            }

            foreach (var forbiddenPackage in ForbiddenTestPackages)
            {
                Assert.DoesNotContain(
                    $"Include=\"{forbiddenPackage}\"",
                    projectContent,
                    StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Solution_DoesNotIncludeAppHostTestProjects()
    {
        var rootPath = GetRepositoryRootPath();
        var solutionContent = File.ReadAllText(Path.Combine(rootPath, "ImportToPlanner.slnx"));

        Assert.DoesNotContain("AppHost.Tests", solutionContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Aspire.Hosting.Testing", solutionContent, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRepositoryRootPath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
}
