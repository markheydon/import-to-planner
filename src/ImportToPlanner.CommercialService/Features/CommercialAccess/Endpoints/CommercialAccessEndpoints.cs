using ImportToPlanner.CommercialService.Features.CommercialAccess.Models;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Services;

namespace ImportToPlanner.CommercialService.Features.CommercialAccess.Endpoints;

/// <summary>
/// Maps endpoints related to resolving commercial access.
/// </summary>
public static class CommercialAccessEndpoints
{
    /// <summary>
    /// Maps commercial access endpoints.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapCommercialAccessEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/access");

        group.MapPost("/resolve", async (
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

        return app;
    }
}
