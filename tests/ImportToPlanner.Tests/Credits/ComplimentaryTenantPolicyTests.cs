using ImportToPlanner.Commercial.Configuration;
using ImportToPlanner.Commercial.Credits;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Tests.Credits;

public sealed class ComplimentaryTenantPolicyTests
{
    private static readonly Guid AllowlistedTenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void IsComplimentary_WhenAllowlistEmpty_ReturnsFalse()
    {
        var policy = CreatePolicy(string.Empty);

        Assert.False(policy.IsComplimentary(AllowlistedTenant.ToString()));
    }

    [Fact]
    public void IsComplimentary_WhenTenantMatchesAllowlistedGuid_ReturnsTrue()
    {
        var policy = CreatePolicy(AllowlistedTenant.ToString());

        Assert.True(policy.IsComplimentary(AllowlistedTenant.ToString()));
    }

    [Fact]
    public void IsComplimentary_WhenTenantDoesNotMatch_ReturnsFalse()
    {
        var policy = CreatePolicy(AllowlistedTenant.ToString());

        Assert.False(policy.IsComplimentary("22222222-2222-2222-2222-222222222222"));
    }

    [Fact]
    public void IsComplimentary_IgnoresWhitespaceAndDuplicateGuids()
    {
        var policy = CreatePolicy($"  {AllowlistedTenant} , {AllowlistedTenant}  ");

        Assert.True(policy.IsComplimentary(AllowlistedTenant.ToString()));
    }

    [Fact]
    public void IsComplimentary_WhenAllowlistContainsInvalidToken_StillAllowlistsValidGuid()
    {
        var policy = CreatePolicy($"not-a-guid,{AllowlistedTenant}");

        Assert.True(policy.IsComplimentary(AllowlistedTenant.ToString()));
        Assert.False(policy.IsComplimentary("33333333-3333-3333-3333-333333333333"));
    }

    [Fact]
    public void IsComplimentary_WhenConfigurationChanges_UsesLatestAllowlist()
    {
        var options = new CommercialCreditModeOptions();
        var monitor = new TestOptionsMonitor(options);
        var policy = new ComplimentaryTenantPolicy(monitor, NullLogger<ComplimentaryTenantPolicy>.Instance);

        Assert.False(policy.IsComplimentary(AllowlistedTenant.ToString()));

        options.ComplimentaryTenantIds = AllowlistedTenant.ToString();
        monitor.RaiseChanged();

        Assert.True(policy.IsComplimentary(AllowlistedTenant.ToString()));

        options.ComplimentaryTenantIds = string.Empty;
        monitor.RaiseChanged();

        Assert.False(policy.IsComplimentary(AllowlistedTenant.ToString()));
    }

    private static ComplimentaryTenantPolicy CreatePolicy(string allowlist)
    {
        var options = new CommercialCreditModeOptions { ComplimentaryTenantIds = allowlist };
        var monitor = new TestOptionsMonitor(options);
        return new ComplimentaryTenantPolicy(monitor, NullLogger<ComplimentaryTenantPolicy>.Instance);
    }

    private sealed class TestOptionsMonitor(CommercialCreditModeOptions current) : IOptionsMonitor<CommercialCreditModeOptions>
    {
        private event Action<CommercialCreditModeOptions, string?>? listeners;

        public CommercialCreditModeOptions CurrentValue => current;

        public CommercialCreditModeOptions Get(string? name) => current;

        public IDisposable OnChange(Action<CommercialCreditModeOptions, string?> listener)
        {
            listeners += listener;
            return new Subscription(() => listeners -= listener);
        }

        public void RaiseChanged() => listeners?.Invoke(current, null);

        private sealed class Subscription(Action dispose) : IDisposable
        {
            public void Dispose() => dispose();
        }
    }
}
