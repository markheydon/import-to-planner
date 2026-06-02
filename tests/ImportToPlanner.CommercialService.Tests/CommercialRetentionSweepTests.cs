using ImportToPlanner.CommercialService.Features.CommercialAccess.Models;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Services;
using ImportToPlanner.CommercialService.Features.CommercialProfile.Services;
using ImportToPlanner.CommercialService.Tests.TestDoubles;
using Moq;

namespace ImportToPlanner.CommercialService.Tests;

public sealed class CommercialRetentionSweepTests
{
    [Fact]
    public async Task PurgeExpiredAsync_RemovesExpiredDeletedAccountsAndExpiredAuditRecords()
    {
        var accounts = new List<CommercialAccount>();
        var events = new List<AccountAuditEvent>();

        var accountStore = new Mock<ICommercialAccountsService>();
        var auditStore = new Mock<ICommercialAuditService>();

        var cutoffUtc = new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero);

        // Seed accounts
        accounts.Add(new CommercialAccount(
            "tenant-001",
            "user-expired",
            cutoffUtc.AddMonths(-8),
            CommercialAccountStatus.Deleted,
            DeletedUtc: cutoffUtc.AddMonths(-7),
            RetentionExpiresUtc: cutoffUtc.AddDays(-1),
            RestoredUtc: null,
            LastSignInOutcomeUtc: cutoffUtc.AddMonths(-7)));

        accounts.Add(new CommercialAccount(
            "tenant-001",
            "user-active",
            cutoffUtc.AddMonths(-1),
            CommercialAccountStatus.Active,
            DeletedUtc: null,
            RetentionExpiresUtc: null,
            RestoredUtc: null,
            LastSignInOutcomeUtc: cutoffUtc.AddDays(-5)));

        // Seed audit events
        events.Add(new AccountAuditEvent(
            "tenant-001",
            "user-expired",
            cutoffUtc.AddMonths(-13),
            AccountAuditEventType.AccountDeleted,
            "account_deleted",
            cutoffUtc.AddDays(-1)));

        events.Add(new AccountAuditEvent(
            "tenant-001",
            "user-active",
            cutoffUtc.AddDays(-2),
            AccountAuditEventType.SignInOutcome,
            "sign_in_allowed",
            cutoffUtc.AddMonths(12)));

        // Setup account store behaviour
        accountStore.Setup(s => s.ListExpiredDeletedAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => accounts
                .Where(a => a.Status == CommercialAccountStatus.Deleted &&
                            a.RetentionExpiresUtc <= cutoffUtc)
                .ToList());

        accountStore.Setup(s => s.PurgeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((tenantId, userId, _) =>
            {
                var account = accounts.Single(a => a.TenantId == tenantId && a.UserId == userId);
                accounts.Remove(account);
            })
            .Returns(Task.CompletedTask);

        // Setup audit store behaviour
        auditStore.Setup(s => s.ListExpiredAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => events
                .Where(e => e.RetentionExpiresUtc <= cutoffUtc)
                .ToList());

        auditStore.Setup(s => s.PurgeExpiredAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<DateTimeOffset, int, CancellationToken>((asOfUtc, batchSize, _) =>
            {
                var expiredEvents = events
                    .Where(e => e.RetentionExpiresUtc <= asOfUtc)
                    .Take(batchSize)
                    .ToList();

                foreach (var evt in expiredEvents)
                {
                    events.Remove(evt);
                }
            })
            .ReturnsAsync((DateTimeOffset asOfUtc, int batchSize, CancellationToken _) =>
            {
                return events.Count(e => e.RetentionExpiresUtc <= asOfUtc);
            });

        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var service = serviceProvider.GetRequiredService<CommercialProfileService>();

        var purgedAccountCount = await service.PurgeExpiredAsync(cutoffUtc, batchSize: 100, CancellationToken.None);

        Assert.Equal(1, purgedAccountCount);

        Assert.DoesNotContain(accounts, account => account.UserId == "user-expired");
        Assert.Contains(accounts, account => account.UserId == "user-active");

        Assert.DoesNotContain(events, audit => audit.UserId == "user-expired");
        Assert.Contains(events, audit => audit.UserId == "user-active");
    }

    [Fact]
    public async Task PurgeExpiredAsync_WhenBatchSizeIsZero_PerformsNoWork()
    {
        var accounts = new List<CommercialAccount>();
        var events = new List<AccountAuditEvent>();

        var accountStore = new Mock<ICommercialAccountsService>();
        var auditStore = new Mock<ICommercialAuditService>();

        var cutoffUtc = new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero);

        accounts.Add(new CommercialAccount(
            "tenant-001",
            "user-expired",
            cutoffUtc.AddMonths(-8),
            CommercialAccountStatus.Deleted,
            DeletedUtc: cutoffUtc.AddMonths(-7),
            RetentionExpiresUtc: cutoffUtc.AddDays(-1),
            RestoredUtc: null,
            LastSignInOutcomeUtc: cutoffUtc.AddMonths(-7)));

        events.Add(new AccountAuditEvent(
            "tenant-001",
            "user-expired",
            cutoffUtc.AddMonths(-13),
            AccountAuditEventType.AccountDeleted,
            "account_deleted",
            cutoffUtc.AddDays(-1)));

        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var service = serviceProvider.GetRequiredService<CommercialProfileService>();

        var purgedAccountCount = await service.PurgeExpiredAsync(cutoffUtc, batchSize: 0, CancellationToken.None);

        Assert.Equal(0, purgedAccountCount);
        Assert.Single(accounts);
        Assert.Single(events);
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

        return services.BuildServiceProvider();
    }
}
