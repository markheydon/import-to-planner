using ImportToPlanner.CommercialService.Features.CommercialProfile.Models;
using ImportToPlanner.CommercialService.Features.CommercialProfile.Services;

namespace ImportToPlanner.CommercialService.Features.CommercialProfile.Endpoints;

/// <summary>
/// Maps endpoints related to commercial profile lifecycle operations.
/// </summary>
public static class CommercialProfileEndpoints
{
    /// <summary>
    /// Maps commercial profile endpoints.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapCommercialProfileEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/profile");

        group.MapPost("/get", async (
            GetCommercialProfileRequest request,
            CommercialProfileService service,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await service.GetProfileAsync(
                request.SessionIdentity,
                cancellationToken).ConfigureAwait(false);

            return Results.Ok(result);
        });

        group.MapPost("/delete", async (
            DeleteCommercialAccountRequest request,
            CommercialProfileService service,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            await service.DeleteAccountAsync(
                request.SessionIdentity,
                request.OccurredUtc,
                cancellationToken).ConfigureAwait(false);

            return Results.NoContent();
        });

        group.MapPost("/restore", async (
            RestoreCommercialAccountRequest request,
            CommercialProfileService service,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await service.RestoreAccountAsync(
                request.SessionIdentity,
                request.OccurredUtc,
                cancellationToken).ConfigureAwait(false);

            return Results.Ok(result);
        });

        group.MapPost("/purge-expired", async (
            PurgeExpiredCommercialDataRequest request,
            CommercialProfileService service,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            var purgedCount = await service.PurgeExpiredAsync(
                request.AsOfUtc,
                request.BatchSize,
                cancellationToken).ConfigureAwait(false);

            return Results.Ok(purgedCount);
        });

        return app;
    }
}
