using ImportToPlanner.Application;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Infrastructure.Graph;
using ImportToPlanner.Web;
using ImportToPlanner.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddWebStorageClients();
builder.AddInfrastructureStorageClients();

ApplyLegacyCertificatePathOverrides(builder.Configuration);
StartupConfigurationValidator.Validate(builder.Configuration);

var tenantAuthorityConfiguration = TenantAuthorityConfiguration.FromConfiguration(builder.Configuration);
var storageConfiguration = StorageConfiguration.FromConfiguration(builder.Configuration);

builder.Services.AddSingleton(tenantAuthorityConfiguration);
builder.Services.AddSingleton(storageConfiguration);
builder.Services.AddSingleton(new ConsentResolutionDefaults(
    tenantAuthorityConfiguration.RequiredScopes,
    tenantAuthorityConfiguration.AdminConsentUri));

// Add services to the container.
builder.Services
    .AddWebHostServices(builder.Configuration)
    .AddApplication()
    .AddImportWorkflow()
    .AddInfrastructure(builder.Configuration);

HostedDataProtectionConfigurator.Configure(builder.Services, storageConfiguration);

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
