using ImportToPlanner.CommercialService.Common.Models;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Models;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Services;
using ImportToPlanner.CommercialService.Features.CommercialProfile.Models;
using ImportToPlanner.CommercialService.Features.CommercialProfile.Services;
using ImportToPlanner.CommercialService.Tests.TestDoubles;
using Moq;

namespace ImportToPlanner.CommercialService.Tests;

public sealed class CommercialAccountLifecycleUseCaseTests
{
    [Fact]
    public async Task DeleteAccountAsync_MarksAccountDeletedAndAppendsAuditEvent()
    {
        var accounts = new List<CommercialAccount>();
        var events = new List<AccountAuditEvent>();

        var accountStore = new Mock<ICommercialAccountsService>();
        var auditStore = new Mock<ICommercialAuditService>();

        var identity = new SessionIdentityContext("tenant-001", "user-001", "user@contoso.com", "Contoso");

        var createdUtc = new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero);
        var deletedUtc = new DateTimeOffset(2026, 5, 28, 9, 0, 0, TimeSpan.Zero);

        // Setup Create + Get + Update behaviour
        accountStore.Setup(s => s.CreateAsync(It.IsAny<CommercialAccount>(), It.IsAny<CancellationToken>()))
            .Returns<CommercialAccount, CancellationToken>((account, _) =>
            {
                accounts.Add(account);
                return Task.CompletedTask;
            });

        accountStore.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((tenantId, userId, _) =>
            {
                return Task.FromResult(accounts.SingleOrDefault(x => x.TenantId == tenantId && x.UserId == userId));
            });

