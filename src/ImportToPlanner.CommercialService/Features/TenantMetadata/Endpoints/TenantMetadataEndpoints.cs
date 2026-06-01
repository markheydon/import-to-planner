using ImportToPlanner.CommercialService.Features.TenantMetadata.Models;
using ImportToPlanner.CommercialService.Features.TenantMetadata.Services;

namespace ImportToPlanner.CommercialService.Features.TenantMetadata.Endpoints;

/// <summary>
/// Maps endpoints related to tenant operational metadata.
/// </summary>
public static class TenantMetadataEndpoints
{
    /// <summary>
    /// Maps tenant metadata endpoints.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapTenantMetadataEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/tenant-metadata");

        group.MapPost("/get", async (
            GetTenantOperationalMetadataRequest request,
            ITenantMetadataService service,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.TenantId))
            {
                return Results.BadRequest("TenantId is required.");
            }

            var metadata = await service.GetAsync(
                request.TenantId,
                cancellationToken).ConfigureAwait(false);

            return Results.Ok(metadata);
        });

        group.MapPost("/upsert", async (
            UpsertTenantOperationalMetadataRequest request,
            ITenantMetadataService service,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Metadata is null)
            {
                return Results.BadRequest("Metadata is required.");
            }

            await service.UpsertAsync(
                request.Metadata,
                cancellationToken).ConfigureAwait(false);

            return Results.NoContent();
        });

        return app;
    }
}
