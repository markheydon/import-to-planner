using ImportToPlanner.CommercialService.Features.CommercialAccess.Endpoints;
using ImportToPlanner.CommercialService.Features.CommercialProfile.Endpoints;
using ImportToPlanner.CommercialService.Features.TenantMetadata.Endpoints;

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

        group.MapCommercialAccessEndpoints()
             .MapCommercialProfileEndpoints()
             .MapTenantMetadataEndpoints();

        return app;
    }
}
