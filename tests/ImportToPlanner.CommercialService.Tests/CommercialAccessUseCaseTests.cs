using ImportToPlanner.CommercialService.CommercialAccounts;
using ImportToPlanner.CommercialService.Models;
using ImportToPlanner.Tests.TestDoubles;

namespace ImportToPlanner.Tests;

public sealed class CommercialAccessUseCaseTests
{
    [Fact]
    public async Task ResolveAccessAsync_WhenCommercialModeEnabledAndAccountMissing_CreatesAccountAndReturnsCreateAccountDecision()
    {
        var accountStore = new CommercialAccountStoreStub();
        var auditStore = new CommercialAuditStoreStub();
        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var useCase = serviceProvider.GetRequiredService<ICommercialAccessUseCase>();
        var occurredUtc = new DateTimeOffset(2026, 5, 28, 10, 15, 0, TimeSpan.Zero);

        var decision = await useCase.ResolveAccessAsync(
            new SessionIdentityContext("tenant-001", "user-001", "user@contoso.com", "Contoso"),
            commercialModeEnabled: true,
            occurredUtc,
            CancellationToken.None);

        Assert.Equal(CommercialAccessDecisionType.CreateAccount, decision.Decision);
        Assert.Equal(CommercialAccountStatus.Active, decision.AccountStatus);
        Assert.False(decision.ShouldSignOut);

        var createdAccount = Assert.Single(accountStore.Accounts);
        Assert.Equal("tenant-001", createdAccount.TenantId);
        Assert.Equal("user-001", createdAccount.UserId);
        Assert.Equal(CommercialAccountStatus.Active, createdAccount.Status);
        Assert.Equal(occurredUtc, createdAccount.CreatedUtc);

        var accountCreatedEvent = Assert.Single(auditStore.Events, evt => evt.EventType == AccountAuditEventType.AccountCreated);
        Assert.Equal("tenant-001", accountCreatedEvent.TenantId);
        Assert.Equal("user-001", accountCreatedEvent.UserId);
        Assert.Equal("account_created", accountCreatedEvent.Outcome);
    }

    [Fact]
    public async Task ResolveAccessAsync_WhenCommercialModeEnabledAndAccountAlreadyExists_ReturnsAllowWithoutCreatingAnotherAccount()
    {
        var accountStore = new CommercialAccountStoreStub();
        var auditStore = new CommercialAuditStoreStub();
        var createdUtc = new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero);

        await accountStore.CreateAsync(
            new CommercialAccount(
                "tenant-001",
                "user-001",
                createdUtc,
                CommercialAccountStatus.Active,
                DeletedUtc: null,
                RetentionExpiresUtc: null,
                RestoredUtc: null,
                LastSignInOutcomeUtc: createdUtc),
            CancellationToken.None);

        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var useCase = serviceProvider.GetRequiredService<ICommercialAccessUseCase>();

        var decision = await useCase.ResolveAccessAsync(
            new SessionIdentityContext("tenant-001", "user-001", "user@contoso.com", "Contoso"),
            commercialModeEnabled: true,
            occurredUtc: new DateTimeOffset(2026, 5, 28, 10, 45, 0, TimeSpan.Zero),
            CancellationToken.None);

        Assert.Equal(CommercialAccessDecisionType.Allow, decision.Decision);
        Assert.Equal(CommercialAccountStatus.Active, decision.AccountStatus);
        Assert.False(decision.ShouldSignOut);
        Assert.Single(accountStore.Accounts);

        var signInOutcomeEvent = Assert.Single(auditStore.Events, evt => evt.EventType == AccountAuditEventType.SignInOutcome);
        Assert.Equal("sign_in_allowed", signInOutcomeEvent.Outcome);
    }

    [Fact]
    public async Task ResolveAccessAsync_WhenCommercialModeDisabled_ReturnsSelfHostedBypassWithoutPersistingAccountData()
    {
        var accountStore = new CommercialAccountStoreStub();
        var auditStore = new CommercialAuditStoreStub();
        await accountStore.CreateAsync(
            new CommercialAccount(
                "tenant-existing",
                "user-existing",
                new DateTimeOffset(2026, 2, 1, 8, 0, 0, TimeSpan.Zero),
                CommercialAccountStatus.Active,
                DeletedUtc: null,
                RetentionExpiresUtc: null,
                RestoredUtc: null,
                LastSignInOutcomeUtc: null),
            CancellationToken.None);

        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var useCase = serviceProvider.GetRequiredService<ICommercialAccessUseCase>();

        var decision = await useCase.ResolveAccessAsync(
            new SessionIdentityContext("tenant-001", "user-001", "user@contoso.com", "Contoso"),
            commercialModeEnabled: false,
            occurredUtc: new DateTimeOffset(2026, 5, 28, 11, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        Assert.Equal(CommercialAccessDecisionType.SelfHostedBypass, decision.Decision);
        Assert.Null(decision.AccountStatus);
        Assert.False(decision.ShouldSignOut);
        Assert.Single(accountStore.Accounts);
        Assert.Empty(auditStore.Events);
    }

    private static ServiceProvider BuildServiceProvider(CommercialAccountStoreStub accountStore, CommercialAuditStoreStub auditStore)
    {
        ArgumentNullException.ThrowIfNull(accountStore);
        ArgumentNullException.ThrowIfNull(auditStore);

        var services = new ServiceCollection();
        services.AddScoped<ICommercialAccountStore>(_ => accountStore);
        services.AddScoped<ICommercialAuditStore>(_ => auditStore);
        services.AddScoped<ICommercialAccessUseCase, CommercialAccessUseCase>();

        return services.BuildServiceProvider();
    }
}
