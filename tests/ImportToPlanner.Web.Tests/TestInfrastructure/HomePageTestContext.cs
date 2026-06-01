using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Bunit;
using ImportToPlanner.Application;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Web.Features.CommercialAccounts.Backend;
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
        CommercialAccountStoreStub? commercialAccountStoreStub = null)
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
        var webAssembly = typeof(TenantAuthorityConfiguration).Assembly;
        var failureDiagnosticsType = webAssembly.GetType("ImportToPlanner.Web.Diagnostics.UserFacingFailureDiagnostics", throwOnError: true)!;
        Services.AddScoped(failureDiagnosticsType, serviceProvider => Activator.CreateInstance(
            failureDiagnosticsType,
            serviceProvider.GetRequiredService<IHttpContextAccessor>(),
            serviceProvider.GetRequiredService<TenantAuthorityConfiguration>())!);
        var sessionIdentityAccessorType = webAssembly.GetType("ImportToPlanner.Web.Features.Authentication.ISessionIdentityContextAccessor", throwOnError: true)!;
        var claimsSessionIdentityAccessorType = webAssembly.GetType("ImportToPlanner.Web.Features.Authentication.ClaimsSessionIdentityContextAccessor", throwOnError: true)!;
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
        Services.AddSingleton(CommercialAccountStore);
        Services.AddSingleton(new CommercialAuditStoreStub());
        Services.AddSingleton(TenantAccessor);
        Services.AddScoped<ICurrentTenantContextAccessor>(_ => TenantAccessor);
        var commercialApiServiceClientType = webAssembly.GetType(
            "ImportToPlanner.Web.Features.CommercialAccounts.Backend.CommercialApiServiceClient",
            throwOnError: true)!;
        Services.AddScoped(
            commercialApiServiceClientType,
            serviceProvider => CreateCommercialApiServiceClient(commercialApiServiceClientType, serviceProvider));
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

    private static object CreateCommercialApiServiceClient(Type clientType, IServiceProvider serviceProvider)
    {
        var handler = new CommercialApiTestHandler(serviceProvider);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://commercialapiservice", UriKind.Absolute),
        };

        var constructor = clientType.GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            [typeof(HttpClient)],
            modifiers: null)
            ?? throw new InvalidOperationException("CommercialApiServiceClient constructor was not found.");

        return constructor.Invoke([httpClient]);
    }

    private sealed class CommercialApiTestHandler(IServiceProvider serviceProvider) : HttpMessageHandler
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            return request.RequestUri?.AbsolutePath switch
            {
                "/internal/commercial/access/resolve" => await ResolveAccessAsync(request, cancellationToken),
                "/internal/commercial/profile/get" => await GetProfileAsync(request, cancellationToken),
                "/internal/commercial/profile/delete" => await DeleteProfileAsync(request, cancellationToken),
                "/internal/commercial/profile/restore" => await RestoreProfileAsync(request, cancellationToken),
                "/internal/commercial/profile/purge-expired" => await PurgeExpiredAsync(request, cancellationToken),
                "/internal/commercial/tenant-metadata/get" => await GetTenantMetadataAsync(request, cancellationToken),
                "/internal/commercial/tenant-metadata/upsert" => await UpsertTenantMetadataAsync(request, cancellationToken),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound),
            };
        }

        private async Task<HttpResponseMessage> ResolveAccessAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var payload = await ReadPayloadAsync<ResolveCommercialAccessRequest>(request, cancellationToken);
            var accountStore = serviceProvider.GetRequiredService<CommercialAccountStoreStub>();

            if (!payload.CommercialModeEnabled)
            {
                return CreateJsonResponse(new CommercialAccessDecision(
                    CommercialAccessDecisionType.SelfHostedBypass,
                    AccountStatus: null,
                    RetentionExpiresUtc: null,
                    ShouldSignOut: false));
            }

            var existingAccount = await accountStore.GetAsync(
                payload.SessionIdentity.TenantId,
                payload.SessionIdentity.UserId,
                cancellationToken);

            if (existingAccount is null)
            {
                var createdAccount = new CommercialAccount(
                    payload.SessionIdentity.TenantId,
                    payload.SessionIdentity.UserId,
                    payload.OccurredUtc,
                    CommercialAccountStatus.Active,
                    DeletedUtc: null,
                    RetentionExpiresUtc: null,
                    RestoredUtc: null,
                    LastSignInOutcomeUtc: payload.OccurredUtc);
                await accountStore.CreateAsync(createdAccount, cancellationToken);

                return CreateJsonResponse(new CommercialAccessDecision(
                    CommercialAccessDecisionType.CreateAccount,
                    CommercialAccountStatus.Active,
                    RetentionExpiresUtc: null,
                    ShouldSignOut: false));
            }

            if (existingAccount.Status == CommercialAccountStatus.Deleted)
            {
                var deletedDecision = existingAccount.RetentionExpiresUtc is not null
                    && existingAccount.RetentionExpiresUtc <= payload.OccurredUtc
                    ? CommercialAccessDecisionType.BlockedDeleted
                    : CommercialAccessDecisionType.OfferRestore;

                return CreateJsonResponse(new CommercialAccessDecision(
                    deletedDecision,
                    existingAccount.Status,
                    existingAccount.RetentionExpiresUtc,
                    ShouldSignOut: false));
            }

            await accountStore.CreateAsync(existingAccount with { LastSignInOutcomeUtc = payload.OccurredUtc }, cancellationToken);

            var result = new CommercialAccessDecision(
                CommercialAccessDecisionType.Allow,
                existingAccount.Status,
                existingAccount.RetentionExpiresUtc,
                ShouldSignOut: false);

            return CreateJsonResponse(result);
        }

        private async Task<HttpResponseMessage> GetProfileAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var payload = await ReadPayloadAsync<GetCommercialProfileRequest>(request, cancellationToken);
            var accountStore = serviceProvider.GetRequiredService<CommercialAccountStoreStub>();
            var result = await accountStore.GetAsync(
                payload.SessionIdentity.TenantId,
                payload.SessionIdentity.UserId,
                cancellationToken);

            return CreateJsonResponse(result);
        }

        private async Task<HttpResponseMessage> DeleteProfileAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var payload = await ReadPayloadAsync<DeleteCommercialAccountRequest>(request, cancellationToken);
            var accountStore = serviceProvider.GetRequiredService<CommercialAccountStoreStub>();
            var auditStore = serviceProvider.GetRequiredService<CommercialAuditStoreStub>();
            var retentionExpiresUtc = payload.OccurredUtc.AddMonths(6);

            await accountStore.MarkDeletedAsync(
                payload.SessionIdentity.TenantId,
                payload.SessionIdentity.UserId,
                payload.OccurredUtc,
                retentionExpiresUtc,
                cancellationToken);
            await auditStore.AppendAsync(retentionExpiresUtc, cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        private async Task<HttpResponseMessage> RestoreProfileAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var payload = await ReadPayloadAsync<RestoreCommercialAccountRequest>(request, cancellationToken);
            var accountStore = serviceProvider.GetRequiredService<CommercialAccountStoreStub>();
            var existingAccount = await accountStore.GetAsync(
                payload.SessionIdentity.TenantId,
                payload.SessionIdentity.UserId,
                cancellationToken);

            var result = existingAccount switch
            {
                null => CommercialAccountRestoreResult.AccountNotFound,
                { Status: not CommercialAccountStatus.Deleted } => CommercialAccountRestoreResult.AccountNotDeleted,
                { RetentionExpiresUtc: not null } account when account.RetentionExpiresUtc <= payload.OccurredUtc => CommercialAccountRestoreResult.RetentionExpired,
                _ => CommercialAccountRestoreResult.Restored,
            };

            if (result == CommercialAccountRestoreResult.Restored)
            {
                await accountStore.RestoreAsync(
                    payload.SessionIdentity.TenantId,
                    payload.SessionIdentity.UserId,
                    payload.OccurredUtc,
                    cancellationToken);
            }

            return CreateJsonResponse(result);
        }

        private async Task<HttpResponseMessage> PurgeExpiredAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var payload = await ReadPayloadAsync<PurgeExpiredCommercialDataRequest>(request, cancellationToken);
            var accountStore = serviceProvider.GetRequiredService<CommercialAccountStoreStub>();
            var auditStore = serviceProvider.GetRequiredService<CommercialAuditStoreStub>();
            var expiredAccounts = await accountStore.ListExpiredDeletedAsync(payload.AsOfUtc, payload.BatchSize, cancellationToken);

            foreach (var expiredAccount in expiredAccounts)
            {
                await accountStore.PurgeAsync(expiredAccount.TenantId, expiredAccount.UserId, cancellationToken);
            }

            var result = expiredAccounts.Count + await auditStore.PurgeExpiredAsync(payload.AsOfUtc, payload.BatchSize, cancellationToken);

            return CreateJsonResponse(result);
        }

        private async Task<HttpResponseMessage> GetTenantMetadataAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var payload = await ReadPayloadAsync<GetTenantOperationalMetadataRequest>(request, cancellationToken);
            var store = serviceProvider.GetRequiredService<ITenantOperationalMetadataStore>();
            var result = await store.GetAsync(payload.TenantId, cancellationToken);

            return CreateJsonResponse(result);
        }

        private async Task<HttpResponseMessage> UpsertTenantMetadataAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var payload = await ReadPayloadAsync<UpsertTenantOperationalMetadataRequest>(request, cancellationToken);
            var store = serviceProvider.GetRequiredService<ITenantOperationalMetadataStore>();
            await store.UpsertAsync(payload.Metadata, cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        private static async Task<T> ReadPayloadAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var payload = await request.Content!.ReadFromJsonAsync<T>(cancellationToken);
            return payload ?? throw new InvalidOperationException($"Expected {typeof(T).Name} payload.");
        }

        private static HttpResponseMessage CreateJsonResponse<T>(T value)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(value, options: SerializerOptions),
            };
        }
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