        accountStore.Setup(s => s.MarkDeletedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, DateTimeOffset, DateTimeOffset, CancellationToken>((tenantId, userId, deleted, retention, _) =>
            {
                var current = accounts.Single(x => x.TenantId == tenantId && x.UserId == userId);
                accounts.Remove(current);
                accounts.Add(current with
                {
                    Status = CommercialAccountStatus.Deleted,
                    DeletedUtc = deleted,
                    RetentionExpiresUtc = retention
                });
                return Task.CompletedTask;
            });

        auditStore.Setup(s => s.AppendAsync(It.IsAny<AccountAuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns<AccountAuditEvent, CancellationToken>((evt, _) =>
            {
                events.Add(evt);
                return Task.CompletedTask;
            });

        // Seed account
        await accountStore.Object.CreateAsync(
            new CommercialAccount(
                identity.TenantId,
                identity.UserId,
                createdUtc,
                CommercialAccountStatus.Active,
                null,
                null,
                null,
                createdUtc),
            CancellationToken.None);

        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var service = serviceProvider.GetRequiredService<CommercialProfileService>();

        await service.DeleteAccountAsync(identity, deletedUtc, CancellationToken.None);

        var deletedAccount = Assert.Single(accounts);
        Assert.Equal(CommercialAccountStatus.Deleted, deletedAccount.Status);
        Assert.Equal(deletedUtc, deletedAccount.DeletedUtc);
        Assert.Equal(deletedUtc.AddMonths(6), deletedAccount.RetentionExpiresUtc);

        var deletedAuditEvent = Assert.Single(events, e => e.EventType == AccountAuditEventType.AccountDeleted);
        Assert.Equal("account_deleted", deletedAuditEvent.Outcome);
    }

    [Fact]
    public async Task RestoreAccountAsync_WhenDeletedWithinRetention_RestoresAccountAndAppendsAuditEvent()
    {
        var accounts = new List<CommercialAccount>();
        var events = new List<AccountAuditEvent>();

        var accountStore = new Mock<ICommercialAccountsService>();
        var auditStore = new Mock<ICommercialAuditService>();

        var identity = new SessionIdentityContext("tenant-001", "user-001", "user@contoso.com", "Contoso");

        var deletedUtc = new DateTimeOffset(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);
        var restoredUtc = new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero);

        accounts.Add(new CommercialAccount(
            identity.TenantId,
            identity.UserId,
            deletedUtc.AddMonths(-2),
            CommercialAccountStatus.Deleted,
            deletedUtc,
            deletedUtc.AddMonths(6),
            null,
            deletedUtc));

        accountStore.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts.Single());

        accountStore.Setup(s => s.RestoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, DateTimeOffset, CancellationToken>((tenantId, userId, restored, _) =>
            {
                var current = accounts.Single();
                accounts.Clear();
                accounts.Add(current with
                {
                    Status = CommercialAccountStatus.Active,
                    DeletedUtc = null,
                    RetentionExpiresUtc = null,
                    RestoredUtc = restored
                });
                return Task.CompletedTask;
            });

        auditStore.Setup(s => s.AppendAsync(It.IsAny<AccountAuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AccountAuditEvent, CancellationToken>((evt, _) => events.Add(evt))
            .Returns(Task.CompletedTask);

        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var service = serviceProvider.GetRequiredService<CommercialProfileService>();

        var result = await service.RestoreAccountAsync(identity, restoredUtc, CancellationToken.None);

        Assert.Equal(CommercialAccountRestoreResult.Restored, result);

        var restoredAccount = Assert.Single(accounts);
        Assert.Equal(CommercialAccountStatus.Active, restoredAccount.Status);
        Assert.Null(restoredAccount.DeletedUtc);
        Assert.Null(restoredAccount.RetentionExpiresUtc);
        Assert.Equal(restoredUtc, restoredAccount.RestoredUtc);

        var restoredAuditEvent = Assert.Single(events, e => e.EventType == AccountAuditEventType.AccountRestored);
        Assert.Equal("account_restored", restoredAuditEvent.Outcome);
    }

    [Fact]
    public async Task ResolveAccessAsync_WhenAccountDeleted_ReturnsOfferRestoreDecision()
    {
        var accountStore = new Mock<ICommercialAccountsService>();
        var auditStore = new Mock<ICommercialAuditService>();

        var identity = new SessionIdentityContext("tenant-001", "user-001", "user@contoso.com", "Contoso");

        var deletedUtc = new DateTimeOffset(2026, 5, 1, 7, 0, 0, TimeSpan.Zero);

        accountStore.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommercialAccount(
                identity.TenantId,
                identity.UserId,
                deletedUtc.AddMonths(-2),
                CommercialAccountStatus.Deleted,
                deletedUtc,
                deletedUtc.AddMonths(6),
                null,
                deletedUtc));

        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var accessService = serviceProvider.GetRequiredService<CommercialAccessService>();

        var decision = await accessService.ResolveAccessAsync(
            identity,
            true,
            new DateTimeOffset(2026, 5, 28, 11, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        Assert.Equal(CommercialAccessDecisionType.OfferRestore, decision.Decision);
        Assert.Equal(CommercialAccountStatus.Deleted, decision.AccountStatus);
        Assert.False(decision.ShouldSignOut);
        Assert.NotNull(decision.RetentionExpiresUtc);
    }

    private static ServiceProvider BuildServiceProvider(
        Mock<ICommercialAccountsService> accountStore,
        Mock<ICommercialAuditService> auditStore)
    {
        var services = new ServiceCollection();

        services.AddSingleton(accountStore.Object);
        services.AddSingleton(auditStore.Object);
        services.AddSingleton<CommercialAccountsService>(serviceProvider => new MockBackedCommercialAccountsService(serviceProvider.GetRequiredService<ICommercialAccountsService>()));
        services.AddSingleton<CommercialAuditService>(serviceProvider => new MockBackedCommercialAuditService(serviceProvider.GetRequiredService<ICommercialAuditService>()));

        services.AddSingleton<CommercialProfileService>();
        services.AddSingleton<CommercialAccessService>();

        return services.BuildServiceProvider();
    }
}
