namespace ImportToPlanner.Web.Features.CommercialAccounts.Backend;

internal sealed class CommercialApiServiceClient(HttpClient httpClient)
{
    public Task<CommercialAccessDecision> ResolveAccessAsync(
        SessionIdentityContext sessionIdentity,
        bool commercialModeEnabled,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);

        return ResolveAccessAsync(
            new ResolveCommercialAccessRequest(sessionIdentity, commercialModeEnabled, occurredUtc),
            cancellationToken);
    }

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

    public Task<CommercialAccount?> GetProfileAsync(SessionIdentityContext sessionIdentity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);

        return GetProfileAsync(new GetCommercialProfileRequest(sessionIdentity), cancellationToken);
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

    public Task DeleteAccountAsync(
        SessionIdentityContext sessionIdentity,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);

        return DeleteAccountAsync(new DeleteCommercialAccountRequest(sessionIdentity, occurredUtc), cancellationToken);
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

    public Task<CommercialAccountRestoreResult> RestoreAccountAsync(
        SessionIdentityContext sessionIdentity,
        DateTimeOffset occurredUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIdentity);

        return RestoreAccountAsync(new RestoreCommercialAccountRequest(sessionIdentity, occurredUtc), cancellationToken);
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

    public Task<int> PurgeExpiredAsync(DateTimeOffset asOfUtc, int batchSize, CancellationToken cancellationToken)
    {
        return PurgeExpiredAsync(new PurgeExpiredCommercialDataRequest(asOfUtc, batchSize), cancellationToken);
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

    public async Task<ImportToPlanner.Application.Models.TenantOperationalMetadata?> GetTenantOperationalMetadataAsync(
        GetTenantOperationalMetadataRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await httpClient
            .PostAsJsonAsync("/internal/commercial/tenant-metadata/get", request, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await response.Content
            .ReadFromJsonAsync<ImportToPlanner.Application.Models.TenantOperationalMetadata>(cancellationToken)
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
