using ImportToPlanner.Application.Models;

namespace ImportToPlanner.Application.Abstractions;

/// <summary>
/// Resolves the signed-in operator identity for application policies.
/// </summary>
public interface IOperatorIdentityContextAccessor
{
    /// <summary>Gets the current session identity when authenticated.</summary>
    SessionIdentityContext? TryGetCurrent();
}
