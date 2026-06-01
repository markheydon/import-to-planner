using ImportToPlanner.CommercialService.Common.Models;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Models;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Services;
using ImportToPlanner.CommercialService.Features.CommercialProfile.Models;
using ImportToPlanner.CommercialService.Features.CommercialProfile.Services;
using ImportToPlanner.CommercialService.Tests.TestDoubles;

namespace ImportToPlanner.CommercialService.Tests;

public sealed class CommercialAccountLifecycleUseCaseTests
{
    [Fact]
    public async Task DeleteAccountAsync_MarksAccountDeletedAndAppendsAuditEvent()
    {
        var accountStore = new CommercialAccountStoreStub();
        var auditStore = new CommercialAuditStoreStub();
        var identity = new SessionIdentityContext("tenant-001", "user-001", "user@contoso.com", "Contoso");
        var createdUtc = new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero);
        var deletedUtc = new DateTimeOffset(2026, 5, 28, 9, 0, 0, TimeSpan.Zero);

        await accountStore.CreateAsync(
            new CommercialAccount(
                identity.TenantId,
                identity.UserId,
                createdUtc,
                CommercialAccountStatus.Active,
                DeletedUtc: null,
                RetentionExpiresUtc: null,
                RestoredUtc: null,
                LastSignInOutcomeUtc: createdUtc),
            CancellationToken.None);

        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var service = serviceProvider.GetRequiredService<CommercialProfileService>();

        await service.DeleteAccountAsync(identity, deletedUtc, CancellationToken.None);

        var deletedAccount = Assert.Single(accountStore.Accounts);
        Assert.Equal(CommercialAccountStatus.Deleted, deletedAccount.Status);
        Assert.Equal(deletedUtc, deletedAccount.DeletedUtc);
        Assert.Equal(deletedUtc.AddMonths(6), deletedAccount.RetentionExpiresUtc);

        var deletedAuditEvent = Assert.Single(auditStore.Events, evt => evt.EventType == AccountAuditEventType.AccountDeleted);
        Assert.Equal("account_deleted", deletedAuditEvent.Outcome);
    }

    [Fact]
    public async Task RestoreAccountAsync_WhenDeletedWithinRetention_RestoresAccountAndAppendsAuditEvent()
    {
        var accountStore = new CommercialAccountStoreStub();
        var auditStore = new CommercialAuditStoreStub();
        var identity = new SessionIdentityContext("tenant-001", "user-001", "user@contoso.com", "Contoso");
        var deletedUtc = new DateTimeOffset(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);
        var restoredUtc = new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero);

        await accountStore.CreateAsync(
            new CommercialAccount(
                identity.TenantId,
                identity.UserId,
                CreatedUtc: deletedUtc.AddMonths(-2),
                CommercialAccountStatus.Deleted,
                DeletedUtc: deletedUtc,
                RetentionExpiresUtc: deletedUtc.AddMonths(6),
                RestoredUtc: null,
                LastSignInOutcomeUtc: deletedUtc),
            CancellationToken.None);

        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var service = serviceProvider.GetRequiredService<CommercialProfileService>();

        var result = await service.RestoreAccountAsync(identity, restoredUtc, CancellationToken.None);

        Assert.Equal(CommercialAccountRestoreResult.Restored, result);

        var restoredAccount = Assert.Single(accountStore.Accounts);
        Assert.Equal(CommercialAccountStatus.Active, restoredAccount.Status);
        Assert.Null(restoredAccount.DeletedUtc);
        Assert.Null(restoredAccount.RetentionExpiresUtc);
        Assert.Equal(restoredUtc, restoredAccount.RestoredUtc);

        var restoredAuditEvent = Assert.Single(auditStore.Events, evt => evt.EventType == AccountAuditEventType.AccountRestored);
        Assert.Equal("account_restored", restoredAuditEvent.Outcome);
    }

    [Fact]
    public async Task RestoreAccountAsync_WhenRetentionExpired_ReturnsRetentionExpired()
    {
        var accountStore = new CommercialAccountStoreStub();
        var auditStore = new CommercialAuditStoreStub();
        var identity = new SessionIdentityContext("tenant-001", "user-001", "user@contoso.com", "Contoso");
        var deletedUtc = new DateTimeOffset(2025, 5, 20, 9, 0, 0, TimeSpan.Zero);
        var restoreAttemptUtc = new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero);

        await accountStore.CreateAsync(
            new CommercialAccount(
                identity.TenantId,
                identity.UserId,
                CreatedUtc: deletedUtc.AddMonths(-2),
                CommercialAccountStatus.Deleted,
                DeletedUtc: deletedUtc,
                RetentionExpiresUtc: deletedUtc.AddMonths(6),
                RestoredUtc: null,
                LastSignInOutcomeUtc: deletedUtc),
            CancellationToken.None);

        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var service = serviceProvider.GetRequiredService<CommercialProfileService>();

        var result = await service.RestoreAccountAsync(identity, restoreAttemptUtc, CancellationToken.None);

        Assert.Equal(CommercialAccountRestoreResult.RetentionExpired, result);
        Assert.DoesNotContain(auditStore.Events, evt => evt.EventType == AccountAuditEventType.AccountRestored);
    }

    [Fact]
    public async Task ResolveAccessAsync_WhenAccountDeleted_ReturnsOfferRestoreDecision()
    {
        var accountStore = new CommercialAccountStoreStub();
        var auditStore = new CommercialAuditStoreStub();
        var identity = new SessionIdentityContext("tenant-001", "user-001", "user@contoso.com", "Contoso");
        var deletedUtc = new DateTimeOffset(2026, 5, 1, 7, 0, 0, TimeSpan.Zero);

        await accountStore.CreateAsync(
            new CommercialAccount(
                identity.TenantId,
                identity.UserId,
                CreatedUtc: deletedUtc.AddMonths(-2),
                CommercialAccountStatus.Deleted,
                DeletedUtc: deletedUtc,
                RetentionExpiresUtc: deletedUtc.AddMonths(6),
                RestoredUtc: null,
                LastSignInOutcomeUtc: deletedUtc),
            CancellationToken.None);

        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var accessService = serviceProvider.GetRequiredService<CommercialAccessService>();

        var decision = await accessService.ResolveAccessAsync(
            identity,
            commercialModeEnabled: true,
            occurredUtc: new DateTimeOffset(2026, 5, 28, 11, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        Assert.Equal(CommercialAccessDecisionType.OfferRestore, decision.Decision);
        Assert.Equal(CommercialAccountStatus.Deleted, decision.AccountStatus);
        Assert.False(decision.ShouldSignOut);
        Assert.NotNull(decision.RetentionExpiresUtc);
    }

    private static ServiceProvider BuildServiceProvider(CommercialAccountStoreStub accountStore, CommercialAuditStoreStub auditStore)
    {
        ArgumentNullException.ThrowIfNull(accountStore);
        ArgumentNullException.ThrowIfNull(auditStore);

        var services = new ServiceCollection();
        services.AddSingleton<CommercialAccountsService>(_ => accountStore);
        services.AddSingleton<CommercialAuditService>(_ => auditStore);
        services.AddSingleton<CommercialProfileService>();
        services.AddSingleton<CommercialAccessService>();

        return services.BuildServiceProvider();
    }
}
