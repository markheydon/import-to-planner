using TenantContextModel = ImportToPlanner.Application.TenantContext.Models.TenantContext;

namespace ImportToPlanner.Application.TenantContext.Abstractions;

/// <summary>
/// Provides access to the currently active tenant context.
/// </summary>
public interface ICurrentTenantContextAccessor
{
    /// <summary>
    /// Gets the required tenant context for the current execution path.
    /// </summary>
    /// <returns>The active tenant context.</returns>
    TenantContextModel GetRequiredContext();
}
