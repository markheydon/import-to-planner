using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Features.Import.Storage;

/// <summary>
/// Browser-local persistence for confirmed CSV column layouts.
/// </summary>
public interface IImportColumnMappingLayoutStore
{
    /// <summary>
    /// Loads a saved mapping for the supplied layout signature, if present.
    /// </summary>
    Task<SavedLayoutMapping?> GetAsync(string layoutSignature, CancellationToken cancellationToken);

    /// <summary>
    /// Persists a saved mapping for the supplied layout signature.
    /// </summary>
    Task SaveAsync(SavedLayoutMapping mapping, CancellationToken cancellationToken);
}
