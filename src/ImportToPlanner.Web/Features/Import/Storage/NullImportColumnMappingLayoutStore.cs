using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Features.Import.Storage;

/// <summary>
/// No-op layout store for environments without browser storage.
/// </summary>
public sealed class NullImportColumnMappingLayoutStore : IImportColumnMappingLayoutStore
{
    /// <inheritdoc />
    public Task<SavedLayoutMapping?> GetAsync(string layoutSignature, CancellationToken cancellationToken)
        => Task.FromResult<SavedLayoutMapping?>(null);

    /// <inheritdoc />
    public Task SaveAsync(SavedLayoutMapping mapping, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
