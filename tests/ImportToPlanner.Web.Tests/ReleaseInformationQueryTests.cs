using System.Reflection;
using System.Reflection.Emit;
using ImportToPlanner.Application;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Application.Services;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Web.Tests;

public sealed class ReleaseInformationQueryTests
{
    [Fact]
    public async Task GetAsync_ReturnsImportToPlannerProductName()
    {
        var query = CreateQuery();

        var result = await query.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Import To Planner", result.ProductName);
    }

    [Fact]
    public async Task GetAsync_ReleaseLabelMatchesFormatterAppliedToAssemblyInformationalVersion()
    {
        var policy = new ReleaseLabelPolicy();
        var formatter = new ReleaseLabelFormatter();
        var query = CreateQuery(policy);
        var informationalVersion = typeof(ReleaseInformationQuery).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        var expectedLabel = formatter.Format(informationalVersion, policy);

        var result = await query.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(expectedLabel, result.ReleaseLabel);
    }

    [Fact]
    public async Task GetAsync_BuildMetadataMatchesAssemblyMetadataAttributes()
    {
        var query = CreateQuery();
        var expectedMetadata = ReadBuildMetadataFromAssembly(typeof(ReleaseInformationQuery).Assembly);

        var result = await query.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(expectedMetadata, result.BuildMetadata);
    }

    [Fact]
    public async Task GetAsync_ReturnsCachedReleaseInformationOnSubsequentCalls()
    {
        var query = CreateQuery();

        var first = await query.GetAsync(TestContext.Current.CancellationToken);
        var second = await query.GetAsync(TestContext.Current.CancellationToken);

        Assert.Same(first, second);
    }

    [Fact]
    public void ReadBuildMetadata_WhenNoMetadataAttributes_ReturnsNull()
    {
        var assembly = CreateAssemblyWithMetadata();

        var metadata = ReadBuildMetadataFromAssembly(assembly);

        Assert.Null(metadata);
    }

    [Fact]
    public void ReadBuildMetadata_WhenSourceRevisionIdProvided_MapsShortSha()
    {
        var assembly = CreateAssemblyWithMetadata(("SourceRevisionId", "abc1234"));

        var metadata = ReadBuildMetadataFromAssembly(assembly);

        Assert.NotNull(metadata);
        Assert.Equal("abc1234", metadata.SourceRevisionId);
        Assert.Null(metadata.BuiltAtUtc);
        Assert.Null(metadata.SourceRevisionUrl);
    }

    [Fact]
    public void ReadBuildMetadata_WhenSourceRevisionIdExceedsTwelveCharacters_TruncatesToTwelve()
    {
        var assembly = CreateAssemblyWithMetadata(("SourceRevisionId", "0123456789abcdef"));

        var metadata = ReadBuildMetadataFromAssembly(assembly);

        Assert.NotNull(metadata);
        Assert.Equal("0123456789ab", metadata.SourceRevisionId);
    }

    [Fact]
    public void ReadBuildMetadata_WhenBuildTimestampUtcProvided_ParsesAsUniversalOffset()
    {
        var assembly = CreateAssemblyWithMetadata(("BuildTimestampUtc", "2026-09-30T12:00:00Z"));

        var metadata = ReadBuildMetadataFromAssembly(assembly);

        Assert.NotNull(metadata);
        Assert.Equal(new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero), metadata.BuiltAtUtc);
        Assert.Null(metadata.SourceRevisionId);
    }

    [Fact]
    public void ReadBuildMetadata_WhenBothProvided_ReturnsCombinedMetadata()
    {
        var assembly = CreateAssemblyWithMetadata(
            ("SourceRevisionId", "deadbeef"),
            ("BuildTimestampUtc", "2026-09-30T08:15:30Z"));

        var metadata = ReadBuildMetadataFromAssembly(assembly);

        Assert.NotNull(metadata);
        Assert.Equal("deadbeef", metadata.SourceRevisionId);
        Assert.Equal(new DateTimeOffset(2026, 9, 30, 8, 15, 30, TimeSpan.Zero), metadata.BuiltAtUtc);
        Assert.Null(metadata.SourceRevisionUrl);
    }

    [Fact]
    public void ReadBuildMetadata_WhenTimestampInvalid_StillMapsSourceRevisionId()
    {
        var assembly = CreateAssemblyWithMetadata(
            ("SourceRevisionId", "cafebabe"),
            ("BuildTimestampUtc", "not-a-timestamp"));

        var metadata = ReadBuildMetadataFromAssembly(assembly);

        Assert.NotNull(metadata);
        Assert.Equal("cafebabe", metadata.SourceRevisionId);
        Assert.Null(metadata.BuiltAtUtc);
    }

    private static ReleaseInformationQuery CreateQuery(ReleaseLabelPolicy? policy = null)
    {
        policy ??= new ReleaseLabelPolicy();
        var formatter = new ReleaseLabelFormatter();
        return new ReleaseInformationQuery(formatter, Options.Create(policy));
    }

    private static BuildMetadata? ReadBuildMetadataFromAssembly(Assembly assembly)
    {
        var method = typeof(ReleaseInformationQuery).GetMethod(
            "ReadBuildMetadata",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        return (BuildMetadata?)method.Invoke(null, [assembly]);
    }

    private static AssemblyBuilder CreateAssemblyWithMetadata(params (string Key, string Value)[] metadata)
    {
        var assemblyName = new AssemblyName($"ReleaseMetadataTest_{Guid.NewGuid():N}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.RunAndCollect);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        _ = moduleBuilder.DefineType("Placeholder").CreateType();

        var attributeConstructor = typeof(AssemblyMetadataAttribute).GetConstructor([typeof(string), typeof(string)]);
        Assert.NotNull(attributeConstructor);

        foreach (var (key, value) in metadata)
        {
            var attributeBuilder = new CustomAttributeBuilder(attributeConstructor, [key, value]);
            assemblyBuilder.SetCustomAttribute(attributeBuilder);
        }

        return assemblyBuilder;
    }
}
