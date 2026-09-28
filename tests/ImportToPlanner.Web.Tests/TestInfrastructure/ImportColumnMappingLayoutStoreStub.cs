using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class ImportColumnMappingLayoutStoreStub : IImportColumnMappingLayoutStore
{
    private readonly Dictionary<string, SavedLayoutMapping> layouts = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, SavedLayoutMapping> Layouts => layouts;

    public Task<SavedLayoutMapping?> GetAsync(string layoutSignature, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        layouts.TryGetValue(layoutSignature, out var mapping);
        return Task.FromResult(mapping);
    }

    public Task SaveAsync(SavedLayoutMapping mapping, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        cancellationToken.ThrowIfCancellationRequested();
        layouts[mapping.LayoutSignature] = mapping;
        return Task.CompletedTask;
    }
}
