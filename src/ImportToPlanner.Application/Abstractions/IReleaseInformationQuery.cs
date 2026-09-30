using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Abstractions;

/// <summary>
/// Returns structured release and optional build metadata for the running deployment.
/// </summary>
public interface IReleaseInformationQuery
{
    /// <summary>
    /// Gets release information for the current application build.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Structured release information for About and support.</returns>
    ValueTask<ReleaseInformation> GetAsync(CancellationToken cancellationToken = default);
}
