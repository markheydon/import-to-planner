using System.Security.Claims;
using Bunit;
using ImportToPlanner.Application;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Commercial.Configuration;
using ImportToPlanner.Commercial.Credits;
using ImportToPlanner.Commercial.Services;
using ImportToPlanner.Web.Features.Demo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
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
        CommercialAuditStoreStub? commercialAuditStoreStub = null,
        CreditEnsureUseCaseSubstitute? creditEnsureUseCaseSubstitute = null,
        ProfileCreditSummaryUseCaseSubstitute? profileCreditSummaryUseCaseSubstitute = null,
        ProfileCreditActivityUseCaseSubstitute? profileCreditActivityUseCaseSubstitute = null,
        IReadOnlyList<CreditPackOptions>? creditPackOptions = null,
        IImportColumnMappingLayoutStore? layoutStore = null,
        ReleaseInformationQueryStub? releaseInformationQueryStub = null,
        string docsBaseUrl = "https://docs.test.importplanner.app")
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
                ["Storage:CommercialCreditLedgerTable"] = "CommercialCreditLedger",
                ["Storage:DataProtectionContainer"] = "dataprotection",
                ["Storage:DataProtectionBlob"] = "keys.xml",
                ["Features:CommercialMode:Enabled"] = commercialModeEnabled.ToString(),
                ["Features:CommercialMode:RetentionSweepEnabled"] = "false",
                ["DocsExternalLinks:DocsBaseUrl"] = docsBaseUrl,
            })
            .Build();

        Services.AddSingleton<IConfiguration>(config);
        Services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
        Services.AddWebApplicationOptions(config);
        Services.AddOptions<CommercialModeOptions>()
            .Bind(config.GetSection(CommercialModeOptions.ConfigurationSectionName));
        Services.AddSingleton(static serviceProvider => serviceProvider.GetRequiredService<IOptions<CommercialModeOptions>>().Value);
        Services.AddOptions<DocsExternalLinksOptions>()
            .Bind(config.GetSection(DocsExternalLinksOptions.ConfigurationSectionName));
        Services.AddSingleton(static serviceProvider => serviceProvider.GetRequiredService<IOptions<DocsExternalLinksOptions>>().Value);

        ReleaseInformationQuery = releaseInformationQueryStub ?? new ReleaseInformationQueryStub();
        Services.AddScoped<IReleaseInformationQuery>(_ => ReleaseInformationQuery);

        var tenantAuthorityConfiguration = TenantAuthorityConfiguration.FromConfiguration(config);
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
        var failureDiagnosticsType = typeof(DependencyInjection).Assembly.GetType("ImportToPlanner.Web.Diagnostics.UserFacingFailureDiagnostics", throwOnError: true)!;
        Services.AddScoped(failureDiagnosticsType, serviceProvider => Activator.CreateInstance(
            failureDiagnosticsType,
            serviceProvider.GetRequiredService<IHttpContextAccessor>(),
            serviceProvider.GetRequiredService<TenantAuthorityConfiguration>())!);
        var sessionIdentityAccessorType = typeof(DependencyInjection).Assembly.GetType("ImportToPlanner.Web.Features.Authentication.ISessionIdentityContextAccessor", throwOnError: true)!;
        var claimsSessionIdentityAccessorType = typeof(DependencyInjection).Assembly.GetType("ImportToPlanner.Web.Features.Authentication.ClaimsSessionIdentityContextAccessor", throwOnError: true)!;
        Services.AddScoped(
            sessionIdentityAccessorType,
            serviceProvider => Activator.CreateInstance(
                claimsSessionIdentityAccessorType,
                serviceProvider.GetRequiredService<IHttpContextAccessor>(),
                serviceProvider.GetRequiredService<TenantAuthorityConfiguration>())!);

        CsvParser = new CsvImportParserSubstitute();
        Services.AddScoped<ICsvImportParser>(_ => CsvParser.Instance);
        Services.AddScoped<IPlannerGateway>(_ => Gateway);
        Services.AddScoped<ITenantOperationalMetadataStore, TenantOperationalMetadataStoreStub>();
        if (commercialModeEnabled)
        {
            CommercialAccountStore = commercialAccountStoreStub ?? new CommercialAccountStoreStub();
            CommercialAuditStore = commercialAuditStoreStub ?? new CommercialAuditStoreStub();
            Services.AddScoped<ICommercialAccountStore>(_ => CommercialAccountStore);
            Services.AddScoped<ICommercialAuditStore>(_ => CommercialAuditStore);
            Services.AddScoped<ICommercialAccessUseCase, CommercialAccessUseCase>();
            Services.AddScoped<GetCommercialProfileUseCase>();
            Services.AddScoped<DeleteCommercialAccountUseCase>();
            Services.AddScoped<RestoreCommercialAccountUseCase>();
            Services.AddScoped<PurgeExpiredCommercialAccountsUseCase>();
            Services.AddScoped<ICommercialProfileUseCase, GetCommercialProfileUseCase>();
            CreditEnsureUseCase = creditEnsureUseCaseSubstitute ?? new CreditEnsureUseCaseSubstitute();
            Services.AddScoped<IEnsureCurrentCreditBalanceUseCase>(_ => CreditEnsureUseCase.Instance);
            ProfileCreditSummaryUseCase = profileCreditSummaryUseCaseSubstitute ?? new ProfileCreditSummaryUseCaseSubstitute();
            ProfileCreditActivityUseCase = profileCreditActivityUseCaseSubstitute ?? new ProfileCreditActivityUseCaseSubstitute();
            Services.AddScoped<IGetProfileCreditSummaryUseCase>(_ => ProfileCreditSummaryUseCase.Instance);
            Services.AddScoped<IGetProfileCreditActivityUseCase>(_ => ProfileCreditActivityUseCase.Instance);

            CreditCheckoutPaymentPort = new CreditCheckoutPaymentPortSubstitute();
            TenantBillingStore = new TenantBillingStoreStub();
            Services.AddSingleton<IComplimentaryTenantPolicy>(DisabledComplimentaryTenantPolicy.Instance);
            Services.AddScoped<ITenantBillingStore>(_ => TenantBillingStore);
            Services.AddScoped<ICreditCheckoutPaymentPort>(_ => CreditCheckoutPaymentPort);
            Services.AddScoped<CreateCreditCheckoutUseCase>();
            Services.AddOptions<StripeOptions>().Configure(options =>
            {
                options.CreditPacks =
                [
                    new CreditPackOptions
                    {
                        PackId = "pack-50",
                        CreditQuantity = 50,
                        StripePriceId = "price_50",
                        DisplayName = "50 credits",
                        AmountMinor = 500,
                        Currency = "gbp",
                        Published = true,
                    },
                    new CreditPackOptions
                    {
                        PackId = "pack-100",
                        CreditQuantity = 100,
                        StripePriceId = "price_100",
                        DisplayName = "100 credits",
                        AmountMinor = 900,
                        Currency = "gbp",
                        Published = true,
                    },
                ];
            });
        }
        else
        {
            CommercialAccountStore = commercialAccountStoreStub ?? new CommercialAccountStoreStub();
            CommercialAuditStore = commercialAuditStoreStub ?? new CommercialAuditStoreStub();
            CreditEnsureUseCase = creditEnsureUseCaseSubstitute ?? new CreditEnsureUseCaseSubstitute();
            ProfileCreditSummaryUseCase = profileCreditSummaryUseCaseSubstitute ?? new ProfileCreditSummaryUseCaseSubstitute();
            ProfileCreditActivityUseCase = profileCreditActivityUseCaseSubstitute ?? new ProfileCreditActivityUseCaseSubstitute();
        }

        Services.AddSingleton(TenantAccessor.Instance);
        Services.AddScoped<ICurrentTenantContextAccessor>(_ => TenantAccessor.Instance);
        Services.AddApplication();
        Services.AddScoped<ImportPlanningPresenter>();
        Services.AddScoped<ImportExecutionPresenter>();
        Services.AddScoped<SessionIdentityPresenter>();
        Services.AddScoped<IImportColumnMappingLayoutStore>(_ => layoutStore ?? new NullImportColumnMappingLayoutStore());
        Services.AddScoped<WorkflowCoordinationState>();
        Services.AddScoped<ImportWorkflowCoordinator>();

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public CsvImportParserSubstitute CsvParser { get; }

    public PlannerGatewayStub Gateway { get; } = new();

    public TenantContextAccessorSubstitute TenantAccessor { get; } = new();

    public CommercialAccountStoreStub CommercialAccountStore { get; }

    public CommercialAuditStoreStub CommercialAuditStore { get; }

    public CreditEnsureUseCaseSubstitute CreditEnsureUseCase { get; }

    public ProfileCreditSummaryUseCaseSubstitute ProfileCreditSummaryUseCase { get; }

    public ProfileCreditActivityUseCaseSubstitute ProfileCreditActivityUseCase { get; }

    public CreditCheckoutPaymentPortSubstitute? CreditCheckoutPaymentPort { get; }

    public TenantBillingStoreStub? TenantBillingStore { get; }

    public ReleaseInformationQueryStub ReleaseInformationQuery { get; }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "ImportToPlanner.Web.Tests";

        public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

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

