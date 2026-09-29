using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Demo;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Web.Features.Demo;

/// <summary>
/// Evaluates deployment allowlists for demonstration mode controls.
/// </summary>
public sealed class DemoModeAuthorisationService(
    IOptions<DemoModeDeploymentPolicy> policyOptions,
    IOperatorIdentityContextAccessor sessionIdentityAccessor) : IDemoModeAuthorisationService
{
    private readonly DemoModeDeploymentPolicy policy = policyOptions.Value;

    /// <inheritdoc />
    public bool CanShowDemoControls() => policy.DemoControlsEnabled && IsAllowlistedOperator();

    /// <inheritdoc />
    public bool CanActivateDemo() => CanShowDemoControls();

    private bool IsAllowlistedOperator()
    {
        if (policy.OperatorAllowlist.Count == 0)
        {
            return false;
        }

        var identity = sessionIdentityAccessor.TryGetCurrent();
        if (identity is null)
        {
            return false;
        }

        foreach (var entry in policy.OperatorAllowlist)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                continue;
            }

            if (Guid.TryParse(entry, out _))
            {
                if (string.Equals(entry, identity.UserId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                continue;
            }

            var normalisedEntry = entry.Trim().ToLowerInvariant();
            var email = identity.EmailAddress?.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(email) && string.Equals(normalisedEntry, email, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
