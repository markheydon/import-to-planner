using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Abstractions;

/// <summary>
/// Provides access to the currently active tenant context.
/// </summary>
public interface ICurrentTenantContextAccessor
{
    /// <summary>
    /// Gets the required tenant context for the current execution path.
    /// </summary>
    /// <returns>The active tenant context.</returns>
    TenantContext GetRequiredContext();
}
