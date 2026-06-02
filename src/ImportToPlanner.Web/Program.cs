using ImportToPlanner.Application;
using ImportToPlanner.Application.Consent.Models;
using ImportToPlanner.Application.TenantContext.Abstractions;
using ImportToPlanner.Infrastructure.Graph;
using ImportToPlanner.Web.Components;
using ImportToPlanner.Web.Features.Authentication;
using ImportToPlanner.Web.Features.CommercialAccounts;
using ImportToPlanner.Web.Features.CommercialAccounts.Backend;
using ImportToPlanner.Web.Features.Import.Presenters;
using ImportToPlanner.Web.Features.Import.Workflows;
using ImportToPlanner.Web.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.UI;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Shared service defaults and storage clients consumed by web and infrastructure adapters.
builder.AddServiceDefaults();
builder.AddAzureBlobServiceClient(connectionName: "blobs");

// Startup configuration normalisation and validation.
ApplyLegacyCertificatePathOverrides(builder.Configuration);
ApplyCertificateBase64Overrides(builder.Configuration);
StartupConfigurationValidator.Validate(builder.Configuration);
AzureAdConfigurationNormalizer.Apply(builder.Configuration);

// Pre-computed startup configuration models used across registration blocks.
var tenantAuthorityConfiguration = TenantAuthorityConfiguration.FromConfiguration(builder.Configuration);
var storageConfiguration = StorageConfiguration.FromConfiguration(builder.Configuration);

// Shared options and immutable startup state.
builder.Services.AddSingleton(tenantAuthorityConfiguration);
builder.Services.AddSingleton(storageConfiguration);
builder.Services.AddSingleton(new ConsentResolutionDefaults(
    tenantAuthorityConfiguration.RequiredScopes,
    tenantAuthorityConfiguration.AdminConsentUri));
builder.Services
    .AddOptions<CommercialModeOptions>()
    .Bind(builder.Configuration.GetSection(CommercialModeOptions.ConfigurationSectionName))
    .ValidateOnStart();
builder.Services.AddSingleton(static serviceProvider => serviceProvider.GetRequiredService<IOptions<CommercialModeOptions>>().Value);

// Web UI composition and feature registrations.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddCascadingAuthenticationState();
builder.Services
    .AddHostedAuthenticationServices(builder.Configuration)
    .AddApplication()
    .AddMicrosoftGraphInfrastructure(builder.Configuration);

builder.Services.AddScoped<ImportPlanningPresenter>();
builder.Services.AddScoped<ImportExecutionPresenter>();
builder.Services.AddScoped<SessionIdentityPresenter>();
builder.Services.AddScoped<WorkflowCoordinationState>();
builder.Services.AddScoped<ImportWorkflowCoordinator>();

// Commercial mode.
builder.Services.AddHttpClient<CommercialApiServiceClient>(static client => client.BaseAddress = new("https+http://commercialapiservice"));
builder.Services.AddSingleton<ITenantOperationalMetadataStore, BackendTenantOperationalMetadataStore>();

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();
builder.Services.AddAuthorization();

// Data protection key-ring persistence for hosted environments.
HostedDataProtectionConfigurator.Configure(builder.Services, storageConfiguration);

var app = builder.Build();

// HTTP pipeline and endpoint mapping.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/debug/service-discovery", (IConfiguration config) =>
{
    return Results.Json(new
    {
        Https0 = config["Services:commercialapiservice:https:0"],
        Http0 = config["Services:commercialapiservice:http:0"],
        CommercialMode = config["Features:CommercialMode:Enabled"]
    });
});

app.MapDefaultEndpoints();

app.Run();

static void ApplyLegacyCertificatePathOverrides(IConfiguration configuration)
{
    ArgumentNullException.ThrowIfNull(configuration);

    if (configuration is not IConfigurationManager configurationManager)
    {
        return;
    }

    var certificatePathOverrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    foreach (var certificateSection in configuration.GetSection("AzureAd:ClientCertificates").GetChildren())
    {
        var certificateIndex = certificateSection.Key;
        var certificateDiskPath = certificateSection["CertificateDiskPath"];
        var legacyCertificatePath = certificateSection["CertificatePath"];

        if (string.IsNullOrWhiteSpace(certificateDiskPath) && !string.IsNullOrWhiteSpace(legacyCertificatePath))
        {
            certificatePathOverrides[$"AzureAd:ClientCertificates:{certificateIndex}:CertificateDiskPath"] = legacyCertificatePath;
        }
    }

    if (certificatePathOverrides.Count > 0)
    {
        configurationManager.AddInMemoryCollection(certificatePathOverrides);
    }
}

static void ApplyCertificateBase64Overrides(IConfiguration configuration)
{
    ArgumentNullException.ThrowIfNull(configuration);

    if (configuration is not IConfigurationManager configurationManager)
    {
        return;
    }

    const string certificateBase64Key = "AzureAd:ClientCertificates:0:CertificateBase64";
    const string certificateDiskPathKey = "AzureAd:ClientCertificates:0:CertificateDiskPath";
    var certificateBase64 = configuration[certificateBase64Key];
    if (string.IsNullOrWhiteSpace(certificateBase64))
    {
        return;
    }

    var certificateDiskPath = configuration[certificateDiskPathKey];
    if (string.IsNullOrWhiteSpace(certificateDiskPath))
    {
        certificateDiskPath = "/tmp/import-to-planner-graph-client.pfx";
        configurationManager.AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                [certificateDiskPathKey] = certificateDiskPath,
            });
    }

    byte[] certificateBytes;
    try
    {
        certificateBytes = Convert.FromBase64String(certificateBase64);
    }
    catch (FormatException ex)
    {
        throw new InvalidOperationException(
            "'AzureAd:ClientCertificates:0:CertificateBase64' is not a valid base64 string.",
            ex);
    }

    var certificateDirectory = Path.GetDirectoryName(certificateDiskPath);
    if (!string.IsNullOrWhiteSpace(certificateDirectory))
    {
        Directory.CreateDirectory(certificateDirectory);
    }

    File.WriteAllBytes(certificateDiskPath, certificateBytes);

    if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
    {
        File.SetUnixFileMode(certificateDiskPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

}

