using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Web.Features.CommercialAccounts.Backend;

internal sealed class CommercialApiServiceClient(HttpClient httpClient)
{
    public async Task<CommercialAccessDecision> ResolveAccessAsync(
        ResolveCommercialAccessRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await httpClient
            .PostAsJsonAsync("/internal/commercial/access/resolve", request, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await response.Content
            .ReadFromJsonAsync<CommercialAccessDecision>(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Commercial access response was empty.");
    }

    public async Task<CommercialAccount?> GetProfileAsync(
        GetCommercialProfileRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await httpClient
            .PostAsJsonAsync("/internal/commercial/profile/get", request, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await response.Content
            .ReadFromJsonAsync<CommercialAccount>(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteAccountAsync(
        DeleteCommercialAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await httpClient
            .PostAsJsonAsync("/internal/commercial/profile/delete", request, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }

    public async Task<CommercialAccountRestoreResult> RestoreAccountAsync(
        RestoreCommercialAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await httpClient
            .PostAsJsonAsync("/internal/commercial/profile/restore", request, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await response.Content
            .ReadFromJsonAsync<CommercialAccountRestoreResult>(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> PurgeExpiredAsync(
        PurgeExpiredCommercialDataRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await httpClient
            .PostAsJsonAsync("/internal/commercial/profile/purge-expired", request, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await response.Content
            .ReadFromJsonAsync<int>(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TenantOperationalMetadata?> GetTenantOperationalMetadataAsync(
        GetTenantOperationalMetadataRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await httpClient
            .PostAsJsonAsync("/internal/commercial/tenant-metadata/get", request, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await response.Content
            .ReadFromJsonAsync<TenantOperationalMetadata>(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpsertTenantOperationalMetadataAsync(
        UpsertTenantOperationalMetadataRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await httpClient
            .PostAsJsonAsync("/internal/commercial/tenant-metadata/upsert", request, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }
}
