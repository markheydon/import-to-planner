using Bunit;
using ImportToPlanner.Application.Abstractions;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Application.Services;
using ImportToPlanner.Web.Presenters;
using ImportToPlanner.Web.Workflows;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace ImportToPlanner.Web.Tests.TestInfrastructure;

internal sealed class HomePageTestContext : BunitContext
{
    public HomePageTestContext(string tenantId = "organizations", bool isAuthenticated = true)
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
                ["Storage:DataProtectionContainer"] = "dataprotection",
                ["Storage:DataProtectionBlob"] = "keys.xml",
            })
            .Build();

        var tenantAuthorityConfiguration = TenantAuthorityConfiguration.FromConfiguration(config);
        var storageConfiguration = StorageConfiguration.FromConfiguration(config);

        Services.AddSingleton<IConfiguration>(config);
        Services.AddSingleton(tenantAuthorityConfiguration);
        Services.AddSingleton(storageConfiguration);
        Services.AddSingleton(new ConsentResolutionDefaults(
            tenantAuthorityConfiguration.RequiredScopes,
            tenantAuthorityConfiguration.AdminConsentUri));

        Services.AddHttpContextAccessor();
        var failureDiagnosticsType = typeof(DependencyInjection).Assembly.GetType("ImportToPlanner.Web.UserFacingFailureDiagnostics", throwOnError: true)!;
        Services.AddScoped(failureDiagnosticsType, serviceProvider => Activator.CreateInstance(
            failureDiagnosticsType,
            serviceProvider.GetRequiredService<IHttpContextAccessor>(),
            serviceProvider.GetRequiredService<TenantAuthorityConfiguration>())!);

        Services.AddScoped<ICsvImportParser, CsvImportParserStub>();
        Services.AddScoped<IPlannerGateway>(_ => Gateway);
        Services.AddScoped<ITenantOperationalMetadataStore, TenantOperationalMetadataStoreStub>();
        Services.AddSingleton(TenantAccessor);
        Services.AddScoped<ICurrentTenantContextAccessor>(_ => TenantAccessor);
        Services.AddScoped<IImportPlanningUseCase, ImportPlanningUseCase>();
        Services.AddScoped<IImportExecutionUseCase, ImportExecutionUseCase>();
        Services.AddScoped<ImportPlanningPresenter>();
        Services.AddScoped<ImportExecutionPresenter>();
        Services.AddScoped<WorkflowCoordinationState>();
        Services.AddScoped<ImportWorkflowCoordinator>();

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public PlannerGatewayStub Gateway { get; } = new();

    public CurrentTenantContextAccessorStub TenantAccessor { get; } = new();
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
