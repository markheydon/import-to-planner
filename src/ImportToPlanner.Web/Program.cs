using ImportToPlanner.Application;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Infrastructure.Graph;
using ImportToPlanner.Web;
using ImportToPlanner.Web.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var deploymentModeConfiguration = ResolveDeploymentModeConfiguration(builder.Configuration);
builder.Services.AddSingleton(deploymentModeConfiguration);

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["DeploymentMode:AuthorityTenant"] = deploymentModeConfiguration.AuthorityTenant,
    ["AzureAd:TenantId"] = deploymentModeConfiguration.AuthorityTenant,
});

var certificatePathOverrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
foreach (var certificateSection in builder.Configuration.GetSection("AzureAd:ClientCertificates").GetChildren())
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
    builder.Configuration.AddInMemoryCollection(certificatePathOverrides);
}

// Add services to the container.
builder.Services
    .AddWebHostServices(builder.Configuration)
    .AddApplication()
    .AddImportWorkflow()
    .AddInfrastructure(builder.Configuration);

ConfigureDataProtection(builder, deploymentModeConfiguration);

var app = builder.Build();

// Configure the HTTP request pipeline.
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
app.MapDefaultEndpoints();

app.Run();

static DeploymentModeConfiguration ResolveDeploymentModeConfiguration(IConfiguration configuration)
{
    ArgumentNullException.ThrowIfNull(configuration);

    var mode = configuration.GetValue("DeploymentMode:Mode", string.Empty) switch
    {
        "HostedSharedMultiTenant" => DeploymentMode.HostedSharedMultiTenant,
        "SelfHostedSingleTenant" => DeploymentMode.SelfHostedSingleTenant,
        _ => string.Equals(configuration["AzureAd:TenantId"], "organizations", StringComparison.OrdinalIgnoreCase)
            ? DeploymentMode.HostedSharedMultiTenant
            : DeploymentMode.SelfHostedSingleTenant,
    };

    var configuredAuthorityTenant = configuration["DeploymentMode:AuthorityTenant"];
    var configuredAzureAdTenant = configuration["AzureAd:TenantId"];

    var authorityTenant = mode switch
    {
        DeploymentMode.SelfHostedSingleTenant =>
            IsConcreteTenantIdentifier(configuredAzureAdTenant)
                ? configuredAzureAdTenant!
                : IsConcreteTenantIdentifier(configuredAuthorityTenant)
                    ? configuredAuthorityTenant!
                    : "common",
        _ => configuredAuthorityTenant ?? configuredAzureAdTenant ?? "organizations",
    };

    authorityTenant = NormalizePlaceholderAuthorityTenant(authorityTenant, mode);

    var scopes = configuration.GetSection("DownstreamApis:MicrosoftGraph:Scopes").Get<string[]>() ?? [];
    var adminConsentUri = BuildAdminConsentUri(configuration, authorityTenant);

    return new DeploymentModeConfiguration(
        mode,
        authorityTenant,
        configuration.GetValue<bool>("PlannerGateway:UseGraph"),
        configuration.GetValue<bool>("HostedStorage:Enabled"),
        configuration["DeploymentMode:InitialHostedReplicaPolicy"] ?? "SingleActiveReplica",
        scopes,
        adminConsentUri);

    bool IsConcreteTenantIdentifier(string? tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return false;
        }

        if (tenantId.StartsWith("__REPLACE", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !string.Equals(tenantId, "common", StringComparison.OrdinalIgnoreCase)
               && !string.Equals(tenantId, "organizations", StringComparison.OrdinalIgnoreCase)
             && !string.Equals(tenantId, AuthTenantConstants.ConsumerTenantId, StringComparison.OrdinalIgnoreCase);
    }

    string NormalizePlaceholderAuthorityTenant(string authorityTenant, DeploymentMode mode)
    {
        if (!authorityTenant.StartsWith("__REPLACE", StringComparison.OrdinalIgnoreCase))
        {
            return authorityTenant;
        }

        return mode == DeploymentMode.HostedSharedMultiTenant
            ? "organizations"
            : "common";
    }
}

static Uri? BuildAdminConsentUri(IConfiguration configuration, string authorityTenant)
{
    var configured = configuration["AzureAd:AdminConsentUri"];
    if (Uri.TryCreate(configured, UriKind.Absolute, out var configuredUri))
    {
        return configuredUri;
    }

    var clientId = configuration["AzureAd:ClientId"];
    if (string.IsNullOrWhiteSpace(clientId))
    {
        return null;
    }

    var instance = configuration["AzureAd:Instance"];
    if (!Uri.TryCreate(instance, UriKind.Absolute, out var authorityInstance))
    {
        return null;
    }

    var adminConsentBuilder = new UriBuilder(authorityInstance)
    {
        Path = $"{authorityTenant.Trim('/')}/v2.0/adminconsent",
        Query = $"client_id={Uri.EscapeDataString(clientId)}",
    };

    return adminConsentBuilder.Uri;
}

static void ConfigureDataProtection(WebApplicationBuilder builder, DeploymentModeConfiguration deploymentModeConfiguration)
{
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(deploymentModeConfiguration);

    if (deploymentModeConfiguration.Mode != DeploymentMode.HostedSharedMultiTenant
        || !deploymentModeConfiguration.HostedStorageEnabled)
    {
        return;
    }

    builder.Services.AddDataProtection();
    builder.Services.Configure<DataProtectionOptions>(options =>
    {
        options.ApplicationDiscriminator = "ImportToPlanner.Hosted";
    });

    builder.Services.Configure<KeyManagementOptions>(options =>
    {
        options.NewKeyLifetime = TimeSpan.FromDays(14);
    });
}
