using System.Globalization;
using System.Reflection;
using ImportToPlanner.Application;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Application.Services;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Web.Infrastructure;

/// <summary>
/// Reads release labels and optional build metadata from the web host assembly.
/// </summary>
public sealed class ReleaseInformationQuery : IReleaseInformationQuery
{
    private const string ProductNameValue = "Import To Planner";
    private const string SourceRevisionMetadataKey = "SourceRevisionId";
    private const string BuildTimestampMetadataKey = "BuildTimestampUtc";

    private readonly ReleaseLabelFormatter formatter;
    private readonly ReleaseLabelPolicy policy;
    private readonly Lazy<ReleaseInformation> cached;

    public ReleaseInformationQuery(ReleaseLabelFormatter formatter, IOptions<ReleaseLabelPolicy> policyOptions)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        ArgumentNullException.ThrowIfNull(policyOptions);

        this.formatter = formatter;
        policy = policyOptions.Value;
        cached = new Lazy<ReleaseInformation>(LoadReleaseInformation, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public ValueTask<ReleaseInformation> GetAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(cached.Value);

    private ReleaseInformation LoadReleaseInformation()
    {
        var assembly = typeof(ReleaseInformationQuery).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        var releaseLabel = formatter.Format(informationalVersion, policy);
        var buildMetadata = ReadBuildMetadata(assembly);

        return new ReleaseInformation(ProductNameValue, releaseLabel, buildMetadata);
    }

    private static BuildMetadata? ReadBuildMetadata(Assembly assembly)
    {
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value, StringComparer.Ordinal);

        DateTimeOffset? builtAtUtc = null;
        if (metadata.TryGetValue(BuildTimestampMetadataKey, out var timestampValue)
            && !string.IsNullOrWhiteSpace(timestampValue)
            && DateTimeOffset.TryParse(
                timestampValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedTimestamp))
        {
            builtAtUtc = parsedTimestamp;
        }

        string? sourceRevisionId = null;
        if (metadata.TryGetValue(SourceRevisionMetadataKey, out var revisionValue)
            && !string.IsNullOrWhiteSpace(revisionValue))
        {
            sourceRevisionId = revisionValue.Length <= 12
                ? revisionValue
                : revisionValue[..12];
        }

        if (builtAtUtc is null && sourceRevisionId is null)
        {
            return null;
        }

        return new BuildMetadata(builtAtUtc, sourceRevisionId, SourceRevisionUrl: null);
    }
}
