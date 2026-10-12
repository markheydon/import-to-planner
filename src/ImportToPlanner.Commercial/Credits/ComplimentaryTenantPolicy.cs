using System.Collections.Concurrent;
using ImportToPlanner.Commercial.Abstractions;
using ImportToPlanner.Commercial.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Commercial.Credits;

/// <summary>
/// Parses the configured complimentary tenant allowlist on each membership check.
/// </summary>
public sealed class ComplimentaryTenantPolicy(
    IOptionsMonitor<CommercialCreditModeOptions> optionsMonitor,
    ILogger<ComplimentaryTenantPolicy> logger) : IComplimentaryTenantPolicy
{
    private readonly ConcurrentDictionary<string, byte> loggedInvalidTokens = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public bool IsComplimentary(string tenantId)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
        {
            return false;
        }

        var allowlist = optionsMonitor.CurrentValue.ComplimentaryTenantIds;
        if (string.IsNullOrWhiteSpace(allowlist))
        {
            return false;
        }

        foreach (var segment in allowlist.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (Guid.TryParse(segment, out var allowlistedGuid))
            {
                if (tenantGuid.Equals(allowlistedGuid))
                {
                    return true;
                }

                continue;
            }

            if (loggedInvalidTokens.TryAdd(segment, 0))
            {
                logger.LogWarning(
                    "Ignoring invalid complimentary tenant allowlist entry {AllowlistToken}",
                    segment);
            }
        }

        return false;
    }
}
