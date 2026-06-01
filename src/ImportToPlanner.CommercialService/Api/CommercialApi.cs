using ImportToPlanner.CommercialService.CommercialAccounts.Models;
using ImportToPlanner.CommercialService.CommercialAccounts.Services;
using ImportToPlanner.CommercialService.TenantMetadata.Models;
using ImportToPlanner.CommercialService.TenantMetadata.Services;

namespace ImportToPlanner.CommercialService;

/// <summary>
/// Maps the internal HTTP surface for the commercial API service.
/// </summary>
public static class CommercialApi
{
    /// <summary>
    /// Maps internal hosted commercial API endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same web application for chaining.</returns>
    public static WebApplication MapCommercialApiEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/internal/commercial");

        group.MapPost("/access/resolve", async (
            ResolveCommercialAccessRequest request,
            CommercialAccessService service,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await service.ResolveAccessAsync(
                request.SessionIdentity,
                request.CommercialModeEnabled,
                request.OccurredUtc,
                cancellationToken).ConfigureAwait(false);

            return Results.Ok(result);
        });

        group.MapPost("/profile/get", async (
            GetCommercialProfileRequest request,
            CommercialProfileService service,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await service.GetProfileAsync(request.SessionIdentity, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/profile/delete", async (
            DeleteCommercialAccountRequest request,
            CommercialProfileService service,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            await service.DeleteAccountAsync(request.SessionIdentity, request.OccurredUtc, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        });

        group.MapPost("/profile/restore", async (
            RestoreCommercialAccountRequest request,
            CommercialProfileService service,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await service.RestoreAccountAsync(request.SessionIdentity, request.OccurredUtc, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/profile/purge-expired", async (
            PurgeExpiredCommercialDataRequest request,
            CommercialProfileService service,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            var purgedCount = await service.PurgeExpiredAsync(request.AsOfUtc, request.BatchSize, cancellationToken).ConfigureAwait(false);
            return Results.Ok(purgedCount);
        });

        group.MapPost("/tenant-metadata/get", async (
            GetTenantOperationalMetadataRequest request,
            TenantMetadataService service,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.TenantId))
            {
                return Results.BadRequest("TenantId is required.");
            }

            var metadata = await service.GetAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
            return Results.Ok(metadata);
        });

        group.MapPost("/tenant-metadata/upsert", async (
            UpsertTenantOperationalMetadataRequest request,
            TenantMetadataService service,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Metadata is null)
            {
                return Results.BadRequest("Metadata is required.");
            }

            await service.UpsertAsync(request.Metadata, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        });

        return app;
    }
}
