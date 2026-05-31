using ImportToPlanner.CommercialService.CommercialAccounts;
using ImportToPlanner.CommercialService.Models;
using ImportToPlanner.Tests.TestDoubles;

namespace ImportToPlanner.Tests;

public sealed class CommercialRetentionSweepTests
{
    [Fact]
    public async Task PurgeExpiredAsync_RemovesExpiredDeletedAccountsAndExpiredAuditRecords()
    {
        var accountStore = new CommercialAccountStoreStub();
        var auditStore = new CommercialAuditStoreStub();
        var cutoffUtc = new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero);

        await accountStore.CreateAsync(
            new CommercialAccount(
                "tenant-001",
                "user-expired",
                cutoffUtc.AddMonths(-8),
                CommercialAccountStatus.Deleted,
                DeletedUtc: cutoffUtc.AddMonths(-7),
                RetentionExpiresUtc: cutoffUtc.AddDays(-1),
                RestoredUtc: null,
                LastSignInOutcomeUtc: cutoffUtc.AddMonths(-7)),
            CancellationToken.None);

        await accountStore.CreateAsync(
            new CommercialAccount(
                "tenant-001",
                "user-active",
                cutoffUtc.AddMonths(-1),
                CommercialAccountStatus.Active,
                DeletedUtc: null,
                RetentionExpiresUtc: null,
                RestoredUtc: null,
                LastSignInOutcomeUtc: cutoffUtc.AddDays(-5)),
            CancellationToken.None);

        await auditStore.AppendAsync(
            new AccountAuditEvent(
                "tenant-001",
                "user-expired",
                cutoffUtc.AddMonths(-13),
                AccountAuditEventType.AccountDeleted,
                "account_deleted",
                cutoffUtc.AddDays(-1)),
            CancellationToken.None);
        await auditStore.AppendAsync(
            new AccountAuditEvent(
                "tenant-001",
                "user-active",
                cutoffUtc.AddDays(-2),
                AccountAuditEventType.SignInOutcome,
                "sign_in_allowed",
                cutoffUtc.AddMonths(12)),
            CancellationToken.None);

        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var useCase = serviceProvider.GetRequiredService<ICommercialProfileUseCase>();

        var purgedAccountCount = await useCase.PurgeExpiredAsync(cutoffUtc, batchSize: 100, CancellationToken.None);

        Assert.Equal(1, purgedAccountCount);
        Assert.DoesNotContain(accountStore.Accounts, account => account.UserId == "user-expired");
        Assert.Contains(accountStore.Accounts, account => account.UserId == "user-active");

        Assert.DoesNotContain(auditStore.Events, audit => audit.UserId == "user-expired");
        Assert.Contains(auditStore.Events, audit => audit.UserId == "user-active");
    }

    [Fact]
    public async Task PurgeExpiredAsync_WhenBatchSizeIsZero_PerformsNoWork()
    {
        var accountStore = new CommercialAccountStoreStub();
        var auditStore = new CommercialAuditStoreStub();
        var cutoffUtc = new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero);

        await accountStore.CreateAsync(
            new CommercialAccount(
                "tenant-001",
                "user-expired",
                cutoffUtc.AddMonths(-8),
                CommercialAccountStatus.Deleted,
                DeletedUtc: cutoffUtc.AddMonths(-7),
                RetentionExpiresUtc: cutoffUtc.AddDays(-1),
                RestoredUtc: null,
                LastSignInOutcomeUtc: cutoffUtc.AddMonths(-7)),
            CancellationToken.None);

        await auditStore.AppendAsync(
            new AccountAuditEvent(
                "tenant-001",
                "user-expired",
                cutoffUtc.AddMonths(-13),
                AccountAuditEventType.AccountDeleted,
                "account_deleted",
                cutoffUtc.AddDays(-1)),
            CancellationToken.None);

        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var useCase = serviceProvider.GetRequiredService<ICommercialProfileUseCase>();

        var purgedAccountCount = await useCase.PurgeExpiredAsync(cutoffUtc, batchSize: 0, CancellationToken.None);

        Assert.Equal(0, purgedAccountCount);
        Assert.Single(accountStore.Accounts);
        Assert.Single(auditStore.Events);
    }

    private static ServiceProvider BuildServiceProvider(CommercialAccountStoreStub accountStore, CommercialAuditStoreStub auditStore)
    {
        ArgumentNullException.ThrowIfNull(accountStore);
        ArgumentNullException.ThrowIfNull(auditStore);

        var services = new ServiceCollection();
        services.AddScoped<ICommercialAccountStore>(_ => accountStore);
        services.AddScoped<ICommercialAuditStore>(_ => auditStore);
        services.AddScoped<DeleteCommercialAccountUseCase>();
        services.AddScoped<RestoreCommercialAccountUseCase>();
        services.AddScoped<PurgeExpiredCommercialAccountsUseCase>();
        services.AddScoped<ICommercialProfileUseCase, GetCommercialProfileUseCase>();

        return services.BuildServiceProvider();
    }
}
