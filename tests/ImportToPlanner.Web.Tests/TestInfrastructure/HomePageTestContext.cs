using System.Security.Claims;
using ImportToPlanner.Application;
using Bunit;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Presenters;
using ImportToPlanner.Web.Workflows;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor.Services;

namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class HomePageTestContext : BunitContext
{
    public HomePageTestContext(
        string tenantId = "organizations",
        bool isAuthenticated = true,
        bool commercialModeEnabled = false,
        CommercialAccountStoreStub? commercialAccountStoreStub = null,
        CommercialAuditStoreStub? commercialAuditStoreStub = null)
    {
        Services.AddMudServices(configuration =>
        {
            configuration.PopoverOptions.CheckForPopoverProvider = false;
        });

        var auth = AddAuthorization();
        if (isAuthenticated)
        {
            auth.SetAuthorized("graph-test-user");
        }
        else
        {
            auth.SetNotAuthorized();
        }

        Services.AddLogging();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = tenantId,
                ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000000",
                ["AzureAd:CallbackPath"] = "/signin-oidc",
                ["DownstreamApis:MicrosoftGraph:Scopes:0"] = "User.Read",
                ["Storage:TenantMetadataTable"] = "TenantOperationalMetadata",
                ["Storage:CommercialAccountsTable"] = "CommercialAccounts",
                ["Storage:CommercialAuditTable"] = "CommercialAccountAuditEvents",
                ["Storage:DataProtectionContainer"] = "dataprotection",
                ["Storage:DataProtectionBlob"] = "keys.xml",
                ["Features:CommercialMode:Enabled"] = commercialModeEnabled.ToString(),
                ["Features:CommercialMode:RetentionSweepEnabled"] = "false",
            })
            .Build();

        var tenantAuthorityConfiguration = TenantAuthorityConfiguration.FromConfiguration(config);
        var storageConfiguration = StorageConfiguration.FromConfiguration(config);

        Services.AddSingleton<IConfiguration>(config);
        Services.AddSingleton(tenantAuthorityConfiguration);
        Services.AddSingleton(storageConfiguration);
        Services.AddOptions<CommercialModeOptions>()
            .Bind(config.GetSection(CommercialModeOptions.ConfigurationSectionName));
        Services.AddSingleton(static serviceProvider => serviceProvider.GetRequiredService<IOptions<CommercialModeOptions>>().Value);
        Services.AddSingleton(new ConsentResolutionDefaults(
            tenantAuthorityConfiguration.RequiredScopes,
            tenantAuthorityConfiguration.AdminConsentUri));

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreatePrincipal(isAuthenticated),
            },
        };

        Services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
        var failureDiagnosticsType = typeof(DependencyInjection).Assembly.GetType("ImportToPlanner.Web.UserFacingFailureDiagnostics", throwOnError: true)!;
        Services.AddScoped(failureDiagnosticsType, serviceProvider => Activator.CreateInstance(
            failureDiagnosticsType,
            serviceProvider.GetRequiredService<IHttpContextAccessor>(),
            serviceProvider.GetRequiredService<TenantAuthorityConfiguration>())!);
        var sessionIdentityAccessorType = typeof(DependencyInjection).Assembly.GetType("ImportToPlanner.Web.ISessionIdentityContextAccessor", throwOnError: true)!;
        var claimsSessionIdentityAccessorType = typeof(DependencyInjection).Assembly.GetType("ImportToPlanner.Web.ClaimsSessionIdentityContextAccessor", throwOnError: true)!;
        Services.AddScoped(
            sessionIdentityAccessorType,
            serviceProvider => Activator.CreateInstance(
                claimsSessionIdentityAccessorType,
                serviceProvider.GetRequiredService<IHttpContextAccessor>(),
                serviceProvider.GetRequiredService<TenantAuthorityConfiguration>())!);

        Services.AddScoped<ICsvImportParser, CsvImportParserStub>();
        Services.AddScoped<IPlannerGateway>(_ => Gateway);
        Services.AddScoped<ITenantOperationalMetadataStore, TenantOperationalMetadataStoreStub>();
        CommercialAccountStore = commercialAccountStoreStub ?? new CommercialAccountStoreStub();
        CommercialAuditStore = commercialAuditStoreStub ?? new CommercialAuditStoreStub();
        Services.AddScoped<ICommercialAccountStore>(_ => CommercialAccountStore);
        Services.AddScoped<ICommercialAuditStore>(_ => CommercialAuditStore);
        Services.AddSingleton(TenantAccessor);
        Services.AddScoped<ICurrentTenantContextAccessor>(_ => TenantAccessor);
        Services.AddApplication();
        Services.AddScoped<ImportPlanningPresenter>();
        Services.AddScoped<ImportExecutionPresenter>();
        Services.AddScoped<SessionIdentityPresenter>();
        Services.AddScoped<WorkflowCoordinationState>();
        Services.AddScoped<ImportWorkflowCoordinator>();

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public PlannerGatewayStub Gateway { get; } = new();

    public CurrentTenantContextAccessorStub TenantAccessor { get; } = new();

    public CommercialAccountStoreStub CommercialAccountStore { get; }

    public CommercialAuditStoreStub CommercialAuditStore { get; }

    private static ClaimsPrincipal CreatePrincipal(bool isAuthenticated)
    {
        if (!isAuthenticated)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        return new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim("tid", "tenant-001"),
                    new Claim("oid", "user-001"),
                    new Claim("preferred_username", "user@contoso.com"),
                    new Claim("tenant_display_name", "Contoso"),
                ],
                authenticationType: "test-auth"));
    }
}

internal sealed class CsvImportParserStub : ICsvImportParser
{
    public Task<CsvParseResult> ParseAsync(string csvContent, CancellationToken cancellationToken, bool ignoreExtraColumns = false)
    {
        return Task.FromResult(new CsvParseResult(
            [new CsvTaskRow(2, "Stub Task", null, null, null, null)],
            []));
    }
}
