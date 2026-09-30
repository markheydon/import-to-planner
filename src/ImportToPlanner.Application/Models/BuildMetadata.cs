namespace ImportToPlanner.Application.Models;

/// <summary>
/// Optional build diagnostics shown on About when supplied at build time.
/// </summary>
/// <param name="BuiltAtUtc">The UTC timestamp when the deployment was built, when known.</param>
/// <param name="SourceRevisionId">A short SHA or full commit identifier, when known.</param>
/// <param name="SourceRevisionUrl">
/// An optional link to the repository commit when a base URL template is configured.
/// </param>
public sealed record BuildMetadata(
    DateTimeOffset? BuiltAtUtc,
    string? SourceRevisionId,
    string? SourceRevisionUrl);
