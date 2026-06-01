using ImportToPlanner.CommercialService.Common.Models;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Models;
using ImportToPlanner.CommercialService.Features.CommercialAccess.Services;
using ImportToPlanner.CommercialService.Tests.TestDoubles;
using Moq;

namespace ImportToPlanner.CommercialService.Tests;

public sealed class CommercialAccessUseCaseTests
{
    [Fact]
    public async Task ResolveAccessAsync_WhenCommercialModeEnabledAndAccountMissing_CreatesAccountAndReturnsCreateAccountDecision()
    {
        var accounts = new List<CommercialAccount>();
        var events = new List<AccountAuditEvent>();

        var accountStore = new Mock<ICommercialAccountsService>();
        var auditStore = new Mock<ICommercialAuditService>();

        var occurredUtc = new DateTimeOffset(2026, 5, 28, 10, 15, 0, TimeSpan.Zero);

        accountStore.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommercialAccount?)null);

        accountStore.Setup(s => s.CreateAsync(It.IsAny<CommercialAccount>(), It.IsAny<CancellationToken>()))
            .Callback<CommercialAccount, CancellationToken>((account, _) => accounts.Add(account))
            .Returns(Task.CompletedTask);

        auditStore.Setup(s => s.AppendAsync(It.IsAny<AccountAuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AccountAuditEvent, CancellationToken>((evt, _) => events.Add(evt))
            .Returns(Task.CompletedTask);

        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var service = serviceProvider.GetRequiredService<CommercialAccessService>();

        var decision = await service.ResolveAccessAsync(
            new SessionIdentityContext("tenant-001", "user-001", "user@contoso.com", "Contoso"),
            commercialModeEnabled: true,
            occurredUtc,
            CancellationToken.None);

        Assert.Equal(CommercialAccessDecisionType.CreateAccount, decision.Decision);
        Assert.Equal(CommercialAccountStatus.Active, decision.AccountStatus);
        Assert.False(decision.ShouldSignOut);

        var createdAccount = Assert.Single(accounts);
        Assert.Equal("tenant-001", createdAccount.TenantId);
        Assert.Equal("user-001", createdAccount.UserId);
        Assert.Equal(CommercialAccountStatus.Active, createdAccount.Status);
        Assert.Equal(occurredUtc, createdAccount.CreatedUtc);

        var accountCreatedEvent = Assert.Single(events, e => e.EventType == AccountAuditEventType.AccountCreated);
        Assert.Equal("tenant-001", accountCreatedEvent.TenantId);
        Assert.Equal("user-001", accountCreatedEvent.UserId);
        Assert.Equal("account_created", accountCreatedEvent.Outcome);
    }

    [Fact]
    public async Task ResolveAccessAsync_WhenCommercialModeEnabledAndAccountAlreadyExists_ReturnsAllowWithoutCreatingAnotherAccount()
    {
        var accounts = new List<CommercialAccount>();
        var events = new List<AccountAuditEvent>();

        var accountStore = new Mock<ICommercialAccountsService>();
        var auditStore = new Mock<ICommercialAuditService>();

        var createdUtc = new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero);

        var existingAccount = new CommercialAccount(
            "tenant-001",
            "user-001",
            createdUtc,
            CommercialAccountStatus.Active,
            null,
            null,
            null,
            createdUtc);

        accounts.Add(existingAccount);

        accountStore.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccount);

        auditStore.Setup(s => s.AppendAsync(It.IsAny<AccountAuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AccountAuditEvent, CancellationToken>((evt, _) => events.Add(evt))
            .Returns(Task.CompletedTask);

        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var service = serviceProvider.GetRequiredService<CommercialAccessService>();

        var decision = await service.ResolveAccessAsync(
            new SessionIdentityContext("tenant-001", "user-001", "user@contoso.com", "Contoso"),
            commercialModeEnabled: true,
            new DateTimeOffset(2026, 5, 28, 10, 45, 0, TimeSpan.Zero),
            CancellationToken.None);

        Assert.Equal(CommercialAccessDecisionType.Allow, decision.Decision);
        Assert.Equal(CommercialAccountStatus.Active, decision.AccountStatus);
        Assert.False(decision.ShouldSignOut);

        Assert.Single(accounts);

        var signInOutcomeEvent = Assert.Single(events, e => e.EventType == AccountAuditEventType.SignInOutcome);
        Assert.Equal("sign_in_allowed", signInOutcomeEvent.Outcome);
    }

    [Fact]
    public async Task ResolveAccessAsync_WhenCommercialModeDisabled_ReturnsSelfHostedBypassWithoutPersistingAccountData()
    {
        var accounts = new List<CommercialAccount>();
        var events = new List<AccountAuditEvent>();

        var accountStore = new Mock<ICommercialAccountsService>();
        var auditStore = new Mock<ICommercialAuditService>();

        accounts.Add(new CommercialAccount(
            "tenant-existing",
            "user-existing",
            new DateTimeOffset(2026, 2, 1, 8, 0, 0, TimeSpan.Zero),
            CommercialAccountStatus.Active,
            null,
            null,
            null,
            null));

        accountStore.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts[0]);

        auditStore.Setup(s => s.AppendAsync(It.IsAny<AccountAuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AccountAuditEvent, CancellationToken>((evt, _) => events.Add(evt))
            .Returns(Task.CompletedTask);

        using var serviceProvider = BuildServiceProvider(accountStore, auditStore);
        var service = serviceProvider.GetRequiredService<CommercialAccessService>();

        var decision = await service.ResolveAccessAsync(
            new SessionIdentityContext("tenant-001", "user-001", "user@contoso.com", "Contoso"),
            commercialModeEnabled: false,
            new DateTimeOffset(2026, 5, 28, 11, 0, 0, TimeSpan.Zero),
            CancellationToken.None);

        Assert.Equal(CommercialAccessDecisionType.SelfHostedBypass, decision.Decision);
        Assert.Null(decision.AccountStatus);
        Assert.False(decision.ShouldSignOut);

        Assert.Single(accounts);
        Assert.DoesNotContain(events, e => e.EventType == AccountAuditEventType.AccountCreated);
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

        services.AddSingleton<CommercialAccessService>();

        return services.BuildServiceProvider();
    }
}
