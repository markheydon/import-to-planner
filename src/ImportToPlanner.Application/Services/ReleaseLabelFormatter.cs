using System.Diagnostics.CodeAnalysis;
using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Services;

/// <summary>
/// Normalises assembly informational versions into honest deployment release labels (FR-005).
/// </summary>
public sealed class ReleaseLabelFormatter
{
    private const string LocalPreReleaseSuffix = "local";

    /// <summary>
    /// Builds a <see cref="DeploymentReleaseLabel"/> from an informational version string.
    /// </summary>
    /// <param name="informationalVersion">The assembly informational version (may include build metadata after <c>+</c>).</param>
    /// <param name="policy">Release label policy for the deployment.</param>
    /// <param name="builtFromOfficialReleaseTag">
    /// When <see langword="true"/>, the build was produced from an annotated <c>vX.Y.Z</c> tag
    /// (embedded at build time via assembly metadata).
    /// </param>
    /// <returns>A normalised deployment release label.</returns>
    [SuppressMessage("Performance", "CA1822:MarkMembersAsStatic", Justification = "Instance service resolved from DI for consistent application composition.")]
    public DeploymentReleaseLabel Format(
        string? informationalVersion,
        ReleaseLabelPolicy policy,
        bool builtFromOfficialReleaseTag = false)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return CreateFallbackLabel("0.0.0-local", isOfficialReleaseTag: false);
        }

        var trimmed = informationalVersion.Trim();
        var plusIndex = trimmed.IndexOf('+', StringComparison.Ordinal);
        var semverPart = plusIndex >= 0 ? trimmed[..plusIndex] : trimmed;
        var buildMetadata = plusIndex >= 0 && plusIndex < trimmed.Length - 1
            ? trimmed[(plusIndex + 1)..]
            : null;

        semverPart = semverPart.TrimStart('v', 'V');
        if (!TryParseCoreVersion(semverPart, out var coreVersion, out var preRelease))
        {
            return CreateFallbackLabel($"0.0.0-{LocalPreReleaseSuffix}", isOfficialReleaseTag: false);
        }

        var hasPreRelease = !string.IsNullOrEmpty(preRelease);
        var looksLikeStableShipping = !hasPreRelease && IsStableRelease(coreVersion);
        var isOfficialReleaseTag = false;

        if (looksLikeStableShipping)
        {
            if (policy.AllowUnqualifiedShippingLabel || builtFromOfficialReleaseTag)
            {
                isOfficialReleaseTag = true;
            }
            else
            {
                preRelease = LocalPreReleaseSuffix;
                hasPreRelease = true;
            }
        }

        var normalisedSemVer = hasPreRelease
            ? $"{coreVersion}-{preRelease}"
            : coreVersion;

        if (!string.IsNullOrEmpty(buildMetadata))
        {
            normalisedSemVer = $"{normalisedSemVer}+{buildMetadata}";
        }

        var displayValue = isOfficialReleaseTag
            ? $"v{coreVersion}"
            : normalisedSemVer;

        return new DeploymentReleaseLabel(displayValue, isOfficialReleaseTag, normalisedSemVer);
    }

    private static bool TryParseCoreVersion(string semverPart, out string coreVersion, out string? preRelease)
    {
        preRelease = null;
        coreVersion = string.Empty;

        var dashIndex = semverPart.IndexOf('-', StringComparison.Ordinal);
        var versionNumbers = dashIndex >= 0 ? semverPart[..dashIndex] : semverPart;
        if (dashIndex >= 0)
        {
            preRelease = semverPart[(dashIndex + 1)..];
        }

        var segments = versionNumbers.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 3)
        {
            return false;
        }

        for (var i = 0; i < 3; i++)
        {
            if (!int.TryParse(segments[i], out _))
            {
                return false;
            }
        }

        coreVersion = $"{segments[0]}.{segments[1]}.{segments[2]}";
        return true;
    }

    private static bool IsStableRelease(string coreVersion)
    {
        var segments = coreVersion.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length != 3)
        {
            return false;
        }

        return segments.All(segment => int.TryParse(segment, out var value) && value >= 0);
    }

    private static DeploymentReleaseLabel CreateFallbackLabel(string displayValue, bool isOfficialReleaseTag)
    {
        var normalised = displayValue.TrimStart('v', 'V');
        return new DeploymentReleaseLabel(displayValue, isOfficialReleaseTag, normalised);
    }
}
