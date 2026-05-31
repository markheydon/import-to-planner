using ImportToPlanner.Application;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Infrastructure.Graph;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.ApiService.Commercial;

/// <summary>
/// Extension methods for registering and mapping the hosted commercial API service.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds hosted commercial API dependencies.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCommercialApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<CommercialModeOptions>()
            .Bind(configuration.GetSection(CommercialModeOptions.ConfigurationSectionName))
            .ValidateOnStart();
        services.AddSingleton(static serviceProvider => serviceProvider.GetRequiredService<IOptions<CommercialModeOptions>>().Value);
        services.AddHostedService<CommercialAccountRetentionHostedService>();

        services
            .AddApplication()
            .AddInfrastructure(configuration);

        return services;
    }

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
            ICommercialAccessUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await useCase.ResolveAccessAsync(
                request.SessionIdentity,
                request.CommercialModeEnabled,
                request.OccurredUtc,
                cancellationToken).ConfigureAwait(false);

            return Results.Ok(result);
        });

        group.MapPost("/profile/get", async (
            GetCommercialProfileRequest request,
            ICommercialProfileUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await useCase.GetProfileAsync(request.SessionIdentity, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/profile/delete", async (
            DeleteCommercialAccountRequest request,
            ICommercialProfileUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            await useCase.DeleteAccountAsync(request.SessionIdentity, request.OccurredUtc, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        });

        group.MapPost("/profile/restore", async (
            RestoreCommercialAccountRequest request,
            ICommercialProfileUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await useCase.RestoreAccountAsync(request.SessionIdentity, request.OccurredUtc, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/profile/purge-expired", async (
            PurgeExpiredCommercialDataRequest request,
            ICommercialProfileUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            var purgedCount = await useCase.PurgeExpiredAsync(request.AsOfUtc, request.BatchSize, cancellationToken).ConfigureAwait(false);
            return Results.Ok(purgedCount);
        });

        group.MapPost("/tenant-metadata/get", async (
            GetTenantOperationalMetadataRequest request,
            ITenantOperationalMetadataStore store,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.TenantId))
            {
                return Results.BadRequest("TenantId is required.");
            }

            var metadata = await store.GetAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
            return Results.Ok(metadata);
        });

        group.MapPost("/tenant-metadata/upsert", async (
            UpsertTenantOperationalMetadataRequest request,
            ITenantOperationalMetadataStore store,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Metadata is null)
            {
                return Results.BadRequest("Metadata is required.");
            }

            await store.UpsertAsync(request.Metadata, cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        });

        return app;
    }
}
