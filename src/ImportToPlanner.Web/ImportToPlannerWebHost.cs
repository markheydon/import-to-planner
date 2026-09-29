using ImportToPlanner.Application;
using ImportToPlanner.Application.Models;
using ImportToPlanner.Commercial;
using ImportToPlanner.Infrastructure.Graph;
using ImportToPlanner.Web.Components;
using ImportToPlanner.Web.Features.Authentication;
using ImportToPlanner.Web.Features.CommercialAccounts;
using ImportToPlanner.Web.Infrastructure;
using Microsoft.Extensions.Options;

namespace ImportToPlanner.Web;

internal static class ImportToPlannerWebHost
{
    public static WebApplicationBuilder CreateWebApplicationBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        var isE2ETesting = E2ETestingHostEnvironment.IsE2ETesting(builder.Environment);
        E2ETestingHostEnvironment.EnsureStartupAllowed(builder.Environment);
        if (!isE2ETesting)
        {
            builder.AddWebStorageClients();
        }

        ApplyLegacyCertificatePathOverrides(builder.Configuration);
        ApplyCertificateBase64Overrides(builder.Configuration);
        StartupConfigurationValidator.Validate(builder.Configuration);
        AzureAdConfigurationNormalizer.Apply(builder.Configuration);

        builder.Services.AddWebApplicationOptions(builder.Configuration);
        builder.Services.AddSingleton(serviceProvider =>
        {
            var tenantAuthorityConfiguration = serviceProvider.GetRequiredService<TenantAuthorityConfiguration>();
            return new ConsentResolutionDefaults(
                tenantAuthorityConfiguration.RequiredScopes,
                tenantAuthorityConfiguration.AdminConsentUri);
        });
        builder.Services
            .AddOptions<CommercialModeOptions>()
            .Bind(builder.Configuration.GetSection(CommercialModeOptions.ConfigurationSectionName))
            .ValidateOnStart();
        builder.Services.AddSingleton(static serviceProvider => serviceProvider.GetRequiredService<IOptions<CommercialModeOptions>>().Value);

        var commercialModeOptions = CommercialModeOptions.FromConfiguration(builder.Configuration);
        var commercialModeEnabled = commercialModeOptions.Enabled;
        if (commercialModeEnabled && !isE2ETesting)
        {
            builder.AddCommercialStorageClients();
        }

        if (commercialModeOptions.RetentionSweepEnabled && commercialModeEnabled && !isE2ETesting)
        {
            builder.Services.AddHostedService<CommercialAccountRetentionHostedService>();
        }

        builder.Services
            .AddWebHostServices(builder.Configuration, builder.Environment)
            .AddApplication()
            .AddImportWorkflow()
            .AddInfrastructure(builder.Configuration);

        if (commercialModeEnabled && !isE2ETesting)
        {
            builder.Services.AddCommercial(builder.Configuration);
        }

        HostedDataProtectionConfigurator.Configure(builder.Services, builder.Environment);

        return builder;
    }

    public static void ConfigureWebApplication(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseAntiforgery();
        app.UseAuthentication();
        app.UseAuthorization();

        var staticAssetsManifestPath = Path.Combine(
            Path.GetDirectoryName(typeof(ImportToPlannerWebHost).Assembly.Location)!,
            "ImportToPlanner.Web.staticwebassets.endpoints.json");
        app.MapStaticAssets(staticAssetsManifestPath);
        app.MapControllers();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        app.MapDefaultEndpoints();
        app.MapE2ETestingEndpointsIfEnabled(app.Environment);
    }

    private static void ApplyLegacyCertificatePathOverrides(ConfigurationManager configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

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
            configuration.AddInMemoryCollection(certificatePathOverrides);
        }
    }

    private static void ApplyCertificateBase64Overrides(ConfigurationManager configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

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
            configuration.AddInMemoryCollection(
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
}
