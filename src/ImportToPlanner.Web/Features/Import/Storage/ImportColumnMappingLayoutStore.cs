using ImportToPlanner.Application.Models;
using Microsoft.JSInterop;

namespace ImportToPlanner.Web.Features.Import.Storage;

/// <summary>
/// Persists column layout mappings in browser local storage.
/// </summary>
public sealed class ImportColumnMappingLayoutStore(IJSRuntime jsRuntime) : IImportColumnMappingLayoutStore
{
    private const string StoreKey = "import-to-planner-column-mappings-v1";

    /// <inheritdoc />
    public async Task<SavedLayoutMapping?> GetAsync(string layoutSignature, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutSignature);
        cancellationToken.ThrowIfCancellationRequested();

        var payload = await jsRuntime.InvokeAsync<StoredLayoutPayload?>(
            "importToPlannerColumnMappingStorage.getLayout",
            cancellationToken,
            StoreKey,
            layoutSignature);

        if (payload is null)
        {
            return null;
        }

        return new SavedLayoutMapping
        {
            LayoutSignature = layoutSignature,
            Assignments = payload.Assignments ?? new Dictionary<string, string?>(),
            UpdatedUtc = payload.UpdatedUtc,
        };
    }

    /// <inheritdoc />
    public async Task SaveAsync(SavedLayoutMapping mapping, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        cancellationToken.ThrowIfCancellationRequested();

        var payload = new StoredLayoutPayload
        {
            Assignments = mapping.Assignments.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            UpdatedUtc = mapping.UpdatedUtc,
        };

        await jsRuntime.InvokeVoidAsync(
            "importToPlannerColumnMappingStorage.saveLayout",
            cancellationToken,
            StoreKey,
            mapping.LayoutSignature,
            payload);
    }

    private sealed class StoredLayoutPayload
    {
        public Dictionary<string, string?>? Assignments { get; set; }

        public DateTimeOffset UpdatedUtc { get; set; }
    }
}
