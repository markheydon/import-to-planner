namespace ImportToPlanner.Application.Models;

/// <summary>
/// Human-readable SemVer string shown in About and quoted in support.
/// </summary>
/// <param name="DisplayValue">
/// The label shown to users (for example <c>v1.0.0</c> or <c>1.0.0-preview.3+abc1234</c>).
/// Official tag builds MUST match the annotated tag name (including <c>v</c> when used on the tag).
/// Non-tag builds MUST include pre-release identifiers unless <see cref="ReleaseLabelPolicy"/> permits
/// an unqualified shipping label.
/// </param>
/// <param name="IsOfficialReleaseTag">
/// Whether the deployment presents an official annotated <c>vX.Y.Z</c> shipping label (tag build or explicit policy).
/// </param>
/// <param name="NormalisedSemVer">
/// Parsed SemVer without a leading <c>v</c> where applicable, for comparisons.
/// </param>
public sealed record DeploymentReleaseLabel(
    string DisplayValue,
    bool IsOfficialReleaseTag,
    string NormalisedSemVer);
